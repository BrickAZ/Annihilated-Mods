# Annihilated-Mods

《勇者小队全灭了。》（Annihilated）的非官方 Mod 与离线杯赛工具。

## 杯赛 Mod · v0.4.11

在游戏内配置指定结局或 Boss、地图种子、战斗方式、起始楼层和限时。比赛使用独立存档，自带计时、重打与死亡额度、罚秒和可选娱乐规则。停表后，选手可导出带本局编号和事件时间轴的成绩 JSON。

[安装与卸载](CupMod/INSTALL.md) · [完整比赛规则](CupMod/README.md)

## 杯赛裁判台

下载并在本地浏览器打开 [Cup-Referee.html](CupMod/Cup-Referee.html)。裁判可载入当届规则，批量导入成绩 JSON，查看规则差异、罚秒和事件时间轴，最后导出暂定排名 CSV。打开网页即可使用。

[裁判台说明](CupMod/REFEREE.md) · [可供试用的虚构成绩](CupMod/examples/example-ritual.json)

## 其他 Mod

| Mod | 版本 | 功能 |
| --- | --- | --- |
| [剧情回放与 CG 画廊](GalleryMod/使用说明.md) | v1.2.0 | 从标题打开 38 个剧情回放条目和 591 张本机 CG，可切换主角外观，回放使用独立存档。 |
| [F9 重打当前房间](RoomRetryMod/使用说明.md) | v1.0.1 | 在战斗开始时建立检查点。进入一场新战斗后，按 F9 回到本场开头。杯赛中由当届规则控制。 |
| [战斗攻击方式切换](AttackModeSwitchMod/使用说明.txt) | v1.1.0 | 在普通战斗中，从原版设置切换 QTE 或概率判定。 |

## 下载与安装

已验证的游戏构建为 **Windows x64、Steam Build 26646212**。使用 [MelonLoader 0.7.3](https://github.com/LavaGang/MelonLoader/releases/tag/v0.7.3)。安装加载器后，先从 Steam 正常启动游戏一次，等待加载器生成 IL2CPP 互操作程序集。

1. 发布后，在 [Releases](../../releases) 的 Assets 中选择需要的 ZIP。GitHub 自动生成的源码 ZIP 用于阅读和构建。
2. 退出游戏并解压 ZIP，把包内 `Mods` 文件夹合并到 `Annihilated.exe` 所在目录。每个包内都附有对应的安装和卸载说明。
3. 杯赛选手还需把 `Launch-Cup.cmd` 复制到游戏目录，保持 Steam 开启并双击它。杯赛包包含同版 F9 插件，是否允许重打由当届规则决定。其他 Mod 从 Steam 正常启动即可使用。

| 安装包 | 内容 |
| --- | --- |
| `Annihilated-Cup-v0.4.11.zip` | 杯赛 Mod、F9 插件、杯赛启动脚本、裁判台与示例 |
| `Annihilated-Gallery-v1.2.0.zip` | 剧情回放与 CG 画廊 Mod |
| `Annihilated-RoomRetry-v1.0.1.zip` | F9 重打当前房间 Mod |
| `Annihilated-AttackModeSwitch-v1.1.0.zip` | 战斗攻击方式切换 Mod |

## 从源码构建

需要 .NET 8 SDK、PowerShell 7，以及已经用 MelonLoader 0.7.3 启动过的本机游戏。四个项目面向 `net6.0`，首次构建可能需要由 SDK 获取对应的参考包。

请在仓库根目录执行下面的命令，并填入你的游戏目录。

```powershell
pwsh -NoProfile -File ./scripts/Build-Releases.ps1 -GameRoot 'D:\SteamLibrary\steamapps\common\Annihilated'
```

脚本会构建四个项目，并在 `dist/` 生成上表中的四个 ZIP 和 `SHA256SUMS.txt`。只构建某个 Mod 时，把上面同一个游戏目录传给 `dotnet build` 的 `-p:GameRoot=...`。下面以杯赛为例。

```powershell
dotnet build ./CupMod/AnnihilatedCup.csproj -c Release '-p:GameRoot=D:\SteamLibrary\steamapps\common\Annihilated'
```

两个独立的逻辑检查可在没有游戏的环境中运行：

```powershell
dotnet run --project ./CupMod/Checks/Checks.csproj
dotnet run --project ./RoomRetryMod/tests/RoomRetry.Tests.csproj
```

## 制作与依赖

Mod 与裁判台由 Brick 制作。原作由 Death Kumiai 开发、Flashlight Games 发行。画廊和回放通过索引从本机游戏加载原作内容，游戏素材与互操作程序集由本机安装提供。

[Steam 游戏页面](https://store.steampowered.com/app/2638080/Annihilated/) · [MelonLoader](https://github.com/LavaGang/MelonLoader)
