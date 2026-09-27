# DirectInput controller inventory

Build and run this read-only x86 tool to record the DirectInput 8 game-controller enumeration order, GUIDs, axes, buttons, POV hats, and force-feedback flag:

```powershell
dotnet build tools/compatibility/DirectInputInventory/DirectInputInventory.csproj -c Release
& tools/compatibility/DirectInputInventory/bin/Release/net10.0-windows/win-x86/DirectInputInventory.exe
```

The x86 process matches the architecture of the three MW4 games. `--all` includes mouse and keyboard for an enumeration sanity check; `--include-hidden` includes hidden and phantom devices for diagnosis. The tool does not change devices, install drivers, send input, modify game configuration, or hook a game process. It queries the Windows DirectInput 8 layer used behind the candidate `dinputto8` adapter; it **does not prove what MW4 selects or consumes**. Run it immediately before a separate game-side test, and compare device order and capabilities with the game result. Review device names and GUIDs before sharing output.

On the development machine, the restricted agent sandbox hid the vJoy controller from DirectInput even though the driver was installed and PnP showed it started. An unsandboxed run enumerated the same device correctly from both x86 and x64. For meaningful device results, run the built executable in the ordinary user session rather than interpreting an empty sandbox result as an MW4 or vJoy defect. The tool itself does not request elevation.

The COM calls and structures follow Microsoft's [`IDirectInput8::EnumDevices`](https://learn.microsoft.com/en-us/previous-versions/windows/desktop/ee417804(v=vs.85)), [`DIDEVICEINSTANCE`](https://learn.microsoft.com/en-us/previous-versions/windows/desktop/ee416610(v=vs.85)), and [`IDirectInputDevice8::GetCapabilities`](https://learn.microsoft.com/en-us/previous-versions/windows/desktop/ee417892(v=vs.85)) contracts. This project contains no third-party code.
