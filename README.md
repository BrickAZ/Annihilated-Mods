# Annihilated-Mods

English | [简体中文](README.zh-CN.md)

Unofficial mods for Annihilated. See what an enemy will do next, retry a fight with F9, or revisit story scenes and CGs. Set up tournaments with the Cup mod and review runs in the offline referee tool.

[Downloads](#downloads) · [Install](#download-and-install) · [Cup and referee tool](#the-cup-and-referee-tool) · [Offline repair](#repair-cpp2il-offline) · [Releases](https://github.com/BrickAZ/Annihilated-Mods/releases)

## Downloads

The [v1.0.6](https://github.com/BrickAZ/Annihilated-Mods/releases/tag/v1.0.6) game installers target Windows x64, Steam Build `26845264`, and MelonLoader 0.7.3. Each mod has its own package.

### Everyday play

| What you want to do | Download | Guide |
| --- | --- | --- |
| See the next attack's damage per hit and hit count, or hover to see available moves | [Enemy Intent Display v0.2.0](https://github.com/BrickAZ/Annihilated-Mods/releases/download/v1.0.6/Annihilated-EnemyIntent-Build26845264-Online.zip) | [Guide](EnemyIntentMod/README.md) |
| Press F9 to restart the current fight | [F9 Room Retry](https://github.com/BrickAZ/Annihilated-Mods/releases/download/v1.0.6/Annihilated-RoomRetry-Build26845264-Online.zip) | [Chinese guide](RoomRetryMod/使用说明.md) |
| Revisit 38 story entries or browse 597 CGs from the title screen | [Story Replay & CG Gallery v1.2.2](https://github.com/BrickAZ/Annihilated-Mods/releases/download/v1.0.6/Annihilated-Gallery-Build26845264-Online.zip) | [Chinese guide](GalleryMod/使用说明.md) |
| Use the settings menu to switch between QTE and probability-based attacks during a fight. The change applies on the next attack | [Attack Mode Switch](https://github.com/BrickAZ/Annihilated-Mods/releases/download/v1.0.6/Annihilated-AttackModeSwitch-Build26845264-Online.zip) | [Chinese guide](AttackModeSwitchMod/使用说明.txt) |
| Expand to 300 slots, with one autosave and 299 manual slots | [SaveSlotUnlimiter v1.25](https://github.com/BrickAZ/Annihilated-Mods/releases/download/v1.0.6/Annihilated-SaveSlotUnlimiter-Build26845264-Online.zip) | [Chinese guide](SaveSlotUnlimiter/README.md) |
| Get an extra weapon fusion option at bonfires when you have the right materials | [Standalone Bonfire Fusion](https://github.com/BrickAZ/Annihilated-Mods/releases/download/v1.0.6/Annihilated-BonfireFusion-Build26845264-Online.zip) | In the ZIP |

Story replays use separate saves and let you choose the protagonist's appearance first. SaveSlotUnlimiter also supports Build `26705080`. Its installer backs up the previous DLL when upgrading.

### Playable Rivals

Play as the Outcast, Villainess, or Dropout in the Hero, Warrior, and Mage companion slots, with signature weapons, floor-unlocked skills, portraits, and battle art. Version 0.1.16 adds the Outcast's 30-point direct-damage cap until the possessed-soldier boss is defeated.

[Download Playable Rivals v0.1.16](https://github.com/BrickAZ/Annihilated-Mods/releases/tag/playable-rivals-v0.1.16) · [English guide](PlayableRivalsMod/README.md) · [中文说明](PlayableRivalsMod/README.zh-CN.md)

Use the [Playable Rivals one-click ZIP](https://github.com/BrickAZ/Annihilated-Mods/releases/download/playable-rivals-v0.1.16/Annihilated-PlayableRivals-v0.1.16-Build26845264-Online.zip): extract it fully, run `Install.cmd`, then start a new save. Missing prerequisites are installed automatically and replaced DLLs are backed up. Supports Steam Build `26845264`; the manual DLL package is also available. Mod-added text is currently Chinese.

### PNG Texture Override and Texture Editor

- Replace enemy battle sprites and supported item icons with PNGs. The editor lets you edit individual frames and restore the original texture. [Texture Override and Texture Editor v1.1.2 (ZIP)](https://github.com/BrickAZ/Annihilated-Mods/releases/download/v1.0.6/Annihilated-TextureOverride-Editor-Build26845264-Online.zip)
- The standalone editor installs the texture mod and any missing prerequisites after you select the game folder. Previewing or exporting original textures requires the local art export folder. [Texture Editor v1.1.2 (standalone EXE)](https://github.com/BrickAZ/Annihilated-Mods/releases/download/v1.0.6/AnnihilatedTextureEditor-v1.1.2.exe) · [Chinese guide](TextureOverrideMod/README.md)
- Install the ready-made replacement for the dragon sprite and Dragon Eye icon. [Nailong Skin (ZIP)](https://github.com/BrickAZ/Annihilated-Mods/releases/download/v1.0.6/Annihilated-DragonSkin-Build26845264-Installer2-Online.zip)

### Cup and reference files

| What you want to do | Download |
| --- | --- |
| Run a tournament with shared rules and a target ending or boss | [Cup v0.5.5 (ZIP)](https://github.com/BrickAZ/Annihilated-Mods/releases/download/v1.0.6/Annihilated-Cup-Build26845264-Online.zip) |
| Review results offline and export provisional rankings | [Offline Cup referee (HTML)](https://github.com/BrickAZ/Annihilated-Mods/releases/download/v1.0.6/Cup-Referee.html) |
| Look up story branches and ending requirements | [Route and ending guide (Simplified Chinese HTML)](https://github.com/BrickAZ/Annihilated-Mods/releases/download/v1.0.6/Annihilated-Route-Guide-zh-CN.html) |
| Look up art entries added or updated in this build | [Art index (CSV)](https://github.com/BrickAZ/Annihilated-Mods/releases/download/v1.0.6/Art-Index-Build26845264.csv) |

[Asset SHA-256 checksums](https://github.com/BrickAZ/Annihilated-Mods/releases/download/v1.0.6/SHA256SUMS.txt)

## Download and install

1. Close the game, extract the entire mod ZIP, and run `Install.cmd` or `一键安装.cmd`.
2. If the installer doesn't find the game, select the folder that contains `Annihilated.exe` or drag that folder onto the CMD file. To find it in Steam, right-click the game in your library and choose **Manage → Browse local files**.
3. Start the game from Steam for regular play. On the first launch, wait for MelonLoader to generate its interop assemblies and for the title screen to appear. For Cup runs, keep Steam open and launch `Launch-Cup.cmd` from the game folder.

The installer downloads MelonLoader and portable .NET 6 when needed, and replaces any Cpp2IL files or plugin files that are missing or fail verification. If it reports missing or invalid game files, verify the game's file integrity in Steam. If the startup log shows `Failed to Execute Cpp2IL`, see [offline repair](#repair-cpp2il-offline).

## The Cup and referee tool

Organizers choose a target ending or boss and set the seed, battle mode, starting floor, and time limit. They can set death and retry limits, time penalties, and optional rules. The Cup ZIP includes F9 Room Retry and applies the event's retry limits and penalties.

- **Players** use separate Cup saves. When the timer stops, export the result JSON and submit it with a recording. The eight-character run ID matches the two files. [Installation, launch, and removal](CupMod/INSTALL.en.md)
- **Referees** open `Cup-Referee.html` from the Cup ZIP in a browser. Load the event rules, import results in bulk, review rule differences, penalties, and event timelines, then export provisional rankings as CSV. Use the top-right button to switch between English and Simplified Chinese. To try it out, apply the rules from `examples/example-ritual.json` and import that same file as a result. [Cup rules and referee guide](CupMod/GUIDE.en.md)

Use the same game build, Cup version, and rules for everyone in the event. The Cup displays Chinese if the last saved game language is Chinese. It displays English for all other settings. Both interface languages can be used in the same event. For Steam Build `26646212`, use [Cup v0.4.11](https://github.com/BrickAZ/Annihilated-Mods/releases/tag/v1.0.1).

## Repair Cpp2IL offline

Use the [Windows x64 offline repair ZIP](https://github.com/BrickAZ/Annihilated-Mods/releases/download/v1.0.5/Annihilated-Offline-Repair-ML0.7.3-Windows-x64.zip) if Cpp2IL fails to download, is damaged, or the startup log shows `Failed to Execute Cpp2IL`. It targets Unity 6000.3.23 and includes MelonLoader 0.7.3, portable .NET 6.0.36, Cpp2IL, and its plugin.

1. Close the game, extract the repair ZIP, run `一键安装.cmd`, and select the game folder.
2. The installer reuses a valid Unity cache. If the cache is missing or damaged, it repairs the other prerequisites first. Download [Managed.zip for Unity 6000.3.23](https://github.com/LavaGang/MelonLoader.UnityDependencies/releases/download/6000.3.23/Managed.zip), place it next to `一键安装.cmd`, and run the installer again to import it offline.

## Build from source

Use the .NET 8 SDK, PowerShell 7, and a local copy of the game with MelonLoader 0.7.3. Launch the game once to generate `MelonLoader/Il2CppAssemblies/`. The mod projects use that directory and `MelonLoader/net6/`, target `net6.0`, and may need to fetch the targeting pack on the first build.

Clone the repository or download **Source code** from a release. Open PowerShell 7 at the repository root and enter the path to the folder containing `Annihilated.exe` when prompted. The path can contain spaces and use any drive letter.

```powershell
$gameRoot = (Read-Host 'Path to the game folder').Trim().Trim('"')
./scripts/Build-Releases.ps1 -GameRoot $gameRoot
```

By default the script builds seven in-game mod projects and the texture editor, then writes eight installers and `SHA256SUMS.txt` to `dist/`. The save-slot package uses the authorized adapted DLL in this repository. To include the Nailong and standalone Bonfire packages, pass the published manual skin ZIP as `-DragonSkinArchive` and the standalone Bonfire DLL as `-BonfireDll`. The script verifies both inputs by SHA-256.

To build only the Cup, use `$gameRoot` from the same PowerShell window.

```powershell
dotnet build ./CupMod/AnnihilatedCup.csproj -c Release "-p:GameRoot=$gameRoot"
```

These three logic checks run without the game and also run on GitHub Actions.

```powershell
dotnet run --project ./CupMod/Checks/Checks.csproj
dotnet run --project ./RoomRetryMod/tests/RoomRetry.Tests.csproj
dotnet run --project ./EnemyIntentMod/tests/Rows/Rows.csproj
```

## Credits and dependencies

Brick made the Cup mod, gallery, F9 Room Retry, Attack Mode Switch, and referee tool. dsh made SaveSlotUnlimiter and gave Brick permission to distribute the adapted DLL.

Death Kumiai developed Annihilated and Flashlight Games published it. The gallery and replays load assets from your local copy of the game. The mods use interop assemblies generated from the local installation. [Annihilated on Steam](https://store.steampowered.com/app/2638080/Annihilated/) · [MelonLoader](https://github.com/LavaGang/MelonLoader)
