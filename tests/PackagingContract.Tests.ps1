$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$inno = Get-Content -LiteralPath (Join-Path $root 'packaging/MechWarrior4Remastered.iss') -Raw
$build = Get-Content -LiteralPath (Join-Path $root 'tools/package/build-release.ps1') -Raw

function Assert-True {
    param([Parameter(Mandatory)][bool]$Condition, [Parameter(Mandatory)][string]$Message)
    if (-not $Condition) { throw $Message }
}

Assert-True ($inno -match 'PrivilegesRequired=lowest') 'Package setup must remain non-elevating and per-user.'
Assert-True ($inno -match 'DefaultDirName=\{localappdata\}\\Programs\\MechWarrior 4 Remastered') 'Package root must remain in the current user profile.'
Assert-True ($inno -match 'Uninstallable=yes') 'Package must retain the standard Inno uninstaller.'
Assert-True ($inno -match 'Filename:\s*"\{uninstallexe\}"') 'Package must expose its standard uninstaller in the Start Menu.'
Assert-True ($inno -notmatch '(?im)^\s*Type:\s*(files|filesandordirs|dirifempty)') 'Package uninstall must not broadly delete media-derived game or user directories.'
Assert-True ($inno -match 'postinstall' -and $inno -match 'skipifsilent') 'Package should offer original-media intake after interactive setup only.'
Assert-True (($inno | Select-String -Pattern 'Source:' -AllMatches).Matches.Count -eq ($inno | Select-String -Pattern 'notimestamp' -AllMatches).Matches.Count) 'Every packaged source must omit source timestamps for reproducibility.'
Assert-True ($build -match '--self-contained true' -and $build -match 'PublishSingleFile=true') 'Installer and launcher publishes must remain self-contained single files.'
Assert-True ($build -match 'assert-release-tree\.ps1') 'Staged package payload must pass the release-tree allowlist gate.'
Assert-True ($build -match '\.sha256') 'Package build must emit a SHA-256 sidecar.'
Assert-True ($build -match 'assemble-black-knight-bundle\.ps1') 'Package staging must consume the exact qualified Black Knight bundle.'
Assert-True ($build -notmatch 'ExecutionPolicy\s+Bypass') 'Package build must not bypass PowerShell execution policy.'

Write-Host 'Packaging contract tests passed.'
