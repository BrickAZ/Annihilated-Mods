# Annihilated Enemy Intent v0.1.0

English | [简体中文](README.zh-CN.md)

[Download the Windows test build](https://github.com/BrickAZ/Annihilated-Mods/releases/tag/enemy-intent-v0.1.0)

The enemy's next-action display shows damage per hit and hit count, for example `12 ×2`. Consecutive attacks share a row when the damage, attack type, target, and warning state match. Block and other effects keep their native icons. The display updates when the game recalculates damage or changes the enemy's next action. The game continues to choose each action.

## Install

1. Close the game. Install [MelonLoader 0.7.3](https://github.com/LavaGang/MelonLoader/releases/tag/v0.7.3) if needed.
2. Extract the ZIP and merge its `Mods` folder into the folder containing `Annihilated.exe`.
3. Launch the game as usual and enter a battle. To uninstall, close the game and remove `Mods/AnnihilatedEnemyIntent.dll`.

Tested on Windows x64, Steam Build `26705080`. Goblin battle tests covered per-hit damage, consecutive attacks, block icons and intent changes. Complex bosses and crowded encounters still need layout testing.

## Build from source

Use the .NET 8 SDK. Launch the game with MelonLoader once to generate its interop assemblies. Run the following commands from this directory.

```powershell
$gameRoot = (Read-Host 'Game folder').Trim().Trim('"')
dotnet build ./src/AnnihilatedEnemyIntent.csproj -c Release "-p:GameRoot=$gameRoot"
dotnet run --project ./tests/Rows/Rows.csproj -c Release
```

The 10 row-format checks run without the game. `tests/Smoke` is for an isolated Cup goblin battle and is not part of the installation ZIP.