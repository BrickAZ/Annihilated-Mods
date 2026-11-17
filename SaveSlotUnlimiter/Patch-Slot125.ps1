#Requires -Version 7.0
param(
    [Parameter(Mandatory)]
    [string]$InputDll,

    [Parameter(Mandatory)]
    [string]$GameRoot,

    [string]$OutputDll = (Join-Path $PSScriptRoot 'patched/SaveSlotUnlimiter.dll')
)

$ErrorActionPreference = 'Stop'
$source = (Get-Item -LiteralPath $InputDll).FullName
$destination = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputDll)
if ($source -ieq $destination) {
    throw 'Choose a different path for the patched DLL.'
}

$originalSha = 'B08ADB31EF98008AC5D57C3F8B0298896FE9DD8C76B18A5D9A528F418E042396'
$patchedSha = '6A6227AC519E27CA1A07198FE0D6C769C3633BB8C92EB4DA3840BF4A0F87F257'
if ((Get-FileHash -LiteralPath $source).Hash -cne $originalSha) {
    throw "Expected the original SaveSlotUnlimiter v1.25 DLL (SHA-256 $originalSha)."
}

$game = (Get-Item -LiteralPath $GameRoot).FullName
$cecil = Join-Path $game 'MelonLoader/net6/Mono.Cecil.dll'
if (!(Test-Path -LiteralPath $cecil -PathType Leaf)) {
    throw 'GameRoot must point to your game directory with MelonLoader 0.7.3 installed.'
}
Add-Type -LiteralPath $cecil

$null = [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($destination))
$pending = "$destination.$([guid]::NewGuid()).tmp"
try {
    $assembly = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($source)
    try {
        $core = $assembly.MainModule.GetType('SaveSlotUnlimiter.Core')
        $update = $core.Methods | Where-Object Name -ceq 'OnUpdate'
        $wrap = $core.Methods | Where-Object Name -ceq 'Wrap'
        $menuType = $core.Fields | Where-Object Name -ceq '_menuType'
        $pointer = $update.Body.Variables[11]
        if ($pointer.VariableType.FullName -cne 'System.IntPtr') {
            throw 'Unexpected native menu pointer local.'
        }

        $il = $update.Body.GetILProcessor()
        # ReadItemsCount and Process both need the SaveLoadMenu wrapper to see its grids.
        foreach ($offset in (0x193, 0x237)) {
            $load = $update.Body.Instructions | Where-Object Offset -eq $offset
            if ($load.OpCode.Code.ToString() -cne 'Ldloc_S' -or $load.Operand.Index -ne 10) {
                throw 'Unexpected menu object load.'
            }
            $load.OpCode = [Mono.Cecil.Cil.OpCodes]::Ldsfld
            $load.Operand = $menuType
            $loadPointer = $il.Create([Mono.Cecil.Cil.OpCodes]::Ldloc_S, $pointer)
            $il.InsertAfter($load, $loadPointer)
            $il.InsertAfter($loadPointer, $il.Create([Mono.Cecil.Cil.OpCodes]::Call, $wrap))
        }

        $skipProcessed = $update.Body.Instructions | Where-Object Offset -eq 0x1CF
        if ($skipProcessed.OpCode.Code.ToString() -cne 'Br_S') {
            throw 'Unexpected processed-menu branch.'
        }
        $skipProcessed.OpCode = [Mono.Cecil.Cil.OpCodes]::Br
        $assembly.Write($pending)
    } finally {
        $assembly.Dispose()
    }

    if ((Get-FileHash -LiteralPath $pending).Hash -cne $patchedSha) {
        throw 'Patched DLL differs from the tested build. Use Mono.Cecil from MelonLoader 0.7.3.'
    }
    [IO.File]::Move($pending, $destination, $true)
    Get-FileHash -LiteralPath $destination -Algorithm SHA256
} finally {
    [IO.File]::Delete($pending)
}
