# Annihilated-Mods

[简体中文](#简体中文) · [English](#english) · [下载 / Downloads](#下载-downloads) · [最新发布 / Latest release](https://github.com/BrickAZ/Annihilated-Mods/releases/latest)

## 简体中文

《勇者小队全灭了。》（Annihilated）的非官方 Mod 和离线杯赛工具。可以组织指定结局或 Boss 的杯赛，从标题界面回看剧情和 CG，用 F9 重打当前房间，切换战斗判定方式，或扩充存档槽位。

### 下载与安装

目前的安装包来自 **v1.0.4**。五个游戏内 Mod 均已在 Windows x64、Steam Build `26705080` 和 MelonLoader 0.7.3 下测试。[下载表](#下载-downloads)

1. 退出游戏，完整解压所需的 Mod ZIP，双击 `一键安装.cmd`。如果没有自动找到游戏，请选择含 `Annihilated.exe` 的目录，或把该目录拖到 CMD 文件上。可以在 Steam 库中右键点击游戏，选择「管理 → 浏览本地文件」。
2. 安装器会检查游戏文件，并校验 Cpp2IL 及其配套插件。它会下载缺失的 MelonLoader 0.7.3、便携版 .NET 6，以及缺失或校验失败的 Cpp2IL 文件。如果提示游戏文件缺失或格式异常，请在 Steam 验证游戏文件的完整性。
3. 日常游玩按平时的方式启动。首次启动时，等 MelonLoader 生成互操作程序集并进入标题界面。参加杯赛时，请保持 Steam 开启，双击游戏目录里的 `Launch-Cup.cmd`。杯赛包已经包含 F9 插件。

存档扩容包内置适配好的 `SaveSlotUnlimiter.dll`。升级时，安装器会先备份旧版同名 DLL。[存档扩容说明](SaveSlotUnlimiter/README.md)

### 离线前置修复

Cpp2IL 下载失败、文件损坏，或启动日志出现 `Failed to Execute Cpp2IL` 时，可以用离线修复包。修复包适用于 Windows x64 和 Unity 6000.3.23，内含 MelonLoader 0.7.3、便携版 .NET 6.0.36、Cpp2IL 和配套插件。[下载表](#下载-downloads)

1. 退出游戏，完整解压修复 ZIP，运行 `一键安装.cmd` 并选择游戏目录。修复过程无需联网。
2. 如果已有可用的 Unity 6000.3.23 缓存，修复工具会直接复用。如果缓存缺失或损坏，工具会先修好其余前置。按提示另行下载 `Managed.zip`，将它放在解压后的 `一键安装.cmd` 旁边，再运行一次安装器即可离线导入。[下载 Managed.zip（Unity 6000.3.23）](https://github.com/LavaGang/MelonLoader.UnityDependencies/releases/download/6000.3.23/Managed.zip)

### 杯赛与裁判台

主办方可以指定结局或 Boss，统一地图种子、战斗方式、起始楼层和限时，也可以设置重打与死亡额度、罚秒和娱乐规则。选手使用独立的比赛存档，停表后把成绩 JSON 和录像一起交给裁判。两份材料用八位本局编号对应，成绩 JSON 内含事件时间轴。

裁判完整解压杯赛 ZIP 后，用浏览器打开 `Cup-Referee.html` 即可离线核验。右上角可切换简体中文和英文。载入当届规则后，可以批量导入成绩、查看规则差异和罚秒、核对事件时间轴，并导出暂定排名 CSV。想先试一遍，可用包内的 `examples/example-ritual.json` 应用规则，再把同一个示例文件作为成绩导入。

杯赛 v0.4.13 面向 Steam Build `26705080`。它读取常规游戏上次保存的语言设置，中文设置下显示中文，其他语言设置下显示英文。同一场比赛可混用中英文界面，选手需要统一游戏构建、杯赛版本和规则。安装、比赛和裁判的详细步骤见该 ZIP 内的 `INSTALL.md`、`GUIDE.en.md` 和 `REFEREE.md`。使用 Build `26646212` 的玩家可下载杯赛 v0.4.11。[旧版发布](https://github.com/BrickAZ/Annihilated-Mods/releases/tag/v1.0.1)

### 其他 Mod

- **剧情回放与 CG 画廊**。从标题界面回看 38 个剧情条目，或浏览 591 张 CG 原图。打开回放前可选择主角外观，回放使用独立存档。[使用说明](GalleryMod/使用说明.md)
- **F9 重打当前房间**。进入新战斗时记录检查点，按 F9 回到这一场的开头。杯赛内的重打额度和罚秒由当届规则决定。[使用说明](RoomRetryMod/使用说明.md)
- **战斗攻击方式切换**。在普通战斗中打开原版设置，可以切换 QTE 和概率判定。新方式从下一次攻击开始生效。[使用说明](AttackModeSwitchMod/使用说明.txt)
- **存档扩容**。适配后的 SaveSlotUnlimiter v1.25 为存读档界面提供 300 个槽位，包括 1 个自动存档槽和 299 个手动槽。[使用说明](SaveSlotUnlimiter/README.md)

### 从源码构建

准备 .NET 8 SDK 和 PowerShell 7。先在游戏目录安装 MelonLoader 0.7.3 并启动一次游戏，生成 `MelonLoader/Il2CppAssemblies/`。构建需要这个目录和 `MelonLoader/net6/`。四个项目面向 `net6.0`，首次构建可能需要获取对应的参考包。

克隆仓库或下载 Release 里的 **Source code**。在含有 `scripts/` 的目录打开 PowerShell 7，运行以下命令，并按提示输入含 `Annihilated.exe` 的目录。支持不同盘符和带空格的文件夹名。

```powershell
$gameRoot = (Read-Host '游戏文件夹路径').Trim().Trim('"')
./scripts/Build-Releases.ps1 -GameRoot $gameRoot
```

脚本按当前检出的源码版本构建四个项目，在 `dist/` 生成五个安装包和 `SHA256SUMS.txt`。存档扩容包使用仓库内获授权分发的适配 DLL。只构建杯赛时，在同一个窗口设置好 `$gameRoot` 后运行以下命令。

```powershell
dotnet build ./CupMod/AnnihilatedCup.csproj -c Release "-p:GameRoot=$gameRoot"
```

以下两项逻辑检查可在没有游戏的环境中运行。

```powershell
dotnet run --project ./CupMod/Checks/Checks.csproj
dotnet run --project ./RoomRetryMod/tests/RoomRetry.Tests.csproj
```

### 制作与依赖

杯赛、画廊、F9 房间重试、战斗攻击方式切换和裁判台由 Brick 制作。SaveSlotUnlimiter 由 dsh 制作，Brick 负责适配和打包，适配 DLL 经 dsh 许可分发。原作由 Death Kumiai 开发、Flashlight Games 发行。画廊与回放按索引从本机游戏读取素材，构建和运行依赖本机游戏生成的互操作程序集。[Steam 游戏页](https://store.steampowered.com/app/2638080/Annihilated/) · [MelonLoader](https://github.com/LavaGang/MelonLoader)

## English

Unofficial mods and offline Cup tools for Annihilated. Organize a Cup around a specific ending or boss, replay story scenes and browse CGs from the title screen, retry the current room with F9, switch attack modes, or expand the game's save slots.

### Download and install

The installers below are from **v1.0.4**. All five in-game mods were tested on Windows x64 with Steam Build `26705080` and MelonLoader 0.7.3. [Downloads](#下载-downloads)

1. Close the game, extract the entire mod ZIP, and run `一键安装.cmd` (the one-click installer). If it can't find the game, select the folder containing `Annihilated.exe` or drag that folder onto the CMD file. In Steam, right-click the game and choose **Manage → Browse local files** to open the folder.
2. The installer checks the game files and verifies Cpp2IL and its plugin. It downloads MelonLoader 0.7.3 or portable .NET 6 if either is missing, and fetches fresh copies of any Cpp2IL files that are missing or fail verification. If the installer reports missing or invalid game files, verify the game's file integrity in Steam.
3. Start the game as usual for regular play. On the first launch, wait for MelonLoader to generate the interop assemblies and for the title screen to appear. For Cup runs, keep Steam open and use `Launch-Cup.cmd` in the game folder. The Cup ZIP includes the F9 plugin.

The save-slot expansion ZIP includes the patched `SaveSlotUnlimiter.dll`. When upgrading, the installer backs up any older copy of that DLL before replacing it. [SaveSlotUnlimiter guide in Chinese](SaveSlotUnlimiter/README.md)

### Repair Cpp2IL offline

Use the offline repair ZIP if Cpp2IL fails to download, its files are corrupt, or the startup log shows `Failed to Execute Cpp2IL`. The package targets Windows x64 and Unity 6000.3.23. It includes MelonLoader 0.7.3, portable .NET 6.0.36, Cpp2IL, and its plugin. [Downloads](#下载-downloads)

1. Close the game, extract the entire repair ZIP, run `一键安装.cmd`, and select the game folder. The repair tool runs offline.
2. The tool reuses a valid Unity 6000.3.23 cache. If the cache is missing or damaged, it repairs the other prerequisites first. Download `Managed.zip` separately, put it next to the extracted `一键安装.cmd`, and run the installer again to import it offline. [Download Managed.zip for Unity 6000.3.23](https://github.com/LavaGang/MelonLoader.UnityDependencies/releases/download/6000.3.23/Managed.zip)

### The Cup and referee tool

Organizers choose a target ending or boss and set the map seed, battle mode, starting floor, and time limit. They can also set retry and death limits, time penalties, and optional rules. Cup runs use separate saves. When the timer stops, players submit a result JSON and a recording. The eight-character run ID pairs the two files, and the JSON includes an event timeline.

Referees can extract the Cup ZIP and open `Cup-Referee.html` in a browser to review results offline. Use the button in the top-right corner to switch between English and Simplified Chinese. Load the event rules, then import results in bulk, review rule differences and penalties, inspect event timelines, and export provisional rankings as a CSV. To try it out, apply the rules from the bundled `examples/example-ritual.json`, then import that same sample file as a result.

Cup v0.4.13 targets Steam Build `26705080`. It displays Chinese when the base game's last saved language is Chinese and English for other languages. Players can use either language in the same event when they share the same game build, Cup version, and rules. See `GUIDE.en.md`, `INSTALL.md`, and `REFEREE.md` in the ZIP for installation, rules, and refereeing. For Build `26646212`, download Cup v0.4.11. [Older release](https://github.com/BrickAZ/Annihilated-Mods/releases/tag/v1.0.1)

### Other mods

- **Story Replay & CG Gallery**. Revisit 38 story entries or browse 591 original CGs from the title screen. Choose either protagonist appearance before opening a replay. Replays use separate saves. [Guide in Chinese](GalleryMod/使用说明.md)
- **F9 Room Retry**. The mod creates a checkpoint at the start of a new battle. Press F9 to return to the start of that fight. In the Cup, the event rules set the retry limit and time penalty. [Guide in Chinese](RoomRetryMod/使用说明.md)
- **Attack Mode Switch**. Open the game's settings during a regular battle to choose QTE or probability-based attacks. The choice applies from the next attack. [Guide in Chinese](AttackModeSwitchMod/使用说明.txt)
- **SaveSlotUnlimiter**. The patched v1.25 adds support for 300 slots in the game's save and load screens, including one autosave slot and 299 manual slots. [Guide in Chinese](SaveSlotUnlimiter/README.md)

### Build from source

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

### Credits and dependencies

Brick made the Cup mod, gallery, F9 Room Retry, Attack Mode Switch, and referee tool. dsh created SaveSlotUnlimiter and gave Brick permission to distribute the adapted DLL. Death Kumiai developed Annihilated and Flashlight Games published it. The gallery and replays use indexes to load assets from your copy of the game. Builds and in-game mods rely on interop assemblies generated from the local installation. [Annihilated on Steam](https://store.steampowered.com/app/2638080/Annihilated/) · [MelonLoader](https://github.com/LavaGang/MelonLoader)

## 下载 Downloads

| 安装包 / Package | [v1.0.4 Release](https://github.com/BrickAZ/Annihilated-Mods/releases/tag/v1.0.4) |
| --- | --- |
| 杯赛 / Cup | [v0.4.13 ZIP](https://github.com/BrickAZ/Annihilated-Mods/releases/download/v1.0.4/Annihilated-Cup-v0.4.13.zip) |
| 剧情回放与 CG 画廊 / Story Replay & CG Gallery | [v1.2.1 ZIP](https://github.com/BrickAZ/Annihilated-Mods/releases/download/v1.0.4/Annihilated-Gallery-v1.2.1.zip) |
| F9 重打当前房间 / F9 Room Retry | [v1.0.1 ZIP](https://github.com/BrickAZ/Annihilated-Mods/releases/download/v1.0.4/Annihilated-RoomRetry-v1.0.1.zip) |
| 战斗攻击方式切换 / Attack Mode Switch | [v1.1.0 ZIP](https://github.com/BrickAZ/Annihilated-Mods/releases/download/v1.0.4/Annihilated-AttackModeSwitch-v1.1.0.zip) |
| 存档扩容 / SaveSlotUnlimiter | [v1.25 ZIP](https://github.com/BrickAZ/Annihilated-Mods/releases/download/v1.0.4/Annihilated-SaveSlotUnlimiter-v1.25-Build26705080.zip) |
| 离线前置修复 / Offline Prerequisite Repair | [Windows x64 ZIP](https://github.com/BrickAZ/Annihilated-Mods/releases/download/v1.0.4/Annihilated-Offline-Repair-ML0.7.3-Windows-x64.zip) |

[Mod 安装包 SHA-256 / Mod package checksums](https://github.com/BrickAZ/Annihilated-Mods/releases/download/v1.0.4/SHA256SUMS.txt) · [离线修复包校验值 / Offline repair checksum](https://github.com/BrickAZ/Annihilated-Mods/releases/tag/v1.0.4#下载-downloads)
