# Headless VM diagnostics

The diagnostic PowerShell process uses `-WindowStyle Hidden`. Launch background native feeders hidden too, retain their process handle, and check their exit code; a successful helper call does not prove that a game bound or consumed input. When using VirtualBox relative hardware mouse input, first capture the actual game hover and only then click. Relative scaling varies across MW4 menus, and a pointer moved outside the image can look like an input hang. A missing pointer or a missed click is not sufficient evidence for graphics reset or a game defect.

`Invoke-HeadlessGuest.ps1` runs a local PowerShell diagnostic inside an existing Windows VirtualBox VM. It copies the script in with `VBoxManage guestcontrol`, runs it with Guest Additions, and can copy exact result files back. It sends no input to the host desktop and works while the guest network is disconnected. Prefer local guest paths and exact result copies; a shared-folder path additionally depends on its configured mapping and Guest Additions filesystem driver.

Keep guest credentials, copied media, screenshots, and results under ignored `.local/`. The password file must contain only the guest password bytes, with no trailing newline; the script passes its path to VirtualBox and never prints the contents. Use a dedicated disposable guest account and a VM that has already been created and booted. The helper does not create a VM or alter the host keyboard or mouse.

```powershell
& tools/vm/Invoke-HeadlessGuest.ps1 `
  -VmName 'MW4 Joystick Lab 20260927' `
  -LocalScript '.local/vm-joystick-20260927/guest-defender-scan.ps1' `
  -PasswordFile '.local/vm-joystick-20260927/guest-password-clean.txt' `
  -ResultFiles @('C:\MW4Lab\DiagnosticOutput\candidate-defender-installed.json') `
  -ResultDirectory '.local/vm-joystick-20260927/output' -Offline
```

`-Offline` disconnects virtual adapter 1 before the run. The caller can also pass `-GuestArguments`, `-TimeoutSeconds`, `-GuestArchitecture x86` (for 32-bit game module inspection), and `-ScreenshotPath`. The screenshot uses VirtualBox first and guest-side GDI as a fallback. Guest-side GDI may show a black Direct3D surface while the VM display is frozen. For a stale or `E_FAIL` capture, use `Recover-HeadlessDisplay.ps1` to send Win+Ctrl+Shift+B **to the VM only**, then take a fresh screenshot. This shortcut can cause the game to recreate its graphics device.

VirtualBox 7.2 supplies the executable name as `argv[0]` from `--exe`; arguments after `--` begin at `argv[1]`. Do not repeat `powershell.exe` there: PowerShell interprets it as a command and starts another shell, complicating process lifetime and output capture. The same applies to direct `cmd.exe` probes. Keep an elevated child job's parent alive with `Start-Process -Wait`, record the child's exit code, and allow enough timeout for observed UAC consent and the job itself.

Before treating a vJoy configuration failure as an input defect, verify its native exit code and independently inventory the resulting device. The lab's x64 `vJoyConfig.exe` requires the x64 Visual C++ runtime; `0xC0000135` means a dependency is missing. Use Microsoft's signed runtime installer inside the guest rather than copying arbitrary DLLs. Guest-only paravirtualization changes are reversible lab experiments; snapshot the powered-off VM and record settings before testing them. They do not require disabling the host hypervisor or Windows security.

An elevated installer can show Windows UAC on the guest secure desktop. Inspect the VM screenshot and approve that prompt using VirtualBox VM input only for the exact, already authorized setup. The runner does not bypass UAC. Save the guest log and exact installer hash in the ignored evidence directory. Do not interpret a timed-out UAC prompt as an installer failure.

If both guestcontrol and the display stall, first preserve the last screenshot and worker log. A basic `guestcontrol run` of `cmd.exe /c echo GUEST_OK` distinguishes a slow diagnostic script from an unresponsive guest. During the five-media clean-install smoke, the VM once stopped writing its virtual disk and stopped accepting guestcontrol sessions; a display reset could not fix it. After recording the stalled worker stage, a VM-only poweroff and headless start restored diagnostics, and a retry of the same exact setup completed. Treat such a run as interrupted evidence. Check the new worker log and installer exit code after recovery; never infer success from a full progress bar or a stale screenshot.
