@echo off
setlocal

rem Check if a parameter is provided
if "%1"=="" (
    echo No parameter provided.
    echo Usage: publish.bat "AllowedUsername"
    exit /b 1
)

rem Define the destination folder for publishing
set "destinationFolder=..\Setup\Service"
set "allowedUsername=%1"

rem Publish the .NET Core application to the destination folder
dotnet publish -c Release -o %destinationFolder%

rem Copy utility files to the destination folder
copy ..\Utility\*.* %destinationFolder%

rem Update the appsettings.Production.json file with the allowedUsername parameter
set "configFile=%destinationFolder%\appsettings.Production.json"
if exist "%configFile%" (
    echo Updating appsettings.Production.json...
    call :addAllowedUsername "%configFile%" "%allowedUsername%"
) else (
    echo appsettings.Production.json not found.
    exit /b 1
)

rem Navigate to the Installer folder
cd ..\Setup

rem Delete the existing Service.7z file
del "Service.7z"

rem Create a new compressed archive (zip) of the Service folder
7z a -tzip "Service.7z" ".\Service\*"

rem Remove the publish directory and its contents
::rmdir /s /q ".\Service"

exit /b 0

:addAllowedUsername
rem Parameters: %1 = configFile, %2 = allowedUsername
setlocal enabledelayedexpansion
set "newline="
for /f "delims=" %%i in ('type "%~1"') do (
    set "line=%%i"
    if "!line!"=="  }" (
        set "newline=,"
        echo !newline!>>"%~1.tmp"
        set  "newline=  "AllowedUsername": "%~2""
        echo !newline!>>"%~1.tmp"
    )
    echo %%i>>"%~1.tmp"
)
move /y "%~1.tmp" "%~1"
endlocal
exit /b 0
