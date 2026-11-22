# Annihilated-Mods

[English](README.md) | 简体中文

[下载](#下载) · [最新发布](https://github.com/BrickAZ/Annihilated-Mods/releases/latest)

《勇者小队全灭了。》（Annihilated）的非官方 Mod 和离线杯赛工具。可以组织指定结局或 Boss 的杯赛，从标题界面回看剧情和 CG，用 F9 重打当前房间，切换战斗判定方式，或扩充存档槽位。

## 下载与安装

目前的安装包来自 **v1.0.6**，面向 Windows x64、Steam Build `26845264` 和 MelonLoader 0.7.3。[下载表](#下载)

1. 退出游戏，完整解压所需的 Mod ZIP，双击 `一键安装.cmd`。如果没有自动找到游戏，请选择含 `Annihilated.exe` 的目录，或把该目录拖到 CMD 文件上。可以在 Steam 库中右键点击游戏，选择「管理 → 浏览本地文件」。
2. 安装器会检查游戏文件，并校验 Cpp2IL 及其配套插件。它会下载缺失的 MelonLoader 0.7.3、便携版 .NET 6，以及缺失或校验失败的 Cpp2IL 文件。如果提示游戏文件缺失或格式异常，请在 Steam 验证游戏文件的完整性。
3. 日常游玩按平时的方式启动。首次启动时，等 MelonLoader 生成互操作程序集并进入标题界面。参加杯赛时，请保持 Steam 开启，双击游戏目录里的 `Launch-Cup.cmd`。杯赛包已经包含 F9 插件。

存档扩容包内置适配好的 `SaveSlotUnlimiter.dll`。升级时，安装器会先备份旧版同名 DLL。[存档扩容说明](SaveSlotUnlimiter/README.md)

## 离线前置修复

Cpp2IL 下载失败、文件损坏，或启动日志出现 `Failed to Execute Cpp2IL` 时，可以用离线修复包。修复包适用于 Windows x64 和 Unity 6000.3.23，内含 MelonLoader 0.7.3、便携版 .NET 6.0.36、Cpp2IL 和配套插件。[下载表](#下载)

1. 退出游戏，完整解压修复 ZIP，运行 `一键安装.cmd` 并选择游戏目录。修复过程无需联网。
2. 如果已有可用的 Unity 6000.3.23 缓存，修复工具会直接复用。如果缓存缺失或损坏，工具会先修好其余前置。按提示另行下载 `Managed.zip`，将它放在解压后的 `一键安装.cmd` 旁边，再运行一次安装器即可离线导入。[下载 Managed.zip（Unity 6000.3.23）](https://github.com/LavaGang/MelonLoader.UnityDependencies/releases/download/6000.3.23/Managed.zip)

## 杯赛与裁判台

主办方可以指定结局或 Boss，统一地图种子、战斗方式、起始楼层和限时，也可以设置重打与死亡额度、罚秒和娱乐规则。选手使用独立的比赛存档，停表后把成绩 JSON 和录像一起交给裁判。两份材料用八位本局编号对应，成绩 JSON 内含事件时间轴。

裁判完整解压杯赛 ZIP 后，用浏览器打开 `Cup-Referee.html` 即可离线核验。右上角可切换简体中文和英文。载入当届规则后，可以批量导入成绩、查看规则差异和罚秒、核对事件时间轴，并导出暂定排名 CSV。想先试一遍，可用包内的 `examples/example-ritual.json` 应用规则，再把同一个示例文件作为成绩导入。

杯赛 v0.5.5 面向 Steam Build `26845264`。它读取常规游戏上次保存的语言设置，中文设置下显示中文，其他语言设置下显示英文。同一场比赛可混用中英文界面，选手需要统一游戏构建、杯赛版本和规则。比赛规则和裁判流程详见 ZIP 内的 `README.md`、`GUIDE.en.md` 和 `REFEREE.md`。使用 Build `26646212` 的玩家可下载杯赛 v0.4.11。[v0.5.5 安装与卸载](CupMod/INSTALL.md) · [旧版发布](https://github.com/BrickAZ/Annihilated-Mods/releases/tag/v1.0.1)

## 其他 Mod

- **怪物意图显示**。在怪物头顶显示单次伤害和攻击次数，合并符合条件的连击。改招或伤害变化后会更新提示。[下载 v0.2.0](https://github.com/BrickAZ/Annihilated-Mods/releases/download/v1.0.6/Annihilated-EnemyIntent-Build26845264-Online.zip) · [使用说明](EnemyIntentMod/README.zh-CN.md)
- **剧情回放与 CG 画廊**。从标题界面回看 38 个剧情条目，或浏览 597 张 CG 原图。打开回放前可选择主角外观，回放使用独立存档。[使用说明](GalleryMod/使用说明.md)
- **F9 重打当前房间**。进入新战斗时记录检查点，按 F9 回到这一场的开头。杯赛内的重打额度和罚秒由当届规则决定。[使用说明](RoomRetryMod/使用说明.md)
- **战斗攻击方式切换**。在普通战斗中打开原版设置，可以切换 QTE 和概率判定。新方式从下一次攻击开始生效。[使用说明](AttackModeSwitchMod/使用说明.txt)
- **存档扩容**。适配后的 SaveSlotUnlimiter v1.25 为存读档界面提供 300 个槽位，包括 1 个自动存档槽和 299 个手动槽。[使用说明](SaveSlotUnlimiter/README.md)

## 从源码构建

准备 .NET 8 SDK 和 PowerShell 7。先在游戏目录安装 MelonLoader 0.7.3 并启动一次游戏，生成 `MelonLoader/Il2CppAssemblies/`。构建需要这个目录和 `MelonLoader/net6/`。游戏内 Mod 项目面向 `net6.0`，首次构建可能需要获取对应的参考包。

克隆仓库或下载 Release 里的 **Source code**。在含有 `scripts/` 的目录打开 PowerShell 7，运行以下命令，并按提示输入含 `Annihilated.exe` 的目录。支持不同盘符和带空格的文件夹名。

```powershell
$gameRoot = (Read-Host '游戏文件夹路径').Trim().Trim('"')
./scripts/Build-Releases.ps1 -GameRoot $gameRoot
```

脚本默认构建六个游戏内 Mod 项目和贴图编辑器，在 `dist/` 生成七个安装包和 `SHA256SUMS.txt`。存档扩容包使用仓库内获授权分发的适配 DLL。若要一起构建奶龙换皮和独立火堆合成包，用 `-DragonSkinArchive` 传入已发布的奶龙手动安装 ZIP，用 `-BonfireDll` 传入已发布的独立火堆合成 DLL。脚本会校验这两个输入的 SHA-256。

只构建杯赛时，在同一个窗口设置好 `$gameRoot` 后运行以下命令。

```powershell
dotnet build ./CupMod/AnnihilatedCup.csproj -c Release "-p:GameRoot=$gameRoot"
```

以下两项逻辑检查可在没有游戏的环境中运行。

```powershell
dotnet run --project ./CupMod/Checks/Checks.csproj
dotnet run --project ./RoomRetryMod/tests/RoomRetry.Tests.csproj
```

## 制作与依赖

杯赛、画廊、F9 房间重试、战斗攻击方式切换和裁判台由 Brick 制作。SaveSlotUnlimiter 由 dsh 制作，Brick 负责适配和打包，适配 DLL 经 dsh 许可分发。原作由 Death Kumiai 开发、Flashlight Games 发行。画廊与回放按索引从本机游戏读取素材，构建和运行依赖本机游戏生成的互操作程序集。[Steam 游戏页](https://store.steampowered.com/app/2638080/Annihilated/) · [MelonLoader](https://github.com/LavaGang/MelonLoader)

## PNG 换皮 Mod 与贴图编辑器

用 PNG 更换怪物的战斗立绘和受支持的物品图标。Windows 编辑器支持预览、逐帧替换和恢复原图。想直接体验奶龙立绘和龙之眼图标，可以下载轻量联网一键包。

打开 v1.1.2 编辑器并选定游戏目录后，缺少的换皮 Mod 和前置会自动安装。

[下载编辑器 v1.1.2](https://github.com/BrickAZ/Annihilated-Mods/releases/download/v1.0.6/AnnihilatedTextureEditor-v1.1.2.exe) · [源码与说明](TextureOverrideMod/README.md)

## 下载

| 安装包 | 下载 |
| --- | --- |
| 杯赛 v0.5.5 | [ZIP](https://github.com/BrickAZ/Annihilated-Mods/releases/download/v1.0.6/Annihilated-Cup-Build26845264-Online.zip) |
| 剧情回放与 CG 画廊 v1.2.2 | [ZIP](https://github.com/BrickAZ/Annihilated-Mods/releases/download/v1.0.6/Annihilated-Gallery-Build26845264-Online.zip) |
| F9 重打当前房间 | [ZIP](https://github.com/BrickAZ/Annihilated-Mods/releases/download/v1.0.6/Annihilated-RoomRetry-Build26845264-Online.zip) |
| 战斗攻击方式切换 | [ZIP](https://github.com/BrickAZ/Annihilated-Mods/releases/download/v1.0.6/Annihilated-AttackModeSwitch-Build26845264-Online.zip) |
| 存档扩容 v1.25 | [ZIP](https://github.com/BrickAZ/Annihilated-Mods/releases/download/v1.0.6/Annihilated-SaveSlotUnlimiter-Build26845264-Online.zip) |
| 怪物意图显示 v0.2.0 | [ZIP](https://github.com/BrickAZ/Annihilated-Mods/releases/download/v1.0.6/Annihilated-EnemyIntent-Build26845264-Online.zip) |
| PNG 换皮与贴图编辑器 v1.1.2 | [ZIP](https://github.com/BrickAZ/Annihilated-Mods/releases/download/v1.0.6/Annihilated-TextureOverride-Editor-Build26845264-Online.zip) |
| 奶龙换皮 | [ZIP](https://github.com/BrickAZ/Annihilated-Mods/releases/download/v1.0.6/Annihilated-DragonSkin-Build26845264-Online.zip) |
| 独立火堆固定合成 | [ZIP](https://github.com/BrickAZ/Annihilated-Mods/releases/download/v1.0.6/Annihilated-BonfireFusion-Build26845264-Online.zip) |
| 贴图编辑器独立 EXE | [v1.1.2 EXE](https://github.com/BrickAZ/Annihilated-Mods/releases/download/v1.0.6/AnnihilatedTextureEditor-v1.1.2.exe) |
| 离线裁判台 | [HTML](https://github.com/BrickAZ/Annihilated-Mods/releases/download/v1.0.6/Cup-Referee.html) |
| 全路线与结局攻略（简中） | [HTML](https://github.com/BrickAZ/Annihilated-Mods/releases/download/v1.0.6/Annihilated-Route-Guide-zh-CN.html) |
| 本次美术资源索引 | [CSV](https://github.com/BrickAZ/Annihilated-Mods/releases/download/v1.0.6/Art-Index-Build26845264.csv) |

[全部附件 SHA-256](https://github.com/BrickAZ/Annihilated-Mods/releases/download/v1.0.6/SHA256SUMS.txt) · [离线前置修复包](https://github.com/BrickAZ/Annihilated-Mods/releases/download/v1.0.5/Annihilated-Offline-Repair-ML0.7.3-Windows-x64.zip)
