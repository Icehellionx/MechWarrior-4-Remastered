$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$launcher = Get-Content -LiteralPath (Join-Path $root 'src/MW4Remastered.Launcher/MainForm.cs') -Raw
$installer = Get-Content -LiteralPath (Join-Path $root 'src/MW4Remastered.Installer/InstallerForm.cs') -Raw
$installerProgram = Get-Content -LiteralPath (Join-Path $root 'src/MW4Remastered.Installer/Program.cs') -Raw
$setup = Get-Content -LiteralPath (Join-Path $root 'packaging/MechWarrior4Remastered.iss') -Raw

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
Assert-True ($installer -match 'ALL THREE GAMES INSTALL DIRECTLY' -and $installer -match 'VengeanceInstallRequest' -and $installer -match 'MercenariesInstallRequest') 'Installer must expose all three qualified media installation paths.'
Assert-True ($installer -match 'INSTALL SELECTED GAMES' -and $installer -match 'installationCoordinator\.Install') 'Validated supported media must enable the actual shared installation coordinator.'
Assert-True ($installer -match 'Installs with Vengeance after exact Patch 3 validation' -and $installer -match 'inner-sphere-mech-pak' -and $installer -match 'clan-mech-pak') 'Qualified Mech Paks must flow into the Vengeance install request with an explicit Patch 3 gate.'
Assert-True ($installer -notmatch 'INSTALL BLACK KNIGHT NOW') 'The product installer must not expose the expansion as a standalone primary install path.'
Assert-True ($installer -notmatch 'blackKnightInstaller|InstallBlackKnightAsync|CreateBlackKnightSelection') 'The product installer must not retain a hidden Black Knight-only execution path.'
Assert-True ($installer -match 'DONE — OPEN LAUNCHER' -and $installer -match 'OpenInstalledLauncher') 'An existing test install may still hand off explicitly to the launcher.'
Assert-True ($installer -notmatch 'BLACK KNIGHT ALREADY INSTALLED') 'Installer must not strand an existing install behind a disabled status-only button.'
Assert-True ($installer -notmatch 'INSTALLATION LOCKED') 'Installer must explain the next action instead of showing an unexplained locked state.'
Assert-True ($installer -match 'InstalledLauncherOrchestrator' -and $installer -match 'installedLauncher\.Start') 'Successful game installation must hand off to the simple launcher.'
Assert-True ($setup -match 'Choose original game media' -and $setup -match 'GetOpenFileNameMulti') 'The package must ask for media before its installation action.'
Assert-True ($setup -notmatch 'Black Knight ISO or ZIP \(required\)') 'The package media page must not privilege or require Black Knight.'
Assert-True ($installerProgram -match 'ParseMediaPaths' -and $installer -match 'InspectInitialMediaAsync') 'Wizard-selected media must enter validation without a second picker.'

Write-Host 'User flow contract tests passed.'
