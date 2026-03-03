@echo off
setlocal EnableExtensions

set "interactive=1"
set "removeAllData=0"

:parseArgs
if "%~1"=="" goto argsDone
if /I "%~1"=="--silent" (
    set "interactive=0"
    shift
    goto parseArgs
)
if /I "%~1"=="--remove-all-data" (
    set "removeAllData=1"
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
set "destinationFolder=C:\ProgramData\DottoressaIorio"
set "service=DottoressaIorio.App"
set "dbBackupFolder=_db_backup"
set "backupTaskName=DottoressaIorio Online Backup"

rem Remove scheduled backup task when present.
schtasks /Delete /TN "%backupTaskName%" /F >nul 2>nul

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

    if "%removeAllData%"=="0" (
        rem Keep SQLite database files for future reinstalls.
        if not exist "%dbBackupFolder%" mkdir "%dbBackupFolder%"
        call :backupDbFile "app.db"
        call :backupDbFile "app.db-shm"
        call :backupDbFile "app.db-wal"
        call :backupDbFile "app_unencrypted.db"
        call :backupDbFile "app_unencrypted.db-shm"
        call :backupDbFile "app_unencrypted.db-wal"
    )

    rem Remove all files except the database backup directory.
    for /F "delims=" %%i in ('dir /B /A:-D "%destinationFolder%"') do (
        del /F /Q "%destinationFolder%\%%i" >nul 2>nul
    )

    rem Remove all directories except the database backup directory.
    if "%removeAllData%"=="0" (
        for /F "delims=" %%i in ('dir /B /A:D "%destinationFolder%" ^| findstr /V /I /X /C:"%dbBackupFolder%"') do (
            rmdir /S /Q "%destinationFolder%\%%i" >nul 2>nul
        )
    ) else (
        for /F "delims=" %%i in ('dir /B /A:D "%destinationFolder%"') do (
            attrib -R -S -H "%destinationFolder%\%%i" /S /D >nul 2>nul
            rmdir /S /Q "%destinationFolder%\%%i" >nul 2>nul
        )
        rem Ensure backup folder is removed even if it was recreated or had protected attributes.
        if exist "%destinationFolder%\%dbBackupFolder%" (
            attrib -R -S -H "%destinationFolder%\%dbBackupFolder%" /S /D >nul 2>nul
            rmdir /S /Q "%destinationFolder%\%dbBackupFolder%" >nul 2>nul
        )
        rem Remove application root when all data deletion is explicitly requested.
        cd /d "%ProgramData%"
        attrib -R -S -H "%destinationFolder%" /S /D >nul 2>nul
        rmdir /S /Q "%destinationFolder%" >nul 2>nul
    )
)

echo Uninstall completed.
if "%interactive%"=="1" pause
exit /b 0

:backupDbFile
set "dbName=%~1"
if exist "%destinationFolder%\%dbName%" copy /Y "%destinationFolder%\%dbName%" "%destinationFolder%\%dbBackupFolder%\%dbName%" >nul
exit /b 0
