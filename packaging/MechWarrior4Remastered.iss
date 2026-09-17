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
; Setup owns the one deliberate UAC boundary required for ISO mounting and all
; compatibility/registration work. The installed launcher and games remain asInvoker.
PrivilegesRequired=admin
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
Source: "{#PayloadRoot}\MW4RemasteredInstallWorker.exe"; DestDir: "{app}"; Flags: ignoreversion notimestamp
Source: "{#PayloadRoot}\MW4RemasteredLauncher.exe"; DestDir: "{app}"; Flags: ignoreversion notimestamp
Source: "{#PayloadRoot}\MW4RemasteredRtpPatchHost.exe"; DestDir: "{app}"; Flags: ignoreversion notimestamp
Source: "{#PayloadRoot}\BlackKnightRuntime.dll"; DestDir: "{app}"; Flags: ignoreversion notimestamp
Source: "{#PayloadRoot}\Compatibility\BlackKnightRuntime\*"; DestDir: "{app}\Compatibility\BlackKnightRuntime"; Flags: ignoreversion notimestamp
Source: "{#PayloadRoot}\Compatibility\dgVoodoo2\*"; DestDir: "{app}\Compatibility\dgVoodoo2"; Flags: ignoreversion notimestamp
Source: "{#PayloadRoot}\THIRD-PARTY-NOTICES.md"; DestDir: "{app}"; Flags: ignoreversion notimestamp
Source: "{#PayloadRoot}\Manuals\*.pdf"; DestDir: "{app}\Manuals"; Flags: ignoreversion notimestamp
Source: "{#PayloadRoot}\Manuals\*.cover.png"; DestDir: "{app}\Manuals"; Flags: ignoreversion notimestamp
Source: "{#PayloadRoot}\Updates\MercenariesPR1\Patchw32.dat"; DestDir: "{app}\Updates\MercenariesPR1"; DestName: "Patchw32.dll"; Flags: ignoreversion notimestamp
Source: "{#PayloadRoot}\Updates\MercenariesPR1\English\MW4MERCS.RTP"; DestDir: "{app}\Updates\MercenariesPR1\English"; Flags: ignoreversion notimestamp

[Icons]
Name: "{group}\MechWarrior 4 Remastered"; Filename: "{app}\MW4RemasteredLauncher.exe"; WorkingDir: "{app}"
Name: "{group}\Uninstall MechWarrior 4 Remastered"; Filename: "{uninstallexe}"
Name: "{autodesktop}\MechWarrior 4 Remastered"; Filename: "{app}\MW4RemasteredLauncher.exe"; WorkingDir: "{app}"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Run]
Filename: "{app}\MW4RemasteredLauncher.exe"; Description: "Launch MechWarrior 4 Remastered"; WorkingDir: "{app}"; Flags: postinstall nowait skipifsilent runasoriginaluser

[InstallDelete]
; Remove the obsolete second-installer shortcut created by builds before 0.5.0.
Type: files; Name: "{group}\Install games from original media.lnk"
; Remove the exact superseded Black Knight retail-loader bundle from older package
; revisions. These are application-shell files, never user game data.
Type: files; Name: "{app}\Compatibility\BlackKnight\version.dll"
Type: files; Name: "{app}\Compatibility\BlackKnight\version.json"
Type: files; Name: "{app}\Compatibility\BlackKnight\SafeDiscLoader2-LICENSE.txt"
Type: files; Name: "{app}\Compatibility\BlackKnight\SafeDiscLoader2-source-f27286a363aa675a0422141cb96fc8619cf8b9d8.zip"
Type: files; Name: "{app}\Compatibility\BlackKnight\SafeDiscLoader2-MW4-BlackKnight.patch"
Type: dirifempty; Name: "{app}\Compatibility\BlackKnight"
; Remove the setup-only capture experiment shipped by internal pre-0.6 builds.
Type: files; Name: "{app}\MW4RemasteredBlackKnightCaptureHost.exe"
Type: files; Name: "{app}\BlackKnightPr1Capture.dll"
Type: files; Name: "{app}\Compatibility\BlackKnightPr1Capture\SafeDiscLoader2-LICENSE.txt"
Type: files; Name: "{app}\Compatibility\BlackKnightPr1Capture\SafeDiscLoader2-source-f27286a363aa675a0422141cb96fc8619cf8b9d8.zip"
Type: files; Name: "{app}\Compatibility\BlackKnightPr1Capture\SafeDiscLoader2-MW4-BlackKnight-PR1-Capture.patch"
Type: dirifempty; Name: "{app}\Compatibility"

