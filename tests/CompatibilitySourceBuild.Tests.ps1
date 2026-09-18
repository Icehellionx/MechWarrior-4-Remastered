$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$lockPath = Join-Path $root 'third_party/SafeDiscLoader2.lock.json'
$scriptPath = Join-Path $root 'tools/compatibility/build-safedisc-loader2.ps1'
$workflowPath = Join-Path $root '.github/workflows/compatibility-build.yml'
$blackKnightBuilderPath = Join-Path $root 'src/MW4Remastered.Core/Install/BlackKnightInstallPlanBuilder.cs'
$capturePatchPath = Join-Path $root 'third_party/patches/SafeDiscLoader2-MW4-BlackKnight-PR1-Capture.patch'
$currentPresentationLockPath = Join-Path $root 'third_party/dgVoodoo2.lock.json'
$currentPresentationBuildPath = Join-Path $root 'tools/compatibility/build-dgvoodoo-mw4-addon.ps1'
$currentPresentationPatchPath = Join-Path $root 'tools/compatibility/patches/dgVoodoo2-MW4-Presentation.patch'

$lock = Get-Content -LiteralPath $lockPath -Raw | ConvertFrom-Json
$script = Get-Content -LiteralPath $scriptPath -Raw
$workflow = Get-Content -LiteralPath $workflowPath -Raw
$blackKnightBuilder = Get-Content -LiteralPath $blackKnightBuilderPath -Raw
$capturePatchHash = (Get-FileHash -LiteralPath $capturePatchPath -Algorithm SHA256).Hash.ToLowerInvariant()
$currentPresentationLock = Get-Content -LiteralPath $currentPresentationLockPath -Raw | ConvertFrom-Json
$currentPresentationBuild = Get-Content -LiteralPath $currentPresentationBuildPath -Raw
$currentPresentationPatchHash = (Get-FileHash -LiteralPath $currentPresentationPatchPath -Algorithm SHA256).Hash.ToLowerInvariant()

function Assert-True {
    param([Parameter(Mandatory)][bool]$Condition, [Parameter(Mandatory)][string]$Message)
    if (-not $Condition) { throw $Message }
}

Assert-True ($lock.commit -match '^[0-9a-f]{40}$') 'Compatibility source must be pinned to a full commit.'
Assert-True ($lock.license -eq 'GPL-3.0-only') 'Compatibility source license must remain explicit.'
Assert-True ($lock.includedProjects.Count -eq 1 -and $lock.includedProjects[0] -eq 'version-proxy.vcxproj') 'Only the DLL project may be built.'
Assert-True ($lock.excludedProjects -contains 'VersionInjector/VersionInjector.vcxproj') 'The elevated upstream injector must remain excluded.'
Assert-True ($script -match [regex]::Escape($lock.commit)) 'Build script must enforce the pinned source commit.'
Assert-True ($script -match 'version-proxy\.vcxproj' -and $script -notmatch 'version-proxy\.sln') 'Build script must build only the DLL project, never the solution containing VersionInjector.'
Assert-True ($script -match '0x014C') 'Build script must verify the output is x86.'
Assert-True ($script -match 'Clear-PeTimestamps' -and $script -match 'IMAGE_DEBUG_DIRECTORY') 'Build must normalize non-semantic MSVC PE timestamps.'
Assert-True ($script -match 'git -C \$source archive --format=zip') 'Build must emit the exact corresponding GPL source archive.'
Assert-True ($script -match [regex]::Escape($capturePatchHash) -and $script -match "Pr1Capture") 'Build script must require the exact setup-only PR1 capture patch.'
Assert-True ($workflow -match [regex]::Escape($lock.commit)) 'CI workflow must check out the pinned upstream commit.'
Assert-True ($workflow -notmatch 'VersionInjector') 'CI workflow must not build or package the elevated injector.'
Assert-True ($workflow -notmatch 'publish-launch-helper') 'CI workflow must not build or package a runtime launch helper.'
Assert-True ($workflow -match 'SafeDiscLoader2-Pr1Capture' -and $workflow -match [regex]::Escape((Split-Path $capturePatchPath -Leaf))) 'CI must build the pinned setup-only PR1 capture DLL without VersionInjector.'
Assert-True ($lock.pr1Capture.localPatchSha256 -eq $capturePatchHash -and $lock.pr1Capture.versionDllSha256 -eq '7fbf1fbd0b251986f0dcd2082f218650eddff40d661ede0deefd4b2ce6b5c6fc') 'The setup-only PR1 capture revision and exact hosted DLL must be locked.'
Assert-True ($lock.pr1Capture.normalLaunchBoundary -match 'no runtime DLL, injector, helper, driver, or elevation') 'The lock must keep capture artifacts outside normal game launch.'
Assert-True ($blackKnightBuilder -notmatch 'QualifiedLaunchHelperSha256') 'Black Knight installation must not package the process-injection helper.'
Assert-True ($currentPresentationLock.upstream -eq 'https://github.com/dege-diosg/dgVoodoo2' -and $currentPresentationLock.tag -eq 'v2.87.5') 'The current presentation layer must record its official upstream and exact release tag.'
Assert-True ($currentPresentationLock.redistribution -match 'individual dgVoodoo files' -and @($currentPresentationLock.files.PSObject.Properties).Count -eq 4) 'The current presentation lock must record the applicable redistribution boundary and every shipped x86 wrapper file.'
Assert-True ((@($currentPresentationLock.productBindings) -join ',') -eq 'vengeance,black-knight,mercenaries') 'The current presentation lock must bind the qualified wrapper to all three products.'
Assert-True ($currentPresentationLock.presentationAddon.upstreamCommit -eq 'de5f360b43c1fa61cc2c47ccfc48bbdd995badf7' -and $currentPresentationLock.presentationAddon.localPatchSha256 -eq $currentPresentationPatchHash) 'The dgVoodoo add-on must pin its exact upstream source and local patch.'
Assert-True ($currentPresentationBuild -match [regex]::Escape($currentPresentationLock.presentationAddon.upstreamCommit) -and $currentPresentationBuild -match 'git -C \$scratchSource apply -p0' -and $currentPresentationBuild -match 'ps_5_1') 'The add-on build must exact-pin upstream, apply the local patch, and rebuild its pixel shader.'
Assert-True ($currentPresentationLock.presentationAddon.reproducibility -match 'byte-identical' -and $currentPresentationLock.presentationAddon.sampleAddonDllSha256 -match '^[0-9a-f]{64}$') 'The reproducible add-on DLL hash must remain recorded.'

Write-Host 'Compatibility source-build contract tests passed.'
