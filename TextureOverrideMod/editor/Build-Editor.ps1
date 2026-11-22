#Requires -Version 7.0
param([string]$OutputPath = (Join-Path $PSScriptRoot '../obj/editor/AnnihilatedTextureEditor.exe'))

$ErrorActionPreference = 'Stop'
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$output = [IO.Path]::GetFullPath($OutputPath)
$null = New-Item -ItemType Directory -Path (Split-Path -Parent $output) -Force
$catalog = Join-Path $PSScriptRoot 'catalog.tsv'

$framework = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319'
$compiler = Join-Path $framework 'csc.exe'
$references = @(
    'System.Drawing.dll', 'System.Windows.Forms.dll', 'System.Xaml.dll', 'System.IO.Compression.dll',
    'WPF/WindowsBase.dll', 'WPF/PresentationCore.dll', 'WPF/PresentationFramework.dll'
) | ForEach-Object { "/reference:$(Join-Path $framework $_)" }
$sources = Get-ChildItem -LiteralPath $PSScriptRoot -Filter '*.cs' | Select-Object -ExpandProperty FullName
$window = Join-Path $PSScriptRoot 'EditorWindow.xaml'
$installer = Join-Path $repo 'scripts/Install-Mod.ps1'
& $compiler /nologo /target:winexe /platform:x64 /optimize+ /codepage:65001 "/out:$output" "$('/resource:' + $window + ',EditorWindow.xaml')" "$('/resource:' + $catalog + ',catalog.tsv')" "$('/resource:' + $installer + ',Install-Mod.ps1')" @references @sources
if ($LASTEXITCODE -ne 0) { throw 'Editor compilation failed.' }
Get-Item -LiteralPath $output
