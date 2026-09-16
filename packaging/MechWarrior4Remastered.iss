#ifndef PayloadRoot
  #error PayloadRoot must identify the validated release payload directory.
#endif
#ifndef PackageOutput
  #error PackageOutput must identify the package output directory.
#endif
#ifndef AppVersion
  #define AppVersion "0.1.0"
#endif
#ifndef ProjectRoot
  #error ProjectRoot must identify the repository root.
#endif

[Setup]
AppId={{C6EC7C9E-86BC-42F8-BE58-875E7BAE1A16}
AppName=MechWarrior 4 Remastered
AppVersion={#AppVersion}
AppPublisher=MechWarrior 4 Remastered Project
DefaultDirName={localappdata}\Programs\MechWarrior 4 Remastered
DefaultGroupName=MechWarrior 4 Remastered
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
Uninstallable=yes
UninstallDisplayIcon={app}\MW4RemasteredLauncher.exe
SetupIconFile={#ProjectRoot}\assets\branding\MW4-Remastered.ico
OutputDir={#PackageOutput}
OutputBaseFilename=MechWarrior-4-Remastered-Setup-{#AppVersion}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes
RestartApplications=no
ArchitecturesAllowed=x64compatible
VersionInfoVersion={#AppVersion}

[Files]
Source: "{#PayloadRoot}\MW4RemasteredInstaller.exe"; DestDir: "{app}"; Flags: ignoreversion notimestamp
Source: "{#PayloadRoot}\MW4RemasteredLauncher.exe"; DestDir: "{app}"; Flags: ignoreversion notimestamp
Source: "{#PayloadRoot}\THIRD-PARTY-NOTICES.md"; DestDir: "{app}"; Flags: ignoreversion notimestamp
Source: "{#PayloadRoot}\Compatibility\BlackKnight\*"; DestDir: "{app}\Compatibility\BlackKnight"; Flags: ignoreversion notimestamp recursesubdirs createallsubdirs

[Icons]
Name: "{group}\MechWarrior 4 Remastered"; Filename: "{app}\MW4RemasteredLauncher.exe"; WorkingDir: "{app}"
Name: "{group}\Install games from original media"; Filename: "{app}\MW4RemasteredInstaller.exe"; WorkingDir: "{app}"
Name: "{group}\Uninstall MechWarrior 4 Remastered"; Filename: "{uninstallexe}"
Name: "{autodesktop}\MechWarrior 4 Remastered"; Filename: "{app}\MW4RemasteredLauncher.exe"; WorkingDir: "{app}"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Run]
Filename: "{app}\MW4RemasteredInstaller.exe"; Description: "Select original MechWarrior 4 media now"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; Intentionally empty. Inno removes only files it installed. Media-derived game trees,
; saves, and configuration are managed by the launcher's ownership-safe per-game action.
