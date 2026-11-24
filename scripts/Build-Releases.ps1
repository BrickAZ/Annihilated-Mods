#Requires -Version 7.0
param(
    [Parameter(Mandatory)]
    [string]$GameRoot,
    [string]$DistDirectory = (Join-Path $PSScriptRoot '../dist'),
    [string]$ArtifactDirectory = (Join-Path $PSScriptRoot '../.work/26845264-build'),
    [string]$Dotnet = 'dotnet',
    [string]$DragonSkinArchive,
    [string]$BonfireDll,
    [ValidateSet('cup', 'gallery', 'room-retry', 'attack-mode', 'save-slot', 'enemy-intent', 'playable-rivals', 'texture-override', 'dragon-skin', 'bonfire-fusion')]
    [string[]]$Package,
    [switch]$UseBuiltArtifacts,
    [switch]$SaveSlotOnly
)

$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$game = (Get-Item -LiteralPath $GameRoot).FullName
$artifacts = [IO.Path]::GetFullPath($ArtifactDirectory)
$build = '26845264'
$assemblyHash = 'AB27FAA27ABF2AE18431C1586662C0B4720F7F59DACEAF6399B6B5BB9F2C8E39'
$catalogHash = '004a514c06f1f4a7563e5f75c24d55e4'

if ((Get-FileHash -LiteralPath (Join-Path $game 'GameAssembly.dll')).Hash -ne $assemblyHash -or
    (Get-Content -Raw -LiteralPath (Join-Path $game 'Annihilated_Data/StreamingAssets/aa/catalog.hash')).Trim() -ne $catalogHash) {
    throw "GameRoot must contain the Steam Build $build game files."
}

$projectPaths = @{
    cup = 'CupMod/AnnihilatedCup.csproj'
    gallery = 'GalleryMod/AnnihilatedGallery.csproj'
    retry = 'RoomRetryMod/src/AnnihilatedRoomRetry.csproj'
    attack = 'AttackModeSwitchMod/src/AnnihilatedAttackModeSwitch.csproj'
    intent = 'EnemyIntentMod/src/AnnihilatedEnemyIntent.csproj'
    rivals = 'PlayableRivalsMod/src/AnnihilatedPlayableRivals.csproj'
    texture = 'TextureOverrideMod/src/AnnihilatedTextureOverride.csproj'
}

