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
Name: "italian"; MessagesFile: "compiler:Languages\Italian.isl"

[Tasks]
Name: "desktopicon"; Description: "Crea un collegamento sul desktop"; GroupDescription: "Seleziona le icone da creare:"

[Files]
Source: "..\DottoressaIorio.App\wwwroot\favicon.ico"; DestDir: "{app}"; Flags: ignoreversion
Source: "tmp\Service.7z"; DestDir: "{app}\_setup"; Flags: ignoreversion
Source: "7z.exe"; DestDir: "{app}\_setup"; Flags: ignoreversion
Source: "7z.dll"; DestDir: "{app}\_setup"; Flags: ignoreversion
Source: "install.bat"; DestDir: "{app}\_setup"; Flags: ignoreversion
Source: "uninstall.bat"; DestDir: "{app}\_setup"; Flags: ignoreversion
Source: "backup_script.ps1"; DestDir: "{app}\_setup"; Flags: ignoreversion
Source: "restore_script.ps1"; DestDir: "{app}\_setup"; Flags: ignoreversion
Source: "tmp\LicenseValidator\DottoressaIorio.LicenseGenerator.exe"; DestDir: "{app}\_setup"; Flags: ignoreversion; StatusMsg: "Preparazione dei componenti di installazione..."
Source: "tmp\LicenseValidator\DottoressaIorio.LicenseGenerator.exe"; Flags: dontcopy

[Run]
Filename: "{app}\_setup\install.bat"; Parameters: "{code:GetInstallParams}"; Flags: runhidden waituntilterminated; StatusMsg: "Configurazione del servizio Windows in corso..."
Filename: "{code:GetConfiguredUrl}"; Description: "Apri l'applicazione al termine dell'installazione"; Flags: postinstall shellexec nowait skipifsilent

[UninstallRun]
Filename: "{app}\_setup\uninstall.bat"; Parameters: "{code:GetUninstallParams}"; Flags: runhidden waituntilterminated skipifdoesntexist; RunOnceId: "DottoressaIorioAppUninstall"

[Icons]
Name: "{group}\Dottoressa Iorio App"; Filename: "{app}\DottoressaIorio.App.exe"
Name: "{group}\Dottoressa Iorio Web"; Filename: "{code:GetConfiguredUrl}"; IconFilename: "{app}\favicon.ico"
Name: "{autodesktop}\Dottoressa Iorio Web"; Filename: "{code:GetConfiguredUrl}"; IconFilename: "{app}\favicon.ico"; Tasks: desktopicon

[Code]
var
  ConfigPage: TInputQueryWizardPage;
  LicenseFilePage: TInputFileWizardPage;
  RestoreOptionPage: TInputOptionWizardPage;
  RestoreFilePage: TInputFileWizardPage;
  AllowedUserHintLabel: TNewStaticText;

function IsValidServiceUrl(const Url: string): Boolean; forward;
function ExtractPortFromUrl(const Url: string): Integer; forward;
function IsPortInUse(PortValue: Integer): Boolean; forward;
function GetUninstallParams(Value: string): string; forward;
function ShouldRestoreDatabase(): Boolean; forward;
function GetRestoreBackupPath(): string; forward;
function ValidateSelectedLicense(): Boolean; forward;

procedure InitializeWizard;
var
  DomainName: string;
