@echo off
setlocal
cd /d "%~dp0"

if not exist "Annihilated.exe" (
    echo Place Launch-Cup.cmd in the folder containing Annihilated.exe.
    pause
    exit /b 1
)
if not exist "MelonLoader\net6\MelonLoader.dll" (
    echo Install MelonLoader 0.7.3 before starting the cup.
    pause
    exit /b 1
)
if not exist "Mods\AnnihilatedCup.dll" (
    echo Install Mods\AnnihilatedCup.dll before starting the cup.
    pause
    exit /b 1
)
powershell.exe -NoProfile -NonInteractive -Command "if (Get-Process -Name Annihilated -ErrorAction SilentlyContinue) { exit 0 } else { exit 1 }"
if not errorlevel 1 (
    echo Close the running game before starting the cup.
    pause
    exit /b 1
)

if exist "MelonLoader\Dependencies\dotnet\dotnet.exe" (
    set "DOTNET_ROOT=%~dp0MelonLoader\Dependencies\dotnet"
    set "DOTNET_ROLL_FORWARD=Major"
)
start "" "%~dp0Annihilated.exe" --annihilated-cup --melonloader.hideconsole
