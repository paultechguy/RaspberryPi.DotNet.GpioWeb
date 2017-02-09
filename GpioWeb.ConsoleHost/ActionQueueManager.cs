// RaspberryPi.GpioWeb
//
// C# / Mono programming for the Raspberry Pi
// Copyright (c) 2017 Paul Carver
//
// RaspberryPi.GpioWeb is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// any later version.
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using GpioWeb.GpioCore;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace GpioWeb.GpioConsoleHost
{
	public class ActionQueueManager
	{
		private static readonly ConcurrentDictionary<string, ActionTaskItem> _actionTasks = new ConcurrentDictionary<string, ActionTaskItem>();
		private static readonly BlockingCollection<ActionQueueItem> _actionQueue = new BlockingCollection<ActionQueueItem>();
		private static readonly Lazy<ActionQueueManager> _instance
			= new Lazy<ActionQueueManager>(() => new ActionQueueManager());
		private static CancellationTokenSource _cancellationSource = null;
		private static Task _managerTask = null;
		private static ActionConfigManager _configManager;
		private static Dictionary<string, IActionHandler> _actionHandlers;

		// do we allow threaded actions?
		private bool _actionThreading = Convert.ToBoolean(ConfigurationManager.AppSettings["ActionAllowThreading"]);

		// do we allow threaded actions?
		private bool _actionSimulation = Convert.ToBoolean(ConfigurationManager.AppSettings["ActionSimulateEnabled"]);

		// private to prevent direct instantiation.
		private ActionQueueManager()
		{
			LoadPlugins();
		}

		private void LoadPlugins()
		{
			// plugins should exist in the "plugin" directory
			var folder = "plugin";
			var directory = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), folder);
			if (!Directory.Exists(directory))
			{
				Error($"\"{folder}\" directory not found; no plugins loaded");
				return;
			}

			IActionHandler[] handlers = LoadDll<IActionHandler>(directory, "*.dll");

			// load all actions and map to a handler; ignore any duplicate issues; first come, first server
			var map = handlers
				.OrderBy(h => h.GetType().Name)
				.SelectMany(h => h.SupportedActions
					.Select(a => new { Handler = h, Action = a }))
				.ToDictionary(x => x.Action.FullName, x => x.Handler);
			_actionHandlers = map;

			// some polite output to show what plugins were loaded
			foreach (var key in _actionHandlers.Keys)
			{
				Log($"{nameof(ActionConfigManager)} loaded plugin: {key}");
			}
		}

		// accessor for instance
		public static ActionQueueManager Instance
		{
			get
			{
				return _instance.Value;
			}
		}

		public void Start()
		{
			// make sure we have a fresh action config manager
			var configFileDir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "config");
			_configManager = new ActionConfigManager(configFileDir);
			_configManager.Start();

			// we can safely do this since plugins have been loaded and all types
			// should be able to be deserialized
			LoadStartupActions();

			_cancellationSource = new CancellationTokenSource();
			_managerTask = Task.Run(() => { ProcessQueue(_cancellationSource.Token); }, _cancellationSource.Token);
		}

		private void LoadStartupActions()
		{
			// startup actions should exist in the "startup" directory
			var folder = "startup";
			var directory = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), folder);
			if (!Directory.Exists(directory))
			{
				Log($"Directory \"{folder}\" does not exist; no startup actions (.json files) loaded");
				return;
			}

			var files = Directory.EnumerateFiles(directory, "*.json");
			foreach (var file in files)
			{
				Log($"{nameof(ActionConfigManager)} loading startup file: {Path.GetFileName(file)}");

				ActionBase[] actions = null;
				try
				{
					var jsonText = File.ReadAllText(file);
					actions = JsonConvert.DeserializeObject<ActionBase[]>(
						jsonText,
						new JsonSerializerSettings
						{
							TypeNameHandling = TypeNameHandling.Objects,
							TypeNameAssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Full,
						});
				}
				catch(JsonSerializationException ex)
				{
					Error($"   File ignored, action deserialization failure: {ex.Message}");
				}

				if (actions != null)
				{
					foreach (var action in actions)
					{
						Log($"   Action queued: {action.GetType().FullName}, config: {action.ConfigName}");

						ActionQueueItem item = new ActionQueueItem(action, action.ConfigName, "localhost");
						this.Enqueue(item);
					}
				}
			}
		}

		private void ProcessQueue(CancellationToken cancelToken)
		{
			while (!_actionQueue.IsCompleted && !cancelToken.IsCancellationRequested)
			{
				ActionQueueItem queueItem = null;
				// Blocks if number.Count == 0
				// IOE means that Take() was called on a completed collection.
				// Some other thread can call CompleteAdding after we pass the
				// IsCompleted check but before we call Take. 
				// In this example, we can simply catch the exception since the 
				// loop will break on the next iteration.
				try
				{
					queueItem = _actionQueue.Take();
				}
				catch (InvalidOperationException)
				{
					// when we shut down, we expect this exception is the queue is complete; but
					// if not complete...
					if (!_actionQueue.IsCompleted)
					{
						throw;
					}
				}

				if (queueItem != null)
				{
					bool doThreaded = _actionThreading && !string.IsNullOrWhiteSpace(queueItem.Action.TaskId);
					if (doThreaded)
					{
						try
						{
							// create a task item without a real task at this point; we'll set it once we know
							// we can add it successfully (and no dups)
							ActionTaskItem taskItem = new ActionTaskItem(queueItem, null, new CancellationTokenSource());
							if (!_actionTasks.TryAdd(queueItem.Action.TaskId, taskItem))
							{
								Error($"Duplicate threadId found; ignoring action.  ID: {queueItem.Action.TaskId.ToString()}");
								continue;
							}
							else
							{
								Log($"Adding thread id: {queueItem.Action.TaskId.ToString()}");
							}

							// run the task, saving "t" as this task; if we did Run and Continue in one statement,
							// the returned task would be the Continue task and would be waiting
							Task t = Task.Run(() => Process(queueItem, taskItem.CancellationTokenSource));
							t.ContinueWith(task =>
							{
								if (task.IsFaulted)
								{
									Error(task.Exception.ToString());
								}

								ActionTaskItem tempTaskItem;
								if (!_actionTasks.TryRemove(queueItem.Action.TaskId, out tempTaskItem))
								{
									Error($"Unable to remove action thread by thread id: {queueItem.Action.TaskId.ToString()}");
								}
								else
								{
									Log($"Removing thread id: {queueItem.Action.TaskId.ToString()}");
								}

							});

							// not that the task is running, set the task item Task
							taskItem.Task = t;
						}
						finally
						{
						}
					}
					else
					{
						Process(queueItem, cancelSource: null);
					}
				}
			}
		}

		private void Process(ActionQueueItem item, CancellationTokenSource cancelSource)
		{
			try
			{
				var simulated = _actionSimulation ? " (simulated)" : string.Empty;

				var threaded = cancelSource != null ? " thread" : string.Empty;
				Console.WriteLine($"Start{threaded}{simulated} action {item.Action.GetType().Name}, config: {item.Action.ConfigName} ({item.Host})");

				if (!_actionSimulation)
				{
					CancellationToken token = cancelSource == null ? CancellationToken.None : cancelSource.Token;
					IActionHandler handler = _actionHandlers[item.Action.GetType().FullName];
					handler.Action(item.Action, token, _configManager[item.ConfigName]);
				}

				Console.WriteLine($"End{threaded}{simulated} action {item.Action.GetType().Name}, config: {item.Action.ConfigName} ({item.Host})");
			}
			catch (Exception ex)
			{
				Error($"Error while processing action: {ex.ToString()}");
			}
		}

		public void Stop()
		{
			if (_managerTask != null)
			{
				StopActionTasks();
				_configManager.Stop();
				_cancellationSource.Cancel();
				_actionQueue.CompleteAdding();
				_managerTask.Wait();
				_managerTask = null;
			}
		}

		private void StopActionTasks()
		{
			int msDelay = Convert.ToInt32(ConfigurationManager.AppSettings["ActionStopWaitMs"]);
			foreach (var taskItem in _actionTasks)
			{
				// cancel the task and then wait for max time
				taskItem.Value.CancellationTokenSource.Cancel();
				var suffix = $"{taskItem.Value.QueueAction.GetType().Name}, config: {taskItem.Value.QueueAction.ConfigName} ({taskItem.Value.QueueAction.Host})";
				if (taskItem.Value.Task.Wait(msDelay >= 0 ? msDelay : -1))
				{
					Log($"Stopped thread action {suffix}");
				}
				else
				{
					Error($"Stop failed, thread action {suffix}");
				}
			}
		}

		public void Enqueue(ActionQueueItem item)
		{
			if (!_configManager.Exists(item.ConfigName))
			{
				throw new ApplicationException("config not found: " + item.ConfigName);
			}

			_actionQueue.Add(item);
		}

		public ActionTaskItem[] Tasks()
		{
			ActionTaskItem[] tasks = _actionTasks.Values
				.ToArray();

			return tasks;
		}

		public ActionTaskItem GetTask(string taskId)
		{
			ActionTaskItem taskItem = null;
			if (_actionTasks.TryGetValue(taskId, out taskItem))
			{
				Log($"{nameof(this.GetTask)} returned , id: {taskItem.ToString()}");
			}
			else
			{
				// not a system type error, so log do normal log
				Log($"{nameof(this.GetTask)}, id does not exist: {taskId.ToString()}");
			}

			return taskItem;
		}

		public bool CancelTask(string taskId)
		{
			ActionTaskItem taskItem;
			bool exists = false;
			if (_actionTasks.TryGetValue(taskId, out taskItem))
			{
				exists = true;
				taskItem.CancellationTokenSource.Cancel();
				// not much we can do here...this lil' call could be done via a web
				// service and we can't wait around
				//
				// we basically need to reply on the task behaving nicely since we've
				// given it a cancellation token

				Log($"Task cancellation requested, id: {taskItem.ToString()}");
			}
			else
			{
				// not a system type error, so log do normal log
				Log($"{nameof(this.CancelTask)}, id does not exist: {taskId.ToString()}");
			}

			return exists;
		}

		private void Log(string message, params object[] args)
		{
			Console.WriteLine(message, args);
		}

		private void Error(string message, params object[] args)
		{
			Console.Error.WriteLine(message, args);
		}

		private T[] LoadDll<T>(string path, string pattern)
		{
			return Directory.GetFiles(Path.GetFullPath(path), pattern)
				 .SelectMany(f => Assembly.LoadFrom(f).GetTypes()
					  .Where(t => !t.IsAbstract && typeof(T).IsAssignableFrom(t))
					  .Select(t => (T)Activator.CreateInstance(t)))
				 .ToArray();
		}
	}
}
