# 杯赛 Mod v0.4.11 安装说明

适用于 Windows x64、Steam Build 26646212 和 [MelonLoader 0.7.3](https://github.com/LavaGang/MelonLoader/releases/tag/v0.7.3)。

## 选手

1. 给已经安装的游戏安装 MelonLoader 0.7.3。先从 Steam 正常启动游戏一次并等待进入标题界面，让加载器生成互操作程序集。
2. 退出游戏，解压 `Annihilated-Cup-v0.4.11.zip`。在 Steam 库右键点击游戏，选择「管理 → 浏览本地文件」，打开含 `Annihilated.exe` 的目录。
3. 把压缩包内的 `Mods` 文件夹合并到该目录，再把 `Launch-Cup.cmd` 复制到同一目录。包里已包含配合杯赛使用的 F9 插件 v1.0.1。
4. 保持 Steam 开启，双击游戏目录中的 `Launch-Cup.cmd`。在标题界面进入「杯赛配置」，按主办方给出的规则开赛。日常游戏仍可从 Steam 正常启动。
5. 停表后点杯赛 HUD 下方的「导出比赛成绩」。提交 `UserData/AnnihilatedCup/exports` 内本局的 JSON 和录像，用八位本局编号对应两份材料。

如果标题上未显示「杯赛配置」，检查 `Mods/AnnihilatedCup.dll` 的位置和 `MelonLoader/Latest.log`，并确认已经用 `Launch-Cup.cmd` 启动。

## 裁判

解压后可直接在浏览器打开 `Cup-Referee.html`，详见[裁判台说明](REFEREE.md)。

## 卸载

退出游戏，删除 `Mods/AnnihilatedCup.dll` 和 `Launch-Cup.cmd`。如果仍想在普通游戏中使用 F9，可以保留 `Mods/AnnihilatedRoomRetry.dll`。比赛配置和历史成绩在 `UserData/AnnihilatedCup`，可按需备份。
