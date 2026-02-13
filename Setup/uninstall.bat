@echo off
setlocal EnableExtensions

rem Check if the script is running as administrator.
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo Please run this script as administrator.
    pause
    exit /b 1
)

rem Define installation and service variables.
set "destinationFolder=C:\Program Files\DottoressaIorio"
set "service=DottoressaIorio.App"
set "dbBackupFolder=_db_backup"
set "dbFile=app.db"

rem Stop and remove the Windows service when present.
sc query "%service%" >nul 2>nul
if %errorlevel% equ 0 (
    sc stop "%service%" >nul 2>nul
    timeout /t 2 /nobreak >nul
    sc delete "%service%" >nul 2>nul
)

if exist "%destinationFolder%" (
    cd /d "%destinationFolder%"

    rem Keep app.db for future reinstalls.
    if not exist "%dbBackupFolder%" mkdir "%dbBackupFolder%"
    if exist "%dbFile%" move /Y "%dbFile%" "%dbBackupFolder%\%dbFile%" >nul

    rem Remove all files and directories except the database backup folder.
    for /F "delims=" %%i in ('dir /B /A:-D "%destinationFolder%" ^| findstr /V /I /C:"%dbBackupFolder%"') do (
        del /Q "%destinationFolder%\%%i"
    )

    for /F "delims=" %%i in ('dir /B /AD "%destinationFolder%" ^| findstr /V /I /C:"%dbBackupFolder%"') do (
        rmdir /S /Q "%destinationFolder%\%%i"
    )
)

echo Uninstall completed.
pause
exit /b 0
