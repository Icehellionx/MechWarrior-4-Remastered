$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot

function Assert-True {
    param([Parameter(Mandatory)][bool]$Condition, [Parameter(Mandatory)][string]$Message)
    if (-not $Condition) { throw $Message }
}

foreach ($component in @('MW4Remastered.Installer', 'MW4Remastered.Launcher', 'MW4Remastered.RtpPatchHost')) {
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

$launchOrchestrator = Get-Content -LiteralPath (Join-Path $root 'src/MW4Remastered.Core/Launch/LaunchOrchestrator.cs') -Raw
$capture = Get-Content -LiteralPath (Join-Path $root 'src/MW4Remastered.Core/Install/BlackKnightPr1ImageCapture.cs') -Raw
$transform = Get-Content -LiteralPath (Join-Path $root 'src/MW4Remastered.Core/Install/BlackKnightPr1ExecutableTransform.cs') -Raw
$transformDefinition = Get-Content -LiteralPath (Join-Path $root 'assets/compatibility/black-knight-pr1-static-transform.json') -Raw | ConvertFrom-Json
Assert-True ($capture -match 'CaptureDllSha256' -and $capture -match 'Process\.Start' -and $capture -notmatch 'CreateRemoteThread|WriteProcessMemory|VirtualAllocEx|VersionInjector') 'Black Knight setup capture must exact-hash its DLL and must not inject from a project helper.'
Assert-True ($capture -match 'DeleteCaptureArtifact' -and $capture -match 'TimeSpan\.FromSeconds\(15\)') 'Black Knight setup capture must wait for the temporary protected child to release exact setup artifacts before cleanup.'
Assert-True ($capture -match 'JobObjectLimitKillOnJobClose' -and $capture -match 'AssignProcessToJobObject') 'Black Knight setup capture must contain and terminate only its owned SafeDisc process family before artifact cleanup.'
Assert-True ($transform -match 'OutputLength' -and $transform -match 'CleanImageSize' -and $transform -match 'WriteUInt16LittleEndian') 'Black Knight transform must exclude the SafeDisc-only tail sections from the installed image.'
Assert-True ($transform -match 'patched != 127' -and $transform -match 'RejectResidualTailBranches' -and $transform -match 'still targets removed SafeDisc code') 'Black Knight transform must repair the complete qualified branch map and reject any executable branch left in the removed SafeDisc tail.'
Assert-True ($transform -match 'ApplyIatCorrections\(protectedBytes' -and $transform -match 'patched\.Count != 110') 'Black Knight static output must apply and count the reviewed import-slot correction set.'
Assert-True ($transform -match 'isIndirectBranchOperand' -and $transform -match 'isEntryPointLoadOperand' -and $transform -match 'not a qualified code operand') 'Black Knight IAT corrections must be constrained to indirect branch operands or the two reviewed entry-point MOV operands.'
Assert-True ($transformDefinition.iatCorrections.Count -eq 110) 'The Black Knight transform definition must retain all 110 reviewed import-slot corrections.'
$entryPointCorrections = @($transformDefinition.iatCorrections | Where-Object { $_[0] -in 0x334c23, 0x334c2a })
$insertMenuCorrections = @($entryPointCorrections | Where-Object { $_[2] -eq 'USER32.dll' -and $_[3] -eq 'InsertMenuItemA' })
$enableMenuCorrections = @($entryPointCorrections | Where-Object { $_[2] -eq 'USER32.dll' -and $_[3] -eq 'EnableMenuItem' })
Assert-True ($entryPointCorrections.Count -eq 2 -and $insertMenuCorrections.Count -eq 1 -and $enableMenuCorrections.Count -eq 1) 'The two entry-point MOV operands must not be misclassified as SendMessageA callsites.'
Assert-True ($launchOrchestrator -notmatch 'BlackKnightPr1Capture|BlackKnightRuntime|CreateRemoteThread|VersionInjector') 'Normal game launch must start the static Black Knight executable directly without a compatibility helper.'
Assert-True (-not (Test-Path -LiteralPath (Join-Path $root 'src/MW4Remastered.BlackKnightCaptureHost/Program.cs')) -and
    -not (Test-Path -LiteralPath (Join-Path $root 'src/MW4Remastered.BlackKnightCaptureHost/MW4Remastered.BlackKnightCaptureHost.csproj'))) 'The retired process-injection capture host source must remain removed.'

Write-Host 'Application privilege boundary tests passed.'
