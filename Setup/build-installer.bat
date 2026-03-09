@echo off
setlocal EnableExtensions

set "appVersion=%~1"
if not "%~2"=="" (
    rem Backward compatibility: old command format was "AllowedUsername" [AppVersion].
    set "appVersion=%~2"
)

set "scriptDir=%~dp0"
for %%I in ("%scriptDir%..") do set "repoDir=%%~fI"
set "publishScript=%repoDir%\DottoressaIorio.App\publish.bat"
set "licenseToolProject=%repoDir%\DottoressaIorio.LicenseGenerator\DottoressaIorio.LicenseGenerator.csproj"
set "issFile=%scriptDir%DottoressaIorioInstaller.iss"
set "outputFile=%scriptDir%Output\DottoressaIorioInstaller.exe"
set "tmpDir=%scriptDir%tmp"
set "tmpArchive=%tmpDir%\Service.7z"
set "validatorDir=%tmpDir%\LicenseValidator"
set "validatorExe=%validatorDir%\DottoressaIorio.LicenseGenerator.exe"
set "versionFile=%tmpDir%\Service.version"

if not exist "%publishScript%" (
    echo Missing publish script: "%publishScript%"
    exit /b 1
)

if not exist "%issFile%" (
    echo Missing installer script: "%issFile%"
    exit /b 1
)

if not exist "%licenseToolProject%" (
    echo Missing license tool project: "%licenseToolProject%"
    exit /b 1
)

where ISCC.exe >nul 2>nul
if %errorlevel% equ 0 (
    set "iscc=ISCC.exe"
) else (
    set "iscc=C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
)

if /I "%iscc%"=="ISCC.exe" (
    where ISCC.exe >nul 2>nul
    if errorlevel 1 (
        echo Inno Setup compiler not found in PATH.
        exit /b 1
    )
) else (
    if not exist "%iscc%" (
        echo Inno Setup compiler not found at "%iscc%".
        exit /b 1
    )
)

call "%publishScript%"
if errorlevel 1 (
    echo Publish failed. Installer was not created.
    exit /b 1
)

if exist "%validatorDir%" rmdir /s /q "%validatorDir%"
dotnet publish "%licenseToolProject%" -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true -o "%validatorDir%"
if errorlevel 1 (
    echo License validator publish failed. Installer was not created.
    exit /b 1
)

if not exist "%validatorExe%" (
    echo License validator publish completed but executable was not found: "%validatorExe%"
    exit /b 1
)

if not exist "%tmpArchive%" (
    echo Publish completed but staging archive was not found: "%tmpArchive%"
    exit /b 1
)

if "%appVersion%"=="" (
    if not exist "%versionFile%" (
        echo Publish completed but version file was not found: "%versionFile%"
        exit /b 1
    )

    set /p appVersion=<"%versionFile%"
)

if "%appVersion%"=="" (
    echo Unable to determine app version.
    exit /b 1
)

"%iscc%" /DAppVersion=%appVersion% "%issFile%"
if errorlevel 1 (
    echo Installer build failed.
    exit /b 1
)

if not exist "%outputFile%" (
    echo Installer build completed but output file was not found: "%outputFile%"
    exit /b 1
)

rem Cleanup temporary staging artifacts after successful installer build.
if exist "%tmpArchive%" del /q "%tmpArchive%"
if exist "%versionFile%" del /q "%versionFile%"
if exist "%validatorDir%" rmdir /s /q "%validatorDir%"
if exist "%tmpDir%" rd "%tmpDir%" 2>nul

echo Installer build completed.
echo Output: "%outputFile%"
exit /b 0
