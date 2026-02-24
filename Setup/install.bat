@echo off
setlocal EnableExtensions

set "interactive=1"
set "allowedUsername="
set "appUrl=http://localhost:5000"
set "scriptDir=%~dp0"
set "logFile=%~dp0install.log"
set "statusFile=%~dp0install.status"

> "%logFile%" echo [%date% %time%] Installer script started.
>> "%logFile%" echo [%date% %time%] Script path=%0
>> "%logFile%" echo [%date% %time%] Script dir=%scriptDir%
if exist "%statusFile%" del /q "%statusFile%" >nul 2>nul

:parseArgs
if "%~1"=="" goto argsDone
if /I "%~1"=="--silent" (
    set "interactive=0"
    shift
    goto parseArgs
)
if /I "%~1"=="--allowed-username" (
    if "%~2"=="" (
        echo Missing value for --allowed-username.
        if "%interactive%"=="1" pause
        exit /b 1
    )
    set "allowedUsername=%~2"
    shift
    shift
    goto parseArgs
)
if /I "%~1"=="--app-url" (
    if "%~2"=="" (
        echo Missing value for --app-url.
        if "%interactive%"=="1" pause
        exit /b 1
    )
    set "appUrl=%~2"
    shift
    shift
    goto parseArgs
)
echo Unknown argument: %~1
if "%interactive%"=="1" pause
exit /b 1

:argsDone
if "%allowedUsername%"=="" (
    >> "%logFile%" echo [%date% %time%] ERROR: Missing required parameter --allowed-username.
    echo Missing required parameter --allowed-username.
    if "%interactive%"=="1" pause
    exit /b 1
)
echo %appUrl% | findstr /I "://">nul
if errorlevel 1 set "appUrl=http://%appUrl%"
>> "%logFile%" echo [%date% %time%] Parsed arguments. interactive=%interactive% appUrl=%appUrl%
call :validateUrl "%appUrl%"
if errorlevel 1 (
    >> "%logFile%" echo [%date% %time%] ERROR: Invalid app URL "%appUrl%".
    echo Invalid service URL. Use format: http://hostname:port or https://hostname:port
    if "%interactive%"=="1" pause
    exit /b 1
)

rem Check if the script is running as administrator.
net session >nul 2>&1
if %errorlevel% neq 0 (
    >> "%logFile%" echo [%date% %time%] ERROR: Script is not running as administrator.
    echo Please run this script as administrator.
    if "%interactive%"=="1" pause
    exit /b 1
)

rem Change to the directory where the script is located.
cd /d "%scriptDir%"

rem Define source file, destination folder, and service-related variables.
set "sourceFile=%scriptDir%Service.7z"
set "sevenZipExe=%scriptDir%7z.exe"
set "destinationFolder=C:\Program Files\DottoressaIorio"
set "service=DottoressaIorio.App"
set "app=DottoressaIorio.App.exe"
set "dbBackupFolder=_db_backup"
set "configFile=%destinationFolder%\appsettings.Production.json"

rem Stop and remove existing service before touching files.
set "serviceDeleted=1"
sc query "%service%" >> "%logFile%" 2>&1
if %errorlevel% equ 0 (
    >> "%logFile%" echo [%date% %time%] Existing service found. Stopping and deleting...
    sc stop "%service%" >> "%logFile%" 2>&1
    timeout /t 2 /nobreak >nul
    sc delete "%service%" >> "%logFile%" 2>&1
    set "serviceDeleted=0"
)
if "%serviceDeleted%"=="0" (
    rem Wait until the service record is actually removed.
    for /L %%I in (1,1,10) do (
        sc query "%service%" >> "%logFile%" 2>&1
        if errorlevel 1 goto continueInstall
        timeout /t 1 /nobreak >nul
    )
    >> "%logFile%" echo [%date% %time%] WARNING: Service is still pending deletion.
    echo WARNING: Service "%service%" is still pending deletion.
)
:continueInstall

if not exist "%sourceFile%" (
    >> "%logFile%" echo [%date% %time%] ERROR: Missing package "%sourceFile%".
    echo Missing package: "%sourceFile%"
    if "%interactive%"=="1" pause
    exit /b 1
)

