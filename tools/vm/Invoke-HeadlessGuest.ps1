# Runs a diagnostic script inside an existing VirtualBox guest. It never sends
# input to the host desktop. Keep credentials and guest output under .local/.
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$VmName,
    [Parameter(Mandatory)][string]$LocalScript,
    [Parameter(Mandatory)][string]$PasswordFile,
    [string[]]$GuestArguments = @(),
    [string]$GuestUser = 'MW4Lab',
    [string]$GuestWorkDirectory = 'C:\MW4Lab',
    [string[]]$ResultFiles = @(),
    [string]$ResultDirectory,
    [int]$TimeoutSeconds = 120,
    [string]$ScreenshotPath,
    [switch]$Offline
)

$ErrorActionPreference = 'Stop'
$vbox = Join-Path $env:ProgramFiles 'Oracle\VirtualBox\VBoxManage.exe'
if (-not (Test-Path -LiteralPath $vbox -PathType Leaf)) {
    throw 'VBoxManage.exe was not found in the installed VirtualBox directory.'
}
$password = (Resolve-Path -LiteralPath $PasswordFile).Path
$script = (Resolve-Path -LiteralPath $LocalScript).Path
$bytes = [IO.File]::ReadAllBytes($password)
if ($bytes.Length -eq 0 -or $bytes[-1] -in @(10, 13)) {
    throw 'Guest password file must be nonempty and have no trailing newline.'
}
if ($TimeoutSeconds -lt 1 -or $TimeoutSeconds -gt 3600) {
    throw 'TimeoutSeconds must be between 1 and 3600.'
}
$info = & $vbox showvminfo $VmName --machinereadable 2>&1
if ($LASTEXITCODE -ne 0) { throw "Cannot inspect VM '$VmName': $info" }
if ($info -notcontains 'VMState="running"') { throw "VM '$VmName' is not running." }
if ($Offline) {
    & $vbox controlvm $VmName setlinkstate1 off
    if ($LASTEXITCODE -ne 0) { throw 'Could not disconnect VM network adapter 1.' }
}

$guestScript = [IO.Path]::Combine($GuestWorkDirectory, [IO.Path]::GetFileName($script))
& $vbox guestcontrol $VmName copyto --username=$GuestUser --passwordfile=$password $script $guestScript
if ($LASTEXITCODE -ne 0) { throw "Could not copy guest diagnostic script to $guestScript." }
$quotedGuestScript = $guestScript.Replace("'", "''")
$literalArguments = ($GuestArguments | ForEach-Object { "'" + $_.Replace("'", "''") + "'" }) -join ' '
$command = "`$ErrorActionPreference='Stop'; `$ProgressPreference='SilentlyContinue'; try { & '$quotedGuestScript' $literalArguments; if (-not `$?) { exit 1 } } catch { [Console]::Error.WriteLine(`$_.Exception.Message); exit 1 }"
$encoded = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($command))
$timeoutMs = $TimeoutSeconds * 1000
& $vbox guestcontrol $VmName run --username=$GuestUser --passwordfile=$password `
    --exe='C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe' `
    --wait-stdout --wait-stderr "--timeout=$timeoutMs" -- `
    powershell.exe -NoProfile -ExecutionPolicy Bypass -EncodedCommand $encoded
$guestExit = $LASTEXITCODE
if ($guestExit -ne 0) { throw "Guest script failed with VBoxManage exit code $guestExit." }
if ($ResultFiles.Count -gt 0) {
    if (-not $ResultDirectory) { throw 'ResultDirectory is required when ResultFiles are requested.' }
    $destination = [IO.Path]::GetFullPath($ResultDirectory)
    [IO.Directory]::CreateDirectory($destination) | Out-Null
    foreach ($result in $ResultFiles) {
        $localFile = [IO.Path]::Combine($destination, [IO.Path]::GetFileName($result))
        & $vbox guestcontrol $VmName copyfrom --username=$GuestUser --passwordfile=$password $result $localFile
        if ($LASTEXITCODE -ne 0) { throw "Could not copy guest result $result." }
        Write-Output "Guest result: $localFile"
    }
}
if ($ScreenshotPath) {
    $capture = [IO.Path]::GetFullPath($ScreenshotPath)
    [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($capture)) | Out-Null
    $ErrorActionPreference = 'Continue'
    try {
        $hostCapture = & $vbox controlvm $VmName screenshotpng $capture 2>&1
        $hostCaptureExit = $LASTEXITCODE
    }
    finally { $ErrorActionPreference = 'Stop' }
    if ($hostCaptureExit -ne 0) {
        # VirtualBox's host screenshot can return E_FAIL while the guest display
        # is still healthy. Capture from inside the guest as a fallback.
        $guestCapture = [IO.Path]::Combine($GuestWorkDirectory, 'vm-diagnostic-screen.png')
        $guestQuoted = $guestCapture.Replace("'", "''")
        $screenCommand = @"
`$ErrorActionPreference='Stop'
`$ProgressPreference='SilentlyContinue'
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms
`$bounds=[System.Windows.Forms.Screen]::PrimaryScreen.Bounds
`$bitmap=New-Object System.Drawing.Bitmap(`$bounds.Width,`$bounds.Height)
try {
  `$graphics=[System.Drawing.Graphics]::FromImage(`$bitmap)
  try { `$graphics.CopyFromScreen(`$bounds.Location,[System.Drawing.Point]::Empty,`$bounds.Size) }
  finally { `$graphics.Dispose() }
  `$bitmap.Save('$guestQuoted',[System.Drawing.Imaging.ImageFormat]::Png)
} finally { `$bitmap.Dispose() }
"@
        $screenEncoded = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($screenCommand))
        & $vbox guestcontrol $VmName run --username=$GuestUser --passwordfile=$password `
            --exe='C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe' `
            --wait-stdout --wait-stderr '--timeout=30000' -- `
            powershell.exe -NoProfile -EncodedCommand $screenEncoded
        if ($LASTEXITCODE -ne 0) { throw "VM screenshot failed on host and guest: $hostCapture" }
        & $vbox guestcontrol $VmName copyfrom --username=$GuestUser --passwordfile=$password $guestCapture $capture
        if ($LASTEXITCODE -ne 0) { throw 'VM guest-side screenshot could not be retrieved.' }
        Write-Output 'VirtualBox host screenshot failed; captured the guest display directly.'
    }
    Write-Output "Guest screenshot: $capture"
}
