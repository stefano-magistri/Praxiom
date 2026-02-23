@echo off
setlocal EnableExtensions

rem Define publish and packaging paths.
set "scriptDir=%~dp0"
for %%I in ("%scriptDir%..\Setup") do set "setupDir=%%~fI"
for %%I in ("%scriptDir%DottoressaIorio.App.csproj") do set "appProject=%%~fI"
set "tmpDir=%setupDir%\tmp"
set "destinationFolder=%setupDir%\Service"
set "archiveFile=%tmpDir%\Service.7z"
set "sevenZipExe=%setupDir%\7z.exe"

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
