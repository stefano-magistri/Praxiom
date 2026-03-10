#define MyAppName "Praxiom"
#ifndef AppVersion
  #define AppVersion "1.0.0"
#endif

[Setup]
AppId={{A53D1775-E8B2-4E8D-9FB3-52312AA1E7A4}
AppName={#MyAppName}
AppVersion={#AppVersion}
AppPublisher=Praxiom
DefaultDirName={commonappdata}\Praxiom
DefaultGroupName={#MyAppName}
DisableDirPage=yes
PrivilegesRequired=admin
PrivilegesRequiredOverridesAllowed=commandline
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=Output
OutputBaseFilename=PraxiomSetup
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
Source: "tmp\LicenseValidator\DottoressaIorio.LicenseGenerator.exe"; DestDir: "{app}\_setup"; DestName: "setup-helper.exe"; Flags: ignoreversion
Source: "tmp\LicenseValidator\DottoressaIorio.LicenseGenerator.exe"; Flags: dontcopy

[Run]
Filename: "{app}\_setup\install.bat"; Parameters: "{code:GetInstallParams}"; Flags: runhidden waituntilterminated; StatusMsg: "Configurazione del servizio Windows in corso..."
Filename: "{code:GetConfiguredUrl}"; Description: "Apri l'applicazione al termine dell'installazione"; Flags: postinstall shellexec nowait skipifsilent

[UninstallRun]
Filename: "{app}\_setup\uninstall.bat"; Parameters: "{code:GetUninstallParams}"; Flags: runhidden waituntilterminated skipifdoesntexist; RunOnceId: "DottoressaIorioAppUninstall"

[Icons]
Name: "{group}\Praxiom App"; Filename: "{app}\DottoressaIorio.App.exe"
Name: "{group}\Praxiom Web"; Filename: "{code:GetConfiguredUrl}"; IconFilename: "{app}\favicon.ico"
Name: "{autodesktop}\Praxiom Web"; Filename: "{code:GetConfiguredUrl}"; IconFilename: "{app}\favicon.ico"; Tasks: desktopicon

[Code]
const
  ExistingInstallUninstallRegKey = 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{A53D1775-E8B2-4E8D-9FB3-52312AA1E7A4}_is1';

var
  LicenseRequestPage: TInputQueryWizardPage;
  ConfigPage: TInputQueryWizardPage;
  LicenseFilePage: TInputFileWizardPage;
  RestoreOptionPage: TInputOptionWizardPage;
  RestoreFilePage: TInputFileWizardPage;
  LicenseRequestNoteLabel: TNewStaticText;

function InitializeSetup(): Boolean; forward;
function GetMachineId(Value: string): string; forward;
function GetAllowedUsername(Value: string): string; forward;

function TryGetExistingUninstaller(var UninstallCommand: string): Boolean; forward;
function SplitCommandLine(const CommandLine: string; var FileName: string; var Parameters: string): Boolean; forward;
function PromptForExistingInstallAction(const UninstallCommand: string): Boolean; forward;
function IsValidServiceUrl(const Url: string): Boolean; forward;
function ExtractPortFromUrl(const Url: string): Integer; forward;
function IsPortInUse(PortValue: Integer): Boolean; forward;
function GetUninstallParams(Value: string): string; forward;
function ShouldRestoreDatabase(): Boolean; forward;
function GetRestoreBackupPath(): string; forward;
function ValidateSelectedLicense(): Boolean; forward;

function InitializeSetup(): Boolean;
var
  ExistingUninstaller: string;
begin
  Result := True;

  if not TryGetExistingUninstaller(ExistingUninstaller) then
    Exit;

  Result := PromptForExistingInstallAction(ExistingUninstaller);
end;

procedure InitializeWizard;
var
  DomainName: string;
  MachineId: string;
  AllowedUsername: string;
begin
  WizardForm.SelectTasksLabel.Caption := 'Seleziona le icone aggiuntive da creare.';

  DomainName := GetEnv('USERDOMAIN');
  if Trim(DomainName) = '' then
    DomainName := GetEnv('COMPUTERNAME');
  if Trim(DomainName) = '' then
    AllowedUsername := ExpandConstant('{username}')
  else
    AllowedUsername := DomainName + '\' + ExpandConstant('{username}');

  if not RegQueryStringValue(HKLM, 'SOFTWARE\Microsoft\Cryptography', 'MachineGuid', MachineId) then
    MachineId := '';

  LicenseRequestPage := CreateInputQueryPage(
    wpSelectTasks,
    'Richiesta licenza',
    'Per ricevere il file di licenza, invia al fornitore i dati seguenti.',
    'La licenza verra generata per questo computer e per questo utente Windows.'
  );
  LicenseRequestPage.Add('Machine ID:', False);
  LicenseRequestPage.Add('Utente Windows autorizzato:', False);
  LicenseRequestPage.Values[0] := MachineId;
  LicenseRequestPage.Values[1] := AllowedUsername;
  LicenseRequestPage.Edits[0].ReadOnly := True;
  LicenseRequestPage.Edits[1].ReadOnly := True;

  LicenseRequestNoteLabel := TNewStaticText.Create(LicenseRequestPage);
  LicenseRequestNoteLabel.Parent := LicenseRequestPage.Surface;
  LicenseRequestNoteLabel.Left := LicenseRequestPage.Edits[1].Left;
  LicenseRequestNoteLabel.Top := LicenseRequestPage.Edits[1].Top + LicenseRequestPage.Edits[1].Height + ScaleY(8);
  LicenseRequestNoteLabel.Width := LicenseRequestPage.SurfaceWidth - LicenseRequestNoteLabel.Left;
  LicenseRequestNoteLabel.Height := ScaleY(42);
  LicenseRequestNoteLabel.Caption :=
    'Invia questi dati al fornitore per ricevere il file di licenza.' + #13#10 +
    'Puoi copiarli dai campi sopra oppure inviare uno screenshot di questa schermata.';
  LicenseRequestNoteLabel.WordWrap := True;

  LicenseFilePage := CreateInputFilePage(
    LicenseRequestPage.ID,
    'File licenza',
    'Seleziona il file di licenza ricevuto dal fornitore, se disponibile.',
    'Se non selezioni alcun file, l''applicazione verra installata in modalita di prova.'
  );
  LicenseFilePage.Add('Percorso file licenza:', 'File licenza (*.dat)|*.dat|Tutti i file (*.*)|*.*', '.dat');

  ConfigPage := CreateInputQueryPage(
    LicenseFilePage.ID,
    'Configurazione applicazione',
    'Scegli l''URL del servizio.',
    'Questo valore viene applicato durante l''installazione e puo essere modificato reinstallando.'
  );

  ConfigPage.Add('URL del servizio:', False);
  ConfigPage.Values[0] := 'http://localhost:5000';

  RestoreOptionPage := CreateInputOptionPage(
    ConfigPage.ID,
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

  if CurPageID = LicenseRequestPage.ID then
  begin
    if Trim(GetMachineId('')) = '' then
    begin
      MsgBox('Machine ID non disponibile. Contatta il fornitore del software.', mbError, MB_OK);
      Result := False;
      Exit;
    end;

    if Trim(GetAllowedUsername('')) = '' then
    begin
      MsgBox('Utente Windows non disponibile. Contatta il fornitore del software.', mbError, MB_OK);
      Result := False;
      Exit;
    end;
  end;

  if CurPageID = ConfigPage.ID then
  begin
    if Trim(ConfigPage.Values[0]) = '' then
    begin
      MsgBox('L''URL del servizio e obbligatorio.', mbError, MB_OK);
      Result := False;
      Exit;
    end;

    UrlToCheck := Trim(ConfigPage.Values[0]);
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
    if (Trim(LicenseFilePage.Values[0]) <> '') and (not FileExists(Trim(LicenseFilePage.Values[0]))) then
    begin
      MsgBox('Il file di licenza selezionato non esiste.', mbError, MB_OK);
      Result := False;
      Exit;
    end;

    if (Trim(LicenseFilePage.Values[0]) <> '') and (not ValidateSelectedLicense()) then
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
    '--silent --allowed-username ' + AddQuotes(GetAllowedUsername('')) +
    ' --app-url ' + AddQuotes(ConfigPage.Values[0]);
  if Trim(LicenseFilePage.Values[0]) <> '' then
    Result := Result + ' --license-file ' + AddQuotes(LicenseFilePage.Values[0]);
  if ShouldRestoreDatabase() then
    Result := Result + ' --skip-auto-db-restore';
end;

function GetConfiguredUrl(Value: string): string;
begin
  Result := ConfigPage.Values[0];
end;

function GetMachineId(Value: string): string;
begin
  Result := Trim(LicenseRequestPage.Values[0]);
end;

function GetAllowedUsername(Value: string): string;
begin
  Result := Trim(LicenseRequestPage.Values[1]);
end;

function ShouldRestoreDatabase(): Boolean;
begin
  Result := RestoreOptionPage.SelectedValueIndex = 1;
end;

function GetRestoreBackupPath(): string;
begin
  Result := Trim(RestoreFilePage.Values[0]);
end;

function TryGetExistingUninstaller(var UninstallCommand: string): Boolean;
begin
  Result :=
    RegQueryStringValue(HKLM64, ExistingInstallUninstallRegKey, 'UninstallString', UninstallCommand) or
    RegQueryStringValue(HKLM, ExistingInstallUninstallRegKey, 'UninstallString', UninstallCommand);
  Result := Result and (Trim(UninstallCommand) <> '');
end;

function SplitCommandLine(const CommandLine: string; var FileName: string; var Parameters: string): Boolean;
var
  TrimmedCommand: string;
  ClosingQuotePos: Integer;
  SeparatorPos: Integer;
begin
  Result := False;
  FileName := '';
  Parameters := '';
  TrimmedCommand := Trim(CommandLine);
  if TrimmedCommand = '' then
    Exit;

  if TrimmedCommand[1] = '"' then
  begin
    ClosingQuotePos := Pos('"', Copy(TrimmedCommand, 2, MaxInt));
    if ClosingQuotePos = 0 then
      Exit;

    ClosingQuotePos := ClosingQuotePos + 1;
    FileName := Copy(TrimmedCommand, 2, ClosingQuotePos - 2);
    Parameters := Trim(Copy(TrimmedCommand, ClosingQuotePos + 1, MaxInt));
    Result := True;
    Exit;
  end;

  SeparatorPos := Pos(' ', TrimmedCommand);
  if SeparatorPos = 0 then
  begin
    FileName := TrimmedCommand;
    Result := True;
    Exit;
  end;

  FileName := Copy(TrimmedCommand, 1, SeparatorPos - 1);
  Parameters := Trim(Copy(TrimmedCommand, SeparatorPos + 1, MaxInt));
  Result := True;
end;

function PromptForExistingInstallAction(const UninstallCommand: string): Boolean;
var
  ButtonResult: Integer;
  UninstallFileName: string;
  UninstallParameters: string;
  ExecResult: Integer;
begin
  ButtonResult := MsgBox(
    'Praxiom risulta giá installato su questo computer.' + #13#10 + #13#10 +
    'Per reinstallare il software devi prima disinstallare la versione esistente.' + #13#10 + #13#10 +
    'Scegli "Si" per aprire il programma di disinstallazione esistente oppure "No" per annullare l''installazione.',
    mbConfirmation,
    MB_YESNO or MB_DEFBUTTON2
  );

  if ButtonResult = IDYES then
  begin
    if not SplitCommandLine(UninstallCommand, UninstallFileName, UninstallParameters) then
      MsgBox(
        'Installazione interrotta. Impossibile individuare il programma di disinstallazione esistente. Disinstalla manualmente il software da "Programmi e funzionalita" e riprova.',
        mbError,
        MB_OK
      )
    else if not Exec(UninstallFileName, UninstallParameters, '', SW_SHOWNORMAL, ewNoWait, ExecResult) then
      MsgBox(
        'Installazione interrotta. Impossibile avviare il programma di disinstallazione esistente. Disinstalla manualmente il software da "Programmi e funzionalita" e riprova.',
        mbError,
        MB_OK
      );
  end;

  Result := False;
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
    ' --allowed-username ' + AddQuotes(GetAllowedUsername(''));

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
