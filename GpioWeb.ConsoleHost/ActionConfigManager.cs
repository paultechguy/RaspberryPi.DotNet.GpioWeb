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

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace GpioWeb.GpioConsoleHost
{
	public class ActionConfigManager
	{
		private readonly string _configDirectory;
		private readonly Dictionary<string, dynamic> _configItems = new Dictionary<string, dynamic>();

		public ActionConfigManager(string configFileDirectory)
		{
			_configDirectory = configFileDirectory;
		}

		private void LoadFiles()
		{
			_configItems.Clear(); // just in case...be defensive
			foreach(var filename in Directory.EnumerateFiles(_configDirectory, "*.json"))
			{
				dynamic json = JsonConvert.DeserializeObject<dynamic>(File.ReadAllText(filename));
				var configName = Path.GetFileNameWithoutExtension(filename);
				_configItems.Add(configName, json);

				Console.WriteLine($"{this.GetType().Name} added configuration: {configName}");
			}
		}

		public void Start()
		{
			LoadFiles();
			StartFileWatcher();
		}

		public void Stop()
		{
			StopFileWatcher();
			_configItems.Clear();
		}

		private void StartFileWatcher()
		{
		}

		private void StopFileWatcher()
		{
		}

		public dynamic this[string name]
		{
			get
			{
				return _configItems[name];
			}
		}

		public bool Exists(string name)
		{
			return _configItems.ContainsKey(name);
		}
	}
}
