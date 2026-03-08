@echo off
setlocal EnableExtensions

set "interactive=1"
set "allowedUsername="
set "appUrl=http://localhost:5000"
set "licenseFile="
set "skipAutoDbRestore=0"
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
if /I "%~1"=="--license-file" (
    if "%~2"=="" (
        echo Missing value for --license-file.
        if "%interactive%"=="1" pause
        exit /b 1
    )
    set "licenseFile=%~2"
    shift
    shift
    goto parseArgs
)
if /I "%~1"=="--skip-auto-db-restore" (
    set "skipAutoDbRestore=1"
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
if "%licenseFile%"=="" (
    >> "%logFile%" echo [%date% %time%] ERROR: Missing required parameter --license-file.
    echo Missing required parameter --license-file.
    if "%interactive%"=="1" pause
    exit /b 1
)
if not exist "%licenseFile%" (
    >> "%logFile%" echo [%date% %time%] ERROR: License file "%licenseFile%" not found.
    echo License file not found: "%licenseFile%"
    if "%interactive%"=="1" pause
    exit /b 1
)
echo %appUrl% | findstr /I "://">nul
if errorlevel 1 set "appUrl=http://%appUrl%"
>> "%logFile%" echo [%date% %time%] Parsed arguments. interactive=%interactive% appUrl=%appUrl% skipAutoDbRestore=%skipAutoDbRestore% licenseFile=%licenseFile%
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
set "destinationFolder=C:\ProgramData\DottoressaIorio"
set "licenseDestination=%destinationFolder%\license.dat"
set "service=DottoressaIorio.App"
set "app=DottoressaIorio.App.exe"
set "dbBackupFolder=_db_backup"
set "legacyBackupDb=C:\Program Files\DottoressaIorio\_db_backup\app.db"
set "configFile=%destinationFolder%\appsettings.Production.json"
set "backupScriptSource=%scriptDir%backup_script.ps1"
set "backupScriptDest=%destinationFolder%\backup_script.ps1"
set "restoreScriptSource=%scriptDir%restore_script.ps1"
set "restoreScriptDest=%destinationFolder%\restore_script.ps1"
set "backupTaskName=DottoressaIorio Online Backup"
set "legacyBackupTaskName=DottoressaIorio Daily Backup"
set "backupTaskUser=%USERDOMAIN%\%USERNAME%"

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

copy /Y "%licenseFile%" "%licenseDestination%" >nul
if errorlevel 1 (
    >> "%logFile%" echo [%date% %time%] ERROR: Failed to copy license file to "%licenseDestination%".
    echo Failed to copy license file to "%licenseDestination%".
    if "%interactive%"=="1" pause
    exit /b 1
)
>> "%logFile%" echo [%date% %time%] License file copied to "%licenseDestination%".

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
if "%skipAutoDbRestore%"=="1" (
    >> "%logFile%" echo [%date% %time%] Automatic database restore from local backup skipped by installer option.
) else (
    call :restoreDbFile "app.db"
    call :restoreDbFile "app.db-shm"
    call :restoreDbFile "app.db-wal"
    call :restoreDbFile "app_unencrypted.db"
    call :restoreDbFile "app_unencrypted.db-shm"
    call :restoreDbFile "app_unencrypted.db-wal"
)

if exist "%backupScriptSource%" (
    copy /Y "%backupScriptSource%" "%backupScriptDest%" >nul
    if errorlevel 1 (
        >> "%logFile%" echo [%date% %time%] WARNING: Failed to copy manual cold-copy fallback script to "%backupScriptDest%".
    ) else (
        >> "%logFile%" echo [%date% %time%] Manual cold-copy fallback script copied to "%backupScriptDest%".
    )
) else (
    >> "%logFile%" echo [%date% %time%] WARNING: Manual cold-copy fallback script not found at "%backupScriptSource%".
)

if exist "%restoreScriptSource%" (
    copy /Y "%restoreScriptSource%" "%restoreScriptDest%" >nul
    if errorlevel 1 (
        >> "%logFile%" echo [%date% %time%] WARNING: Failed to copy restore script to "%restoreScriptDest%".
    ) else (
        >> "%logFile%" echo [%date% %time%] Restore script copied to "%restoreScriptDest%".
    )
) else (
    >> "%logFile%" echo [%date% %time%] WARNING: Restore script not found at "%restoreScriptSource%".
)

rem Configure and install Playwright browser binaries in ProgramData for service account usage.
set "PLAYWRIGHT_BROWSERS_PATH=C:\ProgramData\DottoressaIorio\ms-playwright"
if not exist "%PLAYWRIGHT_BROWSERS_PATH%" mkdir "%PLAYWRIGHT_BROWSERS_PATH%"
setx /M PLAYWRIGHT_BROWSERS_PATH "%PLAYWRIGHT_BROWSERS_PATH%" >nul 2>nul
if exist "%destinationFolder%\playwright.ps1" (
    >> "%logFile%" echo [%date% %time%] Installing Playwright chromium to "%PLAYWRIGHT_BROWSERS_PATH%".
    powershell -NoProfile -ExecutionPolicy Bypass -File "%destinationFolder%\playwright.ps1" install chromium >> "%logFile%" 2>&1
    if errorlevel 1 (
        >> "%logFile%" echo [%date% %time%] WARNING: Playwright browser installation failed.
    )
) else (
    >> "%logFile%" echo [%date% %time%] WARNING: Playwright install script not found at "%destinationFolder%\playwright.ps1".
)

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
        >> "%logFile%" echo [%date% %time%] ERROR: Failed to create service after retry.
        > "%statusFile%" echo ERROR: Service creation failed.
        echo ERROR: Failed to create service "%service%". Check "%logFile%" for details.
        if "%interactive%"=="1" pause
        exit /b 1
    )
)

