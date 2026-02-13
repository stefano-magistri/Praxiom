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
PrivilegesRequiredOverridesAllowed=dialog
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=.
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
Source: "Service.7z"; DestDir: "{app}\_setup"; Flags: ignoreversion
Source: "7z.exe"; DestDir: "{app}\_setup"; Flags: ignoreversion
Source: "7z.dll"; DestDir: "{app}\_setup"; Flags: ignoreversion
Source: "install.bat"; DestDir: "{app}\_setup"; Flags: ignoreversion
Source: "uninstall.bat"; DestDir: "{app}\_setup"; Flags: ignoreversion

[Run]
Filename: "{app}\_setup\install.bat"; Parameters: "--silent"; Flags: runhidden waituntilterminated; StatusMsg: "Configuring Windows service..."

[UninstallRun]
Filename: "{app}\_setup\uninstall.bat"; Parameters: "--silent"; Flags: runhidden waituntilterminated skipifdoesntexist; RunOnceId: "DottoressaIorioAppUninstall"

[Icons]
Name: "{group}\Dottoressa Iorio App"; Filename: "{app}\DottoressaIorio.App.exe"
Name: "{group}\Dottoressa Iorio Web"; Filename: "http://localhost:5000"
Name: "{autodesktop}\Dottoressa Iorio App"; Filename: "{app}\DottoressaIorio.App.exe"; Tasks: desktopicon
