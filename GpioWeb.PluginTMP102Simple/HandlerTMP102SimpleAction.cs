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
using Raspberry.IO.GeneralPurpose;
using Raspberry.IO.InterIntegratedCircuit;
using System;
using System.Threading;

namespace GpioWeb.PluginTMP102Simple
{
	public class HandlerLedSimpleAction : IActionHandler
	{
		private object _state = null;

		public void Action(ActionBase baseAction, CancellationToken cancelToken, dynamic config)
		{
			TMP102SimpleAction action = (TMP102SimpleAction)baseAction;

			// note that config is dynamic so we cast the pin values to integer
			_state = new { state = "setup" };

			_state = new { state = "preDelay" };
			if (cancelToken.WaitHandle.WaitOne(action.PreDelayMs))
			{
				return;
			}

			DateTime startTime = DateTime.Now;
			int i2cAddress = config.i2cAddress;
			ProcessorPin sda = config.i2cSdaBcmPin;
			ProcessorPin scl = config.i2cSclBcmPin;

			using (var driver = new I2cDriver(sda, scl))
			{
				I2cDeviceConnection connection = driver.Connect(i2cAddress);
				while (true)
				{
					if (cancelToken.IsCancellationRequested)
					{
						break;
					}

					// read temperature and set state (json)
					_state = ReadTemperature(connection);

					// don't change state here since we want to keep the last temp reading in our state
					if (cancelToken.WaitHandle.WaitOne(action.ReadDelayMs))
					{
						break;
					}

					// time to quit?
					if (action.DurationMs > 0)
					{
						TimeSpan duration = DateTime.Now - startTime;
						if (duration.TotalMilliseconds > action.DurationMs)
						{
							break;
						}
					}
				}
			}

			_state = new { state = "postDelay" };
			if (cancelToken.WaitHandle.WaitOne(action.PostDelayMs))
			{
				return;
			}
		}

		private object ReadTemperature(I2cDeviceConnection connection)
		{
			byte[] data = connection.Read(2);

			// the msb is the first byte on linux
			byte msb = data[0];

			// the lsb is the second byte on linux
			byte lsb = data[1];

			// now combine then back together with msb first
			int temperature = ((msb << 8) | lsb) >> 4;

			object temps = new
			{
				date = DateTime.UtcNow.ToString("o"),
				temperatureF = (temperature * .0625 * 1.8) + 32,
				temperatureC = temperature * .0625,
			};

			return temps;
		}

		public object CurrentState
		{
			get
			{
				return _state;
			}
		}

		public Type[] SupportedActions { get; } = new Type[]
		{
			typeof(TMP102SimpleAction),
		};
	}
}