[UninstallDelete]
; Exact upgrade cleanup only. Media-derived game trees, saves, and configuration are
; managed by the launcher's ownership-safe per-game action.
Type: files; Name: "{group}\Install games from original media.lnk"
; The contained worker creates this exact project-owned diagnostic file at runtime.
Type: files; Name: "{app}\Logs\InstallWorker.log"
Type: dirifempty; Name: "{app}\Logs"

[Code]
var
  MediaPage: TWizardPage;
  LicensePage: TInputOptionWizardPage;
  MediaList: TNewListBox;
  AddMediaButton: TNewButton;
  RemoveMediaButton: TNewButton;
  MediaFiles: TStringList;

function SetForegroundWindow(hWnd: HWND): Boolean;
  external 'SetForegroundWindow@user32.dll stdcall';

procedure RefreshMediaList;
var
  Index: Integer;
begin
  MediaList.Items.Clear;
  for Index := 0 to MediaFiles.Count - 1 do
    MediaList.Items.Add(MediaFiles[Index]);
  RemoveMediaButton.Enabled := MediaList.ItemIndex >= 0;
end;

procedure AddMediaButtonClick(Sender: TObject);
var
  SelectedFiles: TStringList;
  Index: Integer;
begin
  SelectedFiles := TStringList.Create;
  try
    if GetOpenFileNameMulti('Choose original MechWarrior 4 ISO or ZIP files', SelectedFiles, '',
      'Original media (*.iso;*.zip)|*.iso;*.zip|All files (*.*)|*.*', '') then
      for Index := 0 to SelectedFiles.Count - 1 do
        if MediaFiles.IndexOf(SelectedFiles[Index]) < 0 then
          MediaFiles.Add(SelectedFiles[Index]);
  finally
    SelectedFiles.Free;
  end;
  RefreshMediaList;
end;

procedure RemoveMediaButtonClick(Sender: TObject);
begin
  if MediaList.ItemIndex >= 0 then
    MediaFiles.Delete(MediaList.ItemIndex);
  RefreshMediaList;
end;

procedure MediaListClick(Sender: TObject);
begin
  RemoveMediaButton.Enabled := MediaList.ItemIndex >= 0;
end;

procedure AddCommandLineMedia(Value: String);
var
  Separator: Integer;
  Item: String;
begin
  while Value <> '' do
  begin
    Separator := Pos('|', Value);
    if Separator = 0 then
    begin
      Item := Value;
      Value := '';
    end
    else
    begin
      Item := Copy(Value, 1, Separator - 1);
      Delete(Value, 1, Separator);
    end;
    if (Item <> '') and (MediaFiles.IndexOf(Item) < 0) then
      MediaFiles.Add(Item);
  end;
end;

