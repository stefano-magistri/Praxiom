@echo off
setlocal EnableExtensions

rem Define publish and packaging paths.
set "scriptDir=%~dp0"
for %%I in ("%scriptDir%..\Setup") do set "setupDir=%%~fI"
for %%I in ("%scriptDir%DottoressaIorio.App.csproj") do set "appProject=%%~fI"
set "tmpDir=%setupDir%\tmp"
set "destinationFolder=%tmpDir%\Service"
set "archiveFile=%tmpDir%\Service.7z"
set "archiveTemp=%tmpDir%\Service.%RANDOM%%RANDOM%.7z"
set "sevenZipExe=%setupDir%\7z.exe"
set "versionFile=%tmpDir%\Service.version"
set "publishedExe=%destinationFolder%\DottoressaIorio.App.exe"

if not exist "%sevenZipExe%" (
    echo Missing archive tool: "%sevenZipExe%"
    exit /b 1
)

if not exist "%tmpDir%" mkdir "%tmpDir%"
if exist "%destinationFolder%" rmdir /s /q "%destinationFolder%"

rem Publish a self-contained Windows package so runtime install is not required.
dotnet publish "%appProject%" -c Release -r win-x64 --self-contained true -o "%destinationFolder%"
if errorlevel 1 (
    echo Publish failed.
    exit /b 1
)

if not exist "%publishedExe%" (
    echo Publish completed but executable was not found: "%publishedExe%"
    exit /b 1
)

for /f "usebackq delims=" %%V in (`powershell -NoProfile -Command "(Get-Item '%publishedExe%').VersionInfo.FileVersion"`) do (
    if not "%%V"=="" set "appVersion=%%V"
)
if "%appVersion%"=="" (
    echo Failed to read file version from "%publishedExe%"
    exit /b 1
)

> "%versionFile%" echo %appVersion%

rem Create a new compressed archive (.7z) in a unique temp name first.
if exist "%archiveTemp%" del /q "%archiveTemp%"
"%sevenZipExe%" a -t7z "%archiveTemp%" "%destinationFolder%\*"
if errorlevel 1 (
    echo Failed to create Service.7z archive.
    if exist "%archiveTemp%" del /q "%archiveTemp%"
    exit /b 1
)

rem Replace target archive with retries in case another process briefly holds a lock.
set "replaced=0"
for /L %%I in (1,1,10) do (
    if exist "%archiveFile%" del /q "%archiveFile%" >nul 2>nul
    move /Y "%archiveTemp%" "%archiveFile%" >nul 2>nul
    if exist "%archiveFile%" (
        set "replaced=1"
        goto archiveDone
    )
    timeout /t 1 /nobreak >nul
)

:archiveDone
if not "%replaced%"=="1" (
    echo Failed to replace Service.7z archive. Ensure no process is locking "%archiveFile%".
    if exist "%archiveTemp%" del /q "%archiveTemp%" >nul 2>nul
    exit /b 1
)

rem Remove the publish directory and its contents
rmdir /s /q "%destinationFolder%"

exit /b 0
