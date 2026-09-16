$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$launcher = Get-Content -LiteralPath (Join-Path $root 'src/MW4Remastered.Launcher/MainForm.cs') -Raw
$installer = Get-Content -LiteralPath (Join-Path $root 'src/MW4Remastered.Installer/InstallerForm.cs') -Raw

function Assert-True {
    param([Parameter(Mandatory)][bool]$Condition, [Parameter(Mandatory)][string]$Message)
    if (-not $Condition) { throw $Message }
}

Assert-True ($launcher -match 'operationGrid\.ColumnCount\s*=\s*3' -and $launcher -match 'operationGrid\.RowCount\s*=\s*2') 'Launcher must retain the three-game by game/manual operation grid.'
Assert-True ($launcher -match 'CreateGameButton' -and $launcher -match 'CreateManualButton') 'Launcher must expose separate game and manual operations.'
Assert-True ($launcher -match 'MECH PAKS' -and $launcher -match 'CreatePackIndicator') 'Launcher must keep compact pack indicators below primary operations.'
Assert-True ($launcher -match 'Text\s*=\s*"UNINSTALL"') 'Launcher must retain one clear lower-corner uninstall action.'
Assert-True ($launcher -notmatch 'DIAGNOSTICS|SETTINGS|REMOVE GAME FILES') 'Launcher primary surface must not expose unfinished or maintenance-heavy actions.'
Assert-True ($launcher -match 'CHECKING INSTALLED GAMES' -and $launcher -match 'Task\.Run\(statusReader\.Read\)') 'Launcher must become visible before hashing installed game trees.'

Assert-True ($installer -match 'STEP 1 OF 2' -and $installer -match '1\. CHOOSE ISO / ZIP FILES') 'Installer must lead with an explicit media-selection step.'
Assert-True ($installer -match '2\. INSTALL BLACK KNIGHT NOW') 'Installer must provide an explicit primary install action when qualified media is ready.'
Assert-True ($installer -notmatch 'INSTALLATION LOCKED') 'Installer must explain the next action instead of showing an unexplained locked state.'
Assert-True ($installer -match 'InstalledLauncherOrchestrator' -and $installer -match 'installedLauncher\.Start') 'Successful game installation must hand off to the simple launcher.'

Write-Host 'User flow contract tests passed.'
