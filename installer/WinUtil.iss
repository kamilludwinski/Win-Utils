; Build: ISCC.exe WinUtil.iss /DPublishDir="C:\path\to\publish" /DMyAppVersion=1.2.3
#ifndef MyAppVersion
#define MyAppVersion "1.0.0"
#endif
#ifndef PublishDir
#define PublishDir "..\publish"
#endif

#define MyAppName "WinUtil"
#define MyAppExeName "WinUtil.exe"
#define MyAppPublisher "WinUtil"

[Setup]
AppId={{E4F8A1C2-9B3D-4E7F-8A6C-1D2E3F4A5B6C}
AppName={#MyAppName}
; Default AppVerName is "AppName version AppVersion"; override so Settings → Apps shows plain "WinUtil".
AppVerName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DisableProgramGroupPage=yes
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64
OutputDir=..\dist
OutputBaseFilename=WinUtilSetup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[InstallDelete]
; Runs when installation starts (after the uninstall prompt in [Code] and after the user proceeds in the wizard).
; Removes the chosen install folder if it still exists (leftovers, partial uninstall, stray files).
Type: filesandordirs; Name: "{app}"

[Dirs]
; Writable logs folder under Program Files (users can write without elevation)
Name: "{app}\logs"; Permissions: users-modify

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "{#MyAppName}"; ValueData: """{app}\{#MyAppExeName}"""; Flags: uninsdeletevalue

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
// Same AppId as [Setup] — uninstall registry key is {AppId}_is1 under HKLM Uninstall.
function UninstallPreviousIfPresent: Boolean;
var
  Uninst: String;
  ErrCode: Integer;
begin
  Result := True;
  if not RegQueryStringValue(HKLM64,
    'Software\Microsoft\Windows\CurrentVersion\Uninstall\{E4F8A1C2-9B3D-4E7F-8A6C-1D2E3F4A5B6C}_is1',
    'UninstallString', Uninst) then
    if not RegQueryStringValue(HKLM,
      'Software\Microsoft\Windows\CurrentVersion\Uninstall\{E4F8A1C2-9B3D-4E7F-8A6C-1D2E3F4A5B6C}_is1',
      'UninstallString', Uninst) then
      Exit;

  if MsgBox(
    'A previous version of WinUtil is already installed.' + #13#10 + #13#10 +
    'Click Yes to uninstall it now, then continue with this setup.' + #13#10 +
    'Click No to exit Setup (uninstall manually from Settings if needed).',
    mbConfirmation, MB_YESNO) = IDNO then
  begin
    Result := False;
    Exit;
  end;

  if not Exec(RemoveQuotes(Uninst), '/SILENT /NORESTART /SUPPRESSMSGBOXES', '', SW_HIDE, ewWaitUntilTerminated, ErrCode) then
  begin
    MsgBox('Could not run the uninstaller. Remove WinUtil from Settings → Apps, then run this setup again.', mbError, MB_OK);
    Result := False;
    Exit;
  end;

  if ErrCode <> 0 then
  begin
    MsgBox('The uninstaller reported an error. Remove WinUtil from Settings → Apps, then run this setup again.', mbError, MB_OK);
    Result := False;
  end;
end;

function InitializeSetup: Boolean;
begin
  Result := UninstallPreviousIfPresent;
end;