$packages = [ordered]@{
    'cup' = @{
        Name = 'Cup'; Projects = @('cup', 'retry'); Native = $true
        Files = [ordered]@{
            'Mods/AnnihilatedCup.dll' = "$artifacts/cup/AnnihilatedCup.dll"
            'Mods/AnnihilatedRoomRetry.dll' = "$artifacts/retry/AnnihilatedRoomRetry.dll"
            'Launch-Cup.cmd' = "$repo/CupMod/Launch-Cup.cmd"
            'Cup-Referee.html' = "$repo/CupMod/Cup-Referee.html"
            'INSTALL.md' = "$repo/CupMod/INSTALL.md"
            'INSTALL.en.md' = "$repo/CupMod/INSTALL.en.md"
            'README.md' = "$repo/CupMod/README.md"
            'GUIDE.en.md' = "$repo/CupMod/GUIDE.en.md"
            'REFEREE.md' = "$repo/CupMod/REFEREE.md"
            'examples/example-ritual.json' = "$repo/CupMod/examples/example-ritual.json"
        }
    }
    'gallery' = @{
        Name = 'Gallery'; Projects = @('gallery'); Native = $true
        Files = [ordered]@{
            'Mods/AnnihilatedGallery.dll' = "$artifacts/gallery/AnnihilatedGallery.dll"
            'README.md' = "$repo/GalleryMod/使用说明.md"
        }
    }
    'room-retry' = @{
        Name = 'RoomRetry'; Projects = @('retry')
        Files = [ordered]@{
            'Mods/AnnihilatedRoomRetry.dll' = "$artifacts/retry/AnnihilatedRoomRetry.dll"
            'README.md' = "$repo/RoomRetryMod/使用说明.md"
        }
    }
    'attack-mode' = @{
        Name = 'AttackModeSwitch'; Projects = @('attack')
        Files = [ordered]@{
            'Mods/AnnihilatedAttackModeSwitch.dll' = "$artifacts/attack/AnnihilatedAttackModeSwitch.dll"
            'README.txt' = "$repo/AttackModeSwitchMod/使用说明.txt"
        }
    }
    'save-slot' = @{
        Name = 'SaveSlotUnlimiter'; Native = $true
        Files = [ordered]@{
            'Mods/SaveSlotUnlimiter.dll' = "$repo/SaveSlotUnlimiter/payload/SaveSlotUnlimiter.dll"
            'README.md' = "$repo/SaveSlotUnlimiter/README.md"
        }
    }
    'enemy-intent' = @{
        Name = 'EnemyIntent'; Projects = @('intent')
        Files = [ordered]@{ 'Mods/AnnihilatedEnemyIntent.dll' = "$artifacts/intent/AnnihilatedEnemyIntent.dll" }
    }
    'playable-rivals' = @{
        Name = 'PlayableRivals'; Projects = @('rivals'); Native = $true
        Files = [ordered]@{
            'Mods/AnnihilatedPlayableRivals.dll' = "$artifacts/rivals/AnnihilatedPlayableRivals.dll"
            'README.md' = "$repo/PlayableRivalsMod/README.md"
            'README.zh-CN.md' = "$repo/PlayableRivalsMod/README.zh-CN.md"
            'CHANGELOG.zh-CN.md' = "$repo/PlayableRivalsMod/CHANGELOG.zh-CN.md"
            'docs/verification-v0.1.16.md' = "$repo/PlayableRivalsMod/docs/verification-v0.1.16.md"
        }
    }
    'texture-override' = @{
        Name = 'TextureOverride-Editor'; Projects = @('texture')
        Files = [ordered]@{
            'Mods/AnnihilatedTextureOverride.dll' = "$artifacts/texture/AnnihilatedTextureOverride.dll"
            'AnnihilatedTextureEditor.exe' = "$artifacts/texture/AnnihilatedTextureEditor.exe"
            'README.md' = "$repo/TextureOverrideMod/README.md"
        }
    }
    'dragon-skin' = @{
        Name = 'DragonSkin'; Projects = @('texture')
        Files = [ordered]@{ 'Mods/AnnihilatedTextureOverride.dll' = "$artifacts/texture/AnnihilatedTextureOverride.dll" }
        ExistingArchive = $DragonSkinArchive
        Reuse = [ordered]@{
            'UserData/TextureOverrides/Enemy/dragon.png' = 'UserData/TextureOverrides/Enemy/dragon.png'
            'UserData/TextureOverrides/Item/dragonEye.png' = 'UserData/TextureOverrides/Item/dragonEye.png'
        }
    }
    'bonfire-fusion' = @{
        Name = 'BonfireFusion'
        Files = [ordered]@{ 'Mods/BonfireFusion.dll' = $BonfireDll }
    }
}

if ($SaveSlotOnly) { $Package = @('save-slot') }
if (!$Package) {
    $Package = @($packages.Keys | Where-Object {
        ($_ -ne 'dragon-skin' -or $DragonSkinArchive) -and ($_ -ne 'bonfire-fusion' -or $BonfireDll)
    })
}
$selected = @($Package | Select-Object -Unique)
if ($selected -contains 'dragon-skin' -and (!$DragonSkinArchive -or
    (Get-FileHash -LiteralPath $DragonSkinArchive).Hash -ne '6E53C296E493282F0BB6858AF654098C9EB43204457C4334188F258BD4447314')) {
    throw 'Provide the verified Annihilated-Nailong-Skin-v0.1.0-Manual.zip as -DragonSkinArchive.'
}
if ($selected -contains 'bonfire-fusion' -and (!$BonfireDll -or
    (Get-FileHash -LiteralPath $BonfireDll).Hash -ne 'EA526F31B0ABF84219416790DB751E33C61223E0192DF6C29AD35B2FE81962A1')) {
    throw 'Provide the published BonfireFusion v1.0.0 DLL as -BonfireDll.'
}
$dist = (New-Item -ItemType Directory -Path $DistDirectory -Force).FullName

if (!$UseBuiltArtifacts) {
    $neededProjects = @($selected | ForEach-Object { $packages[$_].Projects } | Select-Object -Unique)
    foreach ($project in $neededProjects) {
        & $Dotnet build (Join-Path $repo $projectPaths[$project]) --configuration Release --nologo -o (Join-Path $artifacts $project) "-p:GameRoot=$game" "-p:PathMap=$repo=/_/Annihilated-Mods" -p:ContinuousIntegrationBuild=true -p:NuGetAudit=false
        if ($LASTEXITCODE -ne 0) { throw "Build failed: $project" }
    }
    if ($selected -contains 'texture-override') {
        & (Join-Path $repo 'TextureOverrideMod/editor/Build-Editor.ps1') -OutputPath "$artifacts/texture/AnnihilatedTextureEditor.exe" | Out-Null
    }
}

