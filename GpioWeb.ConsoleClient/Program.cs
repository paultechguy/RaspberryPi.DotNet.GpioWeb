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

using GpioWeb.PluginBuzzerSimple;
using GpioWeb.PluginLedBuzzerSimple;
using GpioWeb.PluginLedSimple;
using GpioWeb.PluginRgbSimple;
using GpioWeb.GpioClientUtility;
using GpioWeb.GpioCore;
using Newtonsoft.Json;
using System;
using System.Configuration;
using System.IO;
using System.Net;

namespace GpioWeb.GpioConsoleClient
{
	class Program
	{
		private GpioClient _gpioClient;

		static void Main(string[] args)
		{
			new Program().Run(args);
		}

		private void Run(string[] args)
		{
			Initialize();

			string choice = string.Empty;
			do
			{
				try
				{
					Menu();
					choice = Console.ReadLine().Trim().ToLower();
					switch (choice)
					{
						case "1":
							SendJsonFileAsync();
							break;

						case "2":
							SendCsObject();
							break;

						case "":
						case "3":
							break;

						default:
							Console.WriteLine("%Invalid menu choice");
							break;
					}
				}
				catch (Exception ex)
				{
					Console.Error.WriteLine($"%Exception: {ex.Message}");
				}
			} while (choice != "3");
		}

		private void Menu()
		{
			Console.Write(@"Menu:
	1) Send JSON file to host
	2) Send C# ActionBase[] to host
	3) Quit

Choice: ");
		}

		private async void SendJsonFileAsync()
		{
			string json = File.ReadAllText("SendJsonFile.json");
			await _gpioClient.SendActionAsync<dynamic>(JsonConvert.DeserializeObject(json), "/gpio/action");
		}

		private async void SendCsObject()
		{
			var actions = new ActionBase[]
			{
				new BuzzerSimpleAction
				{
					ConfigName = "BuzzerSimpleAction",
					Enabled = true,
					PreDelayMs = 0,
					PostDelayMs = 0,
					LoopCount = 3,
					StartDurationMs = 1000,
					EndDurationMs = 500,
					StartValue = true,
					EndValue = false,
				},
				new LedSimpleAction
				{
					ConfigName = "LedSimpleAction",
					Enabled = true,
					PreDelayMs = 0,
					PostDelayMs = 0,
					LoopCount = 3,
					StartDurationMs = 1000,
					EndDurationMs = 500,
					StartValue = true,
					EndValue = false,
				},
				new RgbSimpleAction
				{
					ConfigName = "RgbSimpleAction",
					Enabled = true,
					PreDelayMs = 0,
					PostDelayMs = 0,
					LoopCount = 3,
					StartDurationMs = 1000,
					EndDurationMs = 500,
					StartValues = new bool[3] { true, true, true }, // red, green, blue
					EndValues = new bool[3] { false, false, false }, // all off
				},
				new LedBuzzerSimpleAction
				{
					ConfigName = "LedBuzzerSimpleAction",
					Enabled = true,
					PreDelayMs = 0,
					PostDelayMs = 0,
					LoopCount = 3,
					StartDurationMs = 1000,
					EndDurationMs = 500,
					StartValue = true,
					EndValue = false,
				},
			};

			string json = JsonConvert.SerializeObject(
				actions,
				new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Objects, TypeNameAssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Full });
			await _gpioClient.SendActionAsync<dynamic>(JsonConvert.DeserializeObject(json), "/gpio/action");
		}

		private void Initialize()
		{
			// default json formatter so that we serialize the $type stuff
			JsonConvert.DefaultSettings = () => new JsonSerializerSettings
			{
				Formatting = Formatting.None,
				TypeNameHandling = TypeNameHandling.Objects,
			};

			// create utility client
			var host = ConfigurationManager.AppSettings["GpioServerHost"];
			var port = Convert.ToInt32(ConfigurationManager.AppSettings["GpioServerPort"]);
			var endpoint = new IPEndPoint(IPAddress.Parse(host), port);
			_gpioClient = new GpioClient(endpoint);
		}
	}
}
