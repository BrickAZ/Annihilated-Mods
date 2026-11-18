# 杯赛 Mod v0.4.13 安装说明

[English](INSTALL.en.md) | 简体中文

适用于 Windows x64 和 Steam Build `26705080`，使用 MelonLoader 0.7.3。游戏目录是含 `Annihilated.exe` 的文件夹，可在 Steam 库中右键点击游戏，选择「管理 → 浏览本地文件」。

[下载 Annihilated-Cup-v0.4.13.zip](https://github.com/BrickAZ/Annihilated-Mods/releases/download/v1.0.4/Annihilated-Cup-v0.4.13.zip) · [SHA-256 校验值](https://github.com/BrickAZ/Annihilated-Mods/releases/download/v1.0.4/Annihilated-Cup-v0.4.13.sha256)

## 安装

1. 退出游戏，完整解压 ZIP，双击 `一键安装.cmd` 或 `Install.cmd`。两个文件运行同一个安装器。如果安装器未找到游戏，请选择游戏目录，或把该目录拖到 CMD 文件上。
2. 安装器会检查游戏文件，安装杯赛 Mod、F9 房间重试 v1.0.1 和 `Launch-Cup.cmd`。缺少 MelonLoader 0.7.3 或便携版 .NET 6.0.36 时会自动下载并校验。Cpp2IL 和配套插件缺失或校验失败时也会从官方来源重新下载。
3. 如果提示游戏文件缺失或格式异常，请在 Steam 验证游戏文件的完整性，再运行安装器。遇到 Cpp2IL 下载失败，或启动日志出现 `Failed to Execute Cpp2IL`，可使用离线修复包。[离线修复步骤](../README.zh-CN.md#离线前置修复)

## 启动杯赛

1. 保持 Steam 开启，双击游戏目录里的 `Launch-Cup.cmd`。首次使用 MelonLoader 启动时，请等到标题界面。日常游玩可以从 Steam 正常启动。
2. 在标题界面打开「杯赛配置」，按主办方给出的规则开赛。杯赛读取游戏上次保存的语言设置，中文设置下显示中文，其他设置下显示英文。切换时先在普通模式更改语言并退出，再运行 `Launch-Cup.cmd`。
3. 停表后，在杯赛 HUD 下方点「导出比赛成绩」。把 `UserData/AnnihilatedCup/exports` 下的成绩 JSON 和录像一起交给裁判，用八位本局编号对应两份材料。完整比赛规则见 ZIP 内的 `README.md` 和 `GUIDE.en.md`。

如果标题界面没有「杯赛配置」，确认是通过 `Launch-Cup.cmd` 启动，并检查游戏目录中是否有 `Mods/AnnihilatedCup.dll`。启动错误可查看 `MelonLoader/Latest.log`。

## 打开裁判台

完整解压杯赛 ZIP 后，在浏览器打开 `Cup-Referee.html` 即可离线使用。右上角可切换简体中文和英文。同一场比赛的选手应使用同一杯赛版本、游戏构建和规则，可以混合导入两种语言客户端的成绩。操作详见包内的 `REFEREE.md`。

## 卸载

退出游戏后，从游戏目录删除 `Mods/AnnihilatedCup.dll` 和 `Launch-Cup.cmd`。如果还想在日常游玩时按 F9 重试，保留 `Mods/AnnihilatedRoomRetry.dll`。比赛配置和历史成绩位于 `UserData/AnnihilatedCup`，可按需备份。
