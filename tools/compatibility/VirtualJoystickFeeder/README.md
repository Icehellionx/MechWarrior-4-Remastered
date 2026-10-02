# Virtual joystick test feeder

This source-built diagnostic sends one bounded axis, button, or POV pulse to a **separately installed** vJoy device. It does not install a driver, modify MW4, or run as part of the launcher. Run it only in the isolated joystick test VM after confirming that vJoy device 1 appears in `DirectInputInventory`.

```powershell
dotnet publish tools/compatibility/VirtualJoystickFeeder/VirtualJoystickFeeder.csproj -c Release -r win-x64 --self-contained true
VirtualJoystickFeeder.exe status
VirtualJoystickFeeder.exe axis x 1500
VirtualJoystickFeeder.exe button 1 1500
VirtualJoystickFeeder.exe pov 9000 1500
```

Each pulse resets the virtual device, explicitly centers every available axis, and neutralizes POV hats before applying the requested input. Driver-defined reset values alone need not be centered. Cleanup repeats neutralization and always releases the device, even if cleanup fails. Release and centering failures produce a nonzero exit. The axis command holds minimum and maximum for the requested duration each; button and POV commands release their input. `status` prints the raw axis-presence results; exactly `1` means present, matching upstream's C# wrapper.

The tool reports calls it made, **not** whether the game bound or consumed them. Capture game-side evidence separately, including a stable neutral baseline and a live post-pulse state. Run background feeders with `Start-Process -WindowStyle Hidden` and check the retained process's exit code. The isolated VM has exercised 31/32/128-button devices with four axes and one continuous POV; these fixtures have no force feedback. Physical controller calibration remains a separate check.

The P/Invoke signatures and HID axis usage values follow upstream [vJoy's `vjoyinterface.h`](https://github.com/shauleiz/vJoy/blob/master/inc/vjoyinterface.h). No upstream source was copied; `vJoyInterface.dll` remains an external, separately installed runtime dependency in the disposable VM. The feeder loads the installed x64 DLL from `Program Files\vJoy\x64` when present and does not copy it into the project.
