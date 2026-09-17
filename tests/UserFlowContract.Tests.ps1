$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$launcher = Get-Content -LiteralPath (Join-Path $root 'src/MW4Remastered.Launcher/MainForm.cs') -Raw
$worker = Get-Content -LiteralPath (Join-Path $root 'src/MW4Remastered.Installer/InstallWorker.cs') -Raw
$installerProgram = Get-Content -LiteralPath (Join-Path $root 'src/MW4Remastered.Installer/Program.cs') -Raw
$setup = Get-Content -LiteralPath (Join-Path $root 'packaging/MechWarrior4Remastered.iss') -Raw
$launch = Get-Content -LiteralPath (Join-Path $root 'src/MW4Remastered.Core/Launch/LaunchOrchestrator.cs') -Raw

function Assert-True {
    param([Parameter(Mandatory)][bool]$Condition, [Parameter(Mandatory)][string]$Message)
    if (-not $Condition) { throw $Message }
}

Assert-True ($launcher -match 'operationGrid\.ColumnCount\s*=\s*3' -and $launcher -match 'operationGrid\.RowCount\s*=\s*2') 'Launcher must retain the three-game by game/manual operation grid.'
Assert-True ($launcher -match 'CreateGameButton' -and $launcher -match 'CreateManualButton') 'Launcher must expose separate game and manual operations.'
Assert-True ($launcher -match 'Mech4\.ico' -and $launcher -match 'Mech4X\.ico' -and $launcher -match 'Mech4Merc\.ico' -and $launcher -match 'TextImageRelation\.ImageBeforeText') 'Each installed game tile must show its own media icon rather than a text-only title mark.'
Assert-True ($launcher -match 'LoadManualCover' -and $launcher -match '\.cover\.png' -and $launcher -match 'CreateManualButton') 'Manual tiles must load the packaged first-page cover art below their games.'
Assert-True ($launcher -match 'MECH PAKS' -and $launcher -match 'CreatePackIndicator') 'Launcher must keep compact pack indicators below primary operations.'
Assert-True ($launcher -match 'Text\s*=\s*"UNINSTALL"') 'Launcher must retain one clear lower-corner uninstall action.'
Assert-True ($launcher -match 'REMOVING LAUNCHER AND SHORTCUTS' -and $launcher -match 'Application\.Exit\(\)') 'Unified uninstall must visibly hand off shell removal and terminate the launcher.'
Assert-True ($launcher -notmatch 'DIAGNOSTICS|SETTINGS|REMOVE GAME FILES') 'Launcher primary surface must not expose unfinished or maintenance-heavy actions.'
Assert-True ($launcher -match 'CHECKING INSTALLED GAMES' -and $launcher -match 'Task\.Run\(statusReader\.Read\)') 'Launcher must become visible before hashing installed game trees.'

Assert-True ($setup -match 'Choose original game media' -and $setup -match 'GetOpenFileNameMulti') 'The package must ask for media before its installation action.'
Assert-True ($setup -notmatch 'Black Knight ISO or ZIP \(required\)') 'The package media page must not privilege or require Black Knight.'
Assert-True ($setup -match '(?s)procedure InitializeWizard;.*?LicensePage := CreateInputOptionPage.*?(?=procedure DeinitializeSetup)' -and $setup -notmatch '(?s)procedure AddMediaButtonClick.*?LicensePage := CreateInputOptionPage.*?(?=procedure RemoveMediaButtonClick)') 'The license page must exist during wizard initialization, before navigation callbacks can inspect it.'
Assert-True ($worker -match 'VengeanceInstallRequest' -and $worker -match 'EffectiveComponents\.Contains\("black-knight"' -and $worker -match 'MercenariesInstallRequest') 'The contained worker must install Black Knight atomically inside the Vengeance family while retaining independent Mercenaries.'
Assert-True ($worker -match 'inner-sphere-mech-pak' -and $worker -match 'clan-mech-pak' -and $worker -match 'GameInstallationCoordinator') 'Qualified Mech Paks must flow into the shared coordinator with their Vengeance gate.'
Assert-True ($worker -match 'mercenaries-pr1' -and $worker -match 'Point Release 1') 'Mercenaries setup must require and route the recognized official PR1 payload before commit.'
Assert-True ($worker -match 'RollBack' -and $worker -match 'OwnedInstallUninstaller') 'A failed unified setup run must roll back games newly committed by that run.'
Assert-True ($worker -match 'LegacyGameRegistration' -and $worker -match 'registration\.Ensure') 'Setup must own legacy game registration before the launcher is offered.'
Assert-True ($launch -match 'ValidateOwned' -and $launch -notmatch 'gameRegistration\.Ensure') 'Normal game launch must validate setup state without performing installation writes.'
Assert-True ($installerProgram -notmatch 'Application\.Run|InstallerForm' -and $installerProgram -match 'InstallWorkerArguments') 'The package must not launch a second visible installer UI.'
Assert-True ($setup -match 'CurStepChanged' -and $setup -match 'SW_HIDE' -and $setup -match 'ewWaitUntilTerminated') 'The sole visible setup wizard must invoke its contained worker synchronously and hidden.'
Assert-True ($setup -match '--install-worker' -and $setup -match '--destination' -and $setup -match '--media') 'The sole setup wizard must forward its destination and every selected media path to the contained worker.'
Assert-True ($setup -match 'postinstall nowait skipifsilent' -and $setup -match 'MW4RemasteredLauncher\.exe') 'Successful unified setup may offer to open only the launcher.'

Write-Host 'User flow contract tests passed.'