sc description "%service%" "Dottoressa Iorio medical management application" >> "%logFile%" 2>&1
sc start "%service%" >> "%logFile%" 2>&1
if errorlevel 1 (
    >> "%logFile%" echo [%date% %time%] ERROR: Service start command failed.
    call :logServiceDiagnostics
    > "%statusFile%" echo ERROR: Service start failed.
    echo ERROR: Service "%service%" failed to start. Check "%logFile%" for details.
    if "%interactive%"=="1" pause
    exit /b 1
)

call :waitForServiceRunning
if errorlevel 1 (
    >> "%logFile%" echo [%date% %time%] ERROR: Service did not reach RUNNING state.
    call :logServiceDiagnostics
    > "%statusFile%" echo ERROR: Service not running after start.
    echo ERROR: Service "%service%" did not reach RUNNING state. Check "%logFile%" for details.
    if "%interactive%"=="1" pause
    exit /b 1
)

if exist "%destinationFolder%\%app%" (
    >> "%logFile%" echo [%date% %time%] Configuring scheduled ONLINE backup task "%backupTaskName%" for user "%backupTaskUser%".
    schtasks /Delete /TN "%legacyBackupTaskName%" /F >> "%logFile%" 2>&1
    schtasks /Delete /TN "%backupTaskName%" /F >> "%logFile%" 2>&1
    schtasks /Create /TN "%backupTaskName%" /TR "\"%destinationFolder%\%app%\" --online-backup" /SC HOURLY /MO 2 /ST 00:00 /RU "%backupTaskUser%" /RL HIGHEST /IT /F >> "%logFile%" 2>&1
    if errorlevel 1 (
        >> "%logFile%" echo [%date% %time%] WARNING: Failed to create scheduled online backup task.
        echo WARNING: Could not create scheduled backup task "%backupTaskName%".
    ) else (
        >> "%logFile%" echo [%date% %time%] Scheduled online backup task created successfully.
        >> "%logFile%" echo [%date% %time%] NOTE: "%backupScriptDest%" is retained only for manual cold-copy fallback.
    )
) else (
    >> "%logFile%" echo [%date% %time%] WARNING: Skipping scheduled task creation because app executable is missing.
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

:waitForServiceRunning
for /L %%I in (1,1,20) do (
    sc query "%service%" | findstr /R /C:"STATE[ ]*:[ ]*[0-9][0-9]*[ ]*RUNNING" >nul
    if not errorlevel 1 exit /b 0
    timeout /t 1 /nobreak >nul
)
exit /b 1
