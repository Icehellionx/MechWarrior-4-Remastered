# Headless VM diagnostics

`Invoke-HeadlessGuest.ps1` runs a local PowerShell diagnostic inside an existing Windows VirtualBox VM. It copies the script in with `VBoxManage guestcontrol`, runs it with Guest Additions, and can copy exact result files back. It sends no input to the host desktop and works while the guest network is disconnected. VirtualBox shared folders such as `\\VBOXSVR` are not available in that offline state.

Keep guest credentials, copied media, screenshots, and results under ignored `.local/`. The password file must contain only the guest password bytes, with no trailing newline; the script passes its path to VirtualBox and never prints the contents. Use a dedicated disposable guest account and a VM that has already been created and booted. The helper does not create a VM or alter the host keyboard or mouse.

```powershell
& tools/vm/Invoke-HeadlessGuest.ps1 `
  -VmName 'MW4 Joystick Lab 20260927' `
  -LocalScript '.local/vm-joystick-20260927/guest-defender-scan.ps1' `
  -PasswordFile '.local/vm-joystick-20260927/guest-password-clean.txt' `
  -ResultFiles @('C:\MW4Lab\DiagnosticOutput\candidate-defender-installed.json') `
  -ResultDirectory '.local/vm-joystick-20260927/output' -Offline
```

`-Offline` disconnects virtual adapter 1 before the run. The caller can also pass `-GuestArguments`, `-TimeoutSeconds`, and `-ScreenshotPath`. The screenshot uses VirtualBox first and guest-side GDI as a fallback. Guest-side GDI may show a black Direct3D surface while the VM display is frozen. For a stale or `E_FAIL` capture, use `Recover-HeadlessDisplay.ps1` to send Win+Ctrl+Shift+B **to the VM only**, then take a fresh screenshot. This shortcut can cause the game to recreate its graphics device.

An elevated installer can show Windows UAC on the guest secure desktop. Inspect the VM screenshot and approve that prompt using VirtualBox VM input only for the exact, already authorized setup. The runner does not bypass UAC. Save the guest log and exact installer hash in the ignored evidence directory. Do not interpret a timed-out UAC prompt as an installer failure.
