#Requires -Version 5.1
param(
    [string]$GameDirectory,
    [string]$DependencyCache = $env:ANNIHILATED_INSTALL_CACHE
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
if ($env:ANNIHILATED_INSTALL_OUTPUT_UTF8 -eq '1') {
    [Console]::OutputEncoding = [Text.UTF8Encoding]::new($false)
}

function Test-GameDirectory([string]$Path) {
    return $Path -and (Test-Path -LiteralPath (Join-Path $Path 'Annihilated.exe') -PathType Leaf)
}

function Find-GameDirectory {
    if (Test-GameDirectory $PSScriptRoot) { return $PSScriptRoot }
    $steamRoots = @(
        (Get-ItemProperty -LiteralPath 'HKCU:\Software\Valve\Steam' -ErrorAction SilentlyContinue).SteamPath
        (Get-ItemProperty -LiteralPath 'HKLM:\SOFTWARE\WOW6432Node\Valve\Steam' -ErrorAction SilentlyContinue).InstallPath
        (Join-Path ${env:ProgramFiles(x86)} 'Steam')
    ) | Where-Object { $_ } | Select-Object -Unique

    foreach ($steamRoot in $steamRoots) {
        $libraries = @($steamRoot)
        $folders = Join-Path $steamRoot 'steamapps/libraryfolders.vdf'
        if (Test-Path -LiteralPath $folders -PathType Leaf) {
            $libraries += [regex]::Matches((Get-Content -LiteralPath $folders -Raw -Encoding UTF8), '"path"\s+"([^"\r\n]+)"') |
                ForEach-Object { $_.Groups[1].Value.Replace('\\', '\') }
        }
        foreach ($library in $libraries) {
            $manifest = Join-Path $library 'steamapps/appmanifest_2638080.acf'
            if (!(Test-Path -LiteralPath $manifest -PathType Leaf)) { continue }
            $name = [regex]::Match((Get-Content -LiteralPath $manifest -Raw -Encoding UTF8), '"installdir"\s+"([^"\\/:\r\n]+)"')
            if (!$name.Success -or $name.Groups[1].Value -in @('.', '..')) { continue }
            $path = Join-Path (Join-Path $library 'steamapps/common') $name.Groups[1].Value
            if (Test-GameDirectory $path) { return $path }
        }
    }

    Add-Type -AssemblyName System.Windows.Forms
    $picker = New-Object System.Windows.Forms.FolderBrowserDialog
    $picker.Description = '请选择含 Annihilated.exe 的游戏文件夹。'
    $picker.ShowNewFolderButton = $false
    try {
        if ($picker.ShowDialog() -eq [System.Windows.Forms.DialogResult]::OK) { return $picker.SelectedPath }
    } finally { $picker.Dispose() }
}

function Test-X64GameBinary([string]$Path) {
    $reader = [IO.BinaryReader]::new([IO.File]::OpenRead($Path))
    try {
        if ($reader.BaseStream.Length -lt 64 -or $reader.ReadUInt16() -ne 0x5A4D) { return $false }
        $reader.BaseStream.Position = 0x3C
        $offset = $reader.ReadInt32()
        if ($offset -lt 64 -or $offset -gt $reader.BaseStream.Length - 6) { return $false }
        $reader.BaseStream.Position = $offset
        return ($reader.ReadUInt32() -eq 0x4550 -and $reader.ReadUInt16() -eq 0x8664)
    } finally { $reader.Dispose() }
}

function Assert-GameFiles([string]$Root) {
    $metadata = 'Annihilated_Data/il2cpp_data/Metadata/global-metadata.dat'
    $binaries = @('Annihilated.exe', 'UnityPlayer.dll', 'GameAssembly.dll')
    foreach ($relative in ($binaries + $metadata)) {
        $path = Join-Path $Root $relative
        if (!(Test-Path -LiteralPath $path -PathType Leaf)) {
            throw "游戏文件缺失 $relative。请用 Steam 验证游戏文件的完整性，再重新安装。"
        }
        if ($relative -eq $metadata) {
            $reader = [IO.BinaryReader]::new([IO.File]::OpenRead($path))
            try {
                $valid = $reader.BaseStream.Length -ge 8 -and
                    [BitConverter]::ToString($reader.ReadBytes(4)) -ceq 'AF-1B-B1-FA'
            } finally { $reader.Dispose() }
        } else {
            $valid = Test-X64GameBinary $path
        }
        if (!$valid) {
            throw "游戏文件格式无效 $relative。请用 Steam 验证游戏文件的完整性，再重新安装。"
        }
    }
}

function Assert-SupportedBuild([string]$Game) {
    $profiles = @(Import-Csv -LiteralPath (Join-Path $PSScriptRoot 'Supported-Builds.csv'))
    if (!$profiles.Count) { throw '安装包缺少游戏版本信息。请完整解压 ZIP。' }
    $catalog = Join-Path $Game 'Annihilated_Data/StreamingAssets/aa/catalog.hash'
    $actualCatalog = if (Test-Path -LiteralPath $catalog -PathType Leaf) {
        (Get-Content -LiteralPath $catalog -Raw).Trim()
    } else { '' }
    $actualAssembly = (Get-FileHash -LiteralPath (Join-Path $Game 'GameAssembly.dll')).Hash
    $match = $profiles | Where-Object {
        $_.GameAssemblySha256 -eq $actualAssembly -and $_.CatalogHash -eq $actualCatalog
    } | Select-Object -First 1
    if (!$match) {
        $installedBuild = '未知（未找到对应 Steam 清单）'
        $manifest = Join-Path (Split-Path -Parent (Split-Path -Parent $Game)) 'appmanifest_2638080.acf'
        if (Test-Path -LiteralPath $manifest -PathType Leaf) {
            $manifestText = Get-Content -LiteralPath $manifest -Raw -Encoding UTF8
            $installDir = [regex]::Match($manifestText, '"installdir"\s+"([^"\r\n]+)"')
            $buildId = [regex]::Match($manifestText, '"buildid"\s+"(\d+)"')
            if ($installDir.Success -and $buildId.Success -and
                $installDir.Groups[1].Value -eq (Split-Path -Leaf $Game)) {
                $installedBuild = $buildId.Groups[1].Value
            }
        }
        throw "此安装包支持 Steam Build $($profiles.Build -join '、')；本机 Steam 清单：$installedBuild。游戏文件指纹不匹配（GameAssembly SHA-256：$actualAssembly；资源目录哈希：$actualCatalog）。请使用与游戏版本匹配的安装包：https://github.com/BrickAZ/Annihilated-Mods/releases 。如果版本号相同，请在 Steam 验证游戏文件完整性。"
    }
    Write-Host "游戏版本校验通过 Steam Build $($match.Build)"
}

function Copy-VerifiedDependency([string]$Source, [string]$Destination, [string]$Algorithm, [string]$Hash) {
    $null = New-Item -ItemType Directory -Path (Split-Path -Parent $Destination) -Force
    $pending = "$Destination.$([guid]::NewGuid().ToString('N')).tmp"
    try {
        Copy-Item -LiteralPath $Source -Destination $pending
        if ((Get-FileHash -LiteralPath $pending -Algorithm $Algorithm).Hash -cne $Hash) {
            throw "依赖文件校验失败 $Destination"
        }
        Move-Item -LiteralPath $pending -Destination $Destination -Force
    } finally { [IO.File]::Delete($pending) }
}

function Get-VerifiedDependency([string]$Name, [string]$Uri, [string]$Algorithm, [string]$Hash) {
    $archive = Join-Path $DependencyCache $Name
    if ((Test-Path -LiteralPath $archive -PathType Leaf) -and
        (Get-FileHash -LiteralPath $archive -Algorithm $Algorithm).Hash -eq $Hash) {
        return $archive
    }

    $pending = Join-Path $staging $Name
    Write-Host "正在下载 $Name"
    [Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12
    Invoke-WebRequest -UseBasicParsing -Uri $Uri -OutFile $pending -TimeoutSec 180
    Copy-VerifiedDependency $pending $archive $Algorithm $Hash
    return $archive
}

function Copy-MissingTree([string]$Source, [string]$Destination, [string[]]$Skip = @()) {
    foreach ($file in Get-ChildItem -LiteralPath $Source -File -Recurse -Force) {
        $relative = $file.FullName.Substring($Source.Length + 1).Replace([IO.Path]::DirectorySeparatorChar, '/')
        if ($relative -in $Skip) { continue }
        $target = Join-Path $Destination $relative
        if (Test-Path -LiteralPath $target -PathType Leaf) { continue }
        $null = New-Item -ItemType Directory -Path (Split-Path -Parent $target) -Force
        $pending = "$target.$([guid]::NewGuid().ToString('N')).tmp"
        try {
            Copy-Item -LiteralPath $file.FullName -Destination $pending
            [IO.File]::Move($pending, $target)
        } finally { [IO.File]::Delete($pending) }
    }
}

$staging = $null
try {
    $packageFile = Join-Path $PSScriptRoot 'Install-Package.txt'
    $package = (Get-Content -LiteralPath $packageFile -Raw).Trim()
    $packageFiles = switch ($package) {
        'cup' { 'Mods/AnnihilatedCup.dll'; 'Mods/AnnihilatedRoomRetry.dll'; 'Launch-Cup.cmd' }
        'gallery' { 'Mods/AnnihilatedGallery.dll' }
        'room-retry' { 'Mods/AnnihilatedRoomRetry.dll' }
        'attack-mode' { 'Mods/AnnihilatedAttackModeSwitch.dll' }
        'save-slot' { 'Mods/SaveSlotUnlimiter.dll' }
        'enemy-intent' { 'Mods/AnnihilatedEnemyIntent.dll' }
        'playable-rivals' { 'Mods/AnnihilatedPlayableRivals.dll' }
        'texture-override' { 'Mods/AnnihilatedTextureOverride.dll'; 'AnnihilatedTextureEditor.exe' }
        'texture-runtime' { 'Mods/AnnihilatedTextureOverride.dll' }
        'dragon-skin' { 'Mods/AnnihilatedTextureOverride.dll'; 'UserData/TextureOverrides/Enemy/dragon.png'; 'UserData/TextureOverrides/Item/dragonEye.png' }
        'bonfire-fusion' { 'Mods/BonfireFusion.dll' }
        default { throw '安装包标识无效。请完整解压 ZIP。' }
    }
    foreach ($file in $packageFiles) {
        if (!(Test-Path -LiteralPath (Join-Path $PSScriptRoot $file) -PathType Leaf)) {
            throw "安装包缺少 $file。请完整解压 ZIP 后重试。"
        }
    }

    if (!$GameDirectory) { $GameDirectory = Find-GameDirectory }
    if (!(Test-GameDirectory $GameDirectory)) { throw '请选择含 Annihilated.exe 的文件夹。' }
    $game = (Get-Item -LiteralPath $GameDirectory).FullName
    $gameExe = Join-Path $game 'Annihilated.exe'
    if (Get-Process -Name Annihilated -ErrorAction SilentlyContinue | Where-Object { !$_.Path -or $_.Path -ieq $gameExe }) {
        throw '请退出游戏后重试。'
    }
    Write-Host "游戏目录  $game"
    Assert-GameFiles $game
    Write-Host '游戏文件检查通过。'

    if ($package -in @('cup', 'gallery', 'save-slot', 'playable-rivals')) {
        Assert-SupportedBuild $game
    }

    $loaderFiles = @{
        'version.dll' = '0CE7A4E530C7F83F172A2C44AED45EEDB3BDCF06B83760F143A0FBA6885FA929'
        'MelonLoader/net6/MelonLoader.dll' = 'BDD43DC0F3893C208C95389B863AB61C967AE14204DA0DD8051647E754C8709B'
    }
    $loaderNeeded = $false
    foreach ($file in $loaderFiles.Keys) {
        $target = Join-Path $game $file
        if (!(Test-Path -LiteralPath $target -PathType Leaf)) { $loaderNeeded = $true; continue }
        if ((Get-FileHash -LiteralPath $target).Hash -ne $loaderFiles[$file]) {
            throw "发现其他版本的加载器或同名文件 $file。请先处理冲突再重试。"
        }
    }
    if (!(Test-Path -LiteralPath (Join-Path $game 'MelonLoader/net6/Il2CppInterop.Runtime.dll') -PathType Leaf) -or
        !(Test-Path -LiteralPath (Join-Path $game 'MelonLoader/Dependencies/Il2CppAssemblyGenerator/Il2CppAssemblyGenerator.dll') -PathType Leaf)) {
        $loaderNeeded = $true
    }

    if (!$DependencyCache) { $DependencyCache = Join-Path $env:TEMP 'Annihilated-Mods-Installer-cache' }
    $staging = Join-Path $env:TEMP ('Annihilated-Mods-Install-' + [guid]::NewGuid().ToString('N'))
    $null = New-Item -ItemType Directory -Path $staging
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $loaderStage = Join-Path $staging 'loader'
    if ($loaderNeeded) {
        $archive = Get-VerifiedDependency 'MelonLoader-0.7.3-x64.zip' 'https://github.com/LavaGang/MelonLoader/releases/download/v0.7.3/MelonLoader.x64.zip' 'SHA256' '5B2B2F3D1CD42B59EC886C5BDC2663EDAE87A0097A4F4A8F58C0965A99DDA416'
        [IO.Compression.ZipFile]::ExtractToDirectory($archive, $loaderStage)
    }

    $runtime = Join-Path $game 'MelonLoader/Dependencies/dotnet'
    $runtimeStage = Join-Path $staging 'dotnet'
    # SHA-256 hashes from the verified .NET 6.0.36 win-x64 runtime archive.
    $runtimeFiles = @{
        'dotnet.exe' = 'D4401F5FBDEA869BB7211B00594746EF6962B7DD2BFCEB749889B920070C3F9F'
        'host/fxr/6.0.36/hostfxr.dll' = '82AB369E059533EB7952855B7CC55916AB6B91FB36B2922F269037B1974BE627'
        'shared/Microsoft.NETCore.App/6.0.36/coreclr.dll' = 'FE0AE2120E2B298E1C40F8FDA3845AA44D3DA6AFE3037AF4F982B0DD63E4356E'
        'shared/Microsoft.NETCore.App/6.0.36/hostpolicy.dll' = '266D2AE85FFF170EB2795B495B019264D5A0E1CBD7F3F1C6A9CFE0317928399F'
        'shared/Microsoft.NETCore.App/6.0.36/System.Private.CoreLib.dll' = '3CE6F27555E6CE2DC1D314012A5798A1A62B4527813C085548F7D937F83DA74A'
    }
    $runtimeToRepair = @(foreach ($relative in $runtimeFiles.Keys) {
        $target = Join-Path $runtime $relative
        if (Test-Path -LiteralPath $target -PathType Container) {
            throw "便携版 .NET 的文件路径被目录占用：$relative。请移走该目录后重试。"
        }
        $exists = Test-Path -LiteralPath $target -PathType Leaf
        $healthy = $exists -and (Get-FileHash -LiteralPath $target).Hash -eq $runtimeFiles[$relative]
        if ($relative -eq 'dotnet.exe' -and $exists -and !$healthy -and (Test-X64GameBinary $target) -and
            (Get-Item -LiteralPath $target).VersionInfo.OriginalFilename -eq '.NET Host') {
            # The shared host can come from another official x64 runtime in this directory.
            $signature = Get-AuthenticodeSignature -LiteralPath $target
            $healthy = $signature.Status -eq 'Valid' -and
                $signature.SignerCertificate.Subject -match '(?:^|,\s*)O=Microsoft Corporation(?:,|$)'
        }
        if (!$healthy) { $relative }
    })
    if ($runtimeToRepair.Count) {
        $archive = Get-VerifiedDependency 'dotnet-runtime-6.0.36-win-x64.zip' 'https://builds.dotnet.microsoft.com/dotnet/Runtime/6.0.36/dotnet-runtime-6.0.36-win-x64.zip' 'SHA512' '935DB5C6CEE19F2C016E67168BFAE7B491044735DE76C673ABB3B125DD325FD5E779D7EFE12BA80178D46689AE70A25E558A3FA846417D44C5F4CA256E7F4BF2'
        [IO.Compression.ZipFile]::ExtractToDirectory($archive, $runtimeStage)
    }

    $cppBase = 'https://github.com/SamboyCoding/Cpp2IL/releases/download/2022.1.0-pre-release.21'
    $cppDirectory = Join-Path $game 'MelonLoader/Dependencies/Il2CppAssemblyGenerator/Cpp2IL'
    $cppFiles = @(
        @{
            Name = 'Cpp2IL-2022.1.0-pre-release.21-Windows.exe'
            Target = 'Cpp2IL.exe'
            Hash = '663FB432433B4371FD1EE0EBC321A8FFF2A9AAC5AC4230C843F9E03DDEE4E04C'
        }
        @{
            Name = 'Cpp2IL.Plugin.StrippedCodeRegSupport.dll'
            Target = 'Plugins/Cpp2IL.Plugin.StrippedCodeRegSupport.dll'
            Hash = '2CC4F8C66541F18B4DAED5410149EE7859C3190454D30BDA5CCD0895D2989EE6'
        }
    )
    $cppPending = foreach ($dependency in $cppFiles) {
        $target = Join-Path $cppDirectory $dependency.Target
        if ((Test-Path -LiteralPath $target -PathType Leaf) -and
            (Get-FileHash -LiteralPath $target).Hash -ceq $dependency.Hash) { continue }
        $source = Get-VerifiedDependency $dependency.Name "$cppBase/$($dependency.Name)" 'SHA256' $dependency.Hash
        @{ Source = $source; Target = $target; Hash = $dependency.Hash }
    }

    # MelonLoader can consume this verified release archive on its first launch.
    $unitySource = $null
    $unity = (Get-Item -LiteralPath (Join-Path $game 'UnityPlayer.dll')).VersionInfo
    if ($unity.FileMajorPart -eq 6000 -and $unity.FileMinorPart -eq 3 -and $unity.FileBuildPart -eq 23) {
        $unityDirectory = Join-Path $game 'MelonLoader/Dependencies/Il2CppAssemblyGenerator'
        $unityConfig = Join-Path $unityDirectory 'Config.cfg'
        $coreModule = Join-Path $unityDirectory 'UnityDependencies/UnityEngine.CoreModule.dll'
        $unityReady = (Test-Path -LiteralPath $coreModule -PathType Leaf) -and
            (Test-Path -LiteralPath $unityConfig -PathType Leaf) -and
            ((Get-Content -LiteralPath $unityConfig -Raw) -match '(?m)^\s*UnityVersion\s*=\s*"6000\.3\.23"')
        if (!$unityReady) {
            $unityName = 'UnityDependencies_6000.3.23.zip'
            $unityTarget = Join-Path $unityDirectory $unityName
            $unityHash = '21ECC95B489C49489A8CC3D0166771D93485EDAAF0C5D2F552E7A87F0C908028'
            if (!(Test-Path -LiteralPath $unityTarget -PathType Leaf) -or
                (Get-FileHash -LiteralPath $unityTarget).Hash -ne $unityHash) {
                $unitySource = Get-VerifiedDependency $unityName 'https://github.com/LavaGang/MelonLoader.UnityDependencies/releases/download/6000.3.23/Managed.zip' 'SHA256' $unityHash
            }
        }
    }

    foreach ($pendingDependency in $cppPending) {
        Copy-VerifiedDependency $pendingDependency.Source $pendingDependency.Target 'SHA256' $pendingDependency.Hash
    }
    Write-Host 'Cpp2IL 与配套插件校验通过。'

    if ($loaderNeeded) { Copy-MissingTree (Join-Path $loaderStage 'MelonLoader') (Join-Path $game 'MelonLoader') }
    if ($runtimeToRepair.Count) {
        # Leave the failed checks in place until the rest of the runtime is copied.
        Copy-MissingTree $runtimeStage $runtime $runtimeToRepair
        foreach ($relative in $runtimeToRepair) {
            Copy-VerifiedDependency (Join-Path $runtimeStage $relative) (Join-Path $runtime $relative) 'SHA256' $runtimeFiles[$relative]
        }
    }
    if ($unitySource) { Copy-VerifiedDependency $unitySource $unityTarget 'SHA256' $unityHash }

    $backup = Join-Path $game ('UserData/AnnihilatedModsInstaller/backups/' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff') + '-' + [guid]::NewGuid().ToString('N').Substring(0, 6))
    foreach ($relative in $packageFiles) {
        $source = Join-Path $PSScriptRoot $relative
        $target = Join-Path $game $relative
        $hash = (Get-FileHash -LiteralPath $source).Hash
        if (Test-Path -LiteralPath $target -PathType Leaf) {
            if ((Get-FileHash -LiteralPath $target).Hash -eq $hash) { continue }
            $saved = Join-Path $backup $relative
            $null = New-Item -ItemType Directory -Path (Split-Path -Parent $saved) -Force
            Copy-Item -LiteralPath $target -Destination $saved
        }
        $null = New-Item -ItemType Directory -Path (Split-Path -Parent $target) -Force
        $pending = "$target.$([guid]::NewGuid()).tmp"
        try {
            Copy-Item -LiteralPath $source -Destination $pending
            if ((Get-FileHash -LiteralPath $pending).Hash -ne $hash) { throw "安装文件校验失败 $relative" }
            Move-Item -LiteralPath $pending -Destination $target -Force
        } finally { [IO.File]::Delete($pending) }
    }

    $bootstrap = Join-Path $game 'version.dll'
    if (!(Test-Path -LiteralPath $bootstrap -PathType Leaf)) {
        Copy-Item -LiteralPath (Join-Path $loaderStage 'version.dll') -Destination $bootstrap
    }
    if (Test-Path -LiteralPath $backup) { Write-Host "被替换的文件已备份到 $backup" }
    Write-Host '安装完成。首次启动时，MelonLoader 会生成游戏互操作程序集，请等到标题画面。' -ForegroundColor Green
    if ($package -eq 'cup') {
        Write-Host '玩杯赛时双击游戏目录中的 Launch-Cup.cmd。日常游玩按平时的方式启动。'
    } else {
        Write-Host '现在可以按平时的方式启动游戏。'
    }
} catch {
    Write-Host "安装失败  $($_.Exception.Message)" -ForegroundColor Red
    exit 1
} finally {
    if ($staging -and (Test-Path -LiteralPath $staging -PathType Container)) {
        # The only recursive removal is the GUID directory allocated directly under TEMP above.
        $resolvedStage = (Get-Item -LiteralPath $staging).FullName
        if ($resolvedStage -ieq $staging -and (Split-Path -Parent $resolvedStage) -ieq (Get-Item -LiteralPath $env:TEMP).FullName) {
            Remove-Item -LiteralPath $resolvedStage -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
}
