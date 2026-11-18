# Annihilated-Mods

《勇者小队全灭了。》（Annihilated）的非官方 Mod 与离线杯赛工具。游戏内 Mod 的测试环境为 **Windows x64、Steam Build 26705080、MelonLoader 0.7.3**。

[查看最新发布](https://github.com/BrickAZ/Annihilated-Mods/releases/latest) · [安装步骤](#下载与安装)

## 杯赛 Mod · v0.4.12

主办方可以指定结局或 Boss，统一地图种子、战斗方式、起始楼层和限时。选手在游戏内按当届规则开赛，使用独立的比赛存档。支持重打和死亡额度、罚秒及可选娱乐规则。停表后，选手导出带八位本局编号和事件时间轴的成绩 JSON，连同录像交给裁判。

v0.4.12 对应 Steam Build `26705080`。使用 Build `26646212` 的玩家可从 [v1.0.1 Release](https://github.com/BrickAZ/Annihilated-Mods/releases/tag/v1.0.1) 下载杯赛 v0.4.11。

[下载最新版杯赛 ZIP](https://github.com/BrickAZ/Annihilated-Mods/releases/latest/download/Annihilated-Cup-v0.4.13.zip) · [安装与卸载](CupMod/INSTALL.md) · [完整比赛规则](CupMod/README.md)

## 杯赛裁判台

解压上面的杯赛 ZIP，双击 `Cup-Referee.html` 即可在浏览器离线使用。裁判只需浏览器。载入当届规则后，可以批量导入选手的成绩，查看规则差异、罚秒和事件时间轴，并导出暂定排名 CSV。

想试玩裁判流程，可以先把 ZIP 内的 `examples/example-ritual.json` 当作规则导入，点击「应用当届规则」，再把同一文件作为成绩导入。该文件使用虚构数据。

[裁判台使用说明](CupMod/REFEREE.md)

## 其他 Mod

| Mod | 功能 | 安装包 |
| --- | --- | --- |
| [剧情回放与 CG 画廊](GalleryMod/使用说明.md) | 从标题界面回看 38 个剧情条目、浏览 591 张 CG。可切换主角外观，回放使用独立存档。 | [下载 v1.2.1](https://github.com/BrickAZ/Annihilated-Mods/releases/latest/download/Annihilated-Gallery-v1.2.1.zip) |
| [F9 重打当前房间](RoomRetryMod/使用说明.md) | 进入新战斗时建立检查点，按 F9 回到本场开头。杯赛中的重打额度和罚秒由当届规则决定。 | [下载 v1.0.1](https://github.com/BrickAZ/Annihilated-Mods/releases/latest/download/Annihilated-RoomRetry-v1.0.1.zip) |
| [战斗攻击方式切换](AttackModeSwitchMod/使用说明.txt) | 在普通战斗中，从原版设置切换 QTE 或概率判定，下一次攻击采用新方式。 | [下载 v1.1.0](https://github.com/BrickAZ/Annihilated-Mods/releases/latest/download/Annihilated-AttackModeSwitch-v1.1.0.zip) |
| [存档扩容 SaveSlotUnlimiter](SaveSlotUnlimiter/README.md) | 一键安装经原作者许可分发的 v1.25 适配版，恢复 300 个存读档槽位。 | [下载独立安装包](https://github.com/BrickAZ/Annihilated-Mods/releases/latest/download/Annihilated-SaveSlotUnlimiter-v1.25-Build26705080.zip) |

## 下载与安装

1. 退出游戏，完整解压对应的 ZIP，双击 **`一键安装.cmd`**。脚本会查找 Steam 游戏目录。如果未找到，请选择含 `Annihilated.exe` 的文件夹。也可以把该文件夹拖到 CMD 上。
2. 若游戏缺少加载器或运行时，脚本会联网下载并校验官方 MelonLoader 0.7.3 和便携版 .NET 6，将依赖和所选 Mod 安装到游戏目录。
3. 安装完成后按平时的方式启动游戏。首次启动加载器会生成互操作程序集，请等到标题界面。玩杯赛时，双击安装到游戏目录的 `Launch-Cup.cmd`。杯赛包附带 F9 插件。

存档扩容包自带适配后的 DLL。升级时安装器会自动备份并替换旧 DLL。[扩容说明](SaveSlotUnlimiter/README.md)

请从 Release 的 **Assets** 获取安装包。GitHub 自动生成的 **Source code** 压缩包用于下面的源码构建。核对安装包时，请使用与它同一 Release 的 [SHA256SUMS.txt](https://github.com/BrickAZ/Annihilated-Mods/releases/latest/download/SHA256SUMS.txt)。

## 从源码构建

需要 .NET 8 SDK 和 PowerShell 7。游戏目录里应已有 MelonLoader 0.7.3 的 `MelonLoader/net6/`，以及首次启动时生成的 `MelonLoader/Il2CppAssemblies/`。四个项目面向 `net6.0`，首次构建可能需要由 SDK 获取对应的参考包。

克隆或下载本仓库源码后，在含 `scripts/` 的仓库根目录打开 PowerShell 7，运行下面两行。在提示处粘贴你电脑上含 `Annihilated.exe` 的文件夹路径。路径可以在任意盘符，文件夹名可以包含空格。

```powershell
$gameRoot = (Read-Host '游戏文件夹路径').Trim().Trim('"')
./scripts/Build-Releases.ps1 -GameRoot $gameRoot
```

脚本会构建四个项目，并在仓库的 `dist/` 生成五个独立安装包和 `SHA256SUMS.txt`。存档扩容包使用仓库中获授权分发的适配 DLL。如果只想构建杯赛 Mod，在同一个窗口设置好 `$gameRoot` 后运行下面这一行。

```powershell
dotnet build ./CupMod/AnnihilatedCup.csproj -c Release "-p:GameRoot=$gameRoot"
```

下面两个逻辑检查可在没有游戏的环境中运行。

```powershell
dotnet run --project ./CupMod/Checks/Checks.csproj
dotnet run --project ./RoomRetryMod/tests/RoomRetry.Tests.csproj
```

## 制作与依赖

杯赛、画廊、F9 房间重试、战斗攻击方式切换与裁判台由 Brick 制作。SaveSlotUnlimiter 原作者为 dsh，Brick 负责游戏版本适配和打包，经 dsh 许可分发。原作由 Death Kumiai 开发、Flashlight Games 发行。画廊和回放通过索引从本机游戏加载原作内容，游戏素材与互操作程序集由本机安装提供。

[Steam 游戏页面](https://store.steampowered.com/app/2638080/Annihilated/) · [MelonLoader](https://github.com/LavaGang/MelonLoader)
