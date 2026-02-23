#define MyAppName "Dottoressa Iorio"
#ifndef AppVersion
  #define AppVersion "1.0.0"
#endif

[Setup]
AppId={{A53D1775-E8B2-4E8D-9FB3-52312AA1E7A4}
AppName={#MyAppName}
AppVersion={#AppVersion}
AppPublisher=Dottoressa Iorio
DefaultDirName={autopf64}\DottoressaIorio
DefaultGroupName={#MyAppName}
DisableDirPage=yes
PrivilegesRequired=admin
PrivilegesRequiredOverridesAllowed=commandline
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=Output
OutputBaseFilename=DottoressaIorioInstaller
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
SetupIconFile=..\DottoressaIorio.App\wwwroot\favicon.ico
UninstallDisplayIcon={app}\DottoressaIorio.App.exe

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional icons:"

[Files]
Source: "tmp\Service.7z"; DestDir: "{app}\_setup"; Flags: ignoreversion
Source: "7z.exe"; DestDir: "{app}\_setup"; Flags: ignoreversion
Source: "7z.dll"; DestDir: "{app}\_setup"; Flags: ignoreversion
Source: "install.bat"; DestDir: "{app}\_setup"; Flags: ignoreversion
Source: "uninstall.bat"; DestDir: "{app}\_setup"; Flags: ignoreversion

[Run]
Filename: "{app}\_setup\install.bat"; Parameters: "{code:GetInstallParams}"; Flags: runhidden waituntilterminated; StatusMsg: "Configuring Windows service..."

[UninstallRun]
Filename: "{app}\_setup\uninstall.bat"; Parameters: "--silent"; Flags: runhidden waituntilterminated skipifdoesntexist; RunOnceId: "DottoressaIorioAppUninstall"

[Icons]
Name: "{group}\Dottoressa Iorio App"; Filename: "{app}\DottoressaIorio.App.exe"
Name: "{group}\Dottoressa Iorio Web"; Filename: "{code:GetConfiguredUrl}"
Name: "{autodesktop}\Dottoressa Iorio Web"; Filename: "{code:GetConfiguredUrl}"; Tasks: desktopicon

[Code]
var
  ConfigPage: TInputQueryWizardPage;

procedure InitializeWizard;
var
  DomainName: string;
begin
  ConfigPage := CreateInputQueryPage(
    wpSelectTasks,
    'Application Configuration',
    'Choose service URL and allowed Windows user',
    'These values are applied during installation and can be changed by reinstalling.'
  );

  ConfigPage.Add('Allowed Username (DOMAIN\username):', False);
  ConfigPage.Add('Service URL:', False);

  DomainName := GetEnv('USERDOMAIN');
  if Trim(DomainName) = '' then
    DomainName := GetEnv('COMPUTERNAME');
  if Trim(DomainName) = '' then
    ConfigPage.Values[0] := ExpandConstant('{username}')
  else
    ConfigPage.Values[0] := DomainName + '\' + ExpandConstant('{username}');
  ConfigPage.Values[1] := 'http://localhost:5000';
end;

function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;
  if CurPageID = ConfigPage.ID then
  begin
    if Trim(ConfigPage.Values[0]) = '' then
    begin
      MsgBox('Allowed Username is required.', mbError, MB_OK);
      Result := False;
      Exit;
    end;

    if Trim(ConfigPage.Values[1]) = '' then
    begin
      MsgBox('Service URL is required.', mbError, MB_OK);
      Result := False;
      Exit;
    end;
  end;
end;

function GetInstallParams(Value: string): string;
begin
  Result :=
    '--silent --allowed-username ' + AddQuotes(ConfigPage.Values[0]) +
    ' --app-url ' + AddQuotes(ConfigPage.Values[1]);
end;

function GetConfiguredUrl(Value: string): string;
begin
  Result := ConfigPage.Values[1];
end;
