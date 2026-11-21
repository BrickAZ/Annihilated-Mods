param([string]$OutputPath)

$ErrorActionPreference = 'Stop'
$gameRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$project = Split-Path -Parent $PSScriptRoot
if (!$OutputPath) {
    $OutputPath = Join-Path $gameRoot 'AnnihilatedTextureEditor.exe'
}
$output = [IO.Path]::GetFullPath($OutputPath)
$build = Join-Path $project 'obj/editor'
New-Item -ItemType Directory -Path $build -Force | Out-Null

$inventory = Join-Path $gameRoot 'mod开发相关/Annihilated_美术资源/资源清单.csv'
$rows = Import-Csv -LiteralPath $inventory
$itemIcons = @(Get-Content -LiteralPath (Join-Path $project 'src/ItemIcons.txt') | Where-Object { $_ })
$listedItems = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
$frameCounts = @{}
foreach ($row in $rows) {
    if ($row.type -eq 'Sprite' -and $row.original_path) {
        $frameCounts[$row.original_path] = 1 + [int]$frameCounts[$row.original_path]
    }
}
$lines = foreach ($row in $rows) {
    if ($row.type -ne 'Texture2D' -or $row.original_path -notmatch '^Assets/Textures/(Enemy|Item)/([^/]+)\.png$') {
        continue
    }
    $group = $Matches[1]
    $id = $Matches[2]
    if ($group -eq 'Item' -and $id -notin $itemIcons) {
        continue
    }
    if ($group -eq 'Item') { [void]$listedItems.Add($id) }
    $section = if ($group -eq 'Enemy') { $row.'子类' } else { $row.'细类' }
    $family = if ($group -eq 'Enemy') { $row.'细类' } else { '' }
    @($group, $id, $row.width, $row.height, [int]$frameCounts[$row.original_path],
      $section, $family, $row.'系列或动作', $row.file) -join "`t"
}
$missing = @($itemIcons | Where-Object { !$listedItems.Contains($_) })
if ($missing.Count -gt 0) { throw "Supported item icons missing from the editor catalog: $($missing -join ', ')" }
$catalog = Join-Path $build 'catalog.tsv'
$lines | Sort-Object -Unique | Set-Content -LiteralPath $catalog -Encoding utf8

$framework = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319'
$compiler = Join-Path $framework 'csc.exe'
$references = @(
    'System.Drawing.dll', 'System.Windows.Forms.dll', 'System.Xaml.dll',
    'WPF/WindowsBase.dll', 'WPF/PresentationCore.dll', 'WPF/PresentationFramework.dll'
) | ForEach-Object { "/reference:$(Join-Path $framework $_)" }
$sources = Get-ChildItem -LiteralPath $PSScriptRoot -Filter '*.cs' | Select-Object -ExpandProperty FullName
$window = Join-Path $PSScriptRoot 'EditorWindow.xaml'
& $compiler /nologo /target:winexe /platform:x64 /optimize+ /codepage:65001 "/out:$output" "$('/resource:' + $window + ',EditorWindow.xaml')" "$('/resource:' + $catalog + ',catalog.tsv')" @references @sources
if ($LASTEXITCODE -ne 0) { throw 'Editor compilation failed.' }
Get-Item -LiteralPath $output
