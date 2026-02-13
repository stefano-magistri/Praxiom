@echo off
setlocal EnableExtensions

set "interactive=1"
if /I "%~1"=="--silent" set "interactive=0"

rem Check if the script is running as administrator.
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo Please run this script as administrator.
    if "%interactive%"=="1" pause
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
set "dbBackupFolder=_db_backup"

if not exist "%sourceFile%" (
    echo Missing package: "%sourceFile%"
    if "%interactive%"=="1" pause
    exit /b 1
)

if not exist "%sevenZipExe%" (
    echo Missing extractor: "%sevenZipExe%"
    if "%interactive%"=="1" pause
    exit /b 1
)

if not exist "%destinationFolder%" mkdir "%destinationFolder%"
if not exist "%destinationFolder%\%dbBackupFolder%" mkdir "%destinationFolder%\%dbBackupFolder%"

rem Backup current database files before replacing files.
call :backupDbFile "app.db"
call :backupDbFile "app.db-shm"
call :backupDbFile "app.db-wal"
call :backupDbFile "app_unencrypted.db"
call :backupDbFile "app_unencrypted.db-shm"
call :backupDbFile "app_unencrypted.db-wal"

rem Extract files from the 7z archive to the destination folder.
"%sevenZipExe%" x "%sourceFile%" -o"%destinationFolder%" *.* -r -y
if errorlevel 1 (
    echo Failed to extract "%sourceFile%".
    if "%interactive%"=="1" pause
    exit /b 1
)

rem Restore database files if backup is present.
call :restoreDbFile "app.db"
call :restoreDbFile "app.db-shm"
call :restoreDbFile "app.db-wal"
call :restoreDbFile "app_unencrypted.db"
call :restoreDbFile "app_unencrypted.db-shm"
call :restoreDbFile "app_unencrypted.db-wal"

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
    if "%interactive%"=="1" pause
    exit /b 1
)

sc description "%service%" "Dottoressa Iorio medical management application"
sc start "%service%" >nul
if errorlevel 1 (
    echo Service "%service%" was created but failed to start.
    if "%interactive%"=="1" pause
    exit /b 1
)

if "%interactive%"=="1" start "" "%appUrl%"
echo Installation completed successfully.
if "%interactive%"=="1" pause
exit /b 0

:backupDbFile
set "dbName=%~1"
if exist "%destinationFolder%\%dbName%" copy /Y "%destinationFolder%\%dbName%" "%destinationFolder%\%dbBackupFolder%\%dbName%" >nul
exit /b 0

:restoreDbFile
set "dbName=%~1"
if exist "%destinationFolder%\%dbBackupFolder%\%dbName%" copy /Y "%destinationFolder%\%dbBackupFolder%\%dbName%" "%destinationFolder%\%dbName%" >nul
exit /b 0
