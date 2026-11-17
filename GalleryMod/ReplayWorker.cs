using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.Json;
using HarmonyLib;
using Il2CppNaninovel;
using Il2CppNaninovel.UI;
using MelonLoader;
using MelonLoader.NativeUtils;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AnnihilatedGallery;

internal sealed class ReplayWorker : IDisposable
{
    private const string WorkerArg = "--gallery-worker=";
    private const string SessionArg = "--gallery-session=";
    private const string LocaleArg = "--gallery-locale=";
    private const string AppearanceArg = "--gallery-appearance=";
    private const string PreludeScript = "GalleryReplayPrelude";
    private static readonly string[] DungeonLayers = { "FrameBackground", "FarBackground", "RoadBackground", "SceneBackground" };
    private enum Stage { Title, Locale, Initializing, Redirect, PrepareBackdrop, BackdropReady, Load, Playing, Done }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void StateConstructor(nint self, nint config, nint engine, nint method);

    private static ReplayWorker active;
    private readonly ReplayEntry entry;
    private readonly Action<string> log;
    private readonly string locale;
    private readonly bool longHair;
    private readonly string profile;
    private readonly int backgroundReturnLine;
    private readonly StateConstructor intercept;
    private readonly NativeHook<StateConstructor> saveHook;
    private Stage stage;
    private bool isolated;
    private bool finish;
    private string finishReason;
    private bool started;
    private float setupDeadline;
    private CustomScriptPlayer player;
    private IManagedUI title;
    private UniTask.Awaiter localeTask;
    private UniTask<Script>.Awaiter scriptTask;
    private UniTask.Awaiter startTask;
    private UniTask.Awaiter backdropTask;
    private bool backdropStarted;
    private GUIStyle exitStyle;

    public static bool IsWorker(string[] arguments) => arguments.Any(arg => arg.StartsWith(WorkerArg, StringComparison.Ordinal));
    public static bool ActivePlayback => active?.stage == Stage.Playing;

    public static ReplayWorker Open(string[] arguments, HarmonyLib.Harmony harmony, Action<string> log)
    {
        var game = GalleryGameBuild.Current
            ?? throw new InvalidOperationException("剧情回放尚未适配当前游戏版本。");
        string entryId = Value(arguments, WorkerArg);
        string sessionId = Value(arguments, SessionArg);
        if (!Guid.TryParseExact(sessionId, "N", out Guid id))
            throw new InvalidOperationException("剧情回放缺少存档隔离标识。");
        var entry = ReplayCatalog.Read().SingleOrDefault(item => item.Id == entryId)
            ?? throw new InvalidOperationException("找不到剧情回放条目。");
        return new ReplayWorker(game, entry, id.ToString("N"), Value(arguments, LocaleArg),
            Value(arguments, AppearanceArg) == "1", harmony, log);
    }

    private ReplayWorker(GalleryGameBuild game, ReplayEntry entry, string session, string locale, bool longHair, HarmonyLib.Harmony harmony, Action<string> log)
    {
        this.entry = entry;
        this.locale = locale;
        this.longHair = longHair;
        this.log = log;
        profile = $"GalleryReplayProfiles/{session}";
        backgroundReturnLine = game.BackgroundReturnLine;

        intercept = SetSaveProfile;
        saveHook = new NativeHook<StateConstructor>(game.StateManagerConstructor,
            Marshal.GetFunctionPointerForDelegate(intercept));
        saveHook.Attach();
        if (saveHook.TrampolineHandle == saveHook.Target)
            throw new InvalidOperationException("剧情回放存档隔离未生效。");

        active = this;
        harmony.Patch(AccessTools.PropertyGetter(typeof(Command), "ShouldExecute"),
            prefix: new HarmonyMethod(typeof(ReplayWorker), nameof(FilterCommand)),
            postfix: new HarmonyMethod(typeof(ReplayWorker), nameof(FinishCommand)));
    }

    private void SetSaveProfile(nint self, nint config, nint engine, nint method)
    {
        var settings = new StateConfiguration(config);
        settings.SaveFolderName = profile;
        isolated = true;
        saveHook.Trampoline(self, config, engine, method);
    }

