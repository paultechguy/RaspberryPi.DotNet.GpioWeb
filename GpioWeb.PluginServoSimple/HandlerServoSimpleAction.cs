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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnitsNet;

namespace GpioWeb.PluginServoSimple
{
	public class HandlerLedSimpleAction : IActionHandler
	{
		private class RotateServoConfiguration
		{
			public Pca9685Connection PcaConnection { get; set; }
			public PwmChannel PwmChannel { get; set; }
			public int PwmMinimumPulse { get; set; }
			public int PwmMaximumPulse { get; set; }
			public int[] RotationDegree { get; set; }
			public int[] RotationDelayMs { get; set; }
		}

		private static object _state = new { state = "init" }; // something for the default on first lock so we don't crash
		private int[] _pwmMinPulse;
		private int[] _pwmMaxPulse;

		public void Action(ActionBase baseAction, CancellationToken cancelToken, dynamic config)
		{
			ServoSimpleAction action = (ServoSimpleAction)baseAction;

			SetState("validateSetup");
			ValidateSetup(config, action);

			ProcessorPin sda = config.i2cSdaBcmPin;
			ProcessorPin scl = config.i2cSclBcmPin;

			using (var driver = new I2cDriver(sda, scl))
			{
				SetState("setup");
				PwmChannel[] channels = Newtonsoft.Json.JsonConvert.DeserializeObject<PwmChannel[]>(config.i2cChannel.ToString());
				int i2cAddress = config.i2cAddress;
				int pwmFrequency = config.pwmFrequency;
				Frequency frequency = Frequency.FromHertz(pwmFrequency);

				// device support and prep channel
				SetState("connection");
				var pcaConnection = new Pca9685Connection(driver.Connect(i2cAddress));
				pcaConnection.SetPwmUpdateRate(frequency);

				// pre delay
				SetState("preDelay");
				if (cancelToken.WaitHandle.WaitOne(action.PreDelayMs))
				{
					return;  // looks like we're cancelling
				}

				// handle each rotation
				//
				// Note: there could be more configured channels (i.e. servos) in the configuration; the user doesn't
				//       have to use them all; we map the user's n array elements to first n configured channels in 
				//       the server plugin config
				List<Task> servoTasks = new List<Task>();
				for (int i = 0; i < action.RotationDegrees.Length; ++i)
				{
					// if we're skipping this servo, loop to next
					if (action.RotationDegrees.Length == 0)
					{
						continue;
					}

					SetState($"startRotate_{i}");

					RotateServoConfiguration options = new RotateServoConfiguration
					{
						PcaConnection = pcaConnection,
						PwmChannel = channels[i],
						PwmMaximumPulse = _pwmMaxPulse[i],
						PwmMinimumPulse = _pwmMinPulse[i],
						RotationDegree = action.RotationDegrees[i],
						RotationDelayMs = action.RotationDelayMs[i],
					};

					Task task = Task.Run(() => RotateServo(options, cancelToken));
					servoTasks.Add(task);
				}

				// wait for all servos to complete; don't set state since tasks will be doing that
				// as the servos rotate
				Task.WaitAll(servoTasks.ToArray());

				// post delay
				SetState("postDelay");
				if (cancelToken.WaitHandle.WaitOne(action.PostDelayMs))
				{
					return;  // looks like we're cancelling
				}

			}
		}

		public object CurrentState
		{
			get
			{
				return _state;
			}
		}

		private static void SetState(string s)
		{
			lock (_state)
			{
				_state = new { state = s };
			}
		}

		private void RotateServo(RotateServoConfiguration rotateConfig, CancellationToken cancelToken)
		{
			// init
			rotateConfig.PcaConnection.SetPwm(rotateConfig.PwmChannel, 0, 0);

			for (int i = 0; i < rotateConfig.RotationDegree.Length; i++)
			{
				SetState($"rotate_{rotateConfig.PwmChannel}_{i}");

				// rotate
				int cycle;
				if (!DegreeToCycle(rotateConfig.RotationDegree[i], out cycle, rotateConfig.PwmMinimumPulse, rotateConfig.PwmMaximumPulse))
				{
					throw new ApplicationException($"Invalid rotation at index {i}: {rotateConfig.RotationDegree[i]}");
				}

				rotateConfig.PcaConnection.SetPwm(rotateConfig.PwmChannel, 0, cycle);

				// rotation wait until possible cancel
				SetState($"rotateDelay_c{rotateConfig.PwmChannel}_{i}");
				if (cancelToken.WaitHandle.WaitOne(rotateConfig.RotationDelayMs[i]))
				{
					break;  // looks like we're cancelling
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

		private void ValidateSetup(dynamic config, ServoSimpleAction action)
		{
			// the rotation degrees and rotation delays arrays should the same length
			if (action.RotationDegrees.Length != action.RotationDelayMs.Length)
			{
				throw new ApplicationException("rotation[] and rotationDelay[] lengths are not the same");
			}

			//
			// From this point we know the rotation degrees and delay are the same length so
			// we can use either of them as a check value
			//

			// user's rotation stuff can't exceed the configured plugin channel count
			int[] channels = Newtonsoft.Json.JsonConvert.DeserializeObject<int[]>(config.i2cChannel.ToString());
			if (action.RotationDegrees.Length> channels.Length)
			{
				throw new ApplicationException($"rotationDegree[] and rotationDelay lengths cannot exceed maximum allowed by plugin configuration ({channels.Length})");
			}

			// rotation delay must be >= 0; rotation degrees must be 0 - 180
			for (int i = 0; i < action.RotationDegrees.Length; i++)
			{
				if (action.RotationDelayMs[i].Any(d => d < 0))
				{
					throw new ApplicationException($"rotationDelay[{i}] has delay less than zero");
				}

				if (action.RotationDegrees[i].Any(r => r < 0 || r > 180))
				{
					throw new ApplicationException($"rotation[{i}] contains invalid rotation; must be in range 0-180");
				}
			}

			// min/max pulses array lengths should be the same, and should be the
			// same length as rotation array length
			_pwmMinPulse = Newtonsoft.Json.JsonConvert.DeserializeObject<int[]>(config.pwmMinPulse.ToString());
			_pwmMaxPulse = Newtonsoft.Json.JsonConvert.DeserializeObject<int[]>(config.pwmMaxPulse.ToString());
			if ((_pwmMinPulse.Length != _pwmMaxPulse.Length) || (_pwmMinPulse.Length != channels.Length))
			{
				throw new ApplicationException($"config error: pwmMinPulse[] and pwmMaxPulse[] must be the same length as i2cChannel[]");
			}
		}

		public Type[] SupportedActions { get; } = new Type[]
		{
			typeof(ServoSimpleAction),
		};
	}
}
