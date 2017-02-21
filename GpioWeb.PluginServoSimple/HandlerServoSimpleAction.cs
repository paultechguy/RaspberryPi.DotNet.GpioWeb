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
using Raspberry.IO.Components.Controllers.Pca9685;
using Raspberry.IO.GeneralPurpose;
using Raspberry.IO.InterIntegratedCircuit;
using System;
using System.Linq;
using System.Threading;
using UnitsNet;

namespace GpioWeb.PluginServoSimple
{
	public class HandlerLedSimpleAction : IActionHandler
	{
		public void Action(ActionBase baseAction, CancellationToken cancelToken, dynamic config)
		{
			ServoSimpleAction action = (ServoSimpleAction)baseAction;
			ValidateAction(action);

			ProcessorPin sda = config.i2cSdaBcmPin;
			ProcessorPin scl = config.i2cSclBcmPin;

			using (var driver = new I2cDriver(sda, scl))
			{
				// get values set
				int pwmMinPulse = config.pmwMinPulse;
				int pwmMaxPulse = config.pmwMaxPulse;
				PwmChannel channel = (PwmChannel)config.i2cChannel;
				int i2cAddress = config.i2cAddress;
				int pwmFrequency = config.pmwFrequency;
				Frequency frequency = Frequency.FromHertz(pwmFrequency);

				// device support and prep channel
				var device = new Pca9685Connection(driver.Connect(i2cAddress));
				device.SetPwm(channel, 0, 0);
				device.SetPwmUpdateRate(frequency);

				// pre delay
				if (cancelToken.WaitHandle.WaitOne(action.PreDelayMs))
				{
					return;  // looks like we're cancelling
				}

				// handle each rotation
				for (int i = 0; i < action.RotationDegrees.Length; ++i)
				{
					// rotate
					int cycle;
					if (!DegreeToCycle(action.RotationDegrees[i], out cycle, pwmMinPulse, pwmMaxPulse))
					{
						throw new ApplicationException($"Invalid rotation at index {i}: {action.RotationDegrees[i]}");
					}
					device.SetPwm(channel, 0, cycle);

					// rotation wait until possible cancel
					if (cancelToken.WaitHandle.WaitOne(action.RotationDelayMs[i]))
					{
						break;  // looks like we're cancelling
					}
				}

				// post delay
				if (cancelToken.WaitHandle.WaitOne(action.PostDelayMs))
				{
					return;  // looks like we're cancelling
				}

			}
		}

		private bool DegreeToCycle(int degrees, out int cycle, int minPulse, int maxPulse)
		{
			cycle = -1;
			if (degrees >= 0 && degrees <= 180)
			{
				cycle = (int)((((maxPulse - minPulse) / 180m) * degrees) + minPulse);
			}

			bool status = cycle >= 0;
			if (!status)
			{
				Console.Error.WriteLine("%Invalid input");
			}

			return status;
		}

		private void ValidateAction(ServoSimpleAction action)
		{
			// the rotation degrees and rotation delays arrays should the same length
			if (action.RotationDegrees.Length != action.RotationDelayMs.Length)
			{
				throw new ApplicationException("RotationDegrees and RotationDelayMs array lengths are not the same");
			}

			// rotation degrees must be 0 - 180
			if (action.RotationDegrees.Any(r => r < 0 || r > 180))
			{
				throw new ApplicationException("RotationDegrees array contains invalid rotation; not in range 0-180");
			}
		}

		public Type[] SupportedActions { get; } = new Type[]
		{
			typeof(ServoSimpleAction),
		};
	}
}