procedure InitializeWizard;
begin
  MediaFiles := TStringList.Create;
  MediaFiles.CaseSensitive := False;
  AddCommandLineMedia(ExpandConstant('{param:MEDIAFILES|}'));
  MediaPage := CreateCustomPage(wpWelcome, 'Choose original game media',
    'Add every MechWarrior 4 ISO or ISO-containing ZIP. For Mercenaries, also add the supported fix ZIP containing the official mercpr1.exe update.');
  LicensePage := CreateInputOptionPage(MediaPage.ID, 'Original game license',
    'Accept the license terms included with your selected original media',
    'Setup records this acceptance now so no game interrupts first launch with a legacy license dialog.',
    True, False);
  LicensePage.Add('I accept the original Microsoft license terms included with the media I selected.');
  LicensePage.Values[0] := ExpandConstant('{param:ACCEPTLICENSE|0}') = '1';

  MediaList := TNewListBox.Create(MediaPage);
  MediaList.Parent := MediaPage.Surface;
  MediaList.SetBounds(0, 0, MediaPage.SurfaceWidth, ScaleY(190));
  MediaList.OnClick := @MediaListClick;

  AddMediaButton := TNewButton.Create(MediaPage);
  AddMediaButton.Parent := MediaPage.Surface;
  AddMediaButton.SetBounds(0, ScaleY(202), ScaleX(150), ScaleY(30));
  AddMediaButton.Caption := 'Add ISO / ZIP files...';
  AddMediaButton.OnClick := @AddMediaButtonClick;

  RemoveMediaButton := TNewButton.Create(MediaPage);
  RemoveMediaButton.Parent := MediaPage.Surface;
  RemoveMediaButton.SetBounds(ScaleX(160), ScaleY(202), ScaleX(110), ScaleY(30));
  RemoveMediaButton.Caption := 'Remove selected';
  RemoveMediaButton.Enabled := False;
  RemoveMediaButton.OnClick := @RemoveMediaButtonClick;
  RefreshMediaList;
  { SetForegroundWindow alone is subject to Windows' foreground-lock policy.
    Setup is a short, explicitly invoked workflow, so keep its wizard above
    ordinary windows for the lifetime of this process. }
  WizardForm.FormStyle := fsStayOnTop;
  { Elevation can leave the newly created wizard behind the window that
    initiated Setup. Explicitly activate the real wizard once it exists. }
  WizardForm.BringToFront;
  SetForegroundWindow(WizardForm.Handle);
end;

procedure CurPageChanged(CurPageID: Integer);
begin
  { Repeat after the wizard becomes visible; InitializeWizard can run before
    Windows completes the post-UAC foreground transition. }
  if (CurPageID = wpWelcome) or (CurPageID = MediaPage.ID) then
  begin
    WizardForm.BringToFront;
    SetForegroundWindow(WizardForm.Handle);
  end;
end;

procedure DeinitializeSetup;
begin
  MediaFiles.Free;
end;

function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;
  if (CurPageID = MediaPage.ID) and (MediaFiles.Count = 0) then
  begin
    MsgBox('Add at least one original MechWarrior 4 ISO, ISO-containing ZIP, or supported official-update ZIP before continuing.', mbError, MB_OK);
    Result := False;
  end;
  if (CurPageID = LicensePage.ID) and (not LicensePage.Values[0]) then
  begin
    MsgBox('You must accept the original game license terms to install and launch the selected games.', mbError, MB_OK);
    Result := False;
  end;
end;

function GetMediaParameters(Param: String): String;
var
  Index: Integer;
begin
  Result := '--install-worker --destination "' + ExpandConstant('{app}') +
    '" --log "' + ExpandConstant('{app}\Logs\InstallWorker.log') + '"';
  for Index := 0 to MediaFiles.Count - 1 do
    Result := Result + ' --media "' + MediaFiles[Index] + '"';
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  ResultCode: Integer;
  WorkerLog: String;
begin
  if CurStep <> ssPostInstall then
    exit;

  if not LicensePage.Values[0] then
    RaiseException('Original game license acceptance is required. Interactive setup records it on the license page; unattended setup must pass /ACCEPTLICENSE=1.');

  WorkerLog := ExpandConstant('{app}\Logs\InstallWorker.log');
  WizardForm.StatusLabel.Caption := 'Installing and verifying selected MechWarrior 4 games...';
  if not Exec(ExpandConstant('{app}\MW4RemasteredInstallWorker.exe'), GetMediaParameters(''),
    ExpandConstant('{app}'), SW_HIDE, ewWaitUntilTerminated, ResultCode) then
    RaiseException('Setup could not start its contained game-installation worker.');
  if ResultCode <> 0 then
    RaiseException('Selected game installation failed safely. The retained diagnostic log is available at: ' + WorkerLog);
end;