begin
  WizardForm.SelectTasksLabel.Caption := 'Seleziona le icone aggiuntive da creare.';

  ConfigPage := CreateInputQueryPage(
    wpSelectTasks,
    'Configurazione applicazione',
    'Scegli l''URL del servizio e l''utente Windows autorizzato',
    'Questi valori vengono applicati durante l''installazione e possono essere modificati reinstallando.'
  );

  ConfigPage.Add('Nome utente autorizzato (DOMINIO\username):', False);
  ConfigPage.Add('URL del servizio:', False);

  DomainName := GetEnv('USERDOMAIN');
  if Trim(DomainName) = '' then
    DomainName := GetEnv('COMPUTERNAME');
  if Trim(DomainName) = '' then
    ConfigPage.Values[0] := ExpandConstant('{username}')
  else
    ConfigPage.Values[0] := DomainName + '\' + ExpandConstant('{username}');
  ConfigPage.Values[1] := 'http://localhost:5000';

  AllowedUserHintLabel := TNewStaticText.Create(ConfigPage);
  AllowedUserHintLabel.Parent := ConfigPage.Surface;
  AllowedUserHintLabel.Left := ConfigPage.Edits[0].Left;
  AllowedUserHintLabel.Top := ConfigPage.Edits[0].Top + ConfigPage.Edits[0].Height + ScaleY(4);
  AllowedUserHintLabel.Width := ConfigPage.SurfaceWidth - AllowedUserHintLabel.Left;
  AllowedUserHintLabel.Height := ScaleY(24);
  AllowedUserHintLabel.Caption := 'Deve corrispondere all''utente Windows presente nella licenza.';
  AllowedUserHintLabel.WordWrap := True;

  ConfigPage.PromptLabels[1].Top := AllowedUserHintLabel.Top + AllowedUserHintLabel.Height + ScaleY(8);
  ConfigPage.Edits[1].Top := ConfigPage.PromptLabels[1].Top + ConfigPage.PromptLabels[1].Height + ScaleY(4);

  LicenseFilePage := CreateInputFilePage(
    ConfigPage.ID,
    'File licenza',
    'Seleziona il file di licenza ricevuto.',
    'Il file di licenza verra copiato in ProgramData e verificato a ogni avvio dell''applicazione.'
  );
  LicenseFilePage.Add('Percorso file licenza:', 'File licenza (*.dat)|*.dat|Tutti i file (*.*)|*.*', '.dat');

  RestoreOptionPage := CreateInputOptionPage(
    LicenseFilePage.ID,
    'Ripristino database',
    'Scegli se ripristinare il database da un file di backup esistente.',
    'Seleziona "Si" solo se vuoi sostituire il database corrente.',
    True,
    False
  );
  RestoreOptionPage.Add('No, usa il database esistente se presente');
  RestoreOptionPage.Add('Si, ripristina da un file di backup');
  RestoreOptionPage.SelectedValueIndex := 0;

  RestoreFilePage := CreateInputFilePage(
    RestoreOptionPage.ID,
    'File di ripristino database',
    'Scegli il file di backup del database da ripristinare.',
    'Seleziona il file di backup `.db` che deve sostituire il database installato.'
  );
  RestoreFilePage.Add('Percorso file di backup:', 'Backup database (*.db)|*.db|Tutti i file (*.*)|*.*', '.db');
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
      MsgBox('Il nome utente autorizzato e obbligatorio.', mbError, MB_OK);
      Result := False;
      Exit;
    end;

    if Trim(ConfigPage.Values[1]) = '' then
    begin
      MsgBox('L''URL del servizio e obbligatorio.', mbError, MB_OK);
      Result := False;
      Exit;
    end;

    UrlToCheck := Trim(ConfigPage.Values[1]);
    if not IsValidServiceUrl(UrlToCheck) then
    begin
      MsgBox('L''URL del servizio deve essere nel formato http://hostname:porta oppure https://hostname:porta.', mbError, MB_OK);
      Result := False;
      Exit;
    end;

    if IsPortInUse(ExtractPortFromUrl(UrlToCheck)) then
    begin
      if MsgBox('La porta selezionata sembra gia essere in uso. Vuoi continuare comunque?', mbConfirmation, MB_YESNO) = IDNO then
      begin
        Result := False;
        Exit;
      end;
    end;
  end;

  if CurPageID = LicenseFilePage.ID then
  begin
    if Trim(LicenseFilePage.Values[0]) = '' then
    begin
      MsgBox('Il file di licenza e obbligatorio.', mbError, MB_OK);
      Result := False;
      Exit;
    end;

    if not FileExists(Trim(LicenseFilePage.Values[0])) then
    begin
      MsgBox('Il file di licenza selezionato non esiste.', mbError, MB_OK);
      Result := False;
      Exit;
    end;

    if not ValidateSelectedLicense() then
    begin
      Result := False;
      Exit;
    end;
  end;

  if CurPageID = RestoreFilePage.ID then
  begin
    if ShouldRestoreDatabase() and (Trim(GetRestoreBackupPath()) = '') then
    begin
      MsgBox('Il percorso del file di backup e obbligatorio quando il ripristino del database e attivato.', mbError, MB_OK);
      Result := False;
      Exit;
    end;

    if ShouldRestoreDatabase() and (not FileExists(GetRestoreBackupPath())) then
    begin
      MsgBox('Il file di backup selezionato non esiste.', mbError, MB_OK);
      Result := False;
      Exit;
    end;
  end;
