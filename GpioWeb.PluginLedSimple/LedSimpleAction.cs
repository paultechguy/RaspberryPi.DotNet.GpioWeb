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

namespace GpioWeb.PluginLedSimple
{
	public class LedSimpleAction : ActionBase
	{
		[JsonProperty(PropertyName = "preDelay", Required = Required.Always)]
		public int PreDelayMs { get; set; } = 0;

		[JsonProperty(PropertyName = "postDelay", Required = Required.Always)]
		public int PostDelayMs { get; set; } = 0;

		[JsonProperty(PropertyName = "startDuration", Required = Required.Always)]
		public int StartDurationMs { get; set; } = 0;

		[JsonProperty(PropertyName = "endDuration", Required = Required.Always)]
		public int EndDurationMs { get; set; } = 0;

		[JsonProperty(PropertyName = "loops", Required = Required.Always)]
		public int LoopCount { get; set; } = 0;

		[JsonProperty(PropertyName = "startValue", Required = Required.Always)]
		public bool StartValue { get; set; } = false;

		[JsonProperty(PropertyName = "endValue", Required = Required.Always)]
		public bool EndValue { get; set; } = false;

	}
}
