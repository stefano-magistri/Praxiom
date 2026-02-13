@echo off
setlocal EnableExtensions

rem Check if the script is running as administrator.
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo Please run this script as administrator.
    pause
    exit /b 1
)

rem Change to the directory where the script is located.
cd /d "%~dp0"

rem Define source file, destination folder, and service-related variables.
set "sourceFile=%~dp0Service.7z"
set "sevenZipExe=%~dp07z.exe"
set "destinationFolder=C:\Program Files\DottoressaIorio"
set "service=DottoressaIorio.App"
set "app=DottoressaIorio.App.exe"
set "appUrl=http://localhost:5000"
set "dbFile=app.db"
set "dbBackupFolder=_db_backup"

if not exist "%sourceFile%" (
    echo Missing package: "%sourceFile%"
    pause
    exit /b 1
)

if not exist "%sevenZipExe%" (
    echo Missing extractor: "%sevenZipExe%"
    pause
    exit /b 1
)

if not exist "%destinationFolder%" mkdir "%destinationFolder%"

rem Backup current database before replacing files.
if exist "%destinationFolder%\%dbFile%" (
    if not exist "%destinationFolder%\%dbBackupFolder%" mkdir "%destinationFolder%\%dbBackupFolder%"
    copy /Y "%destinationFolder%\%dbFile%" "%destinationFolder%\%dbBackupFolder%\%dbFile%" >nul
)

rem Extract files from the 7z archive to the destination folder.
"%sevenZipExe%" x "%sourceFile%" -o"%destinationFolder%" *.* -r -y
if errorlevel 1 (
    echo Failed to extract "%sourceFile%".
    pause
    exit /b 1
)

rem Restore database if backup is present.
if exist "%destinationFolder%\%dbBackupFolder%\%dbFile%" (
    copy /Y "%destinationFolder%\%dbBackupFolder%\%dbFile%" "%destinationFolder%\%dbFile%" >nul
)

rem Runtime installation is not required: payload is self-contained.

rem Recreate the Windows service using native Service Control Manager.
sc query "%service%" >nul 2>nul
if %errorlevel% equ 0 (
    sc stop "%service%" >nul 2>nul
    timeout /t 2 /nobreak >nul
    sc delete "%service%" >nul 2>nul
    timeout /t 2 /nobreak >nul
)

sc create "%service%" binPath= "\"%destinationFolder%\%app%\" --urls %appUrl%" start= auto DisplayName= "Dottoressa Iorio App"
if errorlevel 1 (
    echo Failed to create service "%service%".
    pause
    exit /b 1
)

sc description "%service%" "Dottoressa Iorio medical management application"
sc start "%service%" >nul
if errorlevel 1 (
    echo Service "%service%" was created but failed to start.
    pause
    exit /b 1
)

start "" "%appUrl%"
echo Installation completed successfully.
pause
exit /b 0
