@echo off
setlocal EnableExtensions

set "interactive=1"

:parseArgs
if "%~1"=="" goto argsDone
if /I "%~1"=="--silent" (
    set "interactive=0"
    shift
    goto parseArgs
)
if /I "%~1"=="--remove-all-data" (
    rem Deprecated: uninstall now always preserves backups/logs.
    shift
    goto parseArgs
)
echo Unknown argument: %~1
if "%interactive%"=="1" pause
exit /b 1

:argsDone

rem Check if the script is running as administrator.
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo Please run this script as administrator.
    if "%interactive%"=="1" pause
    exit /b 1
)

rem Define installation and service variables.
set "destinationFolder=C:\ProgramData\Praxiom"
set "service=Praxiom.App"
set "dbBackupFolder=_db_backup"
set "backupTaskName=Praxiom Online Backup"
set "legacyBackupTaskName=DottoressaIorio Online Backup"
set "oneDriveRoot=%OneDrive%"
set "oneDriveBackupFolder="
set "backupTimestamp="

if not "%oneDriveRoot%"=="" (
    if exist "%oneDriveRoot%\" (
        set "oneDriveBackupFolder=%oneDriveRoot%\Praxiom\_db_backup"
        for /f "usebackq delims=" %%T in (`powershell -NoProfile -Command "Get-Date -Format yyyyMMddHHmmss"`) do (
            if not "%%T"=="" set "backupTimestamp=%%T"
        )
    )
)

rem Remove scheduled backup task when present.
schtasks /Delete /TN "%backupTaskName%" /F >nul 2>nul
schtasks /Delete /TN "%legacyBackupTaskName%" /F >nul 2>nul

rem Stop and remove the Windows service when present.
sc query "%service%" >nul 2>nul
if %errorlevel% equ 0 (
    sc stop "%service%" >nul 2>nul
    timeout /t 3 /nobreak >nul

    set "servicePid=0"
    for /f "tokens=3" %%P in ('sc queryex "%service%" ^| findstr /R /C:"PID[ ]*:[ ]*[0-9][0-9]*"') do set "servicePid=%%P"
    if not "%servicePid%"=="0" taskkill /F /PID %servicePid% >nul 2>nul

    sc delete "%service%" >nul 2>nul
)

if exist "%destinationFolder%" (
    cd /d "%destinationFolder%"

    rem Keep SQLite database files for future reinstalls.
    if not exist "%dbBackupFolder%" mkdir "%dbBackupFolder%"
    call :backupDbFile "app.db"
    call :backupDbFile "app.db-shm"
    call :backupDbFile "app.db-wal"
    call :backupDbFile "app_unencrypted.db"
    call :backupDbFile "app_unencrypted.db-shm"
    call :backupDbFile "app_unencrypted.db-wal"

    rem Remove all top-level files (database backups and logs are directories).
    for /F "delims=" %%i in ('dir /B /A:-D "%destinationFolder%"') do (
        del /F /Q "%destinationFolder%\%%i" >nul 2>nul
    )

    rem Keep database backups and logs; remove all other directories.
    for /F "delims=" %%i in ('dir /B /A:D "%destinationFolder%" ^| findstr /V /I /X /C:"%dbBackupFolder%" ^| findstr /V /I /X /C:"logs"') do (
        rmdir /S /Q "%destinationFolder%\%%i" >nul 2>nul
    )
)

echo Uninstall completed.
if "%interactive%"=="1" pause
exit /b 0

:backupDbFile
set "dbName=%~1"
if exist "%destinationFolder%\%dbName%" (
    copy /Y "%destinationFolder%\%dbName%" "%destinationFolder%\%dbBackupFolder%\%dbName%" >nul
    if /I "%dbName%"=="app.db" call :copyBackupToOneDrive "%destinationFolder%\%dbBackupFolder%\%dbName%" "app_backup"
    if /I "%dbName%"=="app_unencrypted.db" call :copyBackupToOneDrive "%destinationFolder%\%dbBackupFolder%\%dbName%" "app_unencrypted_backup"
)
exit /b 0

:copyBackupToOneDrive
setlocal
set "sourceBackup=%~1"
set "targetPrefix=%~2"

if "%oneDriveBackupFolder%"=="" exit /b 0
if "%backupTimestamp%"=="" exit /b 0
if not exist "%sourceBackup%" exit /b 0

if not exist "%oneDriveBackupFolder%" mkdir "%oneDriveBackupFolder%" >nul 2>nul
if not exist "%oneDriveBackupFolder%" exit /b 0

copy /Y "%sourceBackup%" "%oneDriveBackupFolder%\%targetPrefix%_%backupTimestamp%.db" >nul 2>nul
exit /b 0