$installText = @'
关闭游戏，把 ZIP 完整解压到一个文件夹，再双击「一键安装.cmd」。
若安装器没有自动找到游戏，把含 Annihilated.exe 的目录拖到 CMD 上。
安装器会联网从官方来源下载缺失前置并校验。首次启动请等到标题界面。
本包面向 Windows x64、Steam Build 26845264 和 MelonLoader 0.7.3。
'@
$featureNotes = @{
    cup = '用游戏目录里的 Launch-Cup.cmd 开赛。裁判台可直接在浏览器打开。'
    gallery = '从标题菜单打开 CG 画廊和剧情回放。'
    'room-retry' = '在战斗里按 F9 返回当前战斗的入口。'
    'attack-mode' = '在游戏设置中切换战斗攻击方式。'
    'save-slot' = '存读档菜单扩展到 300 槽。原生版本的存档槽较少，高位存档需继续通过适配版 Mod 读取。'
    'enemy-intent' = '敌人头顶显示即将执行的行动。悬停可查看其行动列表。同类连续攻击会合并显示次数。'
    'playable-rivals' = '三名可玩角色 v0.1.16。新开存档，在勇者、战士、魔法师位置分别选择放逐者、反派千金、落选者。升级会备份同名旧 DLL，其他模组和存档保留。'
    'texture-override' = '游戏目录内的 AnnihilatedTextureEditor.exe 可编辑 PNG 换皮。更换贴图后重新启动游戏。'
    'dragon-skin' = '替换四层黄龙与其龙眼的贴图。'
    'bonfire-fusion' = '独立版 v1.0.0。在有合格材料的火堆预留一个武器合成选项。杯赛自带独立的合成规则，比赛配置请只启用杯赛的合成规则。'
}
function Write-ZipText([IO.Compression.ZipArchive]$Zip, [string]$Name, [string]$Content) {
    $writer = [IO.StreamWriter]::new($Zip.CreateEntry($Name).Open(), [Text.UTF8Encoding]::new($true))
    try { $writer.WriteLine($Content) } finally { $writer.Dispose() }
}

foreach ($id in $selected) {
    $spec = $packages[$id]
    $target = Join-Path $dist "Annihilated-$($spec.Name)-Build$build-Online.zip"
    $pending = "$target.$([guid]::NewGuid()).tmp"
    try {
        $zip = [IO.Compression.ZipFile]::Open($pending, [IO.Compression.ZipArchiveMode]::Create)
        try {
            foreach ($launcher in @('一键安装.cmd', 'Install.cmd')) {
                [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, (Join-Path $PSScriptRoot 'Install-Mod.cmd'), $launcher) | Out-Null
            }
            [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, (Join-Path $PSScriptRoot 'Install-Mod.ps1'), 'Install-Mod.ps1') | Out-Null
            Write-ZipText $zip 'Install-Package.txt' $id
            Write-ZipText $zip '安装说明.txt' "$installText$([Environment]::NewLine)$($featureNotes[$id])"
            if ($spec.Native) {
                $buildRows = @('Build,GameAssemblySha256,CatalogHash', "$build,$assemblyHash,$catalogHash")
                if ($id -eq 'save-slot') {
                    $buildRows += '26705080,13B13F97BBE50154B451753351656F99595AE0A542586097F27536F55C24F3C4,c867cf04b05171a82710f970f4122af7'
                }
                Write-ZipText $zip 'Supported-Builds.csv' ($buildRows -join [Environment]::NewLine)
            }
            foreach ($entry in $spec.Files.GetEnumerator()) {
                [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $entry.Value, $entry.Key) | Out-Null
            }
            if ($spec.ExistingArchive) {
                $original = [IO.Compression.ZipFile]::OpenRead($spec.ExistingArchive)
                try {
                    foreach ($entry in $spec.Reuse.GetEnumerator()) {
                        $source = $original.GetEntry($entry.Value)
                        if (!$source) { throw "Missing $($entry.Value) in $($spec.ExistingArchive)" }
                        $sourceStream = $source.Open()
                        $output = $zip.CreateEntry($entry.Key, [IO.Compression.CompressionLevel]::Optimal).Open()
                        try { $sourceStream.CopyTo($output) } finally { $output.Dispose(); $sourceStream.Dispose() }
                    }
                } finally { $original.Dispose() }
            }
        } finally { $zip.Dispose() }
        [IO.File]::Move($pending, $target, $true)
        Write-Host "Created $target"
    } finally { [IO.File]::Delete($pending) }
}

$checksums = Get-ChildItem -LiteralPath $dist -File -Filter "*-Build$build-Online.zip" | Sort-Object Name | ForEach-Object {
    "$((Get-FileHash -LiteralPath $_.FullName).Hash.ToLowerInvariant())  $($_.Name)"
}
$checksums | Set-Content -LiteralPath (Join-Path $dist 'SHA256SUMS.txt') -Encoding utf8
