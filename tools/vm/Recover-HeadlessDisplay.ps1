# Recovers a frozen Windows guest graphics surface without host keyboard input.
# Use only when VM screenshots are stale or VirtualBox returns E_FAIL.
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$VmName,
    [Parameter(Mandatory)][string]$ScreenshotPath
)

$ErrorActionPreference = 'Stop'
$vbox = Join-Path $env:ProgramFiles 'Oracle\VirtualBox\VBoxManage.exe'
if (-not (Test-Path -LiteralPath $vbox -PathType Leaf)) { throw 'VBoxManage.exe was not found.' }
$info = & $vbox showvminfo $VmName --machinereadable 2>&1
if ($LASTEXITCODE -ne 0 -or $info -notcontains 'VMState="running"') {
    throw "VM '$VmName' is not running."
}

# Guest-only Win+Ctrl+Shift+B. This resets Windows' graphics driver and may
# make the game recreate its D3D surface; capture the pre-reset state first
# when VirtualBox still allows it.
& $vbox controlvm $VmName keyboardputscancode e0 5b 1d 2a 30 b0 aa 9d e0 db
if ($LASTEXITCODE -ne 0) { throw 'Could not send the VM graphics reset shortcut.' }
Start-Sleep -Seconds 3
$capture = [IO.Path]::GetFullPath($ScreenshotPath)
[IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($capture)) | Out-Null
& $vbox controlvm $VmName screenshotpng $capture
if ($LASTEXITCODE -ne 0) { throw 'The VM graphics surface did not recover.' }
Write-Output "VM display recovered: $capture"
