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

rem Change to the directory where the script is located
cd /d "%~dp0"

rem Define source file, destination folder, and service-related variables
set "sourceFile=Service.7z"
set "destinationFolder=C:\Program Files\DottoressaIorio\"
set "service=DottoressaIorio.App"
set "app=DottoressaIorio.App.exe"
set "dbBackupFolder=_db_backup"


rem Create the destination folder if it does not exist
if not exist "%destinationFolder%" mkdir "%destinationFolder%"

rem Extract files from the 7z archive to the destination folder
7z x %sourceFile% -o"%destinationFolder%" *.* -r -y

rem Check if winget is available
where winget >nul 2>nul
if %errorlevel% neq 0 (
    echo "Winget is not installed. Skipping package installation."
    goto :afterWinget
)

rem Install .NET 6 Runtime using winget
echo Installing .NET 6 Runtime using winget...
echo y | winget install Microsoft.DotNet.Runtime.6

rem Install .NET 6 AspNetCore using winget
echo Installing .NET 6 AspNetCore using winget...
echo y | winget install Microsoft.DotNet.AspNetCore.6

rem End of script
:afterWinget

rem Navigate to the destination folder
cd "%destinationFolder%"

rem Move files from _db_backup to the destination folder
if exist "%dbBackupFolder%" (
    move /Y "%dbBackupFolder%\*" "%destinationFolder%"
)

rem Install the service using NSSM
nssm install %service% "%destinationFolder%\%app%"

rem Start the installed service using NSSM
nssm start %service%
 
rem Open the default browser to the application's URL
start http://localhost:5000

pause
