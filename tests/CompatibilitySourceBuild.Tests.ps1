$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$lockPath = Join-Path $root 'third_party/SafeDiscLoader2.lock.json'
$scriptPath = Join-Path $root 'tools/compatibility/build-safedisc-loader2.ps1'
$helperScriptPath = Join-Path $root 'tools/compatibility/publish-launch-helper.ps1'
$workflowPath = Join-Path $root '.github/workflows/compatibility-build.yml'

$lock = Get-Content -LiteralPath $lockPath -Raw | ConvertFrom-Json
$script = Get-Content -LiteralPath $scriptPath -Raw
$helperScript = Get-Content -LiteralPath $helperScriptPath -Raw
$workflow = Get-Content -LiteralPath $workflowPath -Raw

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
Assert-True ($script -match 'git -C \$source archive --format=zip') 'Build must emit the exact corresponding GPL source archive.'
Assert-True ($helperScript -match '--runtime win-x86' -and $helperScript -match '--self-contained true') 'Launch helper publish must be self-contained x86.'
Assert-True ($helperScript -match 'PublishSingleFile=true' -and $helperScript -match 'PublishTrimmed=true') 'Launch helper publish must remain one trimmed file.'
Assert-True ($helperScript -match "executionLevel\.level -ne 'asInvoker'") 'Launch helper publish must verify its embedded non-elevating manifest.'
Assert-True ($workflow -match [regex]::Escape($lock.commit)) 'CI workflow must check out the pinned upstream commit.'
Assert-True ($workflow -notmatch 'VersionInjector') 'CI workflow must not build or package the elevated injector.'

Write-Host 'Compatibility source-build contract tests passed.'
