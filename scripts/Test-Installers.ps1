#Requires -Version 7.0
param(
    [Parameter(Mandatory)]
    [string]$GameRoot,
    [string]$DistDirectory = (Join-Path $PSScriptRoot '../dist'),
    [string]$DependencyCache = (Join-Path $PSScriptRoot '../.work/installer-cache'),
    [string[]]$Package,
    [switch]$TestCpp2ILDownload
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false
$game = (Get-Item -LiteralPath $GameRoot).FullName
$dist = (Get-Item -LiteralPath $DistDirectory).FullName
$cache = (Get-Item -LiteralPath $DependencyCache).FullName
$fixtures = (New-Item -ItemType Directory -Path (Join-Path $PSScriptRoot "../.work/online-install-test-$([guid]::NewGuid().ToString('N'))")).FullName
$catalog = 'Annihilated_Data/StreamingAssets/aa/catalog.hash'
$metadata = 'Annihilated_Data/il2cpp_data/Metadata/global-metadata.dat'
$gameFiles = @('Annihilated.exe', 'UnityPlayer.dll', 'GameAssembly.dll', $catalog, $metadata)
$cpp = 'MelonLoader/Dependencies/Il2CppAssemblyGenerator/Cpp2IL/Cpp2IL.exe'
$payloads = [ordered]@{
    Cup = @('Launch-Cup.cmd', 'Mods/AnnihilatedCup.dll', 'Mods/AnnihilatedRoomRetry.dll')
    Gallery = @('Mods/AnnihilatedGallery.dll')
    RoomRetry = @('Mods/AnnihilatedRoomRetry.dll')
    AttackModeSwitch = @('Mods/AnnihilatedAttackModeSwitch.dll')
    SaveSlotUnlimiter = @('Mods/SaveSlotUnlimiter.dll')
    EnemyIntent = @('Mods/AnnihilatedEnemyIntent.dll')
    'TextureOverride-Editor' = @('Mods/AnnihilatedTextureOverride.dll', 'AnnihilatedTextureEditor.exe')
    DragonSkin = @('Mods/AnnihilatedTextureOverride.dll', 'UserData/TextureOverrides/Enemy/dragon.png', 'UserData/TextureOverrides/Item/dragonEye.png')
    BonfireFusion = @('Mods/BonfireFusion.dll')
}
$native = @('Cup', 'Gallery', 'SaveSlotUnlimiter')
$expectedDependencies = @{
    'version.dll' = '0CE7A4E530C7F83F172A2C44AED45EEDB3BDCF06B83760F143A0FBA6885FA929'
    'MelonLoader/net6/MelonLoader.dll' = 'BDD43DC0F3893C208C95389B863AB61C967AE14204DA0DD8051647E754C8709B'
    'MelonLoader/Dependencies/Il2CppAssemblyGenerator/UnityDependencies_6000.3.23.zip' = '21ECC95B489C49489A8CC3D0166771D93485EDAAF0C5D2F552E7A87F0C908028'
    'MelonLoader/Dependencies/Il2CppAssemblyGenerator/Cpp2IL/Plugins/Cpp2IL.Plugin.StrippedCodeRegSupport.dll' = '2CC4F8C66541F18B4DAED5410149EE7859C3190454D30BDA5CCD0895D2989EE6'
}
$expectedDependencies[$cpp] = '663FB432433B4371FD1EE0EBC321A8FFF2A9AAC5AC4230C843F9E03DDEE4E04C'

function Assert-Hash([string]$File, [string]$Expected) {
    if ((Get-FileHash -LiteralPath $File).Hash -ne $Expected) { throw "Unexpected SHA-256: $File" }
}

function New-TestGame([string]$Path) {
    foreach ($relative in $gameFiles) {
        $destination = Join-Path $Path $relative
        $null = New-Item -ItemType Directory -Path (Split-Path -Parent $destination) -Force
        Copy-Item -LiteralPath (Join-Path $game $relative) -Destination $destination
    }
}

function Run-Installer([string]$From, [string]$Target, [string]$Launcher, [bool]$Success, [string]$MessagePattern) {
    $output = & (Join-Path $From $Launcher) $Target 2>&1
    if (($LASTEXITCODE -eq 0) -ne $Success -or ($MessagePattern -and ($output -join ' ') -notmatch $MessagePattern)) {
        throw "$Launcher exited with $LASTEXITCODE for $Target.$([Environment]::NewLine)$($output -join [Environment]::NewLine)"
    }
}

if (!$Package) { $Package = @($payloads.Keys) }
$oldCache = $env:ANNIHILATED_INSTALL_CACHE
$oldPause = $env:ANNIHILATED_INSTALL_NO_PAUSE
try {
    $env:ANNIHILATED_INSTALL_CACHE = $cache
    $env:ANNIHILATED_INSTALL_NO_PAUSE = '1'
    foreach ($id in $Package) {
        if (!$payloads.Contains($id)) { throw "Unknown package: $id" }
        $archive = Join-Path $dist "Annihilated-$id-Build26845264-Online.zip"
        $extracted = Join-Path $fixtures "$id package"
        [IO.Compression.ZipFile]::ExtractToDirectory($archive, $extracted)
        $fixture = Join-Path $fixtures "$id game"
        New-TestGame $fixture

        $metadataFile = Join-Path $fixture $metadata
        [IO.File]::WriteAllText($metadataFile, 'invalid')
        Run-Installer $extracted $fixture '一键安装.cmd' $false 'global-metadata\.dat.*Steam'
        Copy-Item -LiteralPath (Join-Path $game $metadata) -Destination $metadataFile -Force
        if ($id -in $native) {
            $catalogFile = Join-Path $fixture $catalog
            [IO.File]::WriteAllText($catalogFile, ('0' * 32))
            Run-Installer $extracted $fixture '一键安装.cmd' $false '26845264'
            Copy-Item -LiteralPath (Join-Path $game $catalog) -Destination $catalogFile -Force
        }
        if (Test-Path -LiteralPath (Join-Path $fixture 'version.dll')) { throw 'Preflight installed a loader.' }

        Run-Installer $extracted $fixture '一键安装.cmd' $true
        foreach ($relative in $expectedDependencies.Keys) {
            Assert-Hash (Join-Path $fixture $relative) $expectedDependencies[$relative]
        }
        if (!(Test-Path -LiteralPath (Join-Path $fixture 'MelonLoader/Dependencies/dotnet/host/fxr/6.0.36/hostfxr.dll'))) {
            throw "$id did not install portable .NET 6."
        }
        foreach ($relative in $payloads[$id]) {
            Assert-Hash (Join-Path $fixture $relative) (Get-FileHash -LiteralPath (Join-Path $extracted $relative)).Hash
        }

        $cppPath = Join-Path $fixture $cpp
        [IO.File]::WriteAllText($cppPath, 'broken dependency')
        $target = Join-Path $fixture ($payloads[$id] | Where-Object { $_ -like 'Mods/*' } | Select-Object -First 1)
        [IO.File]::WriteAllText($target, 'previous version')
        $previousHash = (Get-FileHash -LiteralPath $target).Hash
        $unrelated = Join-Path $fixture 'Mods/Unrelated.dll'
        [IO.File]::WriteAllText($unrelated, 'unrelated mod')
        Run-Installer $extracted $fixture 'Install.cmd' $true
        Assert-Hash $cppPath $expectedDependencies[$cpp]
        $backup = @(Get-ChildItem -LiteralPath (Join-Path $fixture 'UserData/AnnihilatedModsInstaller/backups') -File -Recurse)
        if ($backup.Count -ne 1) { throw "$id did not back up exactly one replaced DLL." }
        Assert-Hash $backup[0].FullName $previousHash
        if ([IO.File]::ReadAllText($unrelated) -ne 'unrelated mod') { throw "$id replaced another mod." }

        $lastWriteTime = (Get-Item -LiteralPath $cppPath).LastWriteTimeUtc
        Run-Installer $extracted $fixture 'Install.cmd' $true
        if ((Get-Item -LiteralPath $cppPath).LastWriteTimeUtc -ne $lastWriteTime) { throw "$id rewrote a good Cpp2IL." }
        Write-Host "$id passed package install, upgrade backup, dependency repair, and repeated run."
    }

    if ($TestCpp2ILDownload) {
        $fresh = Join-Path $fixtures 'blank download cache'
        $null = New-Item -ItemType Directory -Path $fresh
        $env:ANNIHILATED_INSTALL_CACHE = $fresh
        $fixture = Join-Path $fixtures 'online download game'
        New-TestGame $fixture
        Run-Installer (Join-Path $fixtures "$($Package[0]) package") $fixture 'Install.cmd' $true 'Cpp2IL-2022.1.0-pre-release.21-Windows.exe'
        Assert-Hash (Join-Path $fixture $cpp) $expectedDependencies[$cpp]
        Write-Host 'Fresh official downloads passed.'
    }
} finally {
    $env:ANNIHILATED_INSTALL_CACHE = $oldCache
    $env:ANNIHILATED_INSTALL_NO_PAUSE = $oldPause
}
Write-Host "Fixtures: $fixtures"