    public void Update()
    {
        if (stage == Stage.Done)
            return;
        if (Keyboard.current?.escapeKey.wasPressedThisFrame == true || finish)
        {
            Close();
            return;
        }
        if (!Engine.Initialized)
            return;
        if (!isolated)
            throw new InvalidOperationException("剧情回放拒绝在正式存档目录中运行。");
        if (setupDeadline == 0)
            setupDeadline = Time.unscaledTime + 35;
        if ((stage != Stage.Playing || !started) && Time.unscaledTime > setupDeadline)
            throw new TimeoutException($"剧情回放启动超时（{stage}）。");

        switch (stage)
        {
            case Stage.Title:
                title = Engine.GetService<IUIManager>().GetUI<ITitleUI>()?.TryCast<IManagedUI>();
                if (title?.Visible != true)
                    return;
                player = Engine.GetService<IScriptPlayer>().Cast<CustomScriptPlayer>();
                var localization = Engine.GetService<ILocalizationManager>();
                if (!string.IsNullOrEmpty(locale) && localization.SelectedLocale != locale)
                {
                    localeTask = localization.SelectLocaleAsync(locale).GetAwaiter();
                    stage = Stage.Locale;
                }
                else InitializeStory();
                break;
            case Stage.Locale:
                if (!localeTask.IsCompleted)
                    return;
                localeTask.GetResult();
                InitializeStory();
                break;
            case Stage.Redirect:
                player.Stop();
                player.GosubReturnSpots.Clear();
                SetInitialVariables();
                if (entry.InheritedBackdrop == null && entry.InheritedMusic == null)
                    LoadClip();
                else
                    PrepareBackdrop();
                break;
            case Stage.PrepareBackdrop:
                if (!backdropStarted && backdropTask.IsCompleted)
                {
                    backdropTask.GetResult();
                    backdropStarted = true;
                }
                break;
            case Stage.BackdropReady:
                // Func starts several layers with wait:false. Stopping the preparatory
                // script before those actors are visible can cancel a pending layer.
                if (entry.InheritedBackdrop == null || BackdropVisible())
                    LoadClip();
                break;
            case Stage.Load:
                if (!scriptTask.IsCompleted)
                    return;
                Script script = scriptTask.GetResult() ?? throw new InvalidOperationException("无法载入原版剧情脚本。");
                stage = Stage.Playing;
                startTask = player.PreloadAndPlayAsync(script, entry.Start, 0, null).GetAwaiter();
                break;
            case Stage.Playing:
                if (started || !startTask.IsCompleted)
                    return;
                startTask.GetResult();
                started = true;
                log($"REPLAY_STARTED {entry.Id} {entry.Script}:{entry.Start} isolated={isolated}");
                break;
        }
    }

    public void RequestExit() => finish = true;

    public void DrawExit()
    {
        if (stage == Stage.Done)
            return;
        if (exitStyle == null)
        {
            exitStyle = new GUIStyle
            {
                font = Font.CreateDynamicFontFromOSFont("Microsoft YaHei", 18),
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter
            };
            exitStyle.normal.background = GUI.skin.button.normal.background;
            exitStyle.hover.background = GUI.skin.button.hover.background;
            exitStyle.active.background = GUI.skin.button.active.background;
            exitStyle.normal.textColor = exitStyle.hover.textColor = exitStyle.active.textColor = Color.white;
        }
        int order = GUI.depth;
        GUI.depth = -1000;
        if (GUI.Button(new Rect(Screen.width - 160, 13, 146, 40), "退出回放 (Esc)", exitStyle))
            RequestExit();
        GUI.depth = order;
    }

    private void InitializeStory()
    {
        var button = Resources.FindObjectsOfTypeAll<TitleNewGameButton>()
            .FirstOrDefault(item => item.gameObject.activeInHierarchy)
            ?? throw new InvalidOperationException("找不到原版开始游戏按钮。");
        stage = Stage.Initializing;
        button.OnButtonClick();
    }

    private void SetInitialVariables()
    {
        var variables = Engine.GetService<ICustomVariableManager>();
        string appearance = longHair ? "1" : "2";
        variables.SetVariableValue("playerType", appearance);
        variables.SetVariableValue("playerLooksType", appearance);
        variables.SetVariableValue("itininsyouJp", longHair ? "私" : "僕");
        variables.SetVariableValue("SoldierEnHe", longHair ? "She" : "He");
        variables.SetVariableValue("SoldierEnHim", longHair ? "Her" : "Him");
        if (!string.IsNullOrEmpty(entry.Partner))
        {
            variables.SetVariableValue("partnerCharacter", entry.Partner);
            variables.SetVariableValue("dungeonId", entry.DungeonId.ToString(CultureInfo.InvariantCulture));
            variables.SetVariableValue("sectionId", "1");
        }
        foreach (var pair in entry.Variables)
        {
            var value = pair.Value;
            variables.SetVariableValue(pair.Key, value.ValueKind == JsonValueKind.String ? value.GetString() : value.GetRawText());
        }
    }

