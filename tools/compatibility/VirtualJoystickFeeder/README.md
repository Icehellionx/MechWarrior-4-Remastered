# Virtual joystick test feeder

This source-built diagnostic sends one bounded axis, button, or POV pulse to a **separately installed** vJoy device. It does not install a driver, modify MW4, or run as part of the launcher. Run it only in the isolated joystick test VM after confirming that vJoy device 1 appears in `DirectInputInventory`.

```powershell
dotnet publish tools/compatibility/VirtualJoystickFeeder/VirtualJoystickFeeder.csproj -c Release -r win-x64 --self-contained true
VirtualJoystickFeeder.exe status
VirtualJoystickFeeder.exe axis x 1500
VirtualJoystickFeeder.exe button 1 1500
VirtualJoystickFeeder.exe pov 9000 1500
```

Each pulse resets and releases the virtual device in a `finally` block. The axis command holds minimum and maximum for the requested duration each, then centers it; the button and POV commands return to neutral. The tool reports calls it made, **not** whether the game bound or consumed them. Capture game-side evidence separately. The first isolated VM run used the driver's default 16-axis, 8-button, zero-POV device with no force feedback. The 31-button, four-axis, one-POV target shape still needs game-side testing. A reset may alter other axis values, so observe all in-game motion during and after a pulse.

The P/Invoke signatures and HID axis usage values follow upstream [vJoy's `vjoyinterface.h`](https://github.com/shauleiz/vJoy/blob/master/inc/vjoyinterface.h). No upstream source was copied; `vJoyInterface.dll` remains an external, separately installed runtime dependency in the disposable VM. The feeder loads the installed x64 DLL from `Program Files\vJoy\x64` when present and does not copy it into the project.
