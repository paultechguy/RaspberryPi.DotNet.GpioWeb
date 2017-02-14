GPIO .NET Web Service Platform  
Copyright (c) 2017 Paul Carver

# Web Service Introduction
The GPIO .NET Service Platform allows client applications to POST an array of *actions*, formatted as JSON, in order to control components that are attached to the GPIO pins of the Raspberry Pi.  The service platform is written in C# and several basic actions are included (control LEDs, RGBs, Buzzers), but the strength of the web service is the extensible plugin architecture.  This architecture allows developers to easily write their own .NET custom actions to leverage the Pi GPIO header; the web service host code does not have to be modified in order to begin using a new plugin (extensible).

Several other web service endpoints exist to manage executing actions, these include listing actions and requesting deletion of an executing action.

# Requirements
The repository has been tested with:

* Debian 8.0 (jessie)
* Mono 4.6.2
* .NET 4.6.2
* Raspberry Pi 2 B hardware board
* [Raspberry# IO](https://github.com/raspberry-sharp/raspberry-sharp-io) as of February 1, 2017

# Building Web Service
The GPIO .NET Web Service Platform solution solution, RaspberryPi.GpioWeb.sln, can be built on a Windows computer using Visual Studio 2015.  You must have .NET 4.6.2+ installed.  After building the solution, copy the GpioWeb.ConsoleHost output to the Raspberry Pi to a directory of your choosing; the copy should include all files and sub-directories in the GpioWeb.ConsoleHost Release or Debug directory.

# Starting / Stopping Web Service
On the Raspberry Pi, you can execute the web service as either a console application or as a Linux daemon.  The default IP address and port for hosting the web service are configured in the gpioweb.exe.config file.

To execute the service as a console application, invoke the web service executable using Mono.  This should be done using the Linux *sudo* command since the web service requires low-level permissions to access the Pi GPIO capabilities.

	sudo mono gpioweb.exe

To stop the console application, press Ctrl-C.

To execute the web service as a Linux daemon, perform the following steps:

* Modify the *dir* variable in GpioWeb.ConsoleHost/gpioweb file to reflect the directory where you copied the executables in the previous step
* Copy the file GpioWeb.ConsoleHost/gpioweb to /etc/init.d on your Raspberry Pi
* Create the Raspberry Pi startup scripts by executing:

	sudo update-rc.d -f script defaults

* Reboot the Raspberry Pi (web service should now start automatically)
* Manage the web service on the Raspberry Pi
    * sudo service gpioweb status (is the web service running?)
    * sudo service gpioweb stop
    * sudo service gpioweb start
    * sudo service gpioweb restart
* View log files on the Raspberry Pi
    * Standard: /var/log/gpioweb.log
    * Error: /var/log/gpioweb.err

If you encounter any errors upon starting the service, consider manually executing it via a terminal window with:

	sudo /etc/init.d/gpioweb start

# Removing Web Service
You may remove the GPIO .NET Web Service via the following command:

	sudo update-rc.d -f gpioweb remove

# Testing Web Service
Once you have the web service running and there are no errors in the /var/log/gpioweb.err log file, you are ready to test the service.

## Ping Test
The simplest way to make sure the web service is up and running is to perform a GET on the *ping* endpoint. This can be done using a web browser on your Raspberry Pi computer.  Open up your browser and navigate to (this assumes your are using the default IP address of 127.0.0.1 for hosting the web service):

	http://127.0.0.1:8020/gpio/ping

If everything is working OK, you should receive *ping* text displayed in your web browser. If you do not receive the message, be sure to check the error log file.

## GPIO Test
MANAGING THE GPIO HEADER AND CONNECTING ELECTRONIC COMPONENTS TO THE PI REQUIRES GREAT CARE. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS OF RASPBERRYPI.DOTNET.GPIOWEB BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

This is where things get fun, but it does require you to set your Raspberry Pi up with some level of GPIO functionality.  The web service repository comes with a good example of using the GPIO header pins.  You will need to have a few breadboard items handy.  Follow the following steps to configure and
test the web service GPIO functionality:

* Set up breadboard with components
* Verify web service is running
* Run the console test program

**Breadboard Setup**  
You can view the GPIO example by viewing the .jpg images in the GpioWeb.ConsoleHost/Content directory.  Here you will find images for setting up several components on a breadboard.  There is also a [Fritzing](http://fritzing.org/) project file. You can set up your Rasbperry Pi breadboard with all components, or just the ones you have parts for (if you only have a single component (i.e. LED), that is fine; the Console Test program described below will still work, but with limited functionality). Be sure to configure your breadboard to using the same GPIO header pins so that the example program will work correctly.  These include:

* Magnetic buzzer
* Single color LED
* Three color RGB LED

**Verify Web Service Running**  
See the [Ping Test](#markdown-header-ping-test) section.

**Console Test Program**  
The console test program (GpioWeb.ConsoleClient) is part of the RaspberryPi.DotNet.GpioWeb repository; it will be built along with the other applications included in the Visual Studio solution.  Once built and copied to your Rasberry Pi computer, execute the console application GpioConsoleClient.exe using Mono:

	mono ConsoleClient.exe

The test client allows you to send actions to the GPIO web service via C# code or send actions from the project's included SendJsonFile.json file (see this file for a good example of what a JSON array of actions looks like). This code project also demonstrates how you can both POST plain JSON actions to the web service, as well as construct actions using C# strongly-typed objects.

# Web Service Endpoints
The GPIO web service has two categories of endpoints:

* Actions
* Tasks

Each of these categories is described in the following sections.

## Actions
The GPIO web service has a single endpoint for working with actions.  This endpoint is to submit any array of actions that should be executed on the Pi (i.e. breadboard).  Although actions have a JSON representation, the JSON itself is really just a set of instructions that should be executed by an action plugin.  In fact, there are three distinct elements to using actions:

* Instructions - JSON format
* Plugin code to execute the action - .NET DLL(s)
* Plugin configuration data - JSON format

Action instructions (i.e. JSON properties) are determined by the action plugin creator because the plugin is responsible for executing the action. Instructions are send to the web service endpoint for execution as a JSON array.

Plugin code is given the JSON instructions and will execute the proper logic to control breadboard components.

Plugin configuration data is also given to the plugin code during execution so that the plugin can know about action-specific information.  For example, configuration data for the LED action indicates which GPIO pin the LED is connected to.  Since there could possibly be several individual LEDs on a breadboard, the plugin is given a set of configuration data that related to a specific LED.  Another example of possible configuration data is a Internet URI address that determines where a plugin will send temperature data.

**Included Actions**  
The GPIO web service comes pre-packaged with four (4) actions, along with a default set of configuration data for each action.  These actions are represented by .NET DLL files in the gpioweb.exe *plugin* directory; the configuration data for each action is located in the *config* directory.  The included actions are:

* BuzzerSimpleAction
* LedSimpleAction
* RgbSimpleAction
* LedBuzzerSimpleAction

Example JSON for each action can be found in each example project directory; the example JSON file name in each directory is *ExampleAction.json*.

**Submitting Actions**  
You submit one or more actions, by POSTing a JSON array, to the following web service endpoint (adjust the host IP/name and port number as required):

	http://127.0.0.1:8020/gpio/action

Be sure to include the HTTP header:

	Content-Type: application/json

It is that easy, assuming your breadboard has been set up and the GPIO pins used by the breadboard components match those in the default action configurations (see the *config* directory).

**Configurations**  
As mentioned previously, configurations provide a plugin information specific to an action.  One common example is to provide the GPIO pin number that a breadboard component it connected to. Configuration data is formatted as JSON and must be stored as a file in the *config* directory.  You specify which configuration file to use for an action by setting the JSON *config* action property, without the file extension:

	"config": "config_file_name",

**Note:** The names of configuation files are loaded during web service startup.  If you add, delete, or change a configuration file name, the web service must be restarted.

**Startup Actions**  
There may be times you want to automatically start an action when the GPIO web service initially starts up.  This can be accomplished by placing an action JSON file in the *startup* directory.

The most common case for adding a startup action is when a long-executing action is required.  For example, when the action will monitor something like temperature, light, or moisture.  In these cases you want the action to begin when the web service starts and execute until the web service stops.

**Note:** See [Tasks](#markdown-header-tasks) for information on long-executing actions.

## Tasks
There may be cases where you would like to execute actions, either in parallel with each other, or for a long duration (long-executing action).  Without task support, each action is executed synchronously, one at a time in the order they were submitted to the web service for execution.  Sometimes this synchronous execution is desired, but there may be other times when want to asynchronously execute two actions in parallel or have an action execute until the web service stops.  Asynchronous tasks provide the support to accomplish this.

To convert an action from synchronous to asynchronous, simply add a **unique** *taskId* property to the action JSON:

	"taskId": "867d730e-b6dc-40aa-b47a-7b6ebbb35413",

*taskId* values can be any string identifier, but are commonly formatted as a [Guid](http://en.wikipedia.org/wiki/Globally_unique_identifier) string.  Once an action is submitted as a task, it will continue to execute until the action stops; in some cases the action may never stop (e.g. monitoring temperature).  When an action does not stop on its own, there are two scenarios where it can be stopped:

* Web Service Shutdown - When the web service shuts down, all executing actions will be given an opportunity to stop.  If the action takes more than 20 seconds to stop (a configurable value) the shutdown will abort the action and continue.
* Web Service Request - You can submit a DELETE web service request to stop a action, passing a taskId.  The action will be given an opportunity to stop and the web service request will return a 202 status code.

The DELETE action endpoint is:

	http://127.0.0.1:8020/gpio/task/taskId

You can also get a list of all executing actions, in JSON format, by performing a GET on the following web service endpoint:

	http://127.0.0.1:8020/gpio/task

Finally, if you want to get information on just a single executing action, you can perform a GET on the following web service endpoint:

	http://127.0.0.1:8020/gpio/task/taskId

# Writing GPIO Action Plugins
Coming soon.  If you can't wait, you can use the existing Visual Studio GpioWeb.PluginLedSimple project as a template to create your own project.  Hint...when all is said and done, you will place your new plugin DLL file(s) into the *plugin* directory and create a configuration file in the *instance* directory. The included plugin projects include *Post Build* events that copy the plugin DLL automatically to the GpioWeb.ConsoleHost bin directory so you can just copy/paste these into your own plugin project.

# Additional Learning Resources
The [RaspberryPi.DotNet.GpioExamples](https://bitbucket.org/PaulTechGuy/raspberrypi.dotnet.gpioexamples) repository contains a great set example programs written in C# to help you learn to control components that are attached to the GPIO pins of the Raspberry Pi.


# Contact
You can contact us at <raspberrypi@paultechguy.com>.