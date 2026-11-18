# Annihilated-Mods

English | [简体中文](README.zh-CN.md)

[Downloads](#downloads) · [Latest release](https://github.com/BrickAZ/Annihilated-Mods/releases/latest)

Unofficial mods and offline Cup tools for Annihilated. Organize a Cup around a specific ending or boss, replay story scenes and browse CGs from the title screen, retry the current room with F9, switch attack modes, or expand the game's save slots.

## Download and install

The installers below are from **v1.0.4**. All five in-game mods were tested on Windows x64 with Steam Build `26705080` and MelonLoader 0.7.3. [Downloads](#downloads)

1. Close the game, extract the entire mod ZIP, and run `一键安装.cmd` (the one-click installer). If it can't find the game, select the folder containing `Annihilated.exe` or drag that folder onto the CMD file. In Steam, right-click the game and choose **Manage → Browse local files** to open the folder.
2. The installer checks the game files and verifies Cpp2IL and its plugin. It downloads MelonLoader 0.7.3 or portable .NET 6 if either is missing, and fetches fresh copies of any Cpp2IL files that are missing or fail verification. If the installer reports missing or invalid game files, verify the game's file integrity in Steam.
3. Start the game as usual for regular play. On the first launch, wait for MelonLoader to generate the interop assemblies and for the title screen to appear. For Cup runs, keep Steam open and use `Launch-Cup.cmd` in the game folder. The Cup ZIP includes the F9 plugin.

The save-slot expansion ZIP includes the patched `SaveSlotUnlimiter.dll`. When upgrading, the installer backs up any older copy of that DLL before replacing it. [SaveSlotUnlimiter guide in Chinese](SaveSlotUnlimiter/README.md)

## Repair Cpp2IL offline

Use the offline repair ZIP if Cpp2IL fails to download, its files are corrupt, or the startup log shows `Failed to Execute Cpp2IL`. The package targets Windows x64 and Unity 6000.3.23. It includes MelonLoader 0.7.3, portable .NET 6.0.36, Cpp2IL, and its plugin. [Downloads](#downloads)

1. Close the game, extract the entire repair ZIP, run `一键安装.cmd`, and select the game folder. The repair tool runs offline.
2. The tool reuses a valid Unity 6000.3.23 cache. If the cache is missing or damaged, it repairs the other prerequisites first. Download `Managed.zip` separately, put it next to the extracted `一键安装.cmd`, and run the installer again to import it offline. [Download Managed.zip for Unity 6000.3.23](https://github.com/LavaGang/MelonLoader.UnityDependencies/releases/download/6000.3.23/Managed.zip)

## The Cup and referee tool

Organizers choose a target ending or boss and set the map seed, battle mode, starting floor, and time limit. They can also set retry and death limits, time penalties, and optional rules. Cup runs use separate saves. When the timer stops, players submit a result JSON and a recording. The eight-character run ID pairs the two files, and the JSON includes an event timeline.

Referees can extract the Cup ZIP and open `Cup-Referee.html` in a browser to review results offline. Use the button in the top-right corner to switch between English and Simplified Chinese. Load the event rules, then import results in bulk, review rule differences and penalties, inspect event timelines, and export provisional rankings as a CSV. To try it out, apply the rules from the bundled `examples/example-ritual.json`, then import that same sample file as a result.

Cup v0.4.13 targets Steam Build `26705080`. It displays Chinese when the base game's last saved language is Chinese and English for other languages. Players can use either language in the same event when they share the same game build, Cup version, and rules. See `GUIDE.en.md` in the ZIP for rules and refereeing. For Build `26646212`, download Cup v0.4.11. [v0.4.13 installation and removal](CupMod/INSTALL.en.md) · [Older release](https://github.com/BrickAZ/Annihilated-Mods/releases/tag/v1.0.1)

## Other mods

- **Story Replay & CG Gallery**. Revisit 38 story entries or browse 591 original CGs from the title screen. Choose either protagonist appearance before opening a replay. Replays use separate saves. [Guide in Chinese](GalleryMod/使用说明.md)
- **F9 Room Retry**. The mod creates a checkpoint at the start of a new battle. Press F9 to return to the start of that fight. In the Cup, the event rules set the retry limit and time penalty. [Guide in Chinese](RoomRetryMod/使用说明.md)
- **Attack Mode Switch**. Open the game's settings during a regular battle to choose QTE or probability-based attacks. The choice applies from the next attack. [Guide in Chinese](AttackModeSwitchMod/使用说明.txt)
- **SaveSlotUnlimiter**. The patched v1.25 adds support for 300 slots in the game's save and load screens, including one autosave slot and 299 manual slots. [Guide in Chinese](SaveSlotUnlimiter/README.md)

## Build from source

Use the .NET 8 SDK and PowerShell 7. Install MelonLoader 0.7.3 in the game folder and launch the game once to generate `MelonLoader/Il2CppAssemblies/`. The build needs that folder and `MelonLoader/net6/`. The four projects target `net6.0`, so the SDK may need to fetch the targeting pack on the first build.

Clone this repository or download **Source code** from the release. Open PowerShell 7 in the folder containing `scripts/`, run the following commands, and enter the path to the folder containing `Annihilated.exe`. The path can contain spaces and use any drive letter.

```powershell
$gameRoot = (Read-Host 'Path to the game folder').Trim().Trim('"')
./scripts/Build-Releases.ps1 -GameRoot $gameRoot
```

The script builds four projects from the checked-out source and puts five installers and `SHA256SUMS.txt` in `dist/`. The SaveSlotUnlimiter package uses the patched DLL distributed with permission in this repository. To build just the Cup mod, set `$gameRoot` in the same PowerShell session and run this command.

```powershell
dotnet build ./CupMod/AnnihilatedCup.csproj -c Release "-p:GameRoot=$gameRoot"
```

These two logic checks run without the game installed.

```powershell
dotnet run --project ./CupMod/Checks/Checks.csproj
dotnet run --project ./RoomRetryMod/tests/RoomRetry.Tests.csproj
```

## Credits and dependencies

Brick made the Cup mod, gallery, F9 Room Retry, Attack Mode Switch, and referee tool. dsh created SaveSlotUnlimiter and gave Brick permission to distribute the adapted DLL. Death Kumiai developed Annihilated and Flashlight Games published it. The gallery and replays use indexes to load assets from your copy of the game. Builds and in-game mods rely on interop assemblies generated from the local installation. [Annihilated on Steam](https://store.steampowered.com/app/2638080/Annihilated/) · [MelonLoader](https://github.com/LavaGang/MelonLoader)

## Downloads

| Package | [v1.0.4 Release](https://github.com/BrickAZ/Annihilated-Mods/releases/tag/v1.0.4) |
| --- | --- |
| Cup | [v0.4.13 ZIP](https://github.com/BrickAZ/Annihilated-Mods/releases/download/v1.0.4/Annihilated-Cup-v0.4.13.zip) | |
| Story Replay & CG Gallery | [v1.2.1 ZIP](https://github.com/BrickAZ/Annihilated-Mods/releases/download/v1.0.4/Annihilated-Gallery-v1.2.1.zip) | |
| F9 Room Retry | [v1.0.1 ZIP](https://github.com/BrickAZ/Annihilated-Mods/releases/download/v1.0.4/Annihilated-RoomRetry-v1.0.1.zip) | |
| Attack Mode Switch | [v1.1.0 ZIP](https://github.com/BrickAZ/Annihilated-Mods/releases/download/v1.0.4/Annihilated-AttackModeSwitch-v1.1.0.zip) | |
| SaveSlotUnlimiter | [v1.25 ZIP](https://github.com/BrickAZ/Annihilated-Mods/releases/download/v1.0.4/Annihilated-SaveSlotUnlimiter-v1.25-Build26705080.zip) | |
| Offline Prerequisite Repair | [Windows x64 ZIP](https://github.com/BrickAZ/Annihilated-Mods/releases/download/v1.0.4/Annihilated-Offline-Repair-ML0.7.3-Windows-x64.zip) | |

[Mod package checksums](https://github.com/BrickAZ/Annihilated-Mods/releases/download/v1.0.4/SHA256SUMS.txt) · [Offline repair checksum](https://github.com/BrickAZ/Annihilated-Mods/releases/tag/v1.0.4#下载-downloads)
