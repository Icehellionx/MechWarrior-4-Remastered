$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$inno = Get-Content -LiteralPath (Join-Path $root 'packaging/MechWarrior4Remastered.iss') -Raw
$build = Get-Content -LiteralPath (Join-Path $root 'tools/package/build-release.ps1') -Raw
$innoWithoutExactDirectoryCleanup = $inno `
    -replace '(?im)^\s*Type:\s*dirifempty;\s*Name:\s*"\{app\}\\Logs"\s*$', '' `
    -replace '(?im)^\s*Type:\s*dirifempty;\s*Name:\s*"\{app\}\\Compatibility(\\BlackKnight)?"\s*$', ''

function Assert-True {
    param([Parameter(Mandatory)][bool]$Condition, [Parameter(Mandatory)][string]$Message)
    if (-not $Condition) { throw $Message }
}

Assert-True ($inno -match 'PrivilegesRequired=admin') 'Package setup must own the single elevation boundary required for ISO mounting.'
Assert-True ($inno -match 'Flags:\s*postinstall nowait skipifsilent runasoriginaluser') 'Post-install launcher must return to the original non-elevated user token.'
Assert-True ($inno -match 'DefaultDirName=\{localappdata\}\\Programs\\MechWarrior 4 Remastered') 'Package root must remain in the current user profile.'
Assert-True ($inno -match 'Uninstallable=yes') 'Package must retain the standard Inno uninstaller.'
Assert-True ($inno -match 'Filename:\s*"\{uninstallexe\}"') 'Package must expose its standard uninstaller in the Start Menu.'
Assert-True ($innoWithoutExactDirectoryCleanup -notmatch '(?im)^\s*Type:\s*(filesandordirs|dirifempty)' -and $inno -notmatch '(?im)^\s*Type:\s*files;.*[*?]') 'Package cleanup must not broadly delete media-derived game or user directories.'
Assert-True (($inno | Select-String -Pattern 'Type:\s*files;\s*Name:\s*"\{group\}\\Install games from original media\.lnk"' -AllMatches).Matches.Count -eq 2) 'Install and uninstall must remove the one obsolete second-installer shortcut by exact path.'
Assert-True ($inno -match 'Installing and verifying selected MechWarrior 4 games' -and $inno -match 'ewWaitUntilTerminated') 'Interactive setup must run original-media installation as a synchronous setup stage.'
Assert-True ($inno -notmatch '(?im)^Filename:.*MW4RemasteredInstallWorker\.exe') 'The internal worker must never be exposed as a second Run-section installer.'
Assert-True ($inno -match 'GetOpenFileNameMulti' -and $inno -match 'CreateCustomPage\(wpWelcome') 'Setup must collect any supported media through a neutral multi-file page before installation.'
Assert-True ($inno -match 'CreateInputOptionPage\(MediaPage\.ID' -and $inno -match 'I accept the original Microsoft license terms') 'Setup must obtain original-game license acceptance before recording first-run completion.'
Assert-True ($inno -match '\{param:MEDIAFILES\|\}' -and $inno -match 'AddCommandLineMedia') 'The same media-first flow must support a bounded unattended package smoke without adding a second UI.'
Assert-True ($inno -match '\{app\}\\Logs\\InstallWorker\.log' -and $inno -notmatch '\{tmp\}\\MW4RemasteredInstallWorker\.log') 'Worker failures must retain their diagnostic log outside Inno temporary cleanup.'
Assert-True ($inno -match 'Type:\s*files;\s*Name:\s*"\{app\}\\Logs\\InstallWorker\.log"' -and $inno -match 'Type:\s*dirifempty;\s*Name:\s*"\{app\}\\Logs"') 'The standard uninstaller must remove only the exact project-owned worker log and its empty directory.'
Assert-True ($inno -match 'GetMediaParameters' -and $inno -match '--install-worker' -and $inno -match '--media') 'Setup must forward every selected media path to its contained worker.'
Assert-True (($inno | Select-String -Pattern 'Source:' -AllMatches).Matches.Count -eq ($inno | Select-String -Pattern 'notimestamp' -AllMatches).Matches.Count) 'Every packaged source must omit source timestamps for reproducibility.'
Assert-True ($build -match '--self-contained true' -and $build -match 'PublishSingleFile=true') 'Installer and launcher publishes must remain self-contained single files.'
Assert-True ($build -match 'assert-release-tree\.ps1') 'Staged package payload must pass the release-tree allowlist gate.'
Assert-True ($build -match '\.sha256') 'Package build must emit a SHA-256 sidecar.'
Assert-True ($build -notmatch 'assemble-black-knight-bundle\.ps1' -and $inno -notmatch 'Source:.*Compatibility\\BlackKnight\\') 'Package staging must exclude the superseded retail Black Knight loader experiment.'
Assert-True ($inno -match 'InstallDelete' -and $inno -match 'Compatibility\\BlackKnight\\version\.dll' -and $inno -notmatch 'Type:\s*filesandordirs;\s*Name:\s*"\{app\}\\Compatibility') 'Package upgrade must remove only the exact old Black Knight proxy artifacts, never a broad compatibility tree.'
Assert-True ($build -notmatch "Compatibility/BlackKnight/MW4RemasteredCompatLauncher\.exe") 'Package staging must not allow the runtime process-injection helper.'
Assert-True ($build -match 'MW4Remastered\.RtpPatchHost' -and $inno -match 'MW4RemasteredRtpPatchHost\.exe') 'Package must include the project-owned non-elevating Patch 3 host.'
Assert-True ($build -match 'MW4Remastered\.BlackKnightCaptureHost' -and $inno -match 'MW4RemasteredBlackKnightCaptureHost\.exe' -and $inno -match 'BlackKnightPr1Capture\.dll') 'Package must include the setup-only x86 Black Knight PR1 capture boundary.'
Assert-True ($build -match 'SafeDiscLoader2-source-f27286a363aa675a0422141cb96fc8619cf8b9d8\.zip' -and $inno -match 'Compatibility\\BlackKnightPr1Capture') 'Package must accompany the GPL capture DLL with exact corresponding source and notices.'
Assert-True ($build -match 'manuals\.lock\.json' -and $build -match 'output/pdf' -and $inno -match 'Manuals\\\*\.pdf') 'Package must exact-hash and install all three cleaned manuals.'
Assert-True ($build -match 'output/manual-covers' -and $build -match 'coverSha256' -and $inno -match 'Manuals\\\*\.cover\.png') 'Package must exact-hash and install all three rendered manual covers.'
Assert-True ($build -notmatch 'ExecutionPolicy\s+Bypass') 'Package build must not bypass PowerShell execution policy.'
$launch = Get-Content -LiteralPath (Join-Path $root 'src/MW4Remastered.Core/Launch/LaunchOrchestrator.cs') -Raw
Assert-True ($launch -match '/SILENT' -and $launch -notmatch '/VERYSILENT' -and $launch -match '/NORESTART') 'The already-confirmed unified uninstall must skip a second prompt while retaining visible shell-removal progress.'

Write-Host 'Packaging contract tests passed.'
