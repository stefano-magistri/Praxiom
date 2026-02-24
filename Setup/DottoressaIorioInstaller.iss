#define MyAppName "Dottoressa Iorio"
#ifndef AppVersion
  #define AppVersion "1.0.0"
#endif

[Setup]
AppId={{A53D1775-E8B2-4E8D-9FB3-52312AA1E7A4}
AppName={#MyAppName}
AppVersion={#AppVersion}
AppPublisher=Dottoressa Iorio
DefaultDirName={commonappdata}\DottoressaIorio
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
Source: "backup_script.ps1"; DestDir: "{app}\_setup"; Flags: ignoreversion

[Run]
Filename: "{app}\_setup\install.bat"; Parameters: "{code:GetInstallParams}"; Flags: runhidden waituntilterminated; StatusMsg: "Configuring Windows service..."

[UninstallRun]
Filename: "{app}\_setup\uninstall.bat"; Parameters: "{code:GetUninstallParams}"; Flags: runhidden waituntilterminated skipifdoesntexist; RunOnceId: "DottoressaIorioAppUninstall"

[Icons]
Name: "{group}\Dottoressa Iorio App"; Filename: "{app}\DottoressaIorio.App.exe"
Name: "{group}\Dottoressa Iorio Web"; Filename: "{code:GetConfiguredUrl}"
Name: "{autodesktop}\Dottoressa Iorio Web"; Filename: "{code:GetConfiguredUrl}"; Tasks: desktopicon

[Code]
var
  ConfigPage: TInputQueryWizardPage;
  RemoveAllDataOnUninstall: Boolean;

function IsValidServiceUrl(const Url: string): Boolean; forward;
function ExtractPortFromUrl(const Url: string): Integer; forward;
function IsPortInUse(PortValue: Integer): Boolean; forward;
function GetUninstallParams(Value: string): string; forward;

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
var
  UrlToCheck: string;
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

    UrlToCheck := Trim(ConfigPage.Values[1]);
    if not IsValidServiceUrl(UrlToCheck) then
    begin
      MsgBox('Service URL must be in the format http://hostname:port or https://hostname:port.', mbError, MB_OK);
      Result := False;
      Exit;
    end;

    if IsPortInUse(ExtractPortFromUrl(UrlToCheck)) then
    begin
      if MsgBox('The selected port appears to already be in use. Continue anyway?', mbConfirmation, MB_YESNO) = IDNO then
      begin
        Result := False;
        Exit;
      end;
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

function IsDigits(const Value: string): Boolean;
var
  I: Integer;
begin
  Result := Length(Value) > 0;
  if not Result then
    Exit;
  for I := 1 to Length(Value) do
  begin
    if (Value[I] < '0') or (Value[I] > '9') then
    begin
      Result := False;
      Exit;
    end;
  end;
end;

function LastColonPos(const Value: string): Integer;
var
  I: Integer;
begin
  Result := 0;
  for I := Length(Value) downto 1 do
  begin
    if Value[I] = ':' then
    begin
      Result := I;
      Exit;
    end;
  end;
end;

function ExtractPortFromUrl(const Url: string): Integer;
var
  PrefixPos: Integer;
  HostAndPort: string;
  ColonPos: Integer;
  PortText: string;
begin
  Result := 0;
  PrefixPos := Pos('://', Url);
  if PrefixPos = 0 then
    Exit;
  HostAndPort := Copy(Url, PrefixPos + 3, MaxInt);
  ColonPos := LastColonPos(HostAndPort);
  if ColonPos = 0 then
    Exit;
  PortText := Copy(HostAndPort, ColonPos + 1, MaxInt);
  if (Pos('/', PortText) > 0) or (not IsDigits(PortText)) then
    Exit;
  Result := StrToInt(PortText);
end;

function IsValidServiceUrl(const Url: string): Boolean;
var
  LowerUrl: string;
  PrefixPos: Integer;
  HostAndPort: string;
  ColonPos: Integer;
  HostText: string;
  PortValue: Integer;
begin
  Result := False;
  LowerUrl := LowerCase(Trim(Url));
  if (Pos('http://', LowerUrl) <> 1) and (Pos('https://', LowerUrl) <> 1) then
    Exit;

  PrefixPos := Pos('://', LowerUrl);
  HostAndPort := Copy(LowerUrl, PrefixPos + 3, MaxInt);
  if Pos('/', HostAndPort) > 0 then
    Exit;

  ColonPos := LastColonPos(HostAndPort);
  if ColonPos = 0 then
    Exit;

  HostText := Copy(HostAndPort, 1, ColonPos - 1);
  if Trim(HostText) = '' then
    Exit;

  PortValue := ExtractPortFromUrl(LowerUrl);
  if (PortValue < 1) or (PortValue > 65535) then
    Exit;

  Result := True;
end;

function IsPortInUse(PortValue: Integer): Boolean;
var
  CmdResult: Integer;
  OutputFile: string;
  OutputText: AnsiString;
begin
  Result := False;
  if PortValue <= 0 then
    Exit;

  OutputFile := ExpandConstant('{tmp}\port-check.txt');
  if FileExists(OutputFile) then
    DeleteFile(OutputFile);

  Exec(
    ExpandConstant('{cmd}'),
    '/C netstat -ano | findstr /R /C:":' + IntToStr(PortValue) + ' .*LISTENING" > "' + OutputFile + '"',
    '',
    SW_HIDE,
    ewWaitUntilTerminated,
    CmdResult
  );

  if CmdResult = 0 then
  begin
    if LoadStringFromFile(OutputFile, OutputText) then
      Result := Trim(String(OutputText)) <> '';
  end;
end;

function InitializeUninstall(): Boolean;
begin
  Result := True;
  RemoveAllDataOnUninstall := False;
  if not UninstallSilent then
  begin
    RemoveAllDataOnUninstall :=
      MsgBox(
        'Do you want to remove all local application data (database files) as well?',
        mbConfirmation,
        MB_YESNO
      ) = IDYES;
  end;
end;

function GetUninstallParams(Value: string): string;
begin
  Result := '--silent';
  if RemoveAllDataOnUninstall then
    Result := Result + ' --remove-all-data';
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  StatusFile: string;
  StatusText: AnsiString;
begin
  if CurStep <> ssPostInstall then
    Exit;

  StatusFile := ExpandConstant('{app}\_setup\install.status');
  if not FileExists(StatusFile) then
    Exit;

  if LoadStringFromFile(StatusFile, StatusText) then
  begin
    if Pos('WARNING', UpperCase(String(StatusText))) > 0 then
      MsgBox(
        'Installation completed with warnings. Open "' +
        ExpandConstant('{app}\_setup\install.log') + '" for details.',
        mbInformation,
        MB_OK
      );
  end;
end;