end;

function ShouldSkipPage(PageID: Integer): Boolean;
begin
  Result := False;
  if PageID = RestoreFilePage.ID then
    Result := not ShouldRestoreDatabase();
end;

function GetInstallParams(Value: string): string;
begin
  Result :=
    '--silent --allowed-username ' + AddQuotes(ConfigPage.Values[0]) +
    ' --app-url ' + AddQuotes(ConfigPage.Values[1]) +
    ' --license-file ' + AddQuotes(LicenseFilePage.Values[0]);
  if ShouldRestoreDatabase() then
    Result := Result + ' --skip-auto-db-restore';
end;

function GetConfiguredUrl(Value: string): string;
begin
  Result := ConfigPage.Values[1];
end;

function ShouldRestoreDatabase(): Boolean;
begin
  Result := RestoreOptionPage.SelectedValueIndex = 1;
end;

function GetRestoreBackupPath(): string;
begin
  Result := Trim(RestoreFilePage.Values[0]);
end;

function ValidateSelectedLicense(): Boolean;
var
  ValidatorExe: string;
  CmdResult: Integer;
  Parameters: string;
begin
  Result := False;

  ExtractTemporaryFile('DottoressaIorio.LicenseGenerator.exe');
  ValidatorExe := ExpandConstant('{tmp}\DottoressaIorio.LicenseGenerator.exe');
  Parameters :=
    'validate --license-file ' + AddQuotes(Trim(LicenseFilePage.Values[0])) +
    ' --allowed-username ' + AddQuotes(Trim(ConfigPage.Values[0]));

  if not Exec(ValidatorExe, Parameters, '', SW_HIDE, ewWaitUntilTerminated, CmdResult) then
  begin
    MsgBox('File di licenza non valido. Contatta il fornitore del software.', mbError, MB_OK);
    Exit;
  end;

  if CmdResult = 0 then
  begin
    Result := True;
    Exit;
  end;

  case CmdResult of
    2:
      MsgBox('File di licenza non trovato.', mbError, MB_OK);
    3:
      MsgBox('Questa licenza non e valida per questo computer.', mbError, MB_OK);
    4:
      MsgBox('La licenza non e valida per questo utente Windows.', mbError, MB_OK);
    5:
      MsgBox('File di licenza non valido. Contatta il fornitore del software.', mbError, MB_OK);
    6:
      MsgBox('Il file di licenza non e valido o e danneggiato.', mbError, MB_OK);
    else
      MsgBox('File di licenza non valido. Contatta il fornitore del software.', mbError, MB_OK);
  end;
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

function GetUninstallParams(Value: string): string;
begin
  Result := '--silent';
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  StatusFile: string;
  StatusText: AnsiString;
  RestoreResult: Integer;
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
        'Installazione completata con avvisi. Apri "' +
        ExpandConstant('{app}\_setup\install.log') + '" per i dettagli.',
        mbInformation,
        MB_OK
      );
  end;

  if not ShouldRestoreDatabase() then
    Exit;

  if not Exec(
    ExpandConstant('{sys}\WindowsPowerShell\v1.0\powershell.exe'),
    '-NoProfile -ExecutionPolicy Bypass -File ' +
      AddQuotes(ExpandConstant('{app}\_setup\restore_script.ps1')) +
      ' -BackupPath ' + AddQuotes(GetRestoreBackupPath()),
    '',
    SW_HIDE,
    ewWaitUntilTerminated,
    RestoreResult
  ) then
  begin
    MsgBox(
      'Impossibile avviare lo script di ripristino del database. Potrai eseguirlo manualmente in seguito da "' +
      ExpandConstant('{app}\_setup\restore_script.ps1') + '".',
      mbError,
      MB_OK
    );
    Exit;
  end;

  if RestoreResult <> 0 then
  begin
    MsgBox(
      'Ripristino del database non riuscito. Verifica il file di backup selezionato ed esegui "' +
      ExpandConstant('{app}\_setup\restore_script.ps1') + '" manualmente se necessario.',
      mbError,
      MB_OK
    );
    Exit;
  end;

  MsgBox('Ripristino del database completato con successo.', mbInformation, MB_OK);
end;
