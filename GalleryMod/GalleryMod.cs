using Il2CppNaninovel;
using Il2CppNaninovel.UI;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(AnnihilatedGallery.GalleryMod), "Annihilated CG Gallery", "1.2.1", "Brick")]
[assembly: MelonGame("DeathKumiai", "Annihilated")]

namespace AnnihilatedGallery;

public sealed class GalleryMod : MelonMod
{
    private IUIManager ui;
    private GalleryView view;
    private ReplayWorker worker;
    private StoryCgArchive archive;
    private bool disabled;
    private bool workerProcess;

    public override void OnInitializeMelon()
    {
        var arguments = Environment.GetCommandLineArgs();
        disabled = arguments.Contains("--annihilated-cup", StringComparer.OrdinalIgnoreCase);
        workerProcess = ReplayWorker.IsWorker(arguments);
        if (!workerProcess)
            return;
        try
        {
            worker = ReplayWorker.Open(arguments, HarmonyInstance, msg => LoggerInstance.Msg(msg));
        }
        catch (Exception ex)
        {
            LoggerInstance.Error(ex);
            Application.Quit(21);
        }
    }

    public override void OnUpdate()
    {
        if (disabled)
            return;

        try
        {
            if (Engine.Initialized)
            {
                archive ??= new StoryCgArchive(message => LoggerInstance.Warning(message));
                archive.Update(!workerProcess || ReplayWorker.ActivePlayback);
            }
            if (workerProcess)
            {
                worker?.Update();
                return;
            }
            if (!Engine.Initialized)
                return;
            ui ??= Engine.GetService<IUIManager>();
            bool onTitle = IsVisible<ITitleUI>() &&
                           !IsVisible<ISettingsUI>() && !IsVisible<ISaveLoadUI>() &&
                           !IsVisible<IConfirmationUI>() && !IsVisible<ITipsUI>() && !IsVisible<IMovieUI>();
            if (view == null && onTitle)
            {
                var entries = GalleryCatalog.Read();
                view = new GalleryView(entries, ReplayCatalog.Read(), archive.Read, message => LoggerInstance.Warning(message));
                LoggerInstance.Msg($"CG gallery ready ({entries.Length} entries).");
            }
            view?.Update(onTitle);
        }
        catch (Exception ex)
        {
            disabled = true;
            LoggerInstance.Error(ex);
            if (workerProcess)
                Application.Quit(22);
            view?.Dispose();
            view = null;
        }
    }

    public override void OnGUI() => worker?.DrawExit();

    public override void OnDeinitializeMelon()
    {
        view?.Dispose();
        archive?.Dispose();
        worker?.Dispose();
    }

    private bool IsVisible<T>() where T : Il2CppInterop.Runtime.InteropTypes.Il2CppObjectBase
        => ui?.GetUI<T>()?.TryCast<IManagedUI>()?.Visible == true;
}
