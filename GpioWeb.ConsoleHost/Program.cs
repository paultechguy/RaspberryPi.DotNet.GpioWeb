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

using Microsoft.Owin.Hosting;
using System;
using System.Configuration;
using System.Threading;

namespace GpioWeb.GpioConsoleHost
{
	class Program
	{
		static void Main(string[] args)
		{
			new Program().Run(args);
		}

		private void Run(string[] args)
		{
			var host = ConfigurationManager.AppSettings["GpioServerHost"];
			var port = Convert.ToInt32(ConfigurationManager.AppSettings["GpioServerPort"]);

			// start queue manager
			ActionQueueManager.Instance.Start();

			try
			{
				string baseAddress = $"http://{host}:{port}";
				using (WebApp.Start<Startup>(url: baseAddress))
				{
					Console.WriteLine($"{this.GetType().Namespace} on host {baseAddress}");
					Console.WriteLine("Press Ctrl-C to quit");

					var exitEvent = new ManualResetEvent(false);
					Console.CancelKeyPress += (sender, eventArgs) =>
					{
						eventArgs.Cancel = true;
						exitEvent.Set();
					};
					exitEvent.WaitOne();
				}
			}
			finally
			{
				ActionQueueManager.Instance.Stop();
			}


		}
	}
}
