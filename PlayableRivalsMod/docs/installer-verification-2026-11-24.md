# 安装器验证 · 2026-11-24

本次新增三名可玩角色 v0.1.16 的联网一键安装包，并修订奶龙换皮安装器。角色玩法 DLL 未改动。

## 版本报错原因

- 本机 Steam 清单为 Build `26845264`。SteamCMD 匿名连接 Steam 后执行 `app_info_update 1`、`app_info_print 2638080`，公开分支同样为 `26845264`，Depot 清单为 `341278220428331631`。
- 本机 `GameAssembly.dll` SHA-256 为 `AB27FAA27ABF2AE18431C1586662C0B4720F7F59DACEAF6399B6B5BB9F2C8E39`，资源目录哈希为 `004a514c06f1f4a7563e5f75c24d55e4`。
- 截图中的旧奶龙安装器将 Build `26705080` 的程序集和目录哈希写死。在 Build `26845264` 的隔离副本中原样复现了“此安装包需要 Steam Build 26705080”的失败。
- 结论：旧安装包与当前游戏不匹配，不能据此要求用户降级游戏。当前安装器保留三人角色包的文件指纹校验，版本失败时显示支持版本、本机清单版本及实际文件指纹。

## 安装回归

通过 CMD 调用 Windows PowerShell 5.1 执行真实安装脚本，不以 PowerShell 7 解析成功代替安装测试。

| 场景 | 结果 |
| --- | --- |
| 原安装器识别新的 `playable-rivals` 包 | 修改前拒绝包标识；修改后完整安装通过 |
| 无加载器的干净游戏副本 | 安装模组、MelonLoader、便携 .NET、Cpp2IL 和 Unity 依赖，逐项核对哈希 |
| 空下载缓存 | 从上游来源下载依赖，校验后安装通过 |
| 无效 IL2CPP 元数据 | 安装前拒绝 |
| 游戏资源指纹不匹配 | 安装前拒绝，不写入角色 DLL 或加载器 |
| 中文 Steam `installdir` + UTF-8 无 BOM 清单 | 修改前 Windows PowerShell 未能识别目录名；显式按 UTF-8 读取后正确报告清单版本 |
| 中文、空格路径及另一工作目录启动 | 正常安装 |
| 无参数 CMD | 本地游戏目录识别通过；真实 Steam 库自动定位通过 |
| 旧 DLL 升级 | 同名旧 DLL 备份内容一致；其他模组和存档保留 |
| 重复安装 | 健康依赖保留原修改时间，未重复备份相同 DLL |
| 截断 .NET host、空 coreclr、损坏 Cpp2IL | 修复后哈希正确 |
| 目录占据运行库文件路径 | 安装失败，不误报成功，不提前写入关键运行库文件 |

共享安装器最终回归覆盖 10 种包：Cup、Gallery、RoomRetry、AttackModeSwitch、SaveSlotUnlimiter、EnemyIntent、PlayableRivals、TextureOverride-Editor、DragonSkin、BonfireFusion。均通过安装、升级备份、依赖修复、重复运行和无参数入口测试。此项是安装器回归，不代表重新完成各模组全部游戏内功能验证。

在实际游戏目录执行三人角色最终包的无参数 CMD 后，8 个已有模组文件和 106 个存档文件的哈希均与执行前一致。

## 奶龙修订包

现有 v1.0.6 奶龙附件虽然已能安装到 Build 26845264，但其脚本落后于仓库已修复的前置校验逻辑。以目录占据 `System.Runtime.dll` 的回归场景，旧附件错误返回成功；更新脚本后正确失败，普通安装与升级通过。

`Annihilated-DragonSkin-Build26845264-Installer2-Online.zip` 只替换了 `Install-Mod.ps1`。与 v1.0.6 原奶龙包逐项比较，模组 DLL、两张 PNG 及其他文件字节不变。原附件及原校验文件保留，新修订包另配 `SHA256SUMS-Installer2.txt`。

## 发布内容边界

- 三人角色新旧安装包中的 DLL 相同，SHA-256：`D712112116D1A50AC50FCE502EB417D54BF05C5F10AC751876E7AC8F1964C974`。
- 两个新包使用相同共享安装脚本，SHA-256：`9FD024292C2C91FCB271F5826BEA0E7C9F6C6FC130A184783AB6DD6719FF82AA`。
- Cup、Room Retry、Enemy Intent、Playable Rivals 四组独立逻辑检查均通过。
- 本次未改变角色数值、技能或游戏内贴图逻辑；角色原生战斗验证范围继续见 [v0.1.16 验证记录](verification-v0.1.16.md)。

运行安装回归需要 Windows、本机合法游戏文件与已校验前置缓存：

```powershell
pwsh -File scripts/Test-Installers.ps1 -GameRoot "E:/SteamLibrary/steamapps/common/Annihilated" -DistDirectory ./dist -DependencyCache ./installer-cache -Package PlayableRivals -TestCpp2ILDownload
```

包构建入口：

```powershell
pwsh -File scripts/Build-Releases.ps1 -GameRoot "E:/SteamLibrary/steamapps/common/Annihilated" -Package playable-rivals
```