if not exist "%sevenZipExe%" (
    >> "%logFile%" echo [%date% %time%] ERROR: Missing extractor "%sevenZipExe%".
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
>> "%logFile%" echo [%date% %time%] Extracting package to "%destinationFolder%".
"%sevenZipExe%" x "%sourceFile%" -o"%destinationFolder%" *.* -r -y
if errorlevel 1 (
    >> "%logFile%" echo [%date% %time%] ERROR: Failed to extract package.
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

rem Apply installer wizard configuration to production settings.
if not exist "%configFile%" (
    >> "%logFile%" echo [%date% %time%] ERROR: Missing config file "%configFile%".
    echo Missing configuration file: "%configFile%"
    if "%interactive%"=="1" pause
    exit /b 1
)
call :setAllowedUsernameJson "%configFile%" "%allowedUsername%"
if errorlevel 1 (
    >> "%logFile%" echo [%date% %time%] ERROR: Failed to update appsettings.Production.json.
    echo Failed to update appsettings.Production.json.
    if "%interactive%"=="1" pause
    exit /b 1
)

rem Recreate the Windows service using native Service Control Manager.
>> "%logFile%" echo [%date% %time%] Creating service "%service%" with URL "%appUrl%".
sc create "%service%" binPath= "\"%destinationFolder%\%app%\" --urls \"%appUrl%\"" start= auto DisplayName= "Dottoressa Iorio App" >> "%logFile%" 2>&1
if errorlevel 1 (
    rem Retry once in case the service was still finishing deletion.
    >> "%logFile%" echo [%date% %time%] WARNING: First service creation attempt failed, retrying.
    timeout /t 2 /nobreak >nul
    sc create "%service%" binPath= "\"%destinationFolder%\%app%\" --urls \"%appUrl%\"" start= auto DisplayName= "Dottoressa Iorio App" >> "%logFile%" 2>&1
    if errorlevel 1 (
        >> "%logFile%" echo [%date% %time%] WARNING: Failed to create service. Files remain installed.
        > "%statusFile%" echo WARNING: Service creation failed.
        echo WARNING: Failed to create service "%service%".
        echo Application files were installed; service can be created manually.
        if "%interactive%"=="1" pause
        exit /b 0
    )
)

sc description "%service%" "Dottoressa Iorio medical management application" >> "%logFile%" 2>&1
sc start "%service%" >> "%logFile%" 2>&1
if errorlevel 1 (
    >> "%logFile%" echo [%date% %time%] WARNING: Service created but failed to start.
    call :logServiceDiagnostics
    > "%statusFile%" echo WARNING: Service start failed.
    echo WARNING: Service "%service%" was created but failed to start.
    echo You can inspect service logs and start it manually after installation.
    if "%interactive%"=="1" pause
    exit /b 0
)

if "%interactive%"=="1" start "" "%appUrl%"
> "%statusFile%" echo SUCCESS
>> "%logFile%" echo [%date% %time%] Installation completed successfully.
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

:setAllowedUsernameJson
setlocal
set "configFile=%~1"
set "allowedUsername=%~2"
set "CONFIG_FILE=%configFile%"
set "ALLOWED_USERNAME=%allowedUsername%"

powershell -NoProfile -Command "$ErrorActionPreference='Stop'; $p=$env:CONFIG_FILE; $u=$env:ALLOWED_USERNAME; $json=Get-Content -Raw -Path $p | ConvertFrom-Json; if($null -eq $json.Authentication){ $json | Add-Member -MemberType NoteProperty -Name Authentication -Value (@{}) }; $json.Authentication.AllowedUsername=$u; $json | ConvertTo-Json -Depth 32 | Set-Content -Encoding UTF8 -Path $p"
if errorlevel 1 (
    >> "%logFile%" echo [%date% %time%] ERROR: PowerShell config update failed.
    endlocal
    exit /b 1
)

endlocal
exit /b 0

:validateUrl
setlocal
set "urlToValidate=%~1"
set "URL_TO_VALIDATE=%urlToValidate%"
powershell -NoProfile -Command "$ErrorActionPreference='Stop'; $u=$env:URL_TO_VALIDATE; if($u -notmatch '^(https?)://[^/:\s]+:\d{1,5}$'){ exit 1 }; $port=[int]($u.Split(':')[-1]); if($port -lt 1 -or $port -gt 65535){ exit 1 } else { exit 0 }"
set "exitCode=%errorlevel%"
endlocal & exit /b %exitCode%

:logServiceDiagnostics
>> "%logFile%" echo [%date% %time%] Collecting service diagnostics...
sc query "%service%" >> "%logFile%" 2>&1
sc queryex "%service%" >> "%logFile%" 2>&1
set "DIAG_FILE=%scriptDir%service-diagnostics.tmp.log"
powershell -NoProfile -Command "$ErrorActionPreference='Continue'; try { Get-WinEvent -FilterHashtable @{LogName='Application'; StartTime=(Get-Date).AddMinutes(-15)} -ErrorAction Stop | Where-Object { $_.ProviderName -match 'ASP.NET|Application Error|.NET Runtime|IIS AspNetCore Module V2' -or $_.Message -match 'DottoressaIorio|DottoressaIorio.App.exe|Kestrel|address already in use|failed to start' } | Select-Object -First 20 TimeCreated, ProviderName, Id, LevelDisplayName, Message | Format-List | Out-String -Width 4096 | Set-Content -Encoding UTF8 -Path $env:DIAG_FILE } catch { $_ | Out-String | Set-Content -Encoding UTF8 -Path $env:DIAG_FILE }"
if exist "%scriptDir%service-diagnostics.tmp.log" (
    >> "%logFile%" type "%scriptDir%service-diagnostics.tmp.log"
    del /q "%scriptDir%service-diagnostics.tmp.log" >nul 2>nul
)
exit /b 0
