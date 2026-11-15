using System.Diagnostics;
using System.Runtime.InteropServices;
using Il2Cpp;
using Il2CppNaninovel;
using Il2CppInterop.Runtime;
using MelonLoader.NativeUtils;
using UnityRandom = UnityEngine.Random;


namespace CupCompetition;

internal sealed class NativeRandom : IDisposable
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void MapMoveNext(IntPtr state, IntPtr method);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void StateConstructor(IntPtr instance, IntPtr config, IntPtr engineConfig, IntPtr method);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void MapClick(IntPtr choice, IntPtr method);

    private NativeHook<MapClick>? clickHook;
    private MapClick? clickCallback;
    private NativeHook<StateConstructor>? saveHook;
    private StateConstructor? saveCallback;
    public Il2CppSystem.Func<double>? ItemSource { get; private set; }

    private NativeHook<MapMoveNext>? mapHook;
    private MapMoveNext? trampoline;
    private MapMoveNext? callback;
    private System.Random itemRandom = new(0);

    public void Install()
    {
        var module = Process.GetCurrentProcess().Modules.Cast<ProcessModule>()
            .Single(m => string.Equals(m.ModuleName, "GameAssembly.dll", StringComparison.OrdinalIgnoreCase));
        var address = module.BaseAddress + 0x830e70;
        callback = RunMap;
        var callbackPointer = Marshal.GetFunctionPointerForDelegate(callback);
        mapHook = new NativeHook<MapMoveNext>(address, callbackPointer);
        mapHook.Attach();
        if (mapHook.TrampolineHandle == address) throw new InvalidOperationException("Map generation hook failed.");
        trampoline = mapHook.Trampoline;
        saveCallback = InitializeSaveState;
        saveHook = new NativeHook<StateConstructor>(module.BaseAddress + 0xaf96c0, Marshal.GetFunctionPointerForDelegate(saveCallback));
        saveHook.Attach();
        if (saveHook.TrampolineHandle == saveHook.Target) throw new InvalidOperationException("Save isolation hook failed.");
        clickCallback = SelectMapTile;
        clickHook = new NativeHook<MapClick>(module.BaseAddress + 0x7b0ba0, Marshal.GetFunctionPointerForDelegate(clickCallback));
        clickHook.Attach();
        if (clickHook.TrampolineHandle == clickHook.Target) throw new InvalidOperationException("Map choice hook failed.");
        ItemSource = (Il2CppSystem.Func<double>)NextItem;
    }

    private void InitializeSaveState(IntPtr instance, IntPtr config, IntPtr engineConfig, IntPtr method)
    {
        if (config != IntPtr.Zero) CupMod.IsolateSaves(new StateConfiguration(config));
        saveHook!.Trampoline(instance, config, engineConfig, method);
    }

    private void SelectMapTile(IntPtr choice, IntPtr method)
    {
        if (choice != IntPtr.Zero && CupMod.Active is { UsesMageDetour: true } cup && cup.Run.Running)
        {
            IntPtr owner = Marshal.ReadIntPtr(choice, 0x20);
            IntPtr spaces = owner == IntPtr.Zero ? IntPtr.Zero : Marshal.ReadIntPtr(owner, 0x68);
            int index = Marshal.ReadInt32(choice, 0x18);
            if (spaces != IntPtr.Zero && index >= 0 && index < Marshal.ReadInt64(spaces, 0x18))
            {
                IntPtr tile = Marshal.ReadIntPtr(spaces, 0x20 + index * IntPtr.Size);
                if (tile != IntPtr.Zero && !cup.KeepMapChoice((MapSpaceType)Marshal.ReadInt32(tile, 0x10),
                    IL2CPP.Il2CppStringToManaged(Marshal.ReadIntPtr(tile, 0x20)))) return;
            }
        }
        clickHook!.Trampoline(choice, method);
    }

    private void RunMap(IntPtr state, IntPtr method)
    {
        if (CupMod.Active is not { } cup || !cup.Run.Running || state == IntPtr.Zero)
        {
            trampoline!(state, method);
            return;
        }

        cup.RecordMapHook();
        int dungeon = Marshal.ReadInt32(state, 0x20);
        int section = Marshal.ReadInt32(state, 0x24);
        int seed = CupRandom.Seed(cup.Settings.Seed, "map", dungeon, section);
        var before = UnityRandom.state;
        bool wasGenerating = cup.GeneratingMap;
        try
        {
            cup.GeneratingMap = true;
            DungeonAiProductQualityRandom.Reset(seed);
            UnityRandom.InitState(seed);
            trampoline!(state, method);
        }
        finally
        {
            cup.GeneratingMap = wasGenerating;
            UnityRandom.state = before;
        }
    }

    public void StartTile(int dungeon, int section, int x, int y)
    {
        int seed = CupRandom.Seed(CupMod.Active!.Settings.Seed, "item", dungeon, section, x, y);
        itemRandom = new System.Random(seed);
    }

    public double NextItem() => itemRandom.NextDouble();

    public void Dispose()
    {
        clickHook?.Detach();
        saveHook?.Detach();
        mapHook?.Detach();
        mapHook = null;
    }
}
