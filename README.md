# Annihilated-Mods

《勇者小队全灭了。》（Annihilated）的非官方 Mod 与离线杯赛工具。游戏内 Mod 的测试环境为 **Windows x64、Steam Build 26646212、MelonLoader 0.7.3**。

[查看最新发布](https://github.com/BrickAZ/Annihilated-Mods/releases/latest) · [安装步骤](#下载与安装)

## 杯赛 Mod · v0.4.11

主办方可以指定结局或 Boss，统一地图种子、战斗方式、起始楼层和限时。选手在游戏内按当届规则开赛，使用独立的比赛存档。支持重打和死亡额度、罚秒及可选娱乐规则。停表后，选手导出带八位本局编号和事件时间轴的成绩 JSON，连同录像交给裁判。

[下载杯赛 ZIP](https://github.com/BrickAZ/Annihilated-Mods/releases/download/v1.0.0/Annihilated-Cup-v0.4.11.zip) · [安装与卸载](CupMod/INSTALL.md) · [完整比赛规则](CupMod/README.md)

## 杯赛裁判台

解压上面的杯赛 ZIP，双击 `Cup-Referee.html` 即可在浏览器离线使用。裁判只需浏览器。载入当届规则后，可以批量导入选手的成绩，查看规则差异、罚秒和事件时间轴，并导出暂定排名 CSV。

想试玩裁判流程，可以先把 ZIP 内的 `examples/example-ritual.json` 当作规则导入，点击「应用当届规则」，再把同一文件作为成绩导入。该文件使用虚构数据。

[裁判台使用说明](CupMod/REFEREE.md)

## 其他 Mod

| Mod | 功能 | 安装包 |
| --- | --- | --- |
| [剧情回放与 CG 画廊](GalleryMod/使用说明.md) | 从标题界面回看 38 个剧情条目、浏览 591 张 CG。可切换主角外观，回放使用独立存档。 | [下载 v1.2.0](https://github.com/BrickAZ/Annihilated-Mods/releases/download/v1.0.0/Annihilated-Gallery-v1.2.0.zip) |
| [F9 重打当前房间](RoomRetryMod/使用说明.md) | 进入新战斗时建立检查点，按 F9 回到本场开头。杯赛中的重打额度和罚秒由当届规则决定。 | [下载 v1.0.1](https://github.com/BrickAZ/Annihilated-Mods/releases/download/v1.0.0/Annihilated-RoomRetry-v1.0.1.zip) |
| [战斗攻击方式切换](AttackModeSwitchMod/使用说明.txt) | 在普通战斗中，从原版设置切换 QTE 或概率判定，下一次攻击采用新方式。 | [下载 v1.1.0](https://github.com/BrickAZ/Annihilated-Mods/releases/download/v1.0.0/Annihilated-AttackModeSwitch-v1.1.0.zip) |

## 下载与安装

1. 在 Steam 库右键点击游戏，选择「管理 → 浏览本地文件」，找到 `Annihilated.exe`。给这个游戏安装 [MelonLoader 0.7.3](https://github.com/LavaGang/MelonLoader/releases/tag/v0.7.3)，然后从 Steam 正常启动一次，等到游戏标题界面出现。
2. 退出游戏，解压上面相应的安装包。把包内的 `Mods` 文件夹合并到 `Annihilated.exe` 所在目录。
3. 玩杯赛时，再把包内的 `Launch-Cup.cmd` 复制到同一目录，保持 Steam 开启并双击脚本。杯赛包已经包含 F9 插件。普通游玩及使用其他 Mod 时，从 Steam 正常启动。

每个 ZIP 都附有安装与卸载说明。需要检查下载完整性时，可使用 [SHA256SUMS.txt](https://github.com/BrickAZ/Annihilated-Mods/releases/download/v1.0.0/SHA256SUMS.txt)。

## 从源码构建

需要 .NET 8 SDK、PowerShell 7，以及已经用 MelonLoader 0.7.3 启动过的本机游戏。四个项目面向 `net6.0`，首次构建可能需要由 SDK 获取对应的参考包。

请在仓库根目录执行下面的命令，并填入你的游戏目录。

```powershell
pwsh -NoProfile -File ./scripts/Build-Releases.ps1 -GameRoot 'D:\SteamLibrary\steamapps\common\Annihilated'
```

脚本会构建四个项目，并在 `dist/` 生成四个安装包和 `SHA256SUMS.txt`。也可以单独构建某个 Mod。下面以杯赛为例。

```powershell
dotnet build ./CupMod/AnnihilatedCup.csproj -c Release '-p:GameRoot=D:\SteamLibrary\steamapps\common\Annihilated'
```

下面两个逻辑检查可在没有游戏的环境中运行。

```powershell
dotnet run --project ./CupMod/Checks/Checks.csproj
dotnet run --project ./RoomRetryMod/tests/RoomRetry.Tests.csproj
```

## 制作与依赖

Mod 与裁判台由 Brick 制作。原作由 Death Kumiai 开发、Flashlight Games 发行。画廊和回放通过索引从本机游戏加载原作内容，游戏素材与互操作程序集由本机安装提供。

[Steam 游戏页面](https://store.steampowered.com/app/2638080/Annihilated/) · [MelonLoader](https://github.com/LavaGang/MelonLoader)
