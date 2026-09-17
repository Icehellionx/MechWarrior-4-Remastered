$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot

function Assert-True {
    param([Parameter(Mandatory)][bool]$Condition, [Parameter(Mandatory)][string]$Message)
    if (-not $Condition) { throw $Message }
}

foreach ($component in @('MW4Remastered.Installer', 'MW4Remastered.Launcher', 'MW4Remastered.RtpPatchHost', 'MW4Remastered.BlackKnightCaptureHost')) {
    $componentRoot = Join-Path $root "src/$component"
    $projectPath = Join-Path $componentRoot "$component.csproj"
    $manifestPath = Join-Path $componentRoot 'app.manifest'

    [xml]$project = Get-Content -LiteralPath $projectPath -Raw
    [xml]$manifest = Get-Content -LiteralPath $manifestPath -Raw
    $manifestProperty = $project.Project.PropertyGroup.ApplicationManifest
    $executionLevel = $manifest.SelectSingleNode("//*[local-name()='requestedExecutionLevel']")

    Assert-True ($manifestProperty -eq 'app.manifest') "$component must embed its checked-in manifest."
    Assert-True ($null -ne $executionLevel) "$component must declare an execution level."
    Assert-True ($executionLevel.level -eq 'asInvoker') "$component must never request routine elevation."
    Assert-True ($executionLevel.uiAccess -eq 'false') "$component must not request UIAccess."
}

$captureHost = Get-Content -LiteralPath (Join-Path $root 'src/MW4Remastered.BlackKnightCaptureHost/Program.cs') -Raw
$launchOrchestrator = Get-Content -LiteralPath (Join-Path $root 'src/MW4Remastered.Core/Launch/LaunchOrchestrator.cs') -Raw
Assert-True ($captureHost -match 'args\.Length != 0' -and $captureHost -match '"MW4x\.exe"' -and $captureHost -match '"version\.dll"') 'Black Knight capture must accept no caller-selected target or DLL.'
Assert-True ($captureHost -match 'CreateSuspended' -and $captureHost -match 'CreateRemoteThread' -and $captureHost -match 'CaptureTimeoutMilliseconds = 60_000') 'Setup-only capture must inject before protection startup and retain a bounded wait.'
Assert-True ($captureHost -match '!completed\) TerminateProcess' -and $captureHost -match '!completed && File\.Exists\(output\)') 'Failed capture must terminate its exact child and remove partial output.'
Assert-True (($captureHost | Select-String -Pattern 'SetEnvironmentVariable\(OutputEnvironmentVariable, previousOutput\)' -AllMatches).Matches.Count -ge 2) 'Capture must restore its inherited output variable after both process-creation and post-creation failures.'
Assert-True ($captureHost -match 'if \(remoteCompleted\) VirtualFreeEx') 'Capture must not free the remote DLL-path buffer while its loader thread may still be running.'
Assert-True ($launchOrchestrator -notmatch 'BlackKnightCapture|CreateRemoteThread|BlackKnightPr1Capture') 'Normal game launch must remain independent of the setup-only capture boundary.'

Write-Host 'Application privilege boundary tests passed.'
