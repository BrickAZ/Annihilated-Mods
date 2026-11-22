# Annihilated PNG 换皮 Mod v0.1.0

## Windows 贴图编辑器

从游戏目录打开贴图编辑器，或通过「更改目录」选中游戏。v1.1.2 会检查换皮 Mod 和 MelonLoader，缺失时在后台安装。编辑器从 GitHub 的换皮发布下载经过 SHA-256 校验的 DLL，安装器会补齐缺失的前置。右下角显示安装状态。如果游戏正在运行或下载失败，处理后点击「重试安装」。

双击游戏目录里的 `AnnihilatedTextureEditor.exe`。选择「怪物立绘」或「物品图标」，搜索并选中一项，导入 PNG，核对右侧预览后点「应用替换」。已有替换会显示在列表和预览里。返回窗口时会重新读取贴图，也可按 F5 手动刷新。点「恢复原图」可以停用这一项，之前的 PNG 会备份到 `UserData/TextureOverrides/.editor-backups`。应用或恢复后重启游戏生效。

怪物列表按敌人分组。在右侧选择状态或形态，点下方的帧缩略图切换预览。可以导入 PNG 替换选中的一帧，也可以一次导入同尺寸的完整图集，或选择对应数量的单帧 PNG。批量导入按文件名的数字顺序排帧，例如 `frame2.png` 放在 `frame10.png` 之前。可以连续修改同一状态的几帧，再点一次「应用替换」。切换到另一个状态前先应用当前修改。

普通图片和单帧 PNG 可以等比缩放并补透明边，整张图集需要沿用原尺寸和排布。「导出原图集」可以拿到完整模板。原版图的预览和导出来自本机的美术资源目录。

## Mod 安装

联网一键安装包面向 Windows x64 Steam Build 26845264 和 MelonLoader 0.7.3。也可把 `AnnihilatedTextureOverride.dll` 放进游戏的 `Mods` 目录。首次启动时，Mod 会在游戏目录创建以下文件夹。

```text
UserData/
  TextureOverrides/
    Enemy/
    Item/
```

## 放图

1. 在同一游戏目录的「mod开发相关 → Annihilated_美术资源 → 美术资源导航.html」里找「敌人」或「物品 → 武器」。
2. 从目标的「原始贴图」复制一张 PNG，按原始贴图名保存。资源清单的 `original_path` 列可以核对原名。例如 `Assets/Textures/Enemy/goblin.png` 对应 `Enemy/goblin.png`，`Assets/Textures/Item/ironSword.png` 对应 `Item/ironSword.png`。导出名如果带有为避免重名而加上的后缀，以 `original_path` 为准。
3. 保留完整画布尺寸和透明通道。哥布林是 511 × 481，铁剑是 65 × 65。带多帧的原图需要交一张完整 PNG 图集，将每帧画回原来所在的位置。有独立攻击、破防或变身贴图的怪物，可以分别按原资源名放图。
4. 把敌人战斗立绘放入 `Enemy`，把受支持的物品图标放入 `Item`，然后完整退出并重启游戏。移走 PNG 后重启即可恢复该项。

`Item` 支持 [ItemIcons.txt](src/ItemIcons.txt) 中的图标，包括武器和龙之眼。换图 Mod 和编辑器使用同一份清单。龙之眼的资源名是 `dragonEye`，原尺寸为 65 × 65。角色挥武器的战斗动作图属于游戏的另一套资源。

## 看加载结果

在 `MelonLoader/Latest.log` 查找 `Annihilated_Texture_Override`。有 `Loaded Enemy/...` 或 `Loaded Item/...` 表示该 PNG 已被游戏请求并加载。坏图或尺寸不合时，日志会写明原因，游戏显示原图。

## 实现

在 Unity Addressables 的 AssetBundle 请求完成、结果进入原版缓存之前，用资源路径匹配 PNG。Mod 保留原 Sprite 的矩形、锚点、像素密度、边框、点击轮廓和帧名。同一 PNG 加载一次，供这个资源的各帧复用。原版继续负责怪物状态选择、播放和资源句柄。
