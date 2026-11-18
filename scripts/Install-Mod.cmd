@echo off
setlocal DisableDelayedExpansion
set "PSModulePath="
set "WINPS=%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe"
if "%~1"=="" (
    "%WINPS%" -NoProfile -STA -ExecutionPolicy Bypass -File "%~dp0Install-Mod.ps1"
) else (
    "%WINPS%" -NoProfile -STA -ExecutionPolicy Bypass -File "%~dp0Install-Mod.ps1" -GameDirectory "%~1"
)
set "INSTALL_EXIT=%ERRORLEVEL%"
if not "%ANNIHILATED_INSTALL_NO_PAUSE%"=="1" pause
exit /b %INSTALL_EXIT%
