@echo off
:: Check if the script is run as administrator
>nul 2>&1 "%SYSTEMROOT%\System32\cacls.exe" "%SYSTEMROOT%\System32\config\system" && (
    goto :runCommand
) || (
    echo Please run this script as an administrator.
    echo Exiting...    
    pause
    exit /b
)

:runCommand

setlocal 

rem Define the installation directory and service name
set "destinationFolder=C:\Program Files\DottoressaIorio\"
set "service=DottoressaIorio.App"
set "dbbackup=_db_backup"

rem Navigate to the destination folder
cd "%destinationFolder%"

rem Stop the service using NSSM
nssm stop "%service%"
 
rem Remove the service using NSSM (confirming the removal)
nssm remove "%service%" confirm

rem Create the destination folder if it does not exist
if not exist "%dbbackup%" mkdir "%dbbackup%"

rem Move the app.db file to _db_backup
move "app.db" "%dbbackup%\"

rem Remove all files and subdirectories in the installation directory except _db_backup
for /F "delims=" %%i in ('dir /B /A:-D "%destinationFolder%" ^| findstr /V /C:"%dbbackup%"') do (
    del /Q "%destinationFolder%\%%i"
)

for /F "delims=" %%i in ('dir /B /AD "%destinationFolder%" ^| findstr /V /C:"%dbbackup%"') do (
    rmdir /S /Q "%destinationFolder%\%%i"
)

cd /d "%~dp0"

pause
exit /b
