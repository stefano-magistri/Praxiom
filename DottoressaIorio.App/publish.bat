@echo off
setlocal EnableExtensions

rem Check if a parameter is provided
if "%1"=="" (
    echo No parameter provided.
    echo Usage: publish.bat "AllowedUsername"
    exit /b 1
)

rem Define publish and packaging paths.
set "scriptDir=%~dp0"
for %%I in ("%scriptDir%..\Setup") do set "setupDir=%%~fI"
for %%I in ("%scriptDir%DottoressaIorio.App.csproj") do set "appProject=%%~fI"
set "destinationFolder=%setupDir%\Service"
set "archiveFile=%setupDir%\Service.7z"
set "sevenZipExe=%setupDir%\7z.exe"
set "allowedUsername=%~1"

if not exist "%sevenZipExe%" (
    echo Missing archive tool: "%sevenZipExe%"
    exit /b 1
)

if exist "%destinationFolder%" rmdir /s /q "%destinationFolder%"

rem Publish a self-contained Windows package so runtime install is not required.
dotnet publish "%appProject%" -c Release -r win-x64 --self-contained true -o "%destinationFolder%"
if errorlevel 1 (
    echo Publish failed.
    exit /b 1
)

rem Update the appsettings.Production.json file with the allowedUsername parameter
set "configFile=%destinationFolder%\appsettings.Production.json"
if exist "%configFile%" (
    echo Updating appsettings.Production.json...
    call :setAllowedUsernameJson "%configFile%" "%allowedUsername%"
    if errorlevel 1 (
        echo Failed to update appsettings.Production.json.
        exit /b 1
    )
) else (
    echo appsettings.Production.json not found.
    exit /b 1
)

rem Delete the existing Service.7z file
if exist "%archiveFile%" del /q "%archiveFile%"

rem Create a new compressed archive (.7z) of the Service folder.
"%sevenZipExe%" a -t7z "%archiveFile%" "%destinationFolder%\*"
if errorlevel 1 (
    echo Failed to create Service.7z archive.
    exit /b 1
)

rem Remove the publish directory and its contents
rmdir /s /q "%destinationFolder%"

exit /b 0

:setAllowedUsernameJson
setlocal
set "configFile=%~1"
set "allowedUsername=%~2"
set "CONFIG_FILE=%configFile%"
set "ALLOWED_USERNAME=%allowedUsername%"

powershell -NoProfile -Command "$ErrorActionPreference='Stop'; $p=$env:CONFIG_FILE; $u=($env:ALLOWED_USERNAME -replace '\\+','\'); $json=Get-Content -Raw -Path $p; $uQuoted=($u | ConvertTo-Json -Compress); $uEscaped=$uQuoted.Substring(1,$uQuoted.Length-2); $json=$json.Replace('[AllowedUsername]',$uEscaped); Set-Content -Encoding UTF8 -Path $p -Value $json"
if errorlevel 1 (
    endlocal
    exit /b 1
)

endlocal
exit /b 0
