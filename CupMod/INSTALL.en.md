# Annihilated Cup v0.5.5 installation guide

English | [简体中文](INSTALL.md)

For Windows x64 and Steam Build `26845264`, with MelonLoader 0.7.3. The game folder contains `Annihilated.exe`. To find it, right-click the game in your Steam library and choose **Manage → Browse local files**.

[Download Annihilated-Cup-Build26845264-Online.zip](https://github.com/BrickAZ/Annihilated-Mods/releases/download/v1.0.6/Annihilated-Cup-Build26845264-Online.zip) · [SHA-256 checksum](https://github.com/BrickAZ/Annihilated-Mods/releases/download/v1.0.6/SHA256SUMS.txt)

## Install

1. Close the game and extract the entire ZIP. Double-click `Install.cmd` or `一键安装.cmd`. Both open the same installer. If it can't find the game, select the game folder or drag that folder onto the CMD file.
2. The installer checks your game files and installs the Cup mod, Room Retry v1.0.1 for F9 retries, and `Launch-Cup.cmd`. It downloads and verifies MelonLoader 0.7.3 and portable .NET 6.0.36 when needed. If Cpp2IL or its plugin is missing or fails verification, the installer downloads a fresh copy from the official source.
3. If the installer reports missing or invalid game files, verify the game's file integrity in Steam and run the installer again. If Cpp2IL fails to download or the startup log reports `Failed to Execute Cpp2IL`, use the offline repair package. [Offline repair steps](https://github.com/BrickAZ/Annihilated-Mods#repair-cpp2il-offline)

## Launch the Cup

1. Keep Steam open and double-click `Launch-Cup.cmd` in the game folder. On the first launch with MelonLoader, wait for the title screen. For regular play, launch the game from Steam as usual.
2. Open **Cup Setup** from the title screen and start the run with the organizer's rules. The Cup uses the language last saved in the base game. It displays Chinese for Chinese settings and English for other languages. To switch, change the language during a regular game session, exit, and run `Launch-Cup.cmd` again.
3. When the timer stops, select **Export Cup Result** below the Cup HUD. Submit the JSON from `UserData/AnnihilatedCup/exports` and your recording together. Use the eight-character run code to match the files. For the full rules, see `GUIDE.en.md` or `README.md` inside the ZIP.

If **Cup Setup** doesn't appear, confirm that you launched with `Launch-Cup.cmd` and check for `Mods/AnnihilatedCup.dll` in the game folder. Look in `MelonLoader/Latest.log` for startup errors.

## Open the referee tool

Extract the entire Cup ZIP and open `Cup-Referee.html` in a browser. It runs offline. Use the button in the top-right corner to switch between English and Simplified Chinese. Players in the same event should use the same Cup version, game build, and rules. The referee tool can import results from either language. See `GUIDE.en.md` and `REFEREE.md` in the ZIP for the review workflow.

## Uninstall

Close the game and remove `Mods/AnnihilatedCup.dll` and `Launch-Cup.cmd` from the game folder. Keep `Mods/AnnihilatedRoomRetry.dll` if you use standalone F9 retries. You can back up Cup settings and past results from `UserData/AnnihilatedCup`.
