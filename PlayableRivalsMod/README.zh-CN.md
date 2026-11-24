# 三名可玩角色 · v0.1.16

[English](README.md) | 简体中文

将原版的勇者、战士、魔法师三个同伴槽位分别替换为放逐者、反派千金、落选者。包含专属武器、主动技能、选人横幅、像素小人、理智状态头像及战斗美术。作者：Brick。

适用版本：Windows x64、Steam Build `26845264`、MelonLoader `0.7.3`。模组新增文字目前为中文。

[下载 v0.1.16](https://github.com/BrickAZ/Annihilated-Mods/releases/tag/playable-rivals-v0.1.16) · [版本记录](CHANGELOG.zh-CN.md) · [验证范围](docs/verification-v0.1.16.md)

## 安装与选人

1. 先为游戏配置 MelonLoader 0.7.3，并确认可以进入标题界面。已有本仓库模组环境的玩家可沿用前置。
2. 退出游戏，从 Release 的 Assets 下载 `Annihilated-PlayableRivals-v0.1.16.zip`。这是手动安装包，包含 `Mods/AnnihilatedPlayableRivals.dll` 和 `README.md`，没有一键安装器，也不附带加载器。
3. 解压到含 `Annihilated.exe` 的游戏根目录，让 DLL 位于游戏的 `Mods` 文件夹。升级前备份旧的同名 DLL。
4. 正常启动游戏，新开一个存档。序章选人时，勇者的位置是放逐者，战士的位置是反派千金，魔法师的位置是落选者；僧侣保留原版。

已有原版存档不会自动变成新角色。已有本模组存档可以继续使用；卸载前保留备份，继续原版游玩时使用原版角色存档。卸载时退出游戏，移走 `Mods/AnnihilatedPlayableRivals.dll`。

## 三名角色

| 角色 | 初始专武效果 | 一楼技能 | 二楼技能 | 三楼技能 |
| --- | --- | --- | --- | --- |
| 放逐者 | 魔剑初始伤害 23；命中夺取敌方强化 | 抢夺准备 | 切换匕首 | 元素附魔 |
| 反派千金 | 人偶之线初始伤害 15 | 对战士的爱 | 傀儡 | 召唤傀儡 |
| 落选者 | 魔力法典初始给予 15 点格挡 | 法典 | 召唤屏障 | 覆甲 |

- 放逐者的抢夺准备可用于下一次普通武器攻击。自带不可夺取词条的武器不开放夺取 QTE，但仍消耗准备；落空也消耗准备。附魔不消耗行动，切换匕首消耗行动，匕首从下回合生效。
- 对战士的爱为全体存活敌人施加燃烧和中毒，并治疗千金。施放前生命严格低于 70% 时不消耗行动，仍计算冷却。
- 落选者可提供结界、召唤屏障和转移覆甲。覆甲后魔力归零，改用物理攻击，魔法技能禁用。覆甲后专武单击物理伤害使用减半后的成长公式。
- 后续技能随楼层解锁，选人初期只预览初始技能与专武。剧情槽位、对白及剧情专用动作沿用原作。

## v0.1.16：放逐者的伤害限制

击败赫德夺舍的士兵之前，放逐者一次武器或技能使用中，所有段数、所有目标的直接伤害合计最多 30。单段也不能超过 30。上限在增伤、暴击、减伤和格挡之后、扣除生命之前结算。

一次出手两段各 20 点时，实际为 20＋10；匕首的两次独立行动各自计算。燃烧、中毒等原版分类下的间接伤害不受限制。抢夺仍能取得武器，但不会绕过上限将仍有生命的普通敌人直接处决。

整场目标首领战胜利后解除，击倒第一形态不会提前解锁。解除标记随当前周目存档保存；带有原版已完成标记的旧模组存档会补记解除。卡牌保留基础数值并显示上限提示。Lv21 魔剑基础 48，限制期间实际最多 30，解除后恢复正常结算。

## 从源码构建

需要 .NET 8 SDK、本机游戏及 MelonLoader 0.7.3。先启动一次游戏，生成 `MelonLoader/Il2CppAssemblies`。在仓库根目录运行：

```powershell
dotnet build PlayableRivalsMod/src/AnnihilatedPlayableRivals.csproj -c Release -p:GameRoot="E:/SteamLibrary/steamapps/common/Annihilated"
dotnet run --project PlayableRivalsMod/Checks/Checks.csproj -c Release
```

把示例路径换成自己的游戏目录，也可通过环境变量 `ANNIHILATED_GAME_ROOT` 指定。输出为 `PlayableRivalsMod/src/bin/Release/net6.0/AnnihilatedPlayableRivals.dll`。图片已嵌入 DLL，使用时不需要额外复制美术文件。仓库不提供游戏本体、游戏程序集、MelonLoader 或个人存档。

规则检查不需要游戏，可以在 GitHub Actions 运行；原生战斗、QTE 和首领分支验证需要实际游戏，详见验证记录。角色形象及原作素材属于原作者，仓库内美术仅为本模组使用的替换与衍生素材。
