# Annihilated-Mods

[English](README.md) | 简体中文

这是《勇者小队全灭了。》（Annihilated）的非官方 Mod 合集。平时玩可以装怪物意图、F9 重打或剧情画廊。办比赛可以用杯赛 Mod 指定通关目标，再用离线裁判台核对成绩。

[下载](#下载) · [安装](#下载与安装) · [杯赛与裁判台](#杯赛与裁判台) · [离线修复](#离线前置修复) · [发布记录](https://github.com/BrickAZ/Annihilated-Mods/releases)

## 下载

这批安装包来自 [v1.0.6](https://github.com/BrickAZ/Annihilated-Mods/releases/tag/v1.0.6)，适用于 Windows 64 位的 Steam Build `26845264`，使用 MelonLoader 0.7.3。各个 Mod 可以按需安装。

### 日常游玩

| 想做什么 | 下载 | 说明 |
| --- | --- | --- |
| 看怪物下一招的单次伤害和攻击次数，悬停查看可用招式 | [怪物意图显示 v0.2.0](https://github.com/BrickAZ/Annihilated-Mods/releases/download/v1.0.6/Annihilated-EnemyIntent-Build26845264-Online.zip) | [用法](EnemyIntentMod/README.zh-CN.md) |
| 按 F9 回到当前战斗开头 | [F9 重打当前房间](https://github.com/BrickAZ/Annihilated-Mods/releases/download/v1.0.6/Annihilated-RoomRetry-Build26845264-Online.zip) | [用法](RoomRetryMod/使用说明.md) |
| 从标题界面回看 38 个剧情条目、浏览 597 张 CG | [剧情回放与 CG 画廊 v1.2.2](https://github.com/BrickAZ/Annihilated-Mods/releases/download/v1.0.6/Annihilated-Gallery-Build26845264-Online.zip) | [用法](GalleryMod/使用说明.md) |
| 在普通战斗的设置里切换 QTE 和概率判定，下一次攻击起生效 | [战斗攻击方式切换](https://github.com/BrickAZ/Annihilated-Mods/releases/download/v1.0.6/Annihilated-AttackModeSwitch-Build26845264-Online.zip) | [用法](AttackModeSwitchMod/使用说明.txt) |
| 扩充到 300 个存档槽，含 1 个自动槽和 299 个手动槽 | [SaveSlotUnlimiter v1.25](https://github.com/BrickAZ/Annihilated-Mods/releases/download/v1.0.6/Annihilated-SaveSlotUnlimiter-Build26845264-Online.zip) | [用法](SaveSlotUnlimiter/README.md) |
| 有合适的材料时，在火堆多一个固定的武器合成选项 | [独立火堆固定合成](https://github.com/BrickAZ/Annihilated-Mods/releases/download/v1.0.6/Annihilated-BonfireFusion-Build26845264-Online.zip) | 包内说明 |

剧情回放使用独立存档，开始前可选主角外观。存档扩容也支持 Build `26705080`，升级时会先备份旧的同名 DLL。

### 三名可玩角色

将勇者、战士、魔法师三个同伴槽位替换为放逐者、反派千金、落选者，提供专属武器、随楼层解锁的主动技能、头像与战斗美术。v0.1.16 加入放逐者的直接伤害合计 30 点上限，击败赫德夺舍的士兵后解除。

[下载三名可玩角色 v0.1.16](https://github.com/BrickAZ/Annihilated-Mods/releases/tag/playable-rivals-v0.1.16) · [中文说明](PlayableRivalsMod/README.zh-CN.md) · [English guide](PlayableRivalsMod/README.md)

推荐下载[三人角色一键安装包](https://github.com/BrickAZ/Annihilated-Mods/releases/download/playable-rivals-v0.1.16/Annihilated-PlayableRivals-v0.1.16-Build26845264-Online.zip)，完整解压后双击 `一键安装.cmd`，自动安装缺失前置并备份旧 DLL，再开新存档选人。适用 Steam Build `26845264`；同一 Release 中也保留手动 DLL 包。

### PNG 换皮 Mod 与贴图编辑器

- 可以用 PNG 替换怪物的战斗立绘和受支持的物品图标。编辑器支持逐帧替换和恢复原图。 [换皮 Mod 与贴图编辑器 v1.1.2（ZIP）](https://github.com/BrickAZ/Annihilated-Mods/releases/download/v1.0.6/Annihilated-TextureOverride-Editor-Build26845264-Online.zip)
- 单独打开编辑器并选好游戏目录后，它会补齐缺少的换皮 Mod 和前置。预览和导出原图时需要本机已有的美术资源目录。 [贴图编辑器 v1.1.2（独立 EXE）](https://github.com/BrickAZ/Annihilated-Mods/releases/download/v1.0.6/AnnihilatedTextureEditor-v1.1.2.exe) · [使用说明](TextureOverrideMod/README.md)
- 想直接换成奶龙，用这个包替换龙的立绘和龙之眼图标。 [奶龙换皮（ZIP）](https://github.com/BrickAZ/Annihilated-Mods/releases/download/v1.0.6/Annihilated-DragonSkin-Build26845264-Installer2-Online.zip)

### 杯赛、攻略与资料

| 想做什么 | 下载 |
| --- | --- |
| 按统一规则比赛，目标可以是一个结局或 Boss | [杯赛 v0.5.5（ZIP）](https://github.com/BrickAZ/Annihilated-Mods/releases/download/v1.0.6/Annihilated-Cup-Build26845264-Online.zip) |
| 离线核对成绩，导出暂定排名 | [离线裁判台（HTML）](https://github.com/BrickAZ/Annihilated-Mods/releases/download/v1.0.6/Cup-Referee.html) |
| 查剧情分支和结局条件 | [全路线与结局攻略（简中 HTML）](https://github.com/BrickAZ/Annihilated-Mods/releases/download/v1.0.6/Annihilated-Route-Guide-zh-CN.html) |
| 查询本次更新的美术资源条目 | [美术资源索引（CSV）](https://github.com/BrickAZ/Annihilated-Mods/releases/download/v1.0.6/Art-Index-Build26845264.csv) |

[附件 SHA-256](https://github.com/BrickAZ/Annihilated-Mods/releases/download/v1.0.6/SHA256SUMS.txt)

## 下载与安装

1. 退出游戏，完整解压需要的 Mod ZIP，双击 `一键安装.cmd` 或 `Install.cmd`。
2. 如果安装器没找到游戏，就选含 `Annihilated.exe` 的目录，也可以把该目录拖到 CMD 文件上。在 Steam 库里右键游戏，选「管理 → 浏览本地文件」，就能找到这个目录。
3. 日常游玩从 Steam 正常启动。首次打开游戏时，等 MelonLoader 生成互操作程序集并进入标题界面。参加杯赛时，保持 Steam 开启，用游戏目录里的 `Launch-Cup.cmd` 启动。

安装器会联网补齐缺少的 MelonLoader 和便携版 .NET 6，也会重新下载缺失或校验失败的 Cpp2IL 及其配套插件。如果提示游戏文件缺失或格式异常，先在 Steam 验证游戏文件完整性。启动日志如果出现 `Failed to Execute Cpp2IL`，见[离线前置修复](#离线前置修复)。

## 杯赛与裁判台

主办方指定结局或 Boss，并统一种子、战斗方式、起始楼层和限时。比赛可以设死亡和重打额度、罚秒及额外规则。杯赛包自带 F9，重打次数和罚秒以当届规则为准。

- **选手**用独立存档参赛。停表后导出成绩 JSON，连同录像一起提交。两份材料用同一个八位本局编号对应。 [安装、开赛与卸载](CupMod/INSTALL.md)
- **裁判**用浏览器打开杯赛 ZIP 内的 `Cup-Referee.html`，载入当届规则，再批量导入成绩。可以查看规则差异、罚秒和事件时间轴，也能导出暂定排名 CSV。右上角可切换中英文。第一次用可以先读入 `examples/example-ritual.json` 应用规则，再把同一个示例作为成绩导入。 [裁判台说明](CupMod/REFEREE.md)

比赛要统一游戏构建、杯赛版本和规则。游戏上次保存为中文时，杯赛界面显示中文，其余语言设置显示英文。中英文界面可以同场使用。Steam Build `26646212` 对应 [旧版杯赛 v0.4.11](https://github.com/BrickAZ/Annihilated-Mods/releases/tag/v1.0.1)。

## 离线前置修复

Cpp2IL 下载失败、文件损坏或启动日志出现 `Failed to Execute Cpp2IL` 时，下载 [Windows x64 离线修复包](https://github.com/BrickAZ/Annihilated-Mods/releases/download/v1.0.5/Annihilated-Offline-Repair-ML0.7.3-Windows-x64.zip)。它适用于 Unity 6000.3.23，内含 MelonLoader 0.7.3、便携版 .NET 6.0.36、Cpp2IL 和配套插件。

1. 退出游戏，完整解压修复包，运行 `一键安装.cmd` 并选择游戏目录。
2. 安装器会复用可用的 Unity 缓存。缓存缺失或损坏时会先修复其余前置。按提示另行下载 [Unity 6000.3.23 的 Managed.zip](https://github.com/LavaGang/MelonLoader.UnityDependencies/releases/download/6000.3.23/Managed.zip)，放到 `一键安装.cmd` 旁边，再运行一次离线导入。

## 从源码构建

需要 .NET 8 SDK、PowerShell 7 和带 MelonLoader 0.7.3 的本机游戏。至少启动一次游戏，生成 `MelonLoader/Il2CppAssemblies/`。Mod 项目使用该目录和 `MelonLoader/net6/`，目标框架是 `net6.0`，首次构建可能需要下载参考包。

克隆仓库或下载发布页里的 **Source code**，在仓库根目录打开 PowerShell 7。按提示输入含 `Annihilated.exe` 的路径，路径支持空格和不同盘符。

```powershell
$gameRoot = (Read-Host '游戏文件夹路径').Trim().Trim('"')
./scripts/Build-Releases.ps1 -GameRoot $gameRoot
```

脚本默认构建七个游戏内 Mod 项目和贴图编辑器，在 `dist/` 生成八个安装包和 `SHA256SUMS.txt`。存档扩容包使用仓库内获授权的适配 DLL。要一起构建奶龙和独立火堆合成包，用 `-DragonSkinArchive` 传入已发布的奶龙手动安装 ZIP，用 `-BonfireDll` 传入已发布的独立火堆合成 DLL。脚本会校验这两个输入的 SHA-256。

可以用同一个窗口里的 `$gameRoot` 单独构建杯赛。

```powershell
dotnet build ./CupMod/AnnihilatedCup.csproj -c Release "-p:GameRoot=$gameRoot"
```

三组逻辑检查可在没有游戏的环境中运行，GitHub Actions 也会自动执行。

```powershell
dotnet run --project ./CupMod/Checks/Checks.csproj
dotnet run --project ./RoomRetryMod/tests/RoomRetry.Tests.csproj
dotnet run --project ./EnemyIntentMod/tests/Rows/Rows.csproj
```

## 制作与依赖

杯赛、画廊、F9 重试、战斗攻击方式切换和裁判台由 Brick 制作。SaveSlotUnlimiter 由 dsh 制作，Brick 负责适配和打包，适配 DLL 经 dsh 许可分发。

原作由 Death Kumiai 开发、Flashlight Games 发行。画廊和剧情回放按索引读取本机游戏素材，Mod 使用从本机游戏生成的互操作程序集。 [Steam 游戏页](https://store.steampowered.com/app/2638080/Annihilated/) · [MelonLoader](https://github.com/LavaGang/MelonLoader)
