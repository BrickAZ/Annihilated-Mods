# 怪物意图显示 Mod v0.1.0

[English](README.md) | 简体中文

[下载 Windows 测试版](https://github.com/BrickAZ/Annihilated-Mods/releases/tag/enemy-intent-v0.1.0)

怪物头顶会显示下一招的「单次伤害 × 攻击次数」，例如 `12 ×2`。伤害、攻击类型、目标和警告状态都相同的连续攻击会合并成一行，格挡等效果继续显示原版图标。游戏重算伤害或更换下一招后，提示也会更新。敌人依旧按原版规则选招。

## 安装

1. 退出游戏。如未安装加载器，先安装 [MelonLoader 0.7.3](https://github.com/LavaGang/MelonLoader/releases/tag/v0.7.3)。
2. 解压 ZIP，把里面的 `Mods` 合并到含 `Annihilated.exe` 的游戏目录。
3. 正常启动游戏，进入战斗即可看到提示。卸载时退出游戏，移除 `Mods/AnnihilatedEnemyIntent.dll`。

测试环境为 Windows x64、Steam Build `26705080`。哥布林战斗已验证伤害次数、连续攻击、格挡图标和意图变化。复杂首领和多怪同屏的布局仍待验证。

## 从源码构建

需要 .NET 8 SDK。游戏需用 MelonLoader 启动过一次以生成互操作程序集。在本目录运行以下命令。

```powershell
$gameRoot = (Read-Host '游戏文件夹路径').Trim().Trim('"')
dotnet build ./src/AnnihilatedEnemyIntent.csproj -c Release "-p:GameRoot=$gameRoot"
dotnet run --project ./tests/Rows/Rows.csproj -c Release
```

10 项格式检查可以脱离游戏运行。`tests/Smoke` 用于隔离的杯赛哥布林战斗测试，安装包只含正式 Mod。