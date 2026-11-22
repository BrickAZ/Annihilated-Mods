# SaveSlotUnlimiter v1.25 存档扩容

适用于 Windows x64、Steam Build `26845264` 和 `26705080`，使用 MelonLoader `0.7.3`。安装后，游戏原有的存档与读档界面显示 300 个槽位，含 1 个自动存档槽和 299 个手动槽。Build `26845264` 的原版界面有 180 个槽位。

1. 退出游戏，完整解压 ZIP，双击 `一键安装.cmd`。如果未自动找到游戏，请选择含 `Annihilated.exe` 的目录。
2. 安装器直接安装包内的 `Mods/SaveSlotUnlimiter.dll`，并在缺少时联网补齐 MelonLoader 和 .NET。已有旧版同名 DLL 时，安装器会先将它备份到游戏目录的 `UserData/AnnihilatedModsInstaller/backups/`。
3. 按平时的方式启动游戏。首次使用 MelonLoader 时，请等互操作程序集生成完毕。

卸载时退出游戏并移除 `Mods/SaveSlotUnlimiter.dll`。扩容范围内的存档仍在原有存档目录。卸载后，超出原版槽位上限的档位可能无法从原版菜单选择。

SaveSlotUnlimiter 由 **dsh** 制作，适配与打包由 **Brick** 完成。适配 DLL 经 dsh 许可分发。Build `26845264` 已在隔离档验证原版自动档读回、第 180 和第 299 手动槽的保存与读回、第 180 槽覆盖确认，以及重启后的存档列表。Build `26705080` 已在隔离档验证第 81、299 手动槽的保存与读回。
