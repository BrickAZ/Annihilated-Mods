#Requires -Version 5.1
param(
    [string]$GameDirectory,
    [string]$DependencyCache = $env:ANNIHILATED_INSTALL_CACHE
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

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
            $libraries += [regex]::Matches((Get-Content -LiteralPath $folders -Raw), '"path"\s+"([^"\r\n]+)"') |
                ForEach-Object { $_.Groups[1].Value.Replace('\\', '\') }
        }
        foreach ($library in $libraries) {
            $manifest = Join-Path $library 'steamapps/appmanifest_2638080.acf'
            if (!(Test-Path -LiteralPath $manifest -PathType Leaf)) { continue }
            $name = [regex]::Match((Get-Content -LiteralPath $manifest -Raw), '"installdir"\s+"([^"\\/:\r\n]+)"')
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

function Copy-MissingTree([string]$Source, [string]$Destination) {
    foreach ($file in Get-ChildItem -LiteralPath $Source -File -Recurse -Force) {
        $relative = $file.FullName.Substring($Source.Length + 1)
        $target = Join-Path $Destination $relative
        if (Test-Path -LiteralPath $target -PathType Leaf) { continue }
        $null = New-Item -ItemType Directory -Path (Split-Path -Parent $target) -Force
        Copy-Item -LiteralPath $file.FullName -Destination $target
    }
}

function Test-UnityCache([string]$Directory, [string]$Manifest) {
    if (!(Test-Path -LiteralPath $Directory -PathType Container)) { return $false }
    $entries = @(Get-Content -LiteralPath $Manifest)
    $dlls = @(Get-ChildItem -LiteralPath $Directory -File -Filter '*.dll' -Force)
    if ($entries.Count -ne 77 -or $dlls.Count -ne $entries.Count) { return $false }
    foreach ($entry in $entries) {
        if ($entry -cnotmatch '^([A-F0-9]{64})  (UnityEngine[A-Za-z0-9_.]*\.dll)$') {
            throw 'Unity 缓存校验清单无效。请重新完整解压 ZIP。'
        }
        $path = Join-Path $Directory $Matches[2]
        if (!(Test-Path -LiteralPath $path -PathType Leaf) -or
            (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -cne $Matches[1]) { return $false }
    }
    return $true
}

function Set-UnityCacheVersion([string]$Path, [string]$Version) {
    $original = if (Test-Path -LiteralPath $Path -PathType Leaf) { [IO.File]::ReadAllText($Path) } else { '' }
    $section = [regex]::Match($original, '(?ms)^[ \t]*\[Il2CppAssemblyGenerator\][ \t]*(?:\r?\n|\z)(?<body>.*?)(?=^[ \t]*\[|\z)')
    $setting = 'UnityVersion = "' + $Version + '"'
    $lf = [string][char]10
    $crlf = [string][char]13 + $lf
    $newline = if ($original.Contains($crlf)) { $crlf } else { $lf }
    if ($section.Success) {
        $body = $section.Groups['body']
        $offset = $body.Index
        $key = [regex]::new('(?m)^[ \t]*UnityVersion[ \t]*=[^\r\n]*')
        if ($key.IsMatch($body.Value)) {
            $replacement = $key.Replace($body.Value,$setting,1)
        } else {
            $separator = if ($original[$offset - 1] -ne [char]10) { $newline } else { '' }
            $replacement = $separator + $setting + $newline + $body.Value
        }
        $updated = $original.Substring(0,$offset) + $replacement + $original.Substring($offset + $body.Length)
    } else {
        $prefix = if ($original) { $original.TrimEnd([char]13,[char]10) + $newline + $newline } else { '' }
        $updated = $prefix + '[Il2CppAssemblyGenerator]' + $newline + $setting + $newline
    }
    if ($updated -ceq $original) { return }
    $pending = "$Path.$([guid]::NewGuid().ToString('N')).tmp"
    try {
        [IO.File]::WriteAllText($pending,$updated,[Text.UTF8Encoding]::new($false))
        Move-Item -LiteralPath $pending -Destination $Path -Force
    } finally { [IO.File]::Delete($pending) }
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
        'dragon-skin' { 'Mods/AnnihilatedTextureOverride.dll'; 'UserData/TextureOverrides/Enemy/dragon.png'; 'UserData/TextureOverrides/Item/dragonEye.png' }
        'save-slot' { 'Mods/SaveSlotUnlimiter.dll' }
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

    if ($package -in @('cup', 'save-slot', 'dragon-skin')) {
        $buildHash = '13B13F97BBE50154B451753351656F99595AE0A542586097F27536F55C24F3C4'
        $catalog = Join-Path $game 'Annihilated_Data/StreamingAssets/aa/catalog.hash'
        if (!(Test-Path -LiteralPath $catalog -PathType Leaf) -or
            (Get-Content -LiteralPath $catalog -Raw).Trim() -ne 'c867cf04b05171a82710f970f4122af7' -or
            (Get-FileHash -LiteralPath (Join-Path $game 'GameAssembly.dll')).Hash -ne $buildHash) {
            throw '此安装包需要 Steam Build 26705080。当前游戏版本不匹配。'
        }
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
    $runtimeNeeded = !(Test-Path -LiteralPath (Join-Path $runtime 'host/fxr/6.0.36/hostfxr.dll') -PathType Leaf) -or
        !(Test-Path -LiteralPath (Join-Path $runtime 'shared/Microsoft.NETCore.App/6.0.36/coreclr.dll') -PathType Leaf) -or
        !(Test-Path -LiteralPath (Join-Path $runtime 'dotnet.exe') -PathType Leaf)
    if ($runtimeNeeded) {
        $archive = Get-VerifiedDependency 'dotnet-runtime-6.0.36-win-x64.zip' 'https://builds.dotnet.microsoft.com/dotnet/Runtime/6.0.36/dotnet-runtime-6.0.36-win-x64.zip' 'SHA512' '935DB5C6CEE19F2C016E67168BFAE7B491044735DE76C673ABB3B125DD325FD5E779D7EFE12BA80178D46689AE70A25E558A3FA846417D44C5F4CA256E7F4BF2'
        [IO.Compression.ZipFile]::ExtractToDirectory($archive, $runtimeStage)
    }

    $unityGenerator = Join-Path $game 'MelonLoader/Dependencies/Il2CppAssemblyGenerator'
    $unityConfig = Join-Path $unityGenerator 'Config.cfg'
    $unityArchive = Join-Path $unityGenerator 'UnityDependencies_6000.3.23.zip'
    $unityHash = '21ECC95B489C49489A8CC3D0166771D93485EDAAF0C5D2F552E7A87F0C908028'
    $unitySource = $null
    if ($package -eq 'dragon-skin' -and
        !(Test-UnityCache (Join-Path $unityGenerator 'UnityDependencies') (Join-Path $PSScriptRoot 'unity-cache.sha256'))) {
        if ((Test-Path -LiteralPath $unityArchive -PathType Leaf) -and
            (Get-FileHash -LiteralPath $unityArchive).Hash -ceq $unityHash) {
            $unitySource = $unityArchive
        } else {
            $unitySource = Get-VerifiedDependency 'UnityDependencies_6000.3.23.zip' 'https://github.com/LavaGang/MelonLoader.UnityDependencies/releases/download/6000.3.23/Managed.zip' 'SHA256' $unityHash
        }
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
    foreach ($dependency in $cppFiles) {
        $target = Join-Path $cppDirectory $dependency.Target
        if ((Test-Path -LiteralPath $target -PathType Leaf) -and
            (Get-FileHash -LiteralPath $target).Hash -ceq $dependency.Hash) { continue }
        $source = Get-VerifiedDependency $dependency.Name "$cppBase/$($dependency.Name)" 'SHA256' $dependency.Hash
        Copy-VerifiedDependency $source $target 'SHA256' $dependency.Hash
    }
    Write-Host 'Cpp2IL 与配套插件校验通过。'

    if ($loaderNeeded) { Copy-MissingTree (Join-Path $loaderStage 'MelonLoader') (Join-Path $game 'MelonLoader') }
    if ($runtimeNeeded) { Copy-MissingTree $runtimeStage $runtime }

    if ($package -eq 'dragon-skin') {
        if ($unitySource) {
            if ($unitySource -cne $unityArchive) { Copy-VerifiedDependency $unitySource $unityArchive 'SHA256' $unityHash }
            if (Test-Path -LiteralPath $unityConfig -PathType Leaf) { Set-UnityCacheVersion $unityConfig '0.0.0.0' }
        } else {
            Set-UnityCacheVersion $unityConfig '6000.3.23'
        }
    }

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
