# Logitech Marble Mouse Cruise facility for Windows
This project provides a simple utility emulating the mouse cruise facility implemented by Logitech
for their Marble Mouse for Mac OSX, but not available for Windows in recent releases.

## Build
You will need either MS Visual Studio or [SharpDevelop](http://www.icsharpcode.net/opensource/sd/)
to compile the program. Then clone this project and build the `InterceptMouse.exe` executable.

## Configuration
Ensure your inner mouse buttons are both configured as type _Generic Mouse Button_ in the Logitech
Setpoint application.

You can amend various parameters in the program source code to alter the scroll size or update frequency
as desired.

## Execution
Run the executable, and you can use the mouse inner buttons to scroll windows as in the Mac
facility. The scrolled application does not have to have the focus, which is usually convenient.
You can add a shortcut to your `Startup` folder to have the application run when the OS start.

## Implementation
The program registers a global mouse hook, and if it detects that one of the special mouse buttons is 
pressed then it will send a stream of mouse scrollwheel events to the target window under the cursor while
the mouse button remains down.

## History
1.0.0 Initial version
