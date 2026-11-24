# Playable Rivals · v0.1.16

English | [简体中文](README.zh-CN.md)

Replace the Hero, Warrior, and Mage companion slots with the Outcast (放逐者), Villainess (反派千金), and Dropout (落选者). Includes signature weapons, active skills, selection banners, pixel sprites, sanity portraits, and battle artwork. Author: Brick.

Supported setup: Windows x64, Steam Build `26845264`, and MelonLoader `0.7.3`. Text added by this mod is currently in Chinese.

[Download v0.1.16](https://github.com/BrickAZ/Annihilated-Mods/releases/tag/playable-rivals-v0.1.16) · [Changelog (Chinese)](CHANGELOG.zh-CN.md) · [Verification scope](docs/verification-v0.1.16.md)

## Install and choose a character

1. Close the game and download the [one-click installer ZIP](https://github.com/BrickAZ/Annihilated-Mods/releases/download/playable-rivals-v0.1.16/Annihilated-PlayableRivals-v0.1.16-Build26845264-Online.zip).
2. **Extract the entire ZIP** to a folder and double-click `Install.cmd` (identical to `一键安装.cmd`). You do not need to install MelonLoader manually first.
3. The installer finds the Steam library automatically. If needed, select the folder containing `Annihilated.exe`, or drag that folder onto the CMD. Missing MelonLoader 0.7.3, portable .NET 6, Cpp2IL, and Unity dependencies are downloaded from upstream sources and checksum-verified; first installation needs internet access.
4. After installation succeeds, start the game normally and wait for the title screen on first launch. Start a new save: Hero becomes Outcast, Warrior becomes Villainess, and Mage becomes Dropout. Priest keeps the original behavior.

Other mods and saves are preserved. Replaced versions of the same DLL are backed up under `UserData/AnnihilatedModsInstaller/backups/`. Healthy prerequisites are reused.

The manual package `Annihilated-PlayableRivals-v0.1.16.zip` remains available for existing MelonLoader setups: close the game and merge its `Mods` folder into the game directory. Both packages contain the same mod DLL. GitHub's Source code ZIP is not an installation package.

For a version mismatch, compare the supported build and the detected Steam manifest build in the error. This package supports Build `26845264` and verifies both the game assembly and resource catalog fingerprints. An old installer asking for `26705080` calls for a matching installer, not a game downgrade. If the build numbers match but fingerprints differ, verify game files through Steam.

Existing vanilla saves are not automatically converted. Existing Playable Rivals saves can continue. Keep a backup before uninstalling and use a vanilla-character save for vanilla play. To uninstall, close the game and remove `Mods/AnnihilatedPlayableRivals.dll`.

## Characters

| Character | Starting signature effect | Floor 1 | Floor 2 | Floor 3 |
| --- | --- | --- | --- | --- |
| Outcast | Magic sword: 23 damage; transfers enemy buffs on hit | Prepare Robbery | Switch to Daggers | Elemental Enchantment |
| Villainess | Puppet thread: 15 damage | Love for the Warrior | Domination | Summon a Puppet |
| Dropout | Magic codex: grants 15 block | Codex barrier | Summon a Barrier | Armor Transfer |

- Robbery preparation applies to the Outcast's next weapon attack, including ordinary inventory weapons. A weapon with the native cannot-steal effect keeps that restriction and still consumes preparation. Missing also consumes it. Enchantment is free; switching weapons consumes an action and enables daggers on the following round.
- Love for the Warrior burns and poisons all living enemies and heals the Villainess. It costs no action if her health is strictly below 70% before use. Cooldown still applies.
- Armor Transfer sets the Dropout's magic to zero, disables magical skills, and switches her signature weapon to physical damage using the halved damage formula.
- Skills unlock by floor. Initial selection previews only the starting skill and weapon. Story slots, dialogue, and story-specific actions continue to use the original routes.

## v0.1.16: Outcast damage cap

Until the possessed-soldier boss battle is won, all direct damage from one Outcast weapon or skill use shares a 30-point cap across hits and targets. A single hit is also capped at 30. The cap is applied after damage bonuses, critical hits, damage reduction, and block, immediately before health damage.

Two 20-damage hits in one use deal 20 + 10. Two separate dagger actions receive separate budgets. Native indirect damage, including burning and poison, is excluded. Robbery still takes the weapon, but its normal-enemy execution cannot bypass the cap.

The restriction ends when the entire native `slime` boss encounter reaches victory, not when its first form falls. The unlock is stored in the current run's save. Older mod saves with the native completion flag are migrated after leaving that encounter. Cards retain their base values and show the cap notice: the level-21 sword has 48 base damage, deals at most 30 while restricted, and returns to normal damage after unlocking.

## Build from source

Use the .NET 8 SDK and a local game installation with MelonLoader 0.7.3. Launch the game once to generate `MelonLoader/Il2CppAssemblies`. From the repository root:

```powershell
dotnet build PlayableRivalsMod/src/AnnihilatedPlayableRivals.csproj -c Release -p:GameRoot="E:/SteamLibrary/steamapps/common/Annihilated"
dotnet run --project PlayableRivalsMod/Checks/Checks.csproj -c Release
```

Replace the example path with your own game folder, or set `ANNIHILATED_GAME_ROOT`. The output is `PlayableRivalsMod/src/bin/Release/net6.0/AnnihilatedPlayableRivals.dll`. Artwork is embedded; no separate image folder is needed at runtime. Game files, game assemblies, MelonLoader, and personal saves are not distributed in this source folder.

The rules checks run without the game and are included in GitHub Actions. Native combat, QTE, and boss-branch tests require the actual game; see the verification report for their limits. Original characters and game artwork belong to their creators. The included artwork consists of the replacements and derived assets used by this mod.
