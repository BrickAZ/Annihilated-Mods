#Requires -Version 5.1
param(
    [string]$GameDirectory,
    [string]$OriginalSlotDll,
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

function Select-OriginalSlotDll {
    Add-Type -AssemblyName System.Windows.Forms
    $picker = New-Object System.Windows.Forms.OpenFileDialog
    $picker.Title = '请选择 dsh 原版 v1.25 的 SaveSlotUnlimiter.dll'
    $picker.Filter = 'SaveSlotUnlimiter.dll|SaveSlotUnlimiter.dll'
    try {
        if ($picker.ShowDialog() -eq [System.Windows.Forms.DialogResult]::OK) { return $picker.FileName }
    } finally { $picker.Dispose() }
}

function Get-DependencyArchive([string]$Name, [string]$Uri, [string]$Algorithm, [string]$Hash) {
    $archive = Join-Path $DependencyCache $Name
    if ((Test-Path -LiteralPath $archive -PathType Leaf) -and
        (Get-FileHash -LiteralPath $archive -Algorithm $Algorithm).Hash -eq $Hash) {
        return $archive
    }

    $null = New-Item -ItemType Directory -Path $DependencyCache -Force
    $pending = Join-Path $staging $Name
    Write-Host "正在下载 $Name"
    [Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12
    Invoke-WebRequest -UseBasicParsing -Uri $Uri -OutFile $pending -TimeoutSec 180
    if ((Get-FileHash -LiteralPath $pending -Algorithm $Algorithm).Hash -ne $Hash) {
        throw "下载文件校验失败 $Name"
    }
    Copy-Item -LiteralPath $pending -Destination $archive -Force
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

$staging = $null
try {
    $packageFile = Join-Path $PSScriptRoot 'Install-Package.txt'
    $package = (Get-Content -LiteralPath $packageFile -Raw).Trim()
    $packageFiles = switch ($package) {
        'cup' { 'Mods/AnnihilatedCup.dll'; 'Mods/AnnihilatedRoomRetry.dll'; 'Launch-Cup.cmd' }
        'gallery' { 'Mods/AnnihilatedGallery.dll' }
        'room-retry' { 'Mods/AnnihilatedRoomRetry.dll' }
        'attack-mode' { 'Mods/AnnihilatedAttackModeSwitch.dll' }
        'save-slot' { 'Patch-Slot125.ps1' }
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

    if ($package -eq 'cup') {
        $buildHash = '13B13F97BBE50154B451753351656F99595AE0A542586097F27536F55C24F3C4'
        $catalog = Join-Path $game 'Annihilated_Data/StreamingAssets/aa/catalog.hash'
        if (!(Test-Path -LiteralPath $catalog -PathType Leaf) -or
            (Get-Content -LiteralPath $catalog -Raw).Trim() -ne 'c867cf04b05171a82710f970f4122af7' -or
            (Get-FileHash -LiteralPath (Join-Path $game 'GameAssembly.dll')).Hash -ne $buildHash) {
            throw '杯赛 v0.4.12 对应 Steam Build 26705080。当前游戏版本不匹配。'
        }
    }

    $slotRelative = 'Mods/SaveSlotUnlimiter.dll'
    $patchedSha = '6A6227AC519E27CA1A07198FE0D6C769C3633BB8C92EB4DA3840BF4A0F87F257'
    $patchNeeded = $false
    if ($package -eq 'save-slot') {
        $originalSha = 'B08ADB31EF98008AC5D57C3F8B0298896FE9DD8C76B18A5D9A528F418E042396'
        $installedSlot = Join-Path $game $slotRelative
        $installedSlotHash = if (Test-Path -LiteralPath $installedSlot -PathType Leaf) {
            (Get-FileHash -LiteralPath $installedSlot).Hash
        }
        $patchNeeded = $installedSlotHash -ne $patchedSha
        if ($patchNeeded) {
            if (!$OriginalSlotDll) {
                $OriginalSlotDll = if ($installedSlotHash -eq $originalSha) { $installedSlot } else { Select-OriginalSlotDll }
            }
            if (!$OriginalSlotDll -or !(Test-Path -LiteralPath $OriginalSlotDll -PathType Leaf) -or
                (Get-FileHash -LiteralPath $OriginalSlotDll).Hash -ne $originalSha) {
                throw '请选择 dsh 原版 v1.25 整合包的 Mods/SaveSlotUnlimiter.dll。'
            }
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
        !(Test-Path -LiteralPath (Join-Path $game 'MelonLoader/Dependencies/Il2CppAssemblyGenerator/Il2CppAssemblyGenerator.dll') -PathType Leaf) -or
        ($patchNeeded -and !(Test-Path -LiteralPath (Join-Path $game 'MelonLoader/net472/Mono.Cecil.dll') -PathType Leaf))) {
        $loaderNeeded = $true
    }

    if (!$DependencyCache) { $DependencyCache = Join-Path $env:TEMP 'Annihilated-Mods-Installer-cache' }
    $staging = Join-Path $env:TEMP ('Annihilated-Mods-Install-' + [guid]::NewGuid().ToString('N'))
    $null = New-Item -ItemType Directory -Path $staging
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $loaderStage = Join-Path $staging 'loader'
    if ($loaderNeeded) {
        $archive = Get-DependencyArchive 'MelonLoader-0.7.3-x64.zip' 'https://github.com/LavaGang/MelonLoader/releases/download/v0.7.3/MelonLoader.x64.zip' 'SHA256' '5b2b2f3d1cd42b59ec886c5bdc2663edae87a0097a4f4a8f58c0965a99dda416'
        [IO.Compression.ZipFile]::ExtractToDirectory($archive, $loaderStage)
    }

    $runtime = Join-Path $game 'MelonLoader/Dependencies/dotnet'
    $runtimeStage = Join-Path $staging 'dotnet'
    $runtimeNeeded = !(Test-Path -LiteralPath (Join-Path $runtime 'host/fxr/6.0.36/hostfxr.dll') -PathType Leaf) -or
        !(Test-Path -LiteralPath (Join-Path $runtime 'shared/Microsoft.NETCore.App/6.0.36/coreclr.dll') -PathType Leaf) -or
        !(Test-Path -LiteralPath (Join-Path $runtime 'dotnet.exe') -PathType Leaf)
    if ($runtimeNeeded) {
        $archive = Get-DependencyArchive 'dotnet-runtime-6.0.36-win-x64.zip' 'https://builds.dotnet.microsoft.com/dotnet/Runtime/6.0.36/dotnet-runtime-6.0.36-win-x64.zip' 'SHA512' '935db5c6cee19f2c016e67168bfae7b491044735de76c673abb3b125dd325fd5e779d7efe12ba80178d46689ae70a25e558a3fa846417d44c5f4ca256e7f4bf2'
        [IO.Compression.ZipFile]::ExtractToDirectory($archive, $runtimeStage)
    }

    $payload = [ordered]@{}
    if ($package -eq 'save-slot') {
        if ($patchNeeded) {
            $patchRoot = if ($loaderNeeded) { $loaderStage } else { $game }
            $candidate = Join-Path $staging 'SaveSlotUnlimiter.dll'
            & (Join-Path $PSScriptRoot 'Patch-Slot125.ps1') -InputDll $OriginalSlotDll -GameRoot $patchRoot -OutputDll $candidate | Out-Null
            $payload[$slotRelative] = $candidate
        }
    } else {
        foreach ($relative in $packageFiles) { $payload[$relative] = Join-Path $PSScriptRoot $relative }
    }

    if ($loaderNeeded) { Copy-MissingTree (Join-Path $loaderStage 'MelonLoader') (Join-Path $game 'MelonLoader') }
    if ($runtimeNeeded) { Copy-MissingTree $runtimeStage $runtime }

    $backup = Join-Path $game ('UserData/AnnihilatedModsInstaller/backups/' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff') + '-' + [guid]::NewGuid().ToString('N').Substring(0, 6))
    foreach ($relative in $payload.Keys) {
        $source = $payload[$relative]
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
