# 杯赛 Mod v0.4.12 安装说明

测试环境为 Windows x64、Steam Build 26705080 和 MelonLoader 0.7.3。下文的「游戏目录」是你电脑上含 `Annihilated.exe` 的文件夹。Steam 用户可在库中右键点击游戏，选择「管理 → 浏览本地文件」。

## 选手

1. 按 [MelonLoader 官方安装说明](https://github.com/LavaGang/MelonLoader/blob/v0.7.3/README.md#requirements) 给游戏安装 0.7.3。如果加载器提示缺少 .NET，按官方说明安装 .NET 6 Desktop Runtime（Windows x64）。首次配置后，正常启动游戏并等到标题界面。
2. 退出游戏，完整解压 `Annihilated-Cup-v0.4.12.zip`。把解压出的 `Mods` 文件夹合并到游戏目录，再把 `Launch-Cup.cmd` 复制到 `Annihilated.exe` 旁边。
3. 确认游戏目录里有 `Launch-Cup.cmd`、`Mods/AnnihilatedCup.dll` 和 `Mods/AnnihilatedRoomRetry.dll`。后者随杯赛包附带，版本为 v1.0.1。
4. 打杯赛时双击游戏目录中的 `Launch-Cup.cmd`。Steam 版请保持 Steam 开启。在标题界面进入「杯赛配置」，按主办方给出的规则开赛。日常游玩按平时的方式启动游戏。
5. 停表后点杯赛 HUD 下方的「导出比赛成绩」。提交游戏目录中 `UserData/AnnihilatedCup/exports` 下的本局 JSON 和录像，用八位本局编号对应两份材料。

如果标题上未显示「杯赛配置」，检查 `Mods/AnnihilatedCup.dll` 的位置和 `MelonLoader/Latest.log`，并确认已经用 `Launch-Cup.cmd` 启动。

## 裁判

双击解压目录中的 `Cup-Referee.html` 即可在浏览器离线使用。裁判只需浏览器，操作见[裁判台说明](REFEREE.md)。

## 卸载

退出游戏，删除 `Mods/AnnihilatedCup.dll` 和 `Launch-Cup.cmd`。如果仍想在普通游戏中使用 F9，可以保留 `Mods/AnnihilatedRoomRetry.dll`。比赛配置和历史成绩在 `UserData/AnnihilatedCup`，可按需备份。
