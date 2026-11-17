# 存档扩容 v1.25 适配工具

适用于 Windows Steam Build `26705080` 和 MelonLoader `0.7.3`。用你已有的 dsh 原版 `SaveSlotUnlimiter.dll` 生成适配 DLL，修复游戏更新后存档页仍只有原版槽位的问题。

1. 退出游戏。解压适配工具，在解压目录打开 PowerShell 7。
2. 运行下面的命令，按提示输入原版 v1.25 DLL 和游戏目录。Steam 用户可在游戏库中右键游戏，选择「管理 → 浏览本地文件」。

```powershell
$original = (Read-Host '原版 v1.25 SaveSlotUnlimiter.dll 的路径').Trim().Trim('"')
$gameRoot = (Read-Host '含 Annihilated.exe 的游戏目录').Trim().Trim('"')
./Patch-Slot125.ps1 -InputDll $original -GameRoot $gameRoot
```

3. 把生成的 `patched/SaveSlotUnlimiter.dll` 复制到 `Annihilated.exe` 旁边的 `Mods` 目录。已有同名插件时，先备份再覆盖。
4. 正常启动游戏。存档和读档页会显示 300 格，包含自动存档。适配版在独立存档中通过了第 81 和第 299 手动槽的保存与读回测试。

原始 DLL SHA-256 为 `B08ADB31EF98008AC5D57C3F8B0298896FE9DD8C76B18A5D9A528F418E042396`。脚本会校验输入与输出。
