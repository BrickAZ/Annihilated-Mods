#Requires -Version 7.0
param(
    [Parameter(Mandatory)]
    [string]$GameRoot
)

$ErrorActionPreference = 'Stop'
$game = (Get-Item -LiteralPath $GameRoot).FullName
if (!(Test-Path -LiteralPath (Join-Path $game 'Annihilated.exe') -PathType Leaf)) {
    throw 'GameRoot must point to the folder containing Annihilated.exe.'
}
if (!(Test-Path -LiteralPath (Join-Path $game 'MelonLoader/Il2CppAssemblies/Assembly-CSharp.dll') -PathType Leaf)) {
    throw 'Install MelonLoader 0.7.3 and launch the game once to generate the IL2CPP interop assemblies.'
}

$repo = Split-Path -Parent $PSScriptRoot
$dist = Join-Path $repo 'dist'

$cup = 'CupMod/bin/Release/net6.0/AnnihilatedCup.dll'
$retry = 'RoomRetryMod/src/bin/Release/net6.0/AnnihilatedRoomRetry.dll'
$gallery = 'GalleryMod/bin/Release/net6.0/AnnihilatedGallery.dll'
$attack = 'AttackModeSwitchMod/src/bin/Release/net6.0/AnnihilatedAttackModeSwitch.dll'

$projects = @(
    'CupMod/AnnihilatedCup.csproj'
    'RoomRetryMod/src/AnnihilatedRoomRetry.csproj'
    'GalleryMod/AnnihilatedGallery.csproj'
    'AttackModeSwitchMod/src/AnnihilatedAttackModeSwitch.csproj'
)
foreach ($project in $projects) {
    dotnet build (Join-Path $repo $project) --configuration Release --nologo "-p:GameRoot=$game" -p:ContinuousIntegrationBuild=true
    if ($LASTEXITCODE -ne 0) { throw "Build failed: $project" }
}

$packages = [ordered]@{
    'Annihilated-Cup-v0.4.11.zip' = [ordered]@{
        'Mods/AnnihilatedCup.dll' = $cup
        'Mods/AnnihilatedRoomRetry.dll' = $retry
        'Launch-Cup.cmd' = 'CupMod/Launch-Cup.cmd'
        'Cup-Referee.html' = 'CupMod/Cup-Referee.html'
        'INSTALL.md' = 'CupMod/INSTALL.md'
        'REFEREE.md' = 'CupMod/REFEREE.md'
        'examples/example-ritual.json' = 'CupMod/examples/example-ritual.json'
    }
    'Annihilated-Gallery-v1.2.0.zip' = [ordered]@{
        'Mods/AnnihilatedGallery.dll' = $gallery
        'INSTALL.md' = 'GalleryMod/使用说明.md'
    }
    'Annihilated-RoomRetry-v1.0.1.zip' = [ordered]@{
        'Mods/AnnihilatedRoomRetry.dll' = $retry
        'INSTALL.md' = 'RoomRetryMod/使用说明.md'
    }
    'Annihilated-AttackModeSwitch-v1.1.0.zip' = [ordered]@{
        'Mods/AnnihilatedAttackModeSwitch.dll' = $attack
        'INSTALL.txt' = 'AttackModeSwitchMod/使用说明.txt'
    }
}

New-Item -ItemType Directory -Path $dist -Force | Out-Null
$checksums = foreach ($name in $packages.Keys) {
    $target = Join-Path $dist $name
    $pending = "$target.$([guid]::NewGuid()).tmp"
    try {
        $zip = [IO.Compression.ZipFile]::Open($pending, [IO.Compression.ZipArchiveMode]::Create)
        try {
            foreach ($entry in $packages[$name].GetEnumerator()) {
                $source = Join-Path $repo $entry.Value
                [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $source, $entry.Key) | Out-Null
            }
        } finally {
            $zip.Dispose()
        }
        [IO.File]::Move($pending, $target, $true)
    } finally {
        [IO.File]::Delete($pending)
    }
    "$((Get-FileHash -LiteralPath $target).Hash.ToLowerInvariant())  $name"
    Write-Host "Created $target"
}
$checksums | Set-Content -LiteralPath (Join-Path $dist 'SHA256SUMS.txt') -Encoding utf8
