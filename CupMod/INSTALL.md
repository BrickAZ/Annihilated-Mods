# 杯赛 Mod v0.4.12 安装说明

测试环境为 Windows x64、Steam Build 26705080 和 MelonLoader 0.7.3。下文的「游戏目录」是你电脑上含 `Annihilated.exe` 的文件夹。Steam 用户可在库中右键点击游戏，选择「管理 → 浏览本地文件」。

## 选手

1. 退出游戏，完整解压 `Annihilated-Cup-v0.4.12.zip`，双击 `一键安装.cmd`。如果未自动找到游戏，请选择含 `Annihilated.exe` 的目录。
2. 安装器会联网补齐 MelonLoader 0.7.3 和便携版 .NET 6，并安装 `Launch-Cup.cmd`、杯赛 DLL 及 F9 房间重试 v1.0.1。首次启动时请等到标题画面。
3. 打杯赛时双击游戏目录中的 `Launch-Cup.cmd`。Steam 版请保持 Steam 开启。在标题界面进入「杯赛配置」，按主办方给出的规则开赛。日常游玩按平时的方式启动游戏。
4. 停表后点杯赛 HUD 下方的「导出比赛成绩」。提交游戏目录中 `UserData/AnnihilatedCup/exports` 下的本局 JSON 和录像，用八位本局编号对应两份材料。

如果标题上未显示「杯赛配置」，检查 `Mods/AnnihilatedCup.dll` 的位置和 `MelonLoader/Latest.log`，并确认已经用 `Launch-Cup.cmd` 启动。

## 裁判

双击解压目录中的 `Cup-Referee.html` 即可在浏览器离线使用。裁判只需浏览器，操作见[裁判台说明](REFEREE.md)。

## 卸载

退出游戏，删除 `Mods/AnnihilatedCup.dll` 和 `Launch-Cup.cmd`。如果仍想在普通游戏中使用 F9，可以保留 `Mods/AnnihilatedRoomRetry.dll`。比赛配置和历史成绩在 `UserData/AnnihilatedCup`，可按需备份。
