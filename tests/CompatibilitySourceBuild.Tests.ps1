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
Assert-True ($lock.pr1Capture.localPatchSha256 -eq $capturePatchHash -and $lock.pr1Capture.versionDllSha256 -eq '614c8e95eba2f32d8eeaf7a20877d1a36051e18bc7d8e76c1d447e8acbd015e9' -and $lock.pr1Capture.versionDllSize -eq 207360) 'The setup-only PR1 capture revision and exact hosted DLL must be locked.'
Assert-True ($lock.pr1Capture.runtimeLibrary -eq 'MultiThreaded' -and (@($lock.pr1Capture.importDlls) -join ',') -eq 'KERNEL32.dll,USER32.dll' -and $script.Contains('$env:TZ = ''UTC''')) 'The fresh-Windows capture build must use a static runtime and reproduce its source ZIP across time zones.'
Assert-True ((Get-Content -LiteralPath $capturePatchPath -Raw) -match '<RuntimeLibrary>MultiThreaded</RuntimeLibrary>') 'The exact capture patch must select the static MSVC runtime.'
Assert-True ($lock.pr1Capture.normalLaunchBoundary -match 'no runtime DLL, injector, helper, driver, or elevation') 'The lock must keep capture artifacts outside normal game launch.'
Assert-True ($blackKnightBuilder -notmatch 'QualifiedLaunchHelperSha256') 'Black Knight installation must not package the process-injection helper.'
Assert-True ($currentPresentationLock.upstream -eq 'https://github.com/dege-diosg/dgVoodoo2' -and $currentPresentationLock.tag -eq 'v2.87.5') 'The current presentation layer must record its official upstream and exact release tag.'
Assert-True ($currentPresentationLock.redistribution -match 'individual dgVoodoo files' -and @($currentPresentationLock.files.PSObject.Properties).Count -eq 4) 'The current presentation lock must record the applicable redistribution boundary and every shipped x86 wrapper file.'
Assert-True ((@($currentPresentationLock.productBindings) -join ',') -eq 'vengeance,black-knight,mercenaries') 'The current presentation lock must bind the qualified wrapper to all three products.'
Assert-True ($currentPresentationLock.presentationAddon.upstreamCommit -eq 'de5f360b43c1fa61cc2c47ccfc48bbdd995badf7' -and $currentPresentationLock.presentationAddon.localPatchSha256 -eq $currentPresentationPatchHash) 'The dgVoodoo add-on must pin its exact upstream source and local patch.'
Assert-True ($currentPresentationBuild -match [regex]::Escape($currentPresentationLock.presentationAddon.upstreamCommit) -and $currentPresentationBuild -match 'git -C \$scratchSource apply -p0' -and $currentPresentationBuild -match 'ps_5_1') 'The add-on build must exact-pin upstream, apply the local patch, and rebuild its pixel shader.'
Assert-True ($currentPresentationLock.presentationAddon.reproducibility -match 'byte-identical' -and $currentPresentationLock.presentationAddon.sampleAddonDllSha256 -match '^[0-9a-f]{64}$') 'The reproducible add-on DLL hash must remain recorded.'

$inputLock = Get-Content (Join-Path $root 'third_party/dinputto8-mw4-trial.lock.json') -Raw | ConvertFrom-Json
$inputBuild = Get-Content (Join-Path $root 'tools/compatibility/build-dinputto8-mw4-trial.ps1') -Raw
Assert-True ($inputLock.commit -match '^[0-9a-f]{40}$' -and $inputLock.loggingCommit -match '^[0-9a-f]{40}$' -and $inputLock.license -eq 'Zlib') 'Input trial must pin upstream, Logging and license.'
foreach ($pair in @(@($inputLock.localPatch,$inputLock.localPatchSha256), @($inputLock.policy,$inputLock.policySha256))) {
    Assert-True ((Get-FileHash (Join-Path $root $pair[0])).Hash.ToLowerInvariant() -eq $pair[1]) 'Input trial source hashes must match the reviewed lock.'
}
Assert-True ($inputBuild.Contains('git -C $source archive') -and $inputBuild.Contains('git -C $logging archive') -and $inputBuild.Contains('Trial patch did not modify the build inputs.')) 'Input trial must export pinned sources and prove the patch was applied.'
Assert-True ($inputBuild.Contains('candidateBinarySha256') -and $inputBuild.Contains('candidateBinaryBytes') -and $inputBuild.Contains('-not $Requalify')) 'Input trial must reject an unexpected binary unless explicitly requalifying a research build.'
Assert-True ($inputBuild.Contains('MW4ButtonPolicy.Tests.exe') -and $inputBuild.Contains('& $wrapperExe') -and $inputBuild.Contains('/W4 /WX')) 'Input build must compile and run policy and actual-wrapper regressions.'
$policyAttributes = & git -C $root check-attr eol -- $inputLock.policy
if ($LASTEXITCODE) { throw 'Cannot inspect hash-pinned policy checkout attributes.' }
Assert-True ($policyAttributes -match ': eol: lf$') 'Hash-pinned native policy must retain LF bytes on Windows checkouts.'
$productionInput = Get-Content (Join-Path $root 'third_party/dinputto8.lock.json') -Raw | ConvertFrom-Json
$inputVerifier = Get-Content (Join-Path $root 'src/MW4Remastered.Core/Install/LegacyInputCompatibility.cs') -Raw
Assert-True ($productionInput.binarySha256 -eq $inputLock.candidateBinarySha256 -and $productionInput.binarySize -eq $inputLock.candidateBinaryBytes) 'Production and source-build pins must identify the same tested adapter.'
Assert-True ($inputVerifier.Contains($productionInput.binarySha256) -and $inputVerifier.Contains('273_408')) 'Runtime verification must match the packaged adapter.'
Assert-True ($productionInput.commit -eq $inputLock.commit -and $productionInput.submodule.commit -eq $inputLock.loggingCommit -and $productionInput.localBuild.patchSha256 -eq $inputLock.localPatchSha256 -and $productionInput.localBuild.policySha256 -eq $inputLock.policySha256) 'Production must preserve exact modified-source provenance.'
$nativeWorkflow = Get-Content (Join-Path $root '.github/workflows/application-ci.yml') -Raw
Assert-True ($nativeWorkflow.Contains('native-input-regressions:') -and $nativeWorkflow.Contains($inputLock.commit) -and $nativeWorkflow.Contains('build-dinputto8-mw4-trial.ps1') -and $nativeWorkflow.Contains('submodule update --init --recursive')) 'CI must build the actual patched wrapper from pinned upstream and Logging sources.'
$profileAttributes = & git -C $root check-attr text -- assets/compatibility/dgVoodoo-MW4.conf
if ($LASTEXITCODE) { throw 'Cannot inspect qualified renderer checkout attributes.' }
Assert-True ($profileAttributes -match ': text: unset$') 'Git must not rewrite the byte-qualified renderer profile on checkout.'
Assert-True ($nativeWorkflow.Contains('core.autocrlf=true checkout')) 'Input source checkout must preserve the qualified CRLF license notice.'
Write-Host 'Compatibility source-build contract tests passed.'
