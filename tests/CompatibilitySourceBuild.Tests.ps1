$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$lockPath = Join-Path $root 'third_party/SafeDiscLoader2.lock.json'
$scriptPath = Join-Path $root 'tools/compatibility/build-safedisc-loader2.ps1'
$bundleScriptPath = Join-Path $root 'tools/compatibility/assemble-black-knight-bundle.ps1'
$workflowPath = Join-Path $root '.github/workflows/compatibility-build.yml'
$blackKnightBuilderPath = Join-Path $root 'src/MW4Remastered.Core/Install/BlackKnightInstallPlanBuilder.cs'
$patchPath = Join-Path $root 'third_party/patches/SafeDiscLoader2-MW4-BlackKnight.patch'
$runtimePatchPath = Join-Path $root 'third_party/patches/SafeDiscLoader2-MW4-BlackKnight-PR1-Runtime.patch'
$presentationBuildPath = Join-Path $root 'tools/compatibility/build-ddrawcompat-mw4.ps1'
$presentationLockPath = Join-Path $root 'third_party/DDrawCompat-MW3.lock.json'

$lock = Get-Content -LiteralPath $lockPath -Raw | ConvertFrom-Json
$script = Get-Content -LiteralPath $scriptPath -Raw
$bundleScript = Get-Content -LiteralPath $bundleScriptPath -Raw
$workflow = Get-Content -LiteralPath $workflowPath -Raw
$blackKnightBuilder = Get-Content -LiteralPath $blackKnightBuilderPath -Raw
$patchHash = (Get-FileHash -LiteralPath $patchPath -Algorithm SHA256).Hash.ToLowerInvariant()
$runtimePatchHash = (Get-FileHash -LiteralPath $runtimePatchPath -Algorithm SHA256).Hash.ToLowerInvariant()
$presentationBuild = Get-Content -LiteralPath $presentationBuildPath -Raw
$presentationLock = Get-Content -LiteralPath $presentationLockPath -Raw | ConvertFrom-Json

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
Assert-True ($script -match [regex]::Escape($patchHash)) 'Build script must require the exact local Black Knight patch.'
Assert-True ($script -match [regex]::Escape($runtimePatchHash) -and $script -match "Pr1Runtime") 'Build script must require the exact app-local PR1 runtime patch.'
Assert-True ($workflow -match [regex]::Escape($lock.commit)) 'CI workflow must check out the pinned upstream commit.'
Assert-True ($workflow -notmatch 'VersionInjector') 'CI workflow must not build or package the elevated injector.'
Assert-True ($workflow -notmatch 'publish-launch-helper') 'CI workflow must not build or package a runtime launch helper.'
Assert-True ($workflow -match 'SafeDiscLoader2-Pr1Runtime' -and $workflow -match [regex]::Escape((Split-Path $runtimePatchPath -Leaf))) 'CI must build the pinned PR1 app-local DLL without an injector.'
Assert-True ($lock.qualifiedSourceBuild.reproducibility -match 'byte-identical') 'The lock must state the result of the reproducibility comparison.'
Assert-True ($lock.qualifiedSourceBuild.upstreamSourceCommit -eq $lock.commit) 'The qualified build must identify the exact pinned upstream commit.'
Assert-True ($lock.qualifiedSourceBuild.versionDllSha256 -match '^[0-9a-f]{64}$') 'The qualified loader hash must be recorded.'
Assert-True ($lock.qualifiedSourceBuild.localPatchSha256 -eq $patchHash) 'The qualified local patch hash must be recorded.'
Assert-True ($lock.qualifiedSourceBuild.correspondingSourceArchiveSha256 -match '^[0-9a-f]{64}$') 'The corresponding GPL source archive hash must be recorded.'
Assert-True ($blackKnightBuilder -notmatch [regex]::Escape($lock.qualifiedSourceBuild.versionDllSha256)) 'The superseded retail loader must remain outside the Black Knight production install path.'
Assert-True ($blackKnightBuilder -notmatch 'QualifiedLaunchHelperSha256') 'Black Knight installation must not package the process-injection helper.'
Assert-True ($bundleScript -match [regex]::Escape($lock.qualifiedSourceBuild.versionDllSha256)) 'Bundle assembly must require the qualified loader hash.'
Assert-True ($bundleScript -match [regex]::Escape($patchHash)) 'Bundle assembly must require the exact local patch.'
Assert-True ($bundleScript -match [regex]::Escape($lock.licenseSha256)) 'Bundle assembly must require the qualified license hash.'
Assert-True ($bundleScript -match [regex]::Escape($lock.qualifiedSourceBuild.correspondingSourceArchiveSha256)) 'Bundle assembly must require the corresponding-source archive hash.'
Assert-True ($presentationLock.commit -eq '73ac0f47af16a1d28dcda25c3228053beb3eb5f4' -and $presentationLock.license -eq '0BSD') 'Presentation compatibility must pin the public MW3 fork revision and permissive license.'
Assert-True ($presentationBuild -match [regex]::Escape($presentationLock.commit) -and $presentationBuild -match [regex]::Escape($presentationLock.dllSha256)) 'Presentation build must enforce the pinned source tag and qualified DLL hash.'
Assert-True ($presentationBuild -match [regex]::Escape($presentationLock.mw4PatchSha256) -and $presentationBuild -match 'Clear-PeTimestamps' -and $presentationBuild -match '/Brepro') 'Presentation build must apply the exact patch and normalize compiler/linker variability.'
Assert-True ($presentationBuild -match 'git .* archive --format=zip') 'Presentation build must emit exact corresponding source.'

Write-Host 'Compatibility source-build contract tests passed.'
