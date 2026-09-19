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
Assert-True ($inno -match "ShellExec\('runas',\s*ExpandConstant\('\{app\}\\MW4RemasteredInstallWorker\.exe'\)" -and $inno -notmatch "(?s)Exec\(ExpandConstant\('\{app\}\\MW4RemasteredInstallWorker\.exe'\)") 'Setup must explicitly request an administrative token for its hidden worker even when the outer executable was started from a non-elevated download shell.'
Assert-True ($inno -match 'Flags:\s*postinstall nowait skipifsilent runasoriginaluser') 'Post-install launcher must return to the original non-elevated user token.'
Assert-True ($inno -match 'DefaultDirName=\{localappdata\}\\Programs\\MechWarrior 4 Remastered') 'Package root must remain in the current user profile.'
Assert-True ($inno -match 'Uninstallable=yes') 'Package must retain the standard Inno uninstaller.'
Assert-True ($inno -match 'Filename:\s*"\{uninstallexe\}"') 'Package must expose its standard uninstaller in the Start Menu.'
Assert-True ($inno -match '(?m)^Name:\s*"desktopicon";.*Create a &desktop shortcut.*$' -and $inno -notmatch '(?m)^Name:\s*"desktopicon";.*Flags:\s*unchecked') 'The desktop shortcut task must be selected by default.'
Assert-True ($innoWithoutExactDirectoryCleanup -notmatch '(?im)^\s*Type:\s*(filesandordirs|dirifempty)' -and $inno -notmatch '(?im)^\s*Type:\s*files;.*[*?]') 'Package cleanup must not broadly delete media-derived game or user directories.'
Assert-True (($inno | Select-String -Pattern 'Type:\s*files;\s*Name:\s*"\{group\}\\Install games from original media\.lnk"' -AllMatches).Matches.Count -eq 2) 'Install and uninstall must remove the one obsolete second-installer shortcut by exact path.'
Assert-True ($inno -match 'Installing and verifying selected MechWarrior 4 games' -and $inno -match 'ewWaitUntilTerminated') 'Interactive setup must run original-media installation as a synchronous setup stage.'
Assert-True ($inno -notmatch '(?im)^Filename:.*MW4RemasteredInstallWorker\.exe') 'The internal worker must never be exposed as a second Run-section installer.'
Assert-True ($inno -match 'GetOpenFileNameMulti' -and $inno -match 'CreateCustomPage\(wpWelcome') 'Setup must collect any supported media through a neutral multi-file page before installation.'
Assert-True ($inno -match 'CreateInputOptionPage\(MediaPage\.ID' -and $inno -match 'I accept the original Microsoft license terms') 'Setup must obtain original-game license acceptance before recording first-run completion.'
Assert-True ($inno -match '\{param:MEDIAFILES\|\}' -and $inno -match 'AddCommandLineMedia') 'The same media-first flow must support a bounded unattended package smoke without adding a second UI.'
Assert-True ($inno -match '\{param:ACCEPTLICENSE\|0\}' -and $inno -match '/ACCEPTLICENSE=1') 'Unattended setup must require explicit original-game license acceptance.'
Assert-True ($inno -match '\{app\}\\Logs\\InstallWorker\.log' -and $inno -notmatch '\{tmp\}\\MW4RemasteredInstallWorker\.log') 'Worker failures must retain their diagnostic log outside Inno temporary cleanup.'
Assert-True ($inno -match 'Type:\s*files;\s*Name:\s*"\{app\}\\Logs\\InstallWorker\.log"' -and $inno -match 'Type:\s*dirifempty;\s*Name:\s*"\{app\}\\Logs"') 'The standard uninstaller must remove only the exact project-owned worker log and its empty directory.'
Assert-True ($inno -match 'GetMediaParameters' -and $inno -match '--install-worker' -and $inno -match '--media') 'Setup must forward every selected media path to its contained worker.'
Assert-True ($inno -match [regex]::Escape('{localappdata}\MechWarrior 4 Remastered\Logs\InstallWorker.log') -and $inno -match 'CopyFile\(WorkerLog, RetainedWorkerLog, False\)') 'Setup failure must preserve its diagnostic log outside the rollback-owned application directory.'
Assert-True (($inno | Select-String -Pattern 'Source:' -AllMatches).Matches.Count -eq ($inno | Select-String -Pattern 'notimestamp' -AllMatches).Matches.Count) 'Every packaged source must omit source timestamps for reproducibility.'
Assert-True ($build -match '--self-contained true' -and $build -match 'PublishSingleFile=true') 'Installer and launcher publishes must remain self-contained single files.'
Assert-True ($build -match 'assert-release-tree\.ps1') 'Staged package payload must pass the release-tree allowlist gate.'
Assert-True ($build -match '\.sha256') 'Package build must emit a SHA-256 sidecar.'
Assert-True ($build -notmatch 'assemble-black-knight-bundle\.ps1' -and $inno -notmatch 'Source:.*Compatibility\\BlackKnight\\') 'Package staging must exclude the superseded retail Black Knight loader experiment.'
Assert-True ($inno -match 'InstallDelete' -and $inno -match 'Compatibility\\BlackKnight\\version\.dll' -and $inno -notmatch 'Type:\s*filesandordirs;\s*Name:\s*"\{app\}\\Compatibility') 'Package upgrade must remove only the exact old Black Knight proxy artifacts, never a broad compatibility tree.'
Assert-True ($build -notmatch "Compatibility/BlackKnight/MW4RemasteredCompatLauncher\.exe") 'Package staging must not allow the runtime process-injection helper.'
Assert-True ($build -match 'MW4Remastered\.RtpPatchHost' -and $inno -match 'MW4RemasteredRtpPatchHost\.exe') 'Package must include the project-owned non-elevating Patch 3 host.'
Assert-True ($build -match 'MercenariesPr1Archive' -and $build -match '0c3d0094448e6fe5d2a30fb9ebb24001e8e8c03b39aff9232856bd30000efb30' -and $inno -match 'Updates\\MercenariesPR1') 'Package must embed the exact qualified official Mercenaries PR1 payload so original ISOs are sufficient.'
Assert-True (($inno | Select-String -Pattern 'SetForegroundWindow\(WizardForm\.Handle\)' -AllMatches).Matches.Count -ge 2 -and $inno -match 'procedure CurPageChanged') 'Setup must explicitly foreground its wizard during initialization and again after its first page becomes visible.'
Assert-True ($inno -match 'WizardForm\.FormStyle\s*:=\s*fsStayOnTop') 'Setup must remain above ordinary windows because Windows can reject foreground activation after elevation.'
Assert-True ($inno -match 'Multiplayer network access' -and $inno -match 'ALLOWPRIVATEFIREWALL' -and $inno -match 'not WizardSilent') 'Interactive setup must expose the private-network firewall choice, while unattended setup requires an explicit opt-in.'
Assert-True (($inno | Select-String -Pattern '(?s)action=allow.*?profile=private edge=no protocol=any' -AllMatches).Matches.Count -eq 1 -and ($inno | Select-String -Pattern '(?s)action=block.*?profile=public edge=no protocol=any' -AllMatches).Matches.Count -eq 1 -and $inno -notmatch 'profile=(any|all)') 'Setup must allow exact game executables on private networks and explicitly block them on public networks without edge traversal.'
Assert-True ($inno -match '\{app\}\\vengeance\\MW4\.exe' -and $inno -match '\{app\}\\vengeance\\MW4X\\MW4X\.exe' -and $inno -match '\{app\}\\mercenaries\\MW4Mercs\.exe') 'Firewall preparation must target only the three exact installed game executables.'
Assert-True (($inno | Select-String -Pattern '\[UninstallRun\]' -AllMatches).Matches.Count -eq 1 -and ($inno | Select-String -Pattern 'firewall delete rule name=' -AllMatches).Matches.Count -ge 4) 'Reinstall and uninstall must remove only the project-named firewall rules before replacement or final cleanup.'
Assert-True (($inno | Select-String -Pattern 'RemoveFirewallRules\(' -AllMatches).Matches.Count -eq 5 -and $inno -match '(?s)RemoveFirewallRules\(''MechWarrior 4 Remastered - Mercenaries''\);\s*if FirewallPage\.Values\[0\]') 'Setup must remove stale project rules even when a reinstall opts out of multiplayer firewall access, and roll back a partial rule pair.'
Assert-True ($build -match 'Pr1Capture' -and $inno -match 'BlackKnightPr1Capture\.dll') 'Package must include the exact source-built setup-only Black Knight capture DLL.'
Assert-True ($build -match 'SafeDiscLoader2-source-f27286a363aa675a0422141cb96fc8619cf8b9d8\.zip' -and $inno -match 'Compatibility\\BlackKnightPr1Capture') 'Package must accompany the GPL capture DLL with exact corresponding source and notices.'
Assert-True ($build -match 'DgVoodooArchive' -and $build -match '5ffde6927f7355ca3fdd5d785b581256a8e6539fa13e395a891ade6ba1040850') 'Package must exact-hash the complete evaluated dgVoodoo2 archive.'
Assert-True ($build -match '612a24408a090a3c6f3886557fa18034ee742e94ad0a40ebdf854d2816176c2e' -and $build -match '6a0ca214784be04b7c8b547105aa9d79acf4dc26c0b6f8702b437ddca54058b2') 'Package must exact-hash the stock x86 DirectX wrapper files.'
Assert-True ($build -match 'dgVoodoo-MW4\.conf' -and $inno -match 'Compatibility\\dgVoodoo2') 'Package must install the project-owned MW4 dgVoodoo profile and exact wrapper files.'
Assert-True ($build -match 'fd9413ae24ef4028c19b304b48f7062d53871f25942baaa17d554bfeb1261b93') 'Package staging must exact-hash the reviewed D3D12 MW4 dgVoodoo profile.'
Assert-True ($build -match 'DgVoodooSourceRoot' -and $build -match 'build-dgvoodoo-mw4-addon\.ps1' -and $build -match '33e7fae1c1cb2d297c05c14b5d4f886676fcd492c44eab0602d8e4465bc71ef0') 'Package staging must build and exact-hash the reproducible MW4 presentation add-on from pinned source.'
Assert-True ($build -match 'BlackKnightCaptureBundle' -and $build -match [regex]::Escape('7fbf1fbd0b251986f0dcd2082f218650eddff40d661ede0deefd4b2ce6b5c6fc')) 'A hosted Black Knight capture bundle must be exact-hash validated before packaging.'
Assert-True ($build -notmatch 'BlackKnightCaptureHost|MW4RemasteredBlackKnightCaptureHost') 'Package build must not compile or stage the retired project process-injection helper.'
Assert-True ($build -match 'manuals\.lock\.json' -and $build -match 'output/pdf' -and $inno -match 'Manuals\\\*\.pdf') 'Package must exact-hash and install all three cleaned manuals.'
Assert-True ($build -match 'output/manual-covers' -and $build -match 'coverSha256' -and $inno -match 'Manuals\\\*\.cover\.png') 'Package must exact-hash and install all three rendered manual covers.'
Assert-True ($build -notmatch 'ExecutionPolicy\s+Bypass') 'Package build must not bypass PowerShell execution policy.'
$launch = Get-Content -LiteralPath (Join-Path $root 'src/MW4Remastered.Core/Launch/LaunchOrchestrator.cs') -Raw
Assert-True ($launch -match '/SILENT' -and $launch -notmatch '/VERYSILENT' -and $launch -match '/NORESTART') 'The already-confirmed unified uninstall must skip a second prompt while retaining visible shell-removal progress.'

Write-Host 'Packaging contract tests passed.'
