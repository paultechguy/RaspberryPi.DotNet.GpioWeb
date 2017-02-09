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
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace GpioWeb.GpioConsoleHost.Controllers
{
	public class GpioController : ApiController
	{
		#region public methods

		[HttpGet]
		[Route("~/gpio/ping")]
		public HttpResponseMessage GetPing()
		{
			var resp = new HttpResponseMessage(HttpStatusCode.OK);
			resp.Content = new StringContent("ping", System.Text.Encoding.UTF8, "text/plain");

			return resp;
		}

		[HttpPost]
		[Route("~/gpio/action")]
		public async Task<IHttpActionResult> PostActionCollection(ActionBase[] actions)
		{
			if (actions == null) // invalid json
			{
				return BadRequest("Invalid actions (check JSON format)");
			}
			else if (actions.Length <= 0)
			{
				return BadRequest("No actions found in JSON action array");
			}

			// only enabled actions
			actions = actions
				.Where(a => a.Enabled)
				.ToArray();
			if (actions.Length <= 0)
			{
				return BadRequest("No enabled actions found in JSON action array");
			}

			try
			{
				await DoAction(actions);
				return Ok();
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}
		}

		[HttpGet]
		[Route("~/gpio/task")]
		public ActionTaskItem[] GetActionTaskCollection()
		{
			var tasks = ActionQueueManager.Instance.Tasks();

			return tasks;
		}

		[HttpDelete]
		[Route("~/gpio/task/{id}")]
		public IHttpActionResult DeleteActionTask(string id)
		{
			bool requestCancelExists = ActionQueueManager.Instance.CancelTask(id);
			if (requestCancelExists)
			{
				// all we really do is submit a cancel request so we'll return a status
				// code that reflects we've accepted the request
				return new System.Web.Http.Results.ResponseMessageResult(
					Request.CreateResponse(HttpStatusCode.Accepted));
			}
			else
			{
				return BadRequest();
			}
		}

		[HttpGet]
		[Route("~/gpio/task/{id}")]
		public IHttpActionResult GetActionTask(string id)
		{
			ActionTaskItem taskItem = ActionQueueManager.Instance.GetTask(id);
			if (taskItem != null)
			{
				return Ok(taskItem);
			}
			else
			{
				return BadRequest();
			}
		}

		#endregion

		#region private methods

		private async Task DoAction(ActionBase[] actions)
		{
			Console.WriteLine($"Received {actions.Length} action(s); queuing...");
			await Task.Run(() =>
			{
				foreach (var action in actions)
				{
					try
					{
						string fromHost = Request.GetOwinContext().Request.RemoteIpAddress;
						ActionQueueItem item = new ActionQueueItem(action, action.ConfigName, fromHost);
						ActionQueueManager.Instance.Enqueue(item);
					}
					catch (Exception ex)
					{
						Error($"Error while queuing action: {ex.ToString()}");
					}
				}
			});
		}

		private void Error(string message, params object[] args)
		{
			Console.Error.WriteLine(message, args);
		}

		#endregion
	}
}
