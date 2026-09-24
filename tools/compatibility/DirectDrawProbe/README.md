# DirectDraw startup probe

This source-only x86 diagnostic checks the first two DirectDraw calls implicated in the RTX 5090 Vengeance report. An optional `surface` stage also sets windowed cooperative level and creates a primary surface using a hidden probe window. It does not start a game, change a configuration or display mode, use game media, or verify rendering. It is not part of the installer or release package.

Build a portable executable on a trusted Windows development PC with the .NET 10 SDK:

```powershell
dotnet publish tools/compatibility/DirectDrawProbe/DirectDrawProbe.csproj -c Release --self-contained true -p:PublishSingleFile=true -o .local/directdraw-probe
```

Run from the directory containing the installed game's `DDraw.dll` so dgVoodoo can find its companion files and configuration. Use the **exact affected installation**, not a copied or edited DLL. For a comparison, repeat in a known working game's directory. Run the system check separately:

```powershell
Push-Location 'F:\Mechwarrior Test\app-cue\vengeance'
& 'C:\path\to\DirectDrawProbe.exe' (Join-Path (Get-Location) 'DDraw.dll')
& 'C:\path\to\DirectDrawProbe.exe' (Join-Path (Get-Location) 'DDraw.dll') surface
Pop-Location
& 'C:\path\to\DirectDrawProbe.exe' system
```

Replace the example paths with actual ones. Record Windows version, GPU and driver version, the exact game, whether the game fails before a window or during rendering, the probe's SHA-256 and HRESULTs, and the game's sanitized startup log. Do not send disc images, game binaries, serials, or personal paths. The probe does not print the DLL path, but Windows device descriptions and an exception message may identify local hardware or a path; review output before sharing.

Exit code 0 means each requested call returned success and an object pointer. Exit code 1 means a requested call or DLL load failed; 2 means invalid invocation or a missing file. A successful probe **does not establish that the selected D3D11/D3D12 backend can create a device, present a frame, or run MW4**. In local scratch tests, even an invalid dgVoodoo `OutputAPI` value passed enumeration, object creation, cooperative level, and primary surface creation. This test is useful for separating early DLL/API and surface failures from later game or renderer initialization, not for qualifying RTX 5090 compatibility. The optional surface stage follows the Windows SDK `IDirectDraw7` method layout and [`CreateSurface` contract](https://learn.microsoft.com/en-us/windows/win32/api/ddraw/nf-ddraw-idirectdraw7-createsurface).
