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
using Raspberry.IO.GeneralPurpose;
using System;
using System.Threading;

namespace GpioWeb.PluginLedBuzzerSimple
{
	public class HandlerLedBuzzerSimpleAction : IActionHandler
	{
		public void Action(ActionBase baseAction, CancellationToken cancelToken, dynamic config)
		{
			LedBuzzerSimpleAction action = (LedBuzzerSimpleAction)baseAction;

			// note that config is dynamic so we cast the pin values to integer
			var connection = new MemoryGpioConnectionDriver();
			var pinLed = connection.Out((ProcessorPin)config.pinLed);
			var pinBuzzer = connection.Out((ProcessorPin)config.pinBuzzer);

			if (cancelToken.WaitHandle.WaitOne(action.PreDelayMs))
			{
				return;
			}

			for (int loopCounter = 0; loopCounter < action.LoopCount; ++loopCounter)
			{
				pinLed.Write(action.StartValue);
				pinBuzzer.Write(action.StartValue);

				// wait until possible cancel, but continue if cancelled to at least set end value
				cancelToken.WaitHandle.WaitOne(action.StartDurationMs);

				// only output if it changes
				if (action.EndValue != action.StartValue)
				{
					pinLed.Write(action.EndValue);
					pinBuzzer.Write(action.EndValue);
				}

				if (cancelToken.WaitHandle.WaitOne(action.EndDurationMs))
				{
					return;
				}
			}

			if (cancelToken.WaitHandle.WaitOne(action.PostDelayMs))
			{
				return;
			}
		}

		public Type[] SupportedActions { get; } = new Type[]
		{
			typeof(LedBuzzerSimpleAction),
		};
	}
}
