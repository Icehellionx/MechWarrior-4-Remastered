[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$Path
)

$ErrorActionPreference = 'Stop'
$resolvedTarget = (Resolve-Path -LiteralPath $Path -ErrorAction Stop).Path
$targetItem = Get-Item -LiteralPath $resolvedTarget -Force
if ($targetItem.Attributes -band [IO.FileAttributes]::ReparsePoint) {
    throw "Defender scan target must not be a reparse point: $resolvedTarget"
}

$defenderPlatformRoot = Join-Path $env:ProgramData 'Microsoft\Windows Defender\Platform'
$defenderCommand = Get-ChildItem -LiteralPath $defenderPlatformRoot -Directory -ErrorAction SilentlyContinue |
    Sort-Object Name -Descending |
    ForEach-Object { Join-Path $_.FullName 'MpCmdRun.exe' } |
    Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } |
    Select-Object -First 1
if (-not $defenderCommand) {
    $defenderCommand = Join-Path $env:ProgramFiles 'Windows Defender\MpCmdRun.exe'
}
if (-not (Test-Path -LiteralPath $defenderCommand -PathType Leaf)) {
    throw 'Microsoft Defender MpCmdRun.exe is not available on this device.'
}

$defenderLogName = 'Microsoft-Windows-Windows Defender/Operational'
try {
    $latestDefenderEvent = Get-WinEvent -LogName $defenderLogName -MaxEvents 1 -ErrorAction Stop
}
catch {
    throw "Cannot read the Defender operational log required to distinguish a clean scan from successful remediation: $($_.Exception.Message)"
}
$baselineRecordId = if ($latestDefenderEvent) { $latestDefenderEvent.RecordId } else { 0 }
$scanStartedUtc = [DateTime]::UtcNow

$defenderStatus = Get-MpComputerStatus -ErrorAction Stop
if (-not $defenderStatus.AntivirusEnabled) {
    throw 'Microsoft Defender Antivirus is not enabled; release cleanliness cannot be qualified.'
}

Write-Host "Scanning with Defender engine $($defenderStatus.AMEngineVersion), intelligence $($defenderStatus.AntivirusSignatureVersion): $resolvedTarget"
$scanOutput = & $defenderCommand -Scan -ScanType 3 -File $resolvedTarget 2>&1
$scanExitCode = $LASTEXITCODE
$scanOutput | ForEach-Object { Write-Host $_ }

Start-Sleep -Milliseconds 750
$targetPattern = [regex]::Escape($resolvedTarget)
$newDetections = Get-WinEvent -FilterHashtable @{
    LogName = $defenderLogName
    Id = 1116, 1117
    StartTime = $scanStartedUtc.AddSeconds(-2)
} -ErrorAction SilentlyContinue | Where-Object {
    $_.RecordId -gt $baselineRecordId -and $_.Message -match $targetPattern
}

if ($newDetections) {
    $summary = $newDetections | ForEach-Object { "event $($_.Id) at $($_.TimeCreated.ToString('o'))" }
    throw "Defender detected or remediated content under the scan target ($($summary -join ', '))."
}
if ($scanExitCode -ne 0) {
    throw "Defender custom scan failed or requires action (exit code $scanExitCode)."
}

Write-Host "Defender scan passed with no new detections: $resolvedTarget"
