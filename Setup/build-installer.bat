@echo off
setlocal EnableExtensions

if "%~1"=="" (
    echo Usage: build-installer.bat "AllowedUsername" [AppVersion]
    exit /b 1
)

set "allowedUsername=%~1"
set "appVersion=%~2"
if "%appVersion%"=="" set "appVersion=1.0.0"

set "scriptDir=%~dp0"
for %%I in ("%scriptDir%..") do set "repoDir=%%~fI"
set "publishScript=%repoDir%\DottoressaIorio.App\publish.bat"
set "issFile=%scriptDir%DottoressaIorioInstaller.iss"

if not exist "%publishScript%" (
    echo Missing publish script: "%publishScript%"
    exit /b 1
)

if not exist "%issFile%" (
    echo Missing installer script: "%issFile%"
    exit /b 1
)

where ISCC.exe >nul 2>nul
if %errorlevel% equ 0 (
    set "iscc=ISCC.exe"
) else (
    set "iscc=C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
)

if not exist "%iscc%" (
    echo Inno Setup compiler not found. Install Inno Setup 6 first.
    exit /b 1
)

call "%publishScript%" "%allowedUsername%"
if errorlevel 1 (
    echo Publish failed. Installer was not created.
    exit /b 1
)

"%iscc%" /DAppVersion=%appVersion% "%issFile%"
if errorlevel 1 (
    echo Installer build failed.
    exit /b 1
)

echo Installer build completed.
echo Output: "%scriptDir%Output\DottoressaIorioInstaller.exe"
exit /b 0
