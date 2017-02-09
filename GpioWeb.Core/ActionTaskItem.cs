﻿// RaspberryPi.GpioWeb
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
using System.Threading;
using System.Threading.Tasks;

namespace GpioWeb.GpioCore
{
	public class ActionTaskItem
	{
		[JsonIgnore]
		public Task Task { get; set; }

		// we'll call this "task" since it looks better in the json output
		[JsonProperty(PropertyName = "task")]
		public ActionQueueItem QueueAction { get; set; }

		[JsonIgnore]
		public CancellationTokenSource CancellationTokenSource { get; set; }

		public ActionTaskItem()
		{

		}

		public ActionTaskItem(ActionQueueItem queueAction, Task task, CancellationTokenSource token)
		{
			Task = task;
			QueueAction = queueAction;
			CancellationTokenSource = token;
		}
	}
}