    private void PrepareBackdrop()
    {
        // These are the exploration tracks used by Dungeon before its floor scenes.
        string music = entry.InheritedMusic ?? (entry.InheritedBackdrop switch
        {
            "dungeon_blue" => "m2",
            "heights" => "m3",
            "sea" => "m4",
            "dungeon_red" => "m5",
            _ => throw new InvalidDataException($"未知的回放场景背景：{entry.InheritedBackdrop}")
        });
        string commands = $"@bgm {music}_loop intro:{music}_intro\n";
        if (entry.InheritedBackdrop == null)
            commands += "@stop\n";
        else
        {
            Engine.GetService<ICustomVariableManager>().SetVariableValue("backgroundName", entry.InheritedBackdrop);
            commands = "@vcam offset:0,0,{vcamDefaultZ} rotation:0,0,0 time:0\n" + commands +
                       "@goto Func.ShowBackground\n";
        }
        var prelude = Script.FromScriptText(PreludeScript, commands, (string)null);
        stage = Stage.PrepareBackdrop;
        backdropTask = player.PreloadAndPlayAsync(prelude, 0, 0, null).GetAwaiter();
    }

    private void LoadClip()
    {
        player.Stop();
        scriptTask = Engine.GetService<IScriptManager>().LoadScriptAsync(entry.Script).GetAwaiter();
        stage = Stage.Load;
    }

    private bool BackdropVisible()
    {
        var backgrounds = Engine.GetService<IBackgroundManager>().Cast<IActorManager>();
        foreach (string layer in DungeonLayers)
        {
            if (!backgrounds.ActorExists(layer))
                return false;
            var actor = backgrounds.GetActor(layer);
            if (!actor.Visible || actor.Appearance != entry.InheritedBackdrop)
                return false;
        }
        return true;
    }

    private static bool FilterCommand(Command __instance, ref bool __result)
    {
        var worker = active;
        if (worker == null)
            return true;
        var spot = __instance.PlaybackSpot;
        string type = __instance.GetIl2CppType().Name;
        if (worker.stage == Stage.Initializing && spot.ScriptName == "Opening")
            worker.stage = Stage.Redirect;

        if (worker.stage is Stage.Redirect or Stage.BackdropReady or Stage.Load or Stage.Done || worker.finish ||
            (type is "SteamAchievement" or "AutoSave" or "QuickSave" or "QuickLoad" or "CallSaveLoad"))
        {
            __result = false;
            return false;
        }
        if (worker.stage != Stage.Playing)
            return true;

        bool root = worker.player.GosubReturnSpots.Count == 0;
        bool outside = spot.ScriptName == worker.entry.Script &&
                       (spot.LineIndex < worker.entry.Start || spot.LineIndex >= worker.entry.End);
        bool nextChapter = spot.ScriptName != worker.entry.Script &&
                           !(worker.entry.Ending && spot.ScriptName == "Ending");
        if (root && (outside || nextChapter) && worker.IsCurrent(spot))
        {
            worker.finish = true;
            worker.finishReason = $"边界 {spot.ScriptName}:{spot.LineIndex}";
            __result = false;
            return false;
        }
        return true;
    }

    private static void FinishCommand(Command __instance, ref bool __result)
    {
        var worker = active;
        if (!__result || worker == null)
            return;
        var spot = __instance.PlaybackSpot;
        string type = __instance.GetIl2CppType().Name;
        bool preludeEnd = spot.ScriptName == PreludeScript && type == "Stop" ||
                          spot.ScriptName == "Func" && spot.LineIndex == worker.backgroundReturnLine;
        if (worker.stage == Stage.PrepareBackdrop && preludeEnd && worker.IsCurrent(spot))
        {
            worker.stage = Stage.BackdropReady;
            __result = false;
            return;
        }
        if (worker.stage != Stage.Playing)
            return;
        if ((type == "ExitToTitle" || type == "Return" && worker.player.GosubReturnSpots.Count == 0) &&
            worker.IsCurrent(spot))
        {
            worker.finish = true;
            worker.finishReason = $"{type} {spot.ScriptName}:{spot.LineIndex}";
            __result = false;
        }
    }

    private bool IsCurrent(PlaybackSpot spot)
    {
        var current = player.PlaybackSpot;
        return current.ScriptName == spot.ScriptName && current.LineIndex == spot.LineIndex;
    }

    private void Close()
    {
        stage = Stage.Done;
        player?.Stop();
        log($"REPLAY_FINISHED {entry.Id} {finishReason ?? "主动退出"}");
        Application.Quit(0);
    }

    public void Dispose()
    {
        // The save constructor remains isolated until this process exits.
        stage = Stage.Done;
    }

    internal static string Value(IEnumerable<string> arguments, string prefix)
        => arguments.FirstOrDefault(arg => arg.StartsWith(prefix, StringComparison.Ordinal))?[prefix.Length..] ?? "";
}
