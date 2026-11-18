#Requires -Version 7.0
param(
    [Parameter(Mandatory)]
    [string]$GameRoot,

    [string]$DistDirectory = (Join-Path $PSScriptRoot '../dist'),
    [string]$DependencyCache = (Join-Path $PSScriptRoot '../.work/installer-cache')
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false
Set-StrictMode -Version Latest

function Assert-Condition([bool]$Condition, [string]$Message) {
    if (!$Condition) { throw $Message }
}

function Get-Sha256([string]$Path) {
    return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash
}

function Assert-Sha256([string]$Path, [string]$Expected) {
    Assert-Condition (Test-Path -LiteralPath $Path -PathType Leaf) "Missing file: $Path"
    Assert-Condition ((Get-Sha256 $Path) -ceq $Expected) "Unexpected SHA-256: $Path"
}

function Get-DependencyHashes([string]$ZipPath, [string[]]$Entries, [string]$Prefix = '') {
    $archive = [IO.Compression.ZipFile]::OpenRead($ZipPath)
    try {
        $hashes = @{}
        foreach ($name in $Entries) {
            $entry = $archive.GetEntry($name)
            Assert-Condition ($null -ne $entry) "Missing $name in $ZipPath"
            $stream = $entry.Open()
            try {
                $hashes[$Prefix + $name] = (Get-FileHash -InputStream $stream -Algorithm SHA256).Hash
            } finally { $stream.Dispose() }
        }
        return $hashes
    } finally { $archive.Dispose() }
}

function New-GameFixture([string]$Path) {
    foreach ($relative in ('Annihilated.exe', 'GameAssembly.dll', $catalogPath)) {
        $destination = Join-Path $Path $relative
        $null = New-Item -ItemType Directory -Force -Path (Split-Path -Parent $destination)
        Copy-Item -LiteralPath (Join-Path $sourceGame $relative) -Destination $destination
    }
}

function Invoke-Installer([string]$Package, [string]$Game, [switch]$PowerShell,
    [string]$Launcher = '一键安装.cmd') {
    if ($PowerShell) {
        $arguments = @('-NoProfile', '-NonInteractive', '-ExecutionPolicy', 'Bypass', '-File',
            (Join-Path $Package 'Install-Mod.ps1'), '-GameDirectory', $Game, '-DependencyCache', $cache)
        $output = & powershell.exe @arguments 2>&1
    } else {
        $arguments = @($Game)
        $output = & (Join-Path $Package $Launcher) @arguments 2>&1
    }
    return [pscustomobject]@{ ExitCode = $LASTEXITCODE; Output = ($output -join [Environment]::NewLine) }
}

function Assert-Contents([string[]]$Actual, [string[]]$Expected, [string]$Label) {
    $difference = Compare-Object -ReferenceObject ($Expected | Sort-Object) -DifferenceObject ($Actual | Sort-Object) -CaseSensitive
    Assert-Condition ($null -eq $difference) "$Label contents differ (=> extra, <= missing): `n$($difference | Out-String)"
}

$catalogPath = 'Annihilated_Data/StreamingAssets/aa/catalog.hash'
$sourceGame = (Get-Item -LiteralPath $GameRoot).FullName
$dist = (Get-Item -LiteralPath $DistDirectory).FullName
$cache = (Get-Item -LiteralPath $DependencyCache).FullName

$entries = @('一键安装.cmd', 'Install-Mod.ps1', 'Install-Package.txt')
$packages = @(
    @{
        Id = 'Cup'; Archive = 'Annihilated-Cup-v0.4.12.zip'
        Payload = @('Launch-Cup.cmd', 'Mods/AnnihilatedCup.dll', 'Mods/AnnihilatedRoomRetry.dll')
        Extra = @('Cup-Referee.html', 'INSTALL.md', 'REFEREE.md', 'examples/example-ritual.json')
    }
    @{
        Id = 'Gallery'; Archive = 'Annihilated-Gallery-v1.2.1.zip'
        Payload = @('Mods/AnnihilatedGallery.dll'); Extra = @('INSTALL.md')
    }
    @{
        Id = 'RoomRetry'; Archive = 'Annihilated-RoomRetry-v1.0.1.zip'
        Payload = @('Mods/AnnihilatedRoomRetry.dll'); Extra = @('INSTALL.md')
    }
    @{
        Id = 'AttackModeSwitch'; Archive = 'Annihilated-AttackModeSwitch-v1.1.0.zip'
        Payload = @('Mods/AnnihilatedAttackModeSwitch.dll'); Extra = @('INSTALL.txt')
    }
    @{
        Id = 'SaveSlot'; Archive = 'Annihilated-SaveSlotUnlimiter-v1.25-Build26705080.zip'
        Payload = @('Mods/SaveSlotUnlimiter.dll'); Extra = @('README.md')
    }
)

$root = Join-Path $PSScriptRoot "../.work/installer test $([guid]::NewGuid().ToString('N'))"
$root = (New-Item -ItemType Directory -Path $root).FullName
Write-Host "Installer test fixtures: $root"
foreach ($package in $packages) {
    $zip = Join-Path $dist $package.Archive
    $archive = [IO.Compression.ZipFile]::OpenRead($zip)
    try {
        $archiveFiles = @($archive.Entries | Where-Object { $_.Name } | ForEach-Object FullName)
        Assert-Condition ($archiveFiles -ccontains '一键安装.cmd') "$($package.Archive) is missing 一键安装.cmd"
        Assert-Contents $archiveFiles ($entries + $package.Payload + $package.Extra) $package.Archive
    } finally { $archive.Dispose() }
    $package.Extracted = Join-Path $root "$($package.Id) package"
    [IO.Compression.ZipFile]::ExtractToDirectory($zip, $package.Extracted)
}

Assert-Sha256 (Join-Path $sourceGame 'GameAssembly.dll') '13B13F97BBE50154B451753351656F99595AE0A542586097F27536F55C24F3C4'
Assert-Condition ((Get-Content -LiteralPath (Join-Path $sourceGame $catalogPath) -Raw).Trim() -ceq 'c867cf04b05171a82710f970f4122af7') 'GameRoot must contain Steam Build 26705080.'
$patchedSlotHash = '6A6227AC519E27CA1A07198FE0D6C769C3633BB8C92EB4DA3840BF4A0F87F257'
$save = $packages | Where-Object Id -eq 'SaveSlot'
$slotDll = 'Mods/SaveSlotUnlimiter.dll'
Assert-Sha256 (Join-Path $save.Extracted $slotDll) $patchedSlotHash

$dependencyHashes = Get-DependencyHashes (Join-Path $cache 'MelonLoader-0.7.3-x64.zip') @(
    'version.dll', 'MelonLoader/net6/MelonLoader.dll', 'MelonLoader/net472/Mono.Cecil.dll'
)
$dependencyHashes += Get-DependencyHashes (Join-Path $cache 'dotnet-runtime-6.0.36-win-x64.zip') @(
    'dotnet.exe', 'host/fxr/6.0.36/hostfxr.dll', 'shared/Microsoft.NETCore.App/6.0.36/coreclr.dll'
) 'MelonLoader/Dependencies/dotnet/'

$oldCache = $env:ANNIHILATED_INSTALL_CACHE
$oldNoPause = $env:ANNIHILATED_INSTALL_NO_PAUSE
try {
    $env:ANNIHILATED_INSTALL_CACHE = $cache
    $env:ANNIHILATED_INSTALL_NO_PAUSE = '1'
    foreach ($package in $packages) {
        $fixture = Join-Path $root "$($package.Id) game"
        New-GameFixture $fixture
        $expectedPayload = @{}
        foreach ($relative in $package.Payload) {
            $expectedPayload[$relative] = Get-Sha256 (Join-Path $package.Extracted $relative)
        }
        $install = Invoke-Installer $package.Extracted $fixture
        Assert-Condition ($install.ExitCode -eq 0) "$($package.Id) CMD installation failed ($($install.ExitCode)):`n$($install.Output)"
        foreach ($relative in $dependencyHashes.Keys) {
            Assert-Sha256 (Join-Path $fixture $relative) $dependencyHashes[$relative]
        }
        foreach ($relative in $expectedPayload.Keys) {
            Assert-Sha256 (Join-Path $fixture $relative) $expectedPayload[$relative]
        }
        $expectedMods = @($expectedPayload.Keys | Where-Object { $_ -like 'Mods/*' } | ForEach-Object { Split-Path -Leaf $_ })
        $mods = Join-Path $fixture 'Mods'
        Assert-Contents @(Get-ChildItem -LiteralPath $mods -File -Name -Force) $expectedMods "$($package.Id) installed Mods"
        Assert-Condition ((Test-Path -LiteralPath (Join-Path $fixture 'Launch-Cup.cmd') -PathType Leaf) -eq ($package.Id -eq 'Cup')) "Unexpected Cup launcher in $($package.Id) fixture."

        $preference = Join-Path $fixture 'UserData/Loader.cfg'
        $null = New-Item -ItemType Directory -Force -Path (Split-Path -Parent $preference)
        [IO.File]::WriteAllText($preference, "# Test installer preference sentinel`r`n")
        $unrelated = Join-Path $mods 'Unrelated.dll'
        [IO.File]::WriteAllText($unrelated, 'Test installer unrelated DLL sentinel')
        $sentinelHashes = @{}
        foreach ($file in ($preference, $unrelated)) { $sentinelHashes[$file] = Get-Sha256 $file }
        $bootstrap = Join-Path $fixture 'version.dll'
        (Get-Item -LiteralPath $bootstrap).LastWriteTimeUtc = [DateTime]::new(2019, 5, 23, 1, 0, 0, [DateTimeKind]::Utc)
        $bootstrapTime = (Get-Item -LiteralPath $bootstrap).LastWriteTimeUtc

        $repeat = Invoke-Installer $package.Extracted $fixture -PowerShell
        Assert-Condition ($repeat.ExitCode -eq 0) "$($package.Id) repeat PS installation failed ($($repeat.ExitCode)):`n$($repeat.Output)"
        foreach ($file in $sentinelHashes.Keys) { Assert-Sha256 $file $sentinelHashes[$file] }
        foreach ($relative in ($expectedPayload.Keys + $dependencyHashes.Keys)) {
            $expected = if ($expectedPayload.ContainsKey($relative)) { $expectedPayload[$relative] } else { $dependencyHashes[$relative] }
            Assert-Sha256 (Join-Path $fixture $relative) $expected
        }
        Assert-Condition ((Get-Item -LiteralPath $bootstrap).LastWriteTimeUtc -eq $bootstrapTime) 'Reinstall rewrote the existing loader bootstrap.'
        Assert-Contents @(Get-ChildItem -LiteralPath $mods -File -Name -Force) ($expectedMods + 'Unrelated.dll') "$($package.Id) reinstalled Mods"
        Write-Host "$($package.Id) passed fresh CMD installation and repeated PowerShell installation"
    }

    $cup = $packages | Where-Object Id -eq 'Cup'
    $wrongBuild = Join-Path $root 'Cup wrong build'
    New-GameFixture $wrongBuild
    [IO.File]::WriteAllText((Join-Path $wrongBuild $catalogPath), ('0' * 32))
    $rejected = Invoke-Installer $cup.Extracted $wrongBuild
    Assert-Condition ($rejected.ExitCode -ne 0 -and $rejected.Output -match '26705080') "Cup did not reject wrong build:`n$($rejected.Output)"
    Assert-Contents @(Get-ChildItem -LiteralPath $wrongBuild -Name -Force) @('Annihilated.exe', 'Annihilated_Data', 'GameAssembly.dll') 'Rejected Cup fixture root'
    Write-Host 'Cup passed wrong-build rejection'

    $saveGame = Join-Path $root 'SaveSlot game'
    $installedSlot = Join-Path $saveGame $slotDll
    [IO.File]::WriteAllText($installedSlot, 'Existing SaveSlotUnlimiter DLL sentinel')
    $oldHash = Get-Sha256 $installedSlot
    $upgrade = Invoke-Installer $save.Extracted $saveGame
    Assert-Condition ($upgrade.ExitCode -eq 0) "SaveSlot CMD upgrade failed:`n$($upgrade.Output)"
    Assert-Sha256 $installedSlot $patchedSlotHash
    $backups = @(Get-ChildItem -LiteralPath (Join-Path $saveGame 'UserData/AnnihilatedModsInstaller/backups') -Recurse -File -Filter 'SaveSlotUnlimiter.dll')
    Assert-Condition ($backups.Count -eq 1) 'SaveSlot should back up exactly one replaced DLL.'
    Assert-Sha256 $backups[0].FullName $oldHash
    Write-Host 'SaveSlot passed one-click upgrade with backup'

    $wrongSaveBuild = Join-Path $root 'SaveSlot wrong build'
    New-GameFixture $wrongSaveBuild
    [IO.File]::WriteAllText((Join-Path $wrongSaveBuild $catalogPath), ('0' * 32))
    $rejected = Invoke-Installer $save.Extracted $wrongSaveBuild
    Assert-Condition ($rejected.ExitCode -ne 0 -and $rejected.Output -match '26705080') "SaveSlot did not reject wrong build:`n$($rejected.Output)"
    Assert-Contents @(Get-ChildItem -LiteralPath $wrongSaveBuild -Name -Force) @('Annihilated.exe', 'Annihilated_Data', 'GameAssembly.dll') 'Rejected SaveSlot fixture root'
    Write-Host 'SaveSlot passed wrong-build rejection'
} finally {
    $env:ANNIHILATED_INSTALL_CACHE = $oldCache
    $env:ANNIHILATED_INSTALL_NO_PAUSE = $oldNoPause
}
