@echo off
setlocal
title DewDrop installer

REM Windows runs a .bat from inside a zip by extracting it to a temp folder, without the files next to it.
echo "%~dp0" | findstr /I /C:"%TEMP%" >nul 2>nul
if %ERRORLEVEL% EQU 0 (
    echo.
    echo  Oops! It looks like you opened this from inside the zip file.
    echo  Right-click the zip, choose "Extract All...", then open "Install on Windows.bat"
    echo  from the extracted folder.
    echo.
    pause
    exit /b 1
)

if not exist "%~dp0internal\install.ps1" (
    echo.
    echo  Oops! The installer is missing internal\install.ps1. Your antivirus may have removed it,
    echo  or only part of the zip was extracted. Download the installer again and extract all of it.
    echo.
    pause
    exit /b 1
)

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0internal\install.ps1" -FromLauncher %*
set "code=%ERRORLEVEL%"

REM install.ps1 exits with 0 (done) or 10 (stopped with a message, after waiting for Enter). Anything else means
REM PowerShell couldn't run it at all, so keep the window open long enough to read why.
if "%code%"=="0" exit /b 0
if "%code%"=="10" exit /b 10
echo.
echo  Oops! The installer couldn't start (error %code%). The reason may be shown above.
echo  Common causes: your antivirus blocked it, or your PC doesn't allow PowerShell scripts.
echo  For help, see https://github.com/friessssss/DewDrop/issues
echo.
pause
exit /b %code%
