using System.Reflection;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Encodings.Web;
using System.Text.Json;
using HarmonyLib;
using Il2Cpp;
using Il2CppNaninovel;
using Il2CppNaninovel.Commands;
using Il2CppNaninovel.UI;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(CupCompetition.CupMod), "Annihilated Cup", CupCompetition.CupMod.Version, "Brick")]
[assembly: MelonGame("DeathKumiai", "Annihilated")]

namespace CupCompetition;

public sealed partial class CupMod : MelonMod
{
    public const string Version = "0.4.12";
    internal const int GameBuild = 26705080;
    internal const string BuildHash = "13B13F97BBE50154B451753351656F99595AE0A542586097F27536F55C24F3C4";
    internal const string CatalogHash = "c867cf04b05171a82710f970f4122af7";
    internal const string ModeVariable = "xSwitchGameCursorOperationMode";
    private const string RoomRetrySlot = "AnnihilatedRoomRetryCheckpoint";
    private const string Profile = "AnnihilatedCup";
    internal static CupMod? Active { get; private set; }
    internal readonly CupRun Run = new();
    internal CupSettings Settings { get; private set; } = new();
    private Route? Target { get; set; }
    private BossGoal? Boss { get; set; }
    private string GoalId => Boss?.Id ?? Target?.Id ?? "";
    private string GoalName => Boss?.Name ?? $"{Target?.PartnerName} · {Target?.Name}";
    internal string ModeValue => Settings.BattleMode == "probability" ? "1" : "0";
    internal string Read(string key) => vars?.GetVariableValue(key) ?? "";
    internal int Number(string key) => int.TryParse(Read(key), NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) ? n : 0;
    internal bool Flag(string key) => Read(key) is "1" or "true" or "True";
    internal bool GeneratingMap { get; set; }

    private readonly List<object> splits = new();
    private readonly Dictionary<string, string> mapFingerprints = new();
    private string root = "";
    private string matchHash = "";
    private string runId = "";
    private string error = "";
    private string progressName = "序章";
    private string profileFolder = "";
    private bool isolated;
    private bool startRedirected;
    private bool clockStarted;
    private bool openingRulesApplied;
    private bool openingDeathPending;
    private bool openingDeathInProgress;
    private bool openingDeathMenuReady;
    private bool smoke;
    private bool bossSmoke;
    private bool ritualSmoke;
    private bool mageSmoke;
    private bool limitsSmoke;
    private bool openingRulesSmoke;
    private bool curseUiSmoke;
    private bool knifeSmoke;
    private bool trainingSmoke;
    private bool weaponRulesSmoke;
    private bool weaponOpeningSmoke;
    private int jumpSmokeFloor;
    private bool smokeStarted;
    private bool smokeScreenshotRequested;
    private bool titleReadyLogged;
    private string SmokeScreenshot => Path.Combine(root, "verification", runId + "-setup.png");
    private CupSetup? setup;
    private CupSetupPanel? setupPanel;
    private TitleMenu? titleMenu;
    private SaveLoadMenu? saveMenu;
    private VariableInputPanel? inputPanel;
    private UIManager? uiManager;
    private IntPtr cutsceneCommand;
    private IntPtr dialogueCommand;
    private CupPauseReason pauseReason;
    private bool titleInteractionDisabled;
    private bool previousTitleInteraction;
    private bool allowNewGame;
    private bool hooksInstalled;
    private double launchRequestedAt = -1;
    private string setupNotice = "";
    private CupSettings? queuedSettings;
    private static readonly MethodInfo NewGameClick = AccessTools.Method(typeof(TitleNewGameButton), "OnButtonClick");
    private UniTask? smokeMapTask;
    private string? smokeFirstMap;
    private UniTask<Script>? smokeOpening;
    private int filteredChoices;
    private bool explorationStarted;
    private string lastMapVisit = "";
    private int visitedMaps;
    private int mapHookCalls;
    private string bossBattle = "";
    private double routeNoticeUntil;
    private int seededTiles;
    private int scriptEvents;
    private GUIStyle? hudStyle;
    private Il2CppSystem.Action<Command>? listener;
    private Il2CppSystem.Action<Command>? commandFinished;
    private Il2CppSystem.Action<bool>? skipChanged;
    private CustomVariableManager? vars;
    private CustomScriptPlayer? player;
    private NativeRandom? nativeRandom;
    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public override void OnInitializeMelon()
    {
        root = Path.Combine(AppContext.BaseDirectory, "UserData", Profile);
        Directory.CreateDirectory(root);
        string config = Path.Combine(root, "cup.json");
        if (!File.Exists(config)) File.WriteAllText(config, JsonSerializer.Serialize(Settings, Json));
        File.WriteAllText(Path.Combine(root, "endings.json"), JsonSerializer.Serialize(
            Route.All.Select(r => new { r.Id, Partner = r.PartnerName, Ending = r.Name, r.Kind }), Json));

        if (!Environment.GetCommandLineArgs().Contains("--annihilated-cup", StringComparer.Ordinal))
        {
            LoggerInstance.Msg("Normal mode. Use --annihilated-cup to enter a separate cup profile.");
            return;
        }

        try
        {
            bossSmoke = Environment.GetCommandLineArgs().Contains("--annihilated-cup-boss-smoke", StringComparer.Ordinal);
            ritualSmoke = Environment.GetCommandLineArgs().Contains("--annihilated-cup-ritual-smoke", StringComparer.Ordinal);
            limitsSmoke = Environment.GetCommandLineArgs().Contains("--annihilated-cup-limits-smoke", StringComparer.Ordinal);
            curseUiSmoke = Environment.GetCommandLineArgs().Contains("--annihilated-cup-curse-ui-smoke", StringComparer.Ordinal);
            openingRulesSmoke = curseUiSmoke || Environment.GetCommandLineArgs().Contains("--annihilated-cup-opening-rules-smoke", StringComparer.Ordinal);
            knifeSmoke = Environment.GetCommandLineArgs().Contains("--annihilated-cup-knife-smoke", StringComparer.Ordinal);
            trainingSmoke = Environment.GetCommandLineArgs().Contains("--annihilated-cup-training-smoke", StringComparer.Ordinal);
            weaponRulesSmoke = Environment.GetCommandLineArgs().Contains("--annihilated-cup-weapon-rules-smoke", StringComparer.Ordinal);
            weaponOpeningSmoke = Environment.GetCommandLineArgs().Contains("--annihilated-cup-weapon-opening-smoke", StringComparer.Ordinal);
            jumpSmokeFloor = Enumerable.Range(2, 5).FirstOrDefault(floor =>
                Environment.GetCommandLineArgs().Contains($"--annihilated-cup-floor-{floor}-smoke", StringComparer.Ordinal));
            mageSmoke = limitsSmoke || openingRulesSmoke || knifeSmoke || trainingSmoke || weaponRulesSmoke || weaponOpeningSmoke || jumpSmokeFloor is >= 2 and <= 5 ||
                Environment.GetCommandLineArgs().Contains("--annihilated-cup-mage-smoke", StringComparer.Ordinal);
            smoke = bossSmoke || ritualSmoke || mageSmoke || jumpSmokeFloor == 6 || Environment.GetCommandLineArgs().Contains("--annihilated-cup-smoke", StringComparer.Ordinal);
            string game = AppContext.BaseDirectory;
            using var assembly = File.OpenRead(Path.Combine(game, "GameAssembly.dll"));
            using var sha = SHA256.Create();
            if (Convert.ToHexString(sha.ComputeHash(assembly)) != BuildHash ||
                File.ReadAllText(Path.Combine(game, "Annihilated_Data", "StreamingAssets", "aa", "catalog.hash")).Trim() != CatalogHash)
                throw new InvalidDataException($"游戏版本不匹配。需要 Steam Build {GameBuild}。");
            setup = new CupSetup(smoke ? new CupSettings { EventId = "Smoke" } : ReadLastSetup(config));
            if (smoke)
            {
                setup.SelectPartner("warrior");
                setup.SelectEnding("warriorGlitchBad");
                if (mageSmoke)
                {
                    setup.SelectPartner("mage");
                    setup.SelectEnding("mageMaryoku");
                }
                if (ritualSmoke) setup.SelectPartner("common");
                setup.BossMode = bossSmoke;
                if (bossSmoke) setup.SelectBoss("priest");
                if (jumpSmokeFloor == 6)
                {
                    setup.SelectPartner("priest");
                    setup.SelectEnding("priestLowLoop");
                }
                if (jumpSmokeFloor > 0) setup.StartingFloorText = jumpSmokeFloor.ToString(CultureInfo.InvariantCulture);
                if (weaponRulesSmoke) setup.MaxWeaponsText = "1";
                if (weaponOpeningSmoke)
                {
                    setup.KickOnly = true;
                    setup.StartingCurseText = "100";
                }
                setup.SeedText = mageSmoke ? "20261113" : "-20261113";
                setup.BattleMode = trainingSmoke && Environment.GetCommandLineArgs().Contains(
                    "--annihilated-cup-training-probability-smoke", StringComparer.Ordinal) ? "probability" : "timing";
                setup.LimitText = "25";
                setup.RunKnife = knifeSmoke;
                setup.HarshTraining = trainingSmoke;
                if (trainingSmoke)
                    setup.KickOnly = Environment.GetCommandLineArgs().Contains("--annihilated-cup-training-curse-smoke", StringComparer.Ordinal);
                if (openingRulesSmoke)
                {
                    setup.StartingAllocationText = "6";
                    setup.StartingCurseText = curseUiSmoke ? "800" : "100";
                    setup.DeathPenaltyText = "17";
                }
                if (limitsSmoke)
                {
                    setup.RoomRetriesText = "1";
                    setup.RoomRetryPenaltyText = "7";
                    setup.DeathsText = "1";
                    setup.DeathPenaltyText = "17";
                }
            }
            runId = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) + "-" + Guid.NewGuid().ToString("N")[..8];
            profileFolder = "CupSaves/" + runId;
            Active = this;
            try
            {
                nativeRandom = new NativeRandom();
                nativeRandom.Install();
            }
            catch
            {
                nativeRandom?.Dispose();
                Active = null;
                HarmonyInstance.UnpatchSelf();
                throw;
            }
            LoggerInstance.Msg("Cup mode loaded. Choose the setup on the title screen.");
        }
        catch (Exception ex)
        {
            error = ex.Message;
            LoggerInstance.Error(ex.ToString());
        }
    }

    private CupSettings ReadLastSetup(string path)
    {
        try
        {
            return JsonSerializer.Deserialize<CupSettings>(File.ReadAllBytes(path)) ?? new CupSettings();
        }
        catch (JsonException)
        {
            setupNotice = "上次设置无法读取，已载入默认选项。";
            return new CupSettings();
        }
    }

    private void StartCup(CupSettings settings)
    {
        var button = Resources.FindObjectsOfTypeAll<TitleNewGameButton>()
            .FirstOrDefault(candidate => candidate != null && candidate.gameObject.activeInHierarchy);
        if (button == null)
        {
            setupNotice = "标题界面还在载入，请稍后再试。";
            return;
        }

        if (!smoke)
        {
            try
            {
                string file = Path.Combine(root, "cup.json");
                File.WriteAllText(file + ".tmp", JsonSerializer.Serialize(settings, Json));
                File.Move(file + ".tmp", file, true);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                LoggerInstance.Warning(ex.Message);
                setupNotice = "保存配置失败，请检查游戏目录的写入权限。";
                return;
            }
        }

        Settings = settings;
        Target = Route.All.SingleOrDefault(route => route.Id == Settings.RouteId);
        Boss = BossGoal.All.SingleOrDefault(goal => goal.Id == Settings.BossId);
        if (!hooksInstalled)
        {
            InstallHooks();
            hooksInstalled = true;
        }
        player!.SkipMode = PlayerSkipMode.Everything;
        matchHash = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(
            new { Game = BuildHash, Catalog = CatalogHash, Mod = Version, Settings })));
        setupNotice = "";
        launchRequestedAt = Time.realtimeSinceStartupAsDouble;
        RestoreTitleInteraction();
        allowNewGame = true;
        try
        {
            LoggerInstance.Msg($"Cup armed | {Settings.EventId} | {GoalId} | seed={Settings.Seed} | {Settings.BattleMode}");
            NewGameClick.Invoke(button, null);
        }
        finally { allowNewGame = false; }
    }

    private void RestoreTitleInteraction()
    {
        if (!titleInteractionDisabled) return;
        if (titleMenu != null) titleMenu.Interactable = previousTitleInteraction;
        titleInteractionDisabled = false;
    }

    private void Hook(Type type, string name, string prefix, Type[]? args = null)
    {
        var original = AccessTools.Method(type, name, args) ?? throw new MissingMethodException(type.FullName, name);
        HarmonyInstance.Patch(original, prefix: new HarmonyMethod(typeof(CupMod), prefix));
    }

    private void InstallHooks()
    {
        Hook(typeof(CustomVariableManager), "SetVariableValue", nameof(LockVariables), new[] { typeof(string), typeof(string) });
        HarmonyInstance.Patch(AccessTools.Method(typeof(CustomVariableManager), "GetVariableValue", new[] { typeof(string) }),
            postfix: new HarmonyMethod(typeof(CupMod), nameof(FixedMapSanity)));
        Hook(typeof(Command), "get_ShouldExecute", nameof(FilterScriptCommand));
        Hook(typeof(TitleNewGameButton), "OnButtonClick", nameof(NewGame));
        Hook(typeof(StateManager), "LoadGameAsync", nameof(BlockGameLoad), new[] { typeof(string) });
        Hook(typeof(StateManager), "LoadGameAsync", nameof(BlockGameLoad), new[] { typeof(string), typeof(Il2CppSystem.Func<UniTask>) });
        Hook(typeof(StateManager), "QuickLoadAsync", nameof(BlockQuickLoad));
        Hook(typeof(StateManager), "RollbackAsync", nameof(BlockRollback));
        Hook(typeof(ItemService), "SelectItemsByRankWeight", nameof(SeedItems));
        Hook(typeof(ItemService), "SelectReinforceWeightedIndex", nameof(SeedItems));
        Hook(typeof(ItemService), "GetRandomUnitValue", nameof(SeedItems));
        Hook(typeof(CustomGameSettingsFreeCursorOperationModeDropdown), "OnValueChanged", nameof(BlockModeChange));
        InstallCursePoolHooks();
        InstallWeaponRulesHook();
        if (Settings.HarshTraining) InstallTrainingEligibilityHooks();
    }

    internal static void IsolateSaves(StateConfiguration config)
    {
        if (Active is not { } cup) return;
        config.SaveFolderName = cup.profileFolder;
        config.EnableStateRollback = false;
        cup.isolated = true;
        cup.LoggerInstance.Msg("Cup save folder: " + cup.profileFolder);
    }

    private static void FixedMapSanity(string name, ref string __result)
    {
        if (name == "playerSanity" && Active is { GeneratingMap: true }) __result = "50";
    }

    private static void LockVariables(string name, ref string value)
    {
        if (Active is not { } cup) return;
        if (name == ModeVariable) value = cup.ModeValue;
        else if (name == "xSwitchShowAttackModeChangeChoiceSelected") value = "1";
    }

    private static void SeedItems(ref Il2CppSystem.Func<double>? randomValue)
    {
        if (randomValue == null && Active is { } cup && cup.Run.Running && cup.nativeRandom != null)
            randomValue = cup.nativeRandom.ItemSource;
    }

    private static bool BlockModeChange() => Active == null;

    // Public entry points for the companion F9 mod. Competition counters live outside game snapshots.
    public static bool CanCaptureRoomRetry() => Active is { clockStarted: true } cup &&
        cup.Run.Running && cup.Run.RoomRetries < cup.Settings.MaxRoomRetries;

    public static bool TryBeginRoomRetry()
    {
        if (Active is not { } cup || !CanCaptureRoomRetry() || cup.Number("playerHealth") <= 0 ||
            cup.Flag("playerDeath") || !cup.Run.TryBeginRoomRetry(cup.Settings.MaxRoomRetries)) return false;
        cup.pauseReason = CupPauseReason.RoomRetry;
        cup.Run.SetPaused(true);
        cup.audit!.Record(cup.Run, "sl_begin");
        return true;
    }

    public static void CompleteRoomRetry(bool success)
    {
        if (Active is not { } cup || !cup.Run.AllowsRoomRetryLoad) return;
        try
        {
            bool used = cup.Run.CompleteRoomRetry(success, cup.Settings.RoomRetryPenaltySeconds);
            if (used)
            {
                cup.stealAddPending = cup.stealAddActive = false;
                cup.drushSource = cup.drushReplacement = IntPtr.Zero;
                cup.audit!.Record(cup.Run, "sl_success", penaltySeconds: cup.Settings.RoomRetryPenaltySeconds);
            }
            else cup.audit!.Record(cup.Run, "sl_failed");
            if (used && cup.TimeLimitReached())
            {
                cup.Finish("time_limit");
                return;
            }
            cup.UpdateTimerPause();
            if (!used) return;
            cup.Persist("running");
            cup.LoggerInstance.Msg($"Cup room retries: {cup.Run.RoomRetries}/{cup.Settings.MaxRoomRetries}; SL penalty {cup.Run.RoomRetryPenalty.TotalSeconds}s.");
        }
        catch (Exception ex) { cup.Fail(ex); }
    }

    private static bool BlockGameLoad(string __0, ref UniTask<GameStateMap> __result)
    {
        if (__0 == RoomRetrySlot && Active is { } cup && cup.Run.AllowsRoomRetryLoad) return true;
        return BlockQuickLoad(ref __result);
    }

    private static bool BlockQuickLoad(ref UniTask<GameStateMap> __result)
    {
        if (Active == null) return true;
        __result = new UniTask<GameStateMap>((GameStateMap)null!);
        return false;
    }

    private static bool BlockRollback(ref UniTask<bool> __result)
    {
        if (Active == null) return true;
        __result = new UniTask<bool>(false);
        return false;
    }

    private static bool NewGame()
    {
        if (Active is not { } cup) return true;
        return cup.allowNewGame && cup.isolated && cup.player != null;
    }

    private static bool KeepChoice(CupMod cup, Command command)
    {
        var spot = command.PlaybackSpot;
        bool isOpening = !cup.explorationStarted && spot.ScriptName == "Opening" && spot.LineIndex is 255 or 256;
        bool autoConfirm = !cup.explorationStarted && cup.Target is { Partner: not "common" } &&
            spot.ScriptName == "Opening3" && IsStartingConfirmation(spot.LineIndex);
        if (isOpening || autoConfirm) return false;
        if (cup.Target is not { } target) return true;
        bool returned = target.Partner == "warrior" ? cup.Flag("xSwitchWarriorGlitchEnd") : cup.Flag("xSwitchPriestLowSanEnded");
        if (target.KeepChoice(spot.ScriptName, spot.LineIndex, returned)) return true;
        cup.filteredChoices++;
        return false;
    }

    private static bool IsStartingConfirmation(int line) => line is
        >= 2020 and <= 2022 or >= 2028 and <= 2030 or >= 2402 and <= 2404 or
        >= 2470 and <= 2472 or >= 2859 and <= 2866;

    private static bool FilterScriptCommand(Command __instance, ref bool __result)
    {
        if (Active is not { } cup || !cup.Run.Running) return true;
        string type = __instance.GetIl2CppType().Name;
        if (type == nameof(SteamAchievement))
        {
            __result = false;
            return false;
        }
        if (type is nameof(AddChoice) or nameof(AddChoiceWithReplace) or nameof(AddChoiceWithChoiceIf))
        {
            if (KeepChoice(cup, __instance)) return true;
            __result = false;
            return false;
        }
        var spot = __instance.PlaybackSpot;
        if (cup.Settings.HarshTraining && spot.ScriptName == "Battle" &&
            (spot.LineIndex is 1973 or 2057 || cup.IsTrainingKickChoice && spot.LineIndex is 1907 or 1912 or 1920))
        {
            __result = spot.LineIndex == 2057;
            return false;
        }
        if (cup.openingDeathInProgress && spot.ScriptName == "GameOver")
        {
            if (spot.LineIndex is 39 or 40 or 41 or 126 or 127 or 128)
            {
                // Keep the original starting inventory and skip ordinary death rewards.
                __result = false;
                return false;
            }
            if (spot.LineIndex == 707)
            {
                // Return after revival, keeping the unspent starting curse instead of converting it to XP.
                __result = true;
                return false;
            }
        }
        if (cup.explorationStarted || cup.Target?.Id == "ritual") return true;
        bool skip = spot.ScriptName switch
        {
            "Opening" => type is nameof(PrintText) or nameof(Wait) or nameof(ImpulseDisplay)
                || spot.LineIndex is 249 or 250 or 257 or >= 293 and <= 310,
            "Opening2" => type is nameof(PrintText) or nameof(Wait) or nameof(ImpulseDisplay),
            "Opening3" => type is nameof(PrintText) or nameof(Wait) or nameof(ImpulseDisplay)
                || (cup.Target is { Partner: not "common" } && type == nameof(Stop) && IsStartingConfirmation(spot.LineIndex)),
            "Func" => spot.LineIndex is 1370 or 1372 or 1373,
            _ => false
        };
        if (!skip) return true;
        __result = false;
        return false;
    }

    public override void OnUpdate()
    {
        if (Active != this || error.Length > 0) return;
        try
        {
            if (player == null && Engine.Initialized)
            {
                if (!isolated) throw new InvalidOperationException("杯赛存档隔离未生效，无法开赛。");
                vars = Engine.GetService<ICustomVariableManager>().Cast<CustomVariableManager>();
                player = Engine.GetService<IScriptPlayer>().Cast<CustomScriptPlayer>();
                listener = (Il2CppSystem.Action<Command>)OnCommand;
                player.add_OnCommandExecutionStart(listener);
                commandFinished = (Il2CppSystem.Action<Command>)OnCommandFinished;
                player.add_OnCommandExecutionFinish(commandFinished);
                skipChanged = (Il2CppSystem.Action<bool>)OnSkipChanged;
                player.add_OnSkip(skipChanged);
                var ui = Engine.GetService<IUIManager>();
                uiManager = ui.Cast<UIManager>();
                titleMenu = ui.GetUI<ITitleUI>()?.TryCast<TitleMenu>()
                    ?? Resources.FindObjectsOfTypeAll<TitleMenu>().FirstOrDefault(menu => menu != null && menu.gameObject.activeInHierarchy);
                if (titleMenu == null) throw new InvalidOperationException("找不到游戏标题菜单。");
                saveMenu = ui.GetUI<ISaveLoadUI>()?.TryCast<SaveLoadMenu>();
                if (saveMenu == null) throw new InvalidOperationException("找不到游戏存档界面。");
                inputPanel = ui.GetUI<IVariableInputUI>()?.TryCast<VariableInputPanel>();
                if (inputPanel == null) throw new InvalidOperationException("找不到游戏文字输入界面。");
                LoggerInstance.Msg("Cup services initialized. Waiting for the native title menu.");
            }
            if (player != null && titleMenu != null && launchRequestedAt < 0 && Run.Result == null && titleMenu.Visible)
            {
                if (!titleReadyLogged)
                {
                    titleReadyLogged = true;
                    LoggerInstance.Msg("Cup title menu is visible.");
                }
                if (smoke && !smokeStarted)
                {
                    if (!smokeScreenshotRequested)
                    {
                        smokeScreenshotRequested = true;
                        MelonCoroutines.Start(CaptureSetupForSmoke());
                        return;
                    }
                }
                if (queuedSettings is { } chosen)
                {
                    queuedSettings = null;
                    StartCup(chosen);
                }
            }
            else if (launchRequestedAt >= 0 && !clockStarted && Run.Result == null &&
                     Time.realtimeSinceStartupAsDouble - launchRequestedAt > 30)
            {
                throw new InvalidOperationException("游戏未能进入新一局，请重新启动杯赛。");
            }

            UpdateTimerPause();

            if (smokeMapTask != null && Run.Running) VerifyMapsForSmoke();
            if (smokeOpening != null && smokeOpening.IsCompleted && Run.Running)
            {
                Script opening = smokeOpening.Result;
                smokeOpening = null;
                var playlist = new ScriptPlaylist(opening, Engine.GetService<IScriptManager>());
                var blocked = playlist.GetCommandByLine(120, 0) ?? playlist.GetCommandAfterLine(119, 0);
                if (blocked == null || blocked.PlaybackSpot.LineIndex != 120)
                    throw new InvalidOperationException("Opening lock target was not found.");
                if (bossSmoke && !blocked.ShouldExecute)
                    throw new InvalidOperationException("Boss mode blocked a story choice.");
                if (!bossSmoke && (blocked.ShouldExecute || filteredChoices < 1))
                    throw new InvalidOperationException("Opening choice lock did not fire.");
                VerifyOtherHooksForSmoke();
                MelonCoroutines.Start(VerifySavePauseForSmoke());
            }
            if (smoke && smokeStarted && Run.Running &&
                Time.realtimeSinceStartupAsDouble - launchRequestedAt >= (curseUiSmoke ? 110 : trainingSmoke && jumpSmokeFloor == 4 ? 57 : 45)) Finish("smoke_timeout");
            if (TimeLimitReached())
                Finish("time_limit");
        }
        catch (Exception ex) { Fail(ex); }
    }

    private void OnCommand(Command command)
    {
        try
        {
            var spot = command.PlaybackSpot;
            string script = spot.ScriptName;
            int line = spot.LineIndex;
            if (Run.Result != null) return;
            if (!Run.Running)
            {
                if (script != "First" || launchRequestedAt < 0 || !isolated || vars == null) return;
                Run.Start();
                Run.SetPaused(true);
                audit = new CupAudit(runId);
                audit.Record(Run, "start");
                DungeonAiProductQualityRandom.Reset(CupRandom.Seed(Settings.Seed, "opening"));
                LoggerInstance.Msg($"Cup run started | {runId} | {script}:{line}");
                Persist("running");
            }
            scriptEvents++;
            if ((!explorationStarted && script == "Dungeon" && line is 335 or 336) ||
                (!clockStarted && script == "Opening3" && line is >= 1986 and <= 1989))
            {
                // Let the first screen enter its input loop while the native choice UI finishes loading.
                command.Wait = false;
            }
            if (!explorationStarted && Target?.Id != "ritual")
            {
                if (script == "Opening2" && line == 377)
                    command.Cast<Goto>().Path = new NamedString("Opening3", "PartnerChoice");
                if (script == "Opening3" && Target is { Partner: not "common" } locked)
                {
                    if (line == 1964)
                    {
                        string name = char.ToUpperInvariant(locked.Partner[0]) + locked.Partner[1..];
                        command.Cast<Gosub>().Path = new NamedString("Opening3", "PartnerChoice" + name);
                    }
                    else if (line == 2492)
                        command.Cast<Goto>().Path = new NamedString("Opening3", "MageChoiceNotYakutatazu");
                }
                ShortenOpeningAnimation(command, script, line);
            }
            string commandType = command.GetIl2CppType().Name;
            string? waitMode = commandType == nameof(Wait) ? (string?)command.Cast<Wait>().WaitMode : null;
            cutsceneCommand = IsLockedPresentation(command, commandType, waitMode) ? command.Pointer : IntPtr.Zero;
            dialogueCommand = commandType == nameof(PrintText) || waitMode?.StartsWith("i", StringComparison.Ordinal) == true
                ? command.Pointer : IntPtr.Zero;
            if (!startRedirected && script == "First" && line == 285)
            {
                RedirectCupStart(command.Cast<Goto>());
            }
            if (script == "Dungeon" && line == 151) ApplyStartingFloor(command.Cast<Gosub>());
            ApplyOpeningRules(script, line);
            ObserveCurseMenu(script, line);
            if (WeaponRulesActive) ObserveWeaponRules(script, line);
            if (Settings.RunKnife) HandleKnifeCommand(command, script, line);
            if (Settings.HarshTraining) HandleTrainingCommand(command, script, line);
            bool nativeStart = Boss != null ? (script, line) == ("Opening3", 1990) :
                Target?.Id == "ritual" ? (script, line) == ("Opening3", 1441) :
                !explorationStarted && (script, line) == ("Dungeon", 338);
            if (!clockStarted && (nativeStart || openingDeathMenuReady))
            {
                clockStarted = true;
                progressName = openingDeathInProgress ? "开局分配" :
                    Boss != null ? "选择同伴" : Target?.Id == "ritual" ? "献祭仪式" : $"第 {Settings.StartingFloor} 层";
                audit!.Record(Run, "timer_start", detail: progressName);
                LoggerInstance.Msg($"Cup timer start | {script}:{line} | partner={Read("partnerCharacter")}");
            }
            if (smoke && nativeStart) MelonCoroutines.Start(VerifyStartForSmoke());
            if (Run.Paused || cutsceneCommand != IntPtr.Zero || dialogueCommand != IntPtr.Zero) UpdateTimerPause();

            if (script == "Dungeon" && line == 338)
            {
                ObserveMap();
                if (!explorationStarted && Target is { } target && Read("partnerCharacter") != target.Partner)
                {
                    Finish("wrong_partner");
                    return;
                }
                explorationStarted = true;
            }

            if (script == "Dungeon" && line == 356)
            {
                int dungeon = Number("dungeonId"), section = Number("sectionId");
                int x = Number("dungeonX"), y = Number("mapChosenSpaceY");
                DungeonAiProductQualityRandom.Reset(CupRandom.Seed(Settings.Seed, "tile", dungeon, section, x, y));
                nativeRandom?.StartTile(dungeon, section, x, y);
                seededTiles++;
            }

            if ((script == "GameOver" && line == 0 && !openingDeathInProgress || script == "Opening" && line == 388) &&
                !Flag("xSwitchWarrior5LvUpGameOver") && !Flag("xSwitchTutorialCurseUsage"))
            {
                bool allowed = Run.RecordDeath(Settings.MaxDeaths, Settings.DeathPenaltySeconds);
                audit!.Record(Run, "death", allowed ? "within_limit" : "over_limit", Settings.DeathPenaltySeconds,
                    $"{script}:{line}");
                if (!allowed)
                {
                    Finish("defeat");
                    return;
                }
                if (TimeLimitReached())
                {
                    Finish("time_limit");
                    return;
                }
                Persist("running");
                LoggerInstance.Msg($"Cup deaths: {Run.Deaths}/{Settings.MaxDeaths}; death penalty {Run.DeathPenalty.TotalSeconds}s.");
            }
            if (script == "Title" && line == 20) { Finish("quit_to_title"); return; }
            if (Route.EarlyEndingAt(script, line) is { } earlyEnding)
            {
                Finish(earlyEnding == Target?.Id ? "clear" : "wrong_ending");
                return;
            }
            if (script == "Ending" && line == 33)
            {
                if (Flag("xSwitchEndingSkipHistory")) return;
                string ending = Read("endingType");
                if (Target?.IsEnding(ending, Read("partnerCharacter"), Flag("xSwitchWarriorHakoniwaEnd")) == true)
                    Finish("clear");
                else if (!Flag("xSwitchEndingResumeStory") ||
                         (Boss == null && Target?.ContinuesAfter(ending) != true))
                    Finish("wrong_ending");
                return;
            }

            if (Boss is { } boss)
            {
                if (script == "Battle" && line == 4)
                    bossBattle = BossGoal.BattleId(Read("battleTarget"), Read("addEnemyResourceNames"));
                if (BossGoal.IsVictoryMarker(script, line) && BossGoal.WinAt(script, line, ReadBattleTarget(),
                        Number("dungeonId"), script == "Scenario_Hero5" && Flag("tempHeroRoute5StealHit")) == boss)
                    Finish("clear");
                else
                    ObserveBossProgress(boss, script, line);
                return;
            }

            if (script == "Scenario_Hero3" && line == 2038 && !HeroChoiceAvailable() ||
                script == "Scenario_Priest3" && line == 1167 && !PriestChoiceAvailable())
            {
                Finish("required_choice_locked");
                return;
            }

            if (script == "Dungeon" && UsesMageDetour &&
                Number("dungeonId") == 9 && Number("sectionId") == 2 &&
                (line == 486 || line == 1439))
            {
                Finish("wrong_story_branch");
                return;
            }


            ObserveProgress(script, line);
        }
        catch (Exception ex) { Fail(ex); }
    }

    private void ApplyOpeningRules(string script, int line)
    {
        if (script == "Dungeon" && line == 257 && !openingRulesApplied)
        {
            openingRulesApplied = true;
            if (Settings.StartingCurse > 0 || Settings.OpeningDeath)
            {
                string curse = (Settings.StartingCurse + openingWeaponCurse).ToString(CultureInfo.InvariantCulture);
                vars!.SetVariableValue("curse", curse);
                vars.SetVariableValue("curseCount", curse);
            }
            else if (openingWeaponCurse > 0)
            {
                AddWeaponCurse("curse", openingWeaponCurse);
                AddWeaponCurse("curseCount", openingWeaponCurse);
            }
            openingWeaponCurse = 0;
            if (Settings.OpeningDeath)
            {
                vars!.SetVariableValue("playerAllocationPoint", Settings.StartingAllocationPoints.ToString(CultureInfo.InvariantCulture));
                vars.SetVariableValue("playerDeath", "1");
                vars.SetVariableValue("playerHealth", "0");
                openingDeathPending = true;
                UpdateTimerPause();
            }
        }
        else if (script == "GameOver" && line == 0 && openingDeathPending)
        {
            openingDeathPending = false;
            openingDeathInProgress = true;
            audit!.Record(Run, "opening_death", "exempt");
            LoggerInstance.Msg("Cup opening death: no death count or penalty.");
        }
        else if (script == "GameOver" && line == 214 && openingDeathInProgress && !openingDeathMenuReady)
        {
            openingDeathMenuReady = true;
            if (curseUiSmoke) MelonCoroutines.Start(VerifyCurseUiForSmoke());
            else if (openingRulesSmoke) MelonCoroutines.Start(VerifyOpeningDeathForSmoke());
        }
        else if (script == "Dungeon" && line == 338)
        {
            openingDeathInProgress = false;
            openingDeathMenuReady = false;
        }
    }

    private void RedirectCupStart(Goto command)
    {
        startRedirected = true;
        vars!.SetVariableValue("G_xSwitchFirstDeadOnPriestEventEnd", "1");
        vars.SetVariableValue("G_gameLoopCount", Target?.Id == "priestHighLoop" ? "9" : "0");
        foreach (string route in new[] { "Hero", "Mage", "Warrior" })
            vars.SetVariableValue($"G_gameClear{route}Count", "0");
        vars.SetVariableValue(ModeVariable, ModeValue);
        vars.SetVariableValue("xSwitchShowAttackModeChangeChoiceSelected", "1");
        vars.SetVariableValue("xSwitchIsTutorial", "0");
        vars.SetVariableValue("xSwitchDungeonTutorial", "0");
        if (Target?.Id == "ritual")
            command.Path = new NamedString("Opening3", "priestChoiceGisikiYes");
        else
            command.Path = new NamedString("Opening", "MeChoice1");
    }

    private static void ShortenOpeningAnimation(Command command, string script, int line)
    {
        if (script == "Func" && line != 99 || script is not ("Opening" or "Opening2" or "Opening3" or "Func"))
            return;
        switch (command.GetIl2CppType().Name)
        {
            case nameof(ModifyCharacter): command.Cast<ModifyCharacter>().Duration = 0f; break;
            case nameof(ModifyBackground): command.Cast<ModifyBackground>().Duration = 0f; break;
            case nameof(VCamera): command.Cast<VCamera>().Duration = 0f; break;
            case nameof(HideActors): command.Cast<HideActors>().Duration = 0f; break;
            case nameof(HideAllCharacters): command.Cast<HideAllCharacters>().Duration = 0f; break;
            case nameof(AnimateActor): command.Wait = false; break;
        }
    }

    private bool IsLockedPresentation(Command command, string type, string? waitMode)
    {
        // A plain @wait discards the completion token. @wait i / iN can be advanced.
        if (type == nameof(Wait))
            return command.Wait?.HasValue != true || (bool)command.Wait
                ? CupTiming.IsFixedWait(waitMode)
                : false;
        // The game's flash command waits for its tween with only a cancellation token.
        return type == nameof(Flash) && player!.Configuration.ShouldWait(command) &&
            (float)command.Cast<Flash>().Duration > 0;
    }

    private void OnCommandFinished(Command command)
    {
        IntPtr pointer = command.Pointer;
        try
        {
            var spot = command.PlaybackSpot;
            if (Run.Running && Settings.HarshTraining && spot.ScriptName == "Action" && spot.LineIndex == 1315)
            {
                AllowTrainingStealAfterHit();
                if (trainingSmoke) TraceTrainingHitForSmoke();
            }
            if (Run.Running && spot.ScriptName == "Func" && spot.LineIndex == 137)
            {
                stealAddActive = false;
                if (Settings.RunKnife && waitingForStolenItem) CaptureStolenItem();
            }
            if (pointer != cutsceneCommand && pointer != dialogueCommand) return;
            if (pointer == cutsceneCommand) cutsceneCommand = IntPtr.Zero;
            if (pointer == dialogueCommand) dialogueCommand = IntPtr.Zero;
            UpdateTimerPause();
        }
        catch (Exception ex) { Fail(ex); }
    }

    private void OnSkipChanged(bool enabled)
    {
        try { UpdateTimerPause(); }
        catch (Exception ex) { Fail(ex); }
    }

    private void UpdateTimerPause()
    {
        if (!Run.Running) return;
        bool saving = saveMenu!.Visible && saveMenu.PresentationMode == SaveLoadUIPresentationMode.Save;
        bool presenting = (cutsceneCommand != IntPtr.Zero || dialogueCommand != IntPtr.Zero) &&
            player!.Playing && !uiManager!.ScenarioProgressHeld;
        IntPtr played = presenting ? player!.PlayedCommand?.Pointer ?? IntPtr.Zero : IntPtr.Zero;
        bool cutscene = played != IntPtr.Zero && played == cutsceneCommand;
        bool advancingDialogue = played != IntPtr.Zero && played == dialogueCommand;
        bool skipping = advancingDialogue && player!.SkipActive;
        bool canAct = (cutscene || skipping) && (HasActiveControls() || (cutscene && player!.WaitingForInput));
        pauseReason = CupTiming.PauseFor(saving, cutscene, canAct, preparing: !clockStarted ||
            openingDeathPending || (openingDeathInProgress && !openingDeathMenuReady),
            input: inputPanel!.Visible, skipActive: skipping, advancingDialogue: advancingDialogue,
            roomRetry: Run.AllowsRoomRetryLoad);
        Run.SetPaused(pauseReason != CupPauseReason.None);
    }

    private static bool HasActiveControls()
    {
        foreach (var choice in UnityEngine.Object.FindObjectsByType<ChoiceHandlerButton>(FindObjectsSortMode.None))
        {
            if (ActiveButton(choice) != null) return true;
        }
        return false;
    }

    private static UnityEngine.UI.Button? ActiveButton(ChoiceHandlerButton choice) =>
        choice.isActiveAndEnabled && choice.GetComponent<UnityEngine.UI.Button>() is { } button && button.IsInteractable()
            ? button : null;

    private System.Collections.IEnumerator CaptureSetupForSmoke()
    {
        yield return new WaitForEndOfFrame();
        try
        {
            string path = SmokeScreenshot;
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var capture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            try
            {
                capture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0, false);
                capture.Apply();
                File.WriteAllBytes(path, ImageConversion.EncodeToPNG(capture));
                LoggerInstance.Msg("Cup setup: screenshot saved.");
            }
            finally { UnityEngine.Object.Destroy(capture); }
        }
        catch (Exception ex) { LoggerInstance.Warning($"Setup screenshot unavailable: {ex.Message}"); }
        smokeStarted = true;
        if (setup!.TryGetSettings(out var chosen, out string validation)) queuedSettings = chosen;
        else Fail(new InvalidOperationException(validation));
    }

    private void VerifyMapsForSmoke()
    {
        if (!smokeMapTask!.IsCompleted) return;
        smokeMapTask.GetAwaiter().GetResult();
        ObserveMap();
        if (!mapFingerprints.TryGetValue("1/1", out var hash)) throw new InvalidOperationException("Smoke map missing.");
        if (smokeFirstMap == null)
        {
            smokeFirstMap = hash;
            UnityEngine.Random.InitState(66);
            DungeonAiProductQualityRandom.Reset(99);
            lastMapVisit = "";
            mapFingerprints.Remove("1/1");
            smokeMapTask = Engine.GetService<MapService>().CreateMap(1, 1);
            return;
        }
        smokeMapTask = null;
        if (mapHookCalls < 2) throw new InvalidOperationException("Native map hook was not called.");
        if (hash != smokeFirstMap) throw new InvalidOperationException("Seed did not reproduce the smoke map.");
        if ((bossSmoke ? Boss?.Id != "priest" || Target != null : Target?.Id != "warriorGlitchBad" || Boss != null) ||
            Settings.Seed != -20261113 || Settings.LimitMinutes != 25 || Settings.BattleMode != "timing")
            throw new InvalidOperationException("The run did not use the selected setup.");
        if (Read(ModeVariable) != ModeValue) throw new InvalidOperationException("Battle mode was not set.");
        vars!.SetVariableValue(ModeVariable, ModeValue == "1" ? "0" : "1");
        if (Read(ModeVariable) != ModeValue) throw new InvalidOperationException("Battle mode write was not blocked.");
        smokeOpening = Engine.GetService<IScriptManager>().LoadScriptAsync("Opening");
    }

    private System.Collections.IEnumerator VerifyStartForSmoke()
    {
        double deadline = Time.realtimeSinceStartupAsDouble + 5;
        while (Run.Running && Time.realtimeSinceStartupAsDouble < deadline)
        {
            yield return null;
            var spot = player!.PlaybackSpot;
            if (bossSmoke)
            {
                var available = UnityEngine.Object.FindObjectsByType<ChoiceHandlerButton>(FindObjectsSortMode.None);
                if (available.Count(choice => ActiveButton(choice) != null) < 4) continue;
            }
            else if (ritualSmoke)
            {
                if (spot.ScriptName != "Opening3" || spot.LineIndex < 1441) continue;
            }
            else if (spot.ScriptName != "Dungeon" || spot.LineIndex != 338 ||
                     Read("partnerCharacter") != (jumpSmokeFloor == 6 ? "priest" : mageSmoke ? "mage" : "warrior"))
                continue;

            LoggerInstance.Msg($"Cup smoke: native start ready | {spot.ScriptName}:{spot.LineIndex}");
            if (ritualSmoke) VerifyGoalForSmoke();
            else if (bossSmoke) MelonCoroutines.Start(ChooseBossSmokePartner());
            else if (trainingSmoke && jumpSmokeFloor == 4) MelonCoroutines.Start(VerifyGiantTrainingForSmoke());
            else if (jumpSmokeFloor > 0) MelonCoroutines.Start(VerifyJumpForSmoke());
            else if (weaponOpeningSmoke) VerifyWeaponOpeningForSmoke();
            else if (mageSmoke) MelonCoroutines.Start(VerifyMageOpeningForSmoke());
            else MelonCoroutines.Start(VerifyMapSkipForSmoke());
            yield break;
        }
        Fail(new InvalidOperationException("The cup start screen did not become ready."));
    }

    private System.Collections.IEnumerator VerifyMageOpeningForSmoke()
    {
        var map = Engine.GetService<MapService>();
        double deadline = Time.realtimeSinceStartupAsDouble + 5;
        while (Run.Running && Time.realtimeSinceStartupAsDouble < deadline)
        {
            yield return null;
            var tile = map.mapChoiceButtonContainer?.GetComponentsInChildren<UnityEngine.UI.Button>()
                .FirstOrDefault(button => button.isActiveAndEnabled && button.IsInteractable());
            if (tile == null) continue;
            var items = Enumerable.Range(1, 12).Select(ItemCustomFunctions.GetItemResourceNameBySlotNumber).ToArray();
            if (!Flag("xSwitchMageOutWay") || items.Contains("ironSword") == Settings.KickOnly || !items.Contains("potion"))
            {
                Fail(new InvalidOperationException($"Mage opening initialization failed. Skill={Read("xSwitchMageOutWay")}; items={string.Join(",", items)}"));
                yield break;
            }
            LoggerInstance.Msg("Mage opening verified: native skill, iron sword and potion present on a playable floor 1 map.");
            tile.onClick.Invoke();
            deadline = Time.realtimeSinceStartupAsDouble + 10;
            while (Run.Running && Time.realtimeSinceStartupAsDouble < deadline)
            {
                yield return null;
                if (player!.PlaybackSpot.ScriptName != "Battle" || !Flag("showBattleModeChanger") ||
                    Read(ModeVariable) == "1" && !trainingSmoke) continue;
                string enemy = Read("enemy1ResourceName");
                if (!enemy.Contains("goblin", StringComparison.OrdinalIgnoreCase)) continue;
                LoggerInstance.Msg($"Mage opening verified: entered {enemy} battle; mode={Read(ModeVariable)}; break control={Read("showBattleModeChanger")}.");
                if (limitsSmoke)
                {
                    MelonCoroutines.Start(VerifyLimitsForSmoke());
                    yield break;
                }
                if (trainingSmoke)
                {
                    MelonCoroutines.Start(VerifyTrainingForSmoke());
                    yield break;
                }
                if (knifeSmoke)
                {
                    MelonCoroutines.Start(VerifyRunKnifeForSmoke());
                    yield break;
                }
                if (weaponRulesSmoke)
                {
                    MelonCoroutines.Start(VerifyWeaponRulesForSmoke());
                    yield break;
                }
                Finish("smoke_mage_opening_pass");
                yield break;
            }
            Fail(new InvalidOperationException("The first goblin battle did not expose its native break control."));
            yield break;
        }
        Fail(new InvalidOperationException("No playable first-floor tile in the Mage opening."));
    }

    private System.Collections.IEnumerator VerifyLimitsForSmoke()
    {
        var state = Engine.GetService<IStateManager>().Cast<StateManager>();
        var retry = RegisteredMelons.Single(mod => mod.GetType().FullName == "AnnihilatedRoomRetry.RoomRetryMod");
        var retryBattle = AccessTools.Method(retry.GetType(), "RetryBattle");
        double deadline = Time.realtimeSinceStartupAsDouble + 6;
        while (Run.Running && Time.realtimeSinceStartupAsDouble < deadline &&
            (state.inFlightSave || !state.GameSlotManager.Cast<ISaveSlotManager>().SaveSlotExists(RoomRetrySlot)))
            yield return null;
        if (!state.GameSlotManager.Cast<ISaveSlotManager>().SaveSlotExists(RoomRetrySlot) ||
            state.LoadGameAsync(RoomRetrySlot).GetAwaiter().GetResult() != null)
        {
            Fail(new InvalidOperationException("The F9 slot was missing or allowed without a reservation."));
            yield break;
        }
        string health = Read("playerHealth");
        vars!.SetVariableValue("playerHealth", "0");
        retryBattle.Invoke(retry, null);
        if (state.inFlightLoad || Run.RoomRetries != 0 || pauseReason == CupPauseReason.RoomRetry)
        {
            Fail(new InvalidOperationException("F9 was allowed on lethal damage before GameOver."));
            yield break;
        }
        vars.SetVariableValue("playerHealth", "3");
        if (!TryBeginRoomRetry() || !Run.Paused)
        {
            Fail(new InvalidOperationException("A reserved F9 retry did not pause the timer."));
            yield break;
        }
        CompleteRoomRetry(false);
        if (Run.RoomRetries != 0 || Run.RoomRetryPenalty != TimeSpan.Zero || pauseReason == CupPauseReason.RoomRetry)
        {
            Fail(new InvalidOperationException("A failed F9 retry did not release its timer pause."));
            yield break;
        }
        retryBattle.Invoke(retry, null);
        TimeSpan frozen = Run.Elapsed;
        double loadStarted = Time.realtimeSinceStartupAsDouble;
        deadline = Time.realtimeSinceStartupAsDouble + 10;
        bool sawPendingFrame = false;
        while (Run.Running && Run.RoomRetries == 0 && Time.realtimeSinceStartupAsDouble < deadline)
        {
            if (pauseReason != CupPauseReason.RoomRetry || !Run.Paused || Run.Elapsed != frozen)
            {
                Fail(new InvalidOperationException("The cup timer advanced while F9 was restoring the battle."));
                yield break;
            }
            sawPendingFrame = true;
            yield return null;
        }
        if (Run.RoomRetries != 1 || Run.RoomRetryPenalty != TimeSpan.FromSeconds(7) || Read("playerHealth") != health)
        {
            Fail(new InvalidOperationException($"F9 did not restore the checkpoint and consume one retry. Used={Run.RoomRetries}, HP={Read("playerHealth")}, expected={health}."));
            yield break;
        }
        double loadSeconds = Time.realtimeSinceStartupAsDouble - loadStarted;
        deadline = Time.realtimeSinceStartupAsDouble + 5;
        TimeSpan afterCharge = frozen + TimeSpan.FromSeconds(7);
        while (Run.Elapsed <= afterCharge && Run.Running && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
        if (!sawPendingFrame || Run.Elapsed <= afterCharge || pauseReason == CupPauseReason.RoomRetry)
        {
            Fail(new InvalidOperationException("The cup timer did not resume after the F9 reload."));
            yield break;
        }
        LoggerInstance.Msg($"Cup SL timer verified: frozen during {loadSeconds:F2}s of native loading, added 7s once, then resumed.");
        vars.SetVariableValue("playerHealth", "2");
        retryBattle.Invoke(retry, null);
        yield return null;
        if (state.inFlightLoad || Read("playerHealth") != "2" || Run.RoomRetries != 1 ||
            Run.RoomRetryPenalty != TimeSpan.FromSeconds(7) || pauseReason == CupPauseReason.RoomRetry)
        {
            Fail(new InvalidOperationException("F9 exceeded the configured limit."));
            yield break;
        }
        LoggerInstance.Msg("Cup limits smoke: native F9 restored HP and charged once; lethal-HP and over-quota attempts were denied.");
        var gameOver = Engine.GetService<IScriptManager>().LoadScriptAsync("GameOver");
        deadline = Time.realtimeSinceStartupAsDouble + 10;
        while (!gameOver.IsCompleted && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
        if (!gameOver.IsCompleted)
        {
            Fail(new InvalidOperationException("The native GameOver script did not load."));
            yield break;
        }
        Script defeatScript = gameOver.GetAwaiter().GetResult();
        vars.SetVariableValue("playerHealth", "0");
        player!.Stop();
        player.PreloadAndPlayAsync(defeatScript, 0, 0, null);
        deadline = Time.realtimeSinceStartupAsDouble + 10;
        while (Run.Running && Time.realtimeSinceStartupAsDouble < deadline &&
            (player.PlaybackSpot.ScriptName != "GameOver" || player.PlaybackSpot.LineIndex < 8)) yield return null;
        if (!Run.Running || Run.Deaths != 1 || Run.DeathPenalty != TimeSpan.FromSeconds(17) ||
            player.PlaybackSpot.ScriptName != "GameOver" || player.PlaybackSpot.LineIndex < 8)
        {
            Fail(new InvalidOperationException("The first GameOver did not advance with exactly one penalty."));
            yield break;
        }
        player.Stop();
        player.Play(defeatScript, 0);
        deadline = Time.realtimeSinceStartupAsDouble + 5;
        while (Run.Running && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
        if (Run.Result != "defeat" || Run.Deaths != 2 || Run.RoomRetries != 1 ||
            Run.DeathPenalty != TimeSpan.FromSeconds(34) || Run.Penalty != TimeSpan.FromSeconds(41))
        {
            Fail(new InvalidOperationException("The second GameOver did not end the run with the correct totals."));
            yield break;
        }
        LoggerInstance.Msg("Cup limits smoke passed: native F9, native GameOver script entries and cumulative penalty.");
        try
        {
            VerifyExportForSmoke("defeat", new[]
                { "start", "timer_start", "sl_begin", "sl_failed", "sl_begin", "sl_success", "death", "death", "finish" }, 41000);
        }
        catch (Exception ex) { Fail(ex); }
    }

    private System.Collections.IEnumerator ChooseBossSmokePartner()
    {
        foreach (string destination in new[] { ".PartnerChoiceHero", ".PartnerChoiceKakunin", ".PartnerChoiceEnd" })
        {
            double deadline = Time.realtimeSinceStartupAsDouble + 6;
            UnityEngine.UI.Button? selected = null;
            while (selected == null && Run.Running && Time.realtimeSinceStartupAsDouble < deadline)
            {
                yield return null;
                foreach (var choice in UnityEngine.Object.FindObjectsByType<ChoiceHandlerButton>(FindObjectsSortMode.None))
                {
                    if (ActiveButton(choice) is not { } button ||
                        choice.ChoiceState.OnSelectScript?.TrimEnd().EndsWith(destination, StringComparison.Ordinal) != true)
                        continue;
                    selected = button;
                    break;
                }
            }
            if (selected == null)
            {
                Fail(new InvalidOperationException($"The native partner choice {destination} did not appear."));
                yield break;
            }
            selected.onClick.Invoke();
        }
        double mapDeadline = Time.realtimeSinceStartupAsDouble + 12;
        while (!explorationStarted && Run.Running && Time.realtimeSinceStartupAsDouble < mapDeadline)
            yield return null;
        if (!explorationStarted)
        {
            Fail(new InvalidOperationException("Selecting the companion did not reach the native dungeon map."));
            yield break;
        }
        if (jumpSmokeFloor > 0) MelonCoroutines.Start(VerifyJumpForSmoke());
        else MelonCoroutines.Start(VerifyMapSkipForSmoke());
    }

    private System.Collections.IEnumerator VerifyMapSkipForSmoke()
    {
        var map = Engine.GetService<MapService>();
        double readyDeadline = Time.realtimeSinceStartupAsDouble + 5;
        UnityEngine.UI.Button? tile = null;
        while (tile == null && Run.Running && Time.realtimeSinceStartupAsDouble < readyDeadline)
        {
            yield return null;
            tile = map.mapChoiceButtonContainer?.GetComponentsInChildren<UnityEngine.UI.Button>()
                .FirstOrDefault(button => button.isActiveAndEnabled && button.IsInteractable());
        }
        if (tile == null)
        {
            Fail(new InvalidOperationException("No clickable dungeon tile appeared for the skip input check."));
            yield break;
        }
        var skipInput = Engine.GetService<IInputManager>().GetSampler(InputConfiguration.SkipName);
        TimeSpan before = Run.Elapsed;
        double deadline = Time.realtimeSinceStartupAsDouble + 0.4;
        bool sawSkip = false;
        while (Run.Running && Time.realtimeSinceStartupAsDouble < deadline)
        {
            skipInput.Activate(1);
            sawSkip |= player!.SkipActive;
            yield return null;
            if (Run.Paused || player.PlaybackSpot.LineIndex != 338 || !tile.IsInteractable()) break;
        }
        skipInput.Activate(0);
        if (!sawSkip || Run.Paused || !tile.IsInteractable() ||
            Run.Elapsed - before < TimeSpan.FromMilliseconds(300))
        {
            Fail(new InvalidOperationException("The native skip input did not keep the map-selection timer running."));
            yield break;
        }
        LoggerInstance.Msg($"Cup smoke: native skip input held on the real dungeon map with a clickable tile; " +
            $"timer advanced {(Run.Elapsed - before).TotalMilliseconds:F0} ms.");
        smokeMapTask = map.CreateMap(1, 1);
    }

    private void VerifyOtherHooksForSmoke()
    {
        int seed = CupRandom.Seed(Settings.Seed, "tile", 1, 1, 2, 0);
        DungeonAiProductQualityRandom.Reset(seed);
        int first = DungeonAiProductQualityRandom.Range(1, 101, "SetProbability");
        DungeonAiProductQualityRandom.Range(1, 101, "OtherTile");
        DungeonAiProductQualityRandom.Reset(seed);
        if (DungeonAiProductQualityRandom.Range(1, 101, "SetProbability") != first)
            throw new InvalidOperationException("Seed did not reproduce the probability sequence.");
        var state = Engine.GetService<IStateManager>().Cast<StateManager>();
        if (!state.QuickLoadAsync().IsCompleted || !state.RollbackAsync(null!).IsCompleted)
            throw new InvalidOperationException("Load or rollback lock did not complete synchronously.");
        var randomUnit = AccessTools.Method(typeof(ItemService), "GetRandomUnitValue");
        nativeRandom!.StartTile(1, 1, 2, 0);
        double roll = (double)randomUnit.Invoke(null, new object?[] { null })!;
        nativeRandom.StartTile(1, 1, 3, 0);
        double other = (double)randomUnit.Invoke(null, new object?[] { null })!;
        nativeRandom.StartTile(1, 1, 2, 0);
        double repeated = (double)randomUnit.Invoke(null, new object?[] { null })!;
        if (roll != repeated || roll == other) throw new InvalidOperationException("Item roll did not follow its tile seed.");
        LoggerInstance.Msg($"Cup smoke: maps={mapHookCalls}, choice_filters={filteredChoices}, item sequence, mode and rewind locks checked.");
    }

    private void VerifyGoalForSmoke()
    {
        var command = new SetCustomVariable();
        void Observe(string script, int line)
        {
            command.PlaybackSpot = new PlaybackSpot(script, line, 0);
            OnCommand(command);
        }

        vars!.SetVariableValue("endingType", "warriorGlitchBad");
        vars.SetVariableValue("xSwitchEndingResumeStory", "1");
        if (ritualSmoke)
        {
            Observe("Opening3", 1510);
        }
        else if (bossSmoke)
        {
            Observe("Ending", 33);
            vars.SetVariableValue("dungeonId", "9");
            vars.SetVariableValue("battleTarget", "priest");
            Observe("Battle", 4960);
            vars.SetVariableValue("battleTarget", "outcastRe");
            Observe("Battle", 3552);
            if (!Run.Running || filteredChoices != 0)
                throw new InvalidOperationException("Boss mode stopped before a target victory or restricted story choices.");
            vars.SetVariableValue("battleTarget", "priest");
            Observe("Battle", 3552);
        }
        else
        {
            vars.SetVariableValue("xSwitchEndingSkipHistory", "1");
            Observe("Ending", 33);
            if (!Run.Running) throw new InvalidOperationException("An ending preview stopped the timer.");
            vars.SetVariableValue("xSwitchEndingSkipHistory", "0");
            Observe("Ending", 33);
        }
        if (Run.Result != "clear") throw new InvalidOperationException("The selected goal did not stop the timer.");
        LoggerInstance.Msg($"Cup smoke passed: simulated goal events for {GoalId}.");
        if (ritualSmoke) MelonCoroutines.Start(VerifyExportButtonForSmoke());
        else VerifyExportForSmoke("clear", new[] { "start", "timer_start", "finish" }, 0);
    }

    private System.Collections.IEnumerator VerifySavePauseForSmoke()
    {
        var menu = saveMenu!;
        menu.PresentationMode = SaveLoadUIPresentationMode.Save;
        menu.Visible = true;
        yield return null;

        double deadline = Time.realtimeSinceStartupAsDouble + 5;
        while (!Run.Paused && Run.Running && Time.realtimeSinceStartupAsDouble < deadline)
            yield return null;
        TimeSpan frozen = Run.Elapsed;
        deadline = Time.realtimeSinceStartupAsDouble + 0.4;
        while (Run.Paused && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
        if (!Run.Paused || Run.Elapsed != frozen)
        {
            Fail(new InvalidOperationException("The open save screen did not freeze the cup timer."));
            yield break;
        }

        menu.Visible = false;
        deadline = Time.realtimeSinceStartupAsDouble + 5;
        while (Run.Paused && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
        if (Run.Paused || Run.Elapsed <= frozen)
        {
            Fail(new InvalidOperationException("Closing the save screen did not resume the cup timer."));
            yield break;
        }
        LoggerInstance.Msg("Cup smoke: opening the native save screen froze the timer; closing it resumed the timer.");
        MelonCoroutines.Start(VerifyInputForSmoke());
    }

    private System.Collections.IEnumerator VerifyInputForSmoke()
    {
        inputPanel!.Show("playerName", "Cup timer check", Read("playerName"), false);
        double deadline = Time.realtimeSinceStartupAsDouble + 5;
        while (pauseReason != CupPauseReason.Input && Run.Running && Time.realtimeSinceStartupAsDouble < deadline)
            yield return null;
        TimeSpan frozen = Run.Elapsed;
        yield return new WaitForSecondsRealtime(0.25f);
        if (pauseReason != CupPauseReason.Input || Run.Elapsed != frozen)
        {
            Fail(new InvalidOperationException("The native text input screen did not freeze the cup timer."));
            yield break;
        }
        inputPanel.Visible = false;
        yield return null;
        LoggerInstance.Msg("Cup smoke: native text input paused the timer.");
        MelonCoroutines.Start(VerifyCutsceneForSmoke());
    }

    private System.Collections.IEnumerator VerifyCutsceneForSmoke()
    {
        const string scriptName = "CupTimerSmoke";
        player!.Stop();
        player.SetSkipEnabled(false);
        var script = Script.FromScriptText(scriptName,
            "@clearChoice * wait:false\n@hideUI MapUI time:0\n" +
            "@wait 0.7\n@wait i0.7\n@flash color:#220000 time:0.6 wait:true\n" +
            "@choice Continue button:Buttons/ChoiceButton_Me1 handler:Image goto:.Done time:0 wait:false\n" +
            "@wait 1.5\n@stop\n# Done\n@stop", (string?)null);
        var playback = player.PreloadAndPlayAsync(script);
        while (!playback.IsCompleted && Run.Running) yield return null;
        playback.GetAwaiter().GetResult();
        var firstSamples = new Dictionary<int, TimeSpan>();
        var verified = new HashSet<int>();
        double deadline = Time.realtimeSinceStartupAsDouble + 8;
        while (verified.Count < 4 && Run.Running && Time.realtimeSinceStartupAsDouble < deadline)
        {
            yield return null;
            var spot = player.PlaybackSpot;
            if (spot.ScriptName != scriptName || spot.LineIndex is not (2 or 3 or 4 or 6)) continue;
            int line = spot.LineIndex;
            TimeSpan elapsed = Run.Elapsed;
            bool shouldPause = line is 2 or 4;
            if (shouldPause ? pauseReason != CupPauseReason.Cutscene : Run.Paused) continue;
            if (firstSamples.TryAdd(line, elapsed)) continue;
            if (shouldPause ? elapsed == firstSamples[line] : elapsed > firstSamples[line])
                verified.Add(line);
        }
        if (verified.Count != 4)
        {
            Fail(new InvalidOperationException($"Cutscene timer checks did not pass (lines: {string.Join(", ", verified)})."));
            yield break;
        }
        LoggerInstance.Msg("Cup smoke: fixed wait and blocking flash paused; skippable wait and visible choice kept counting.");
        MelonCoroutines.Start(VerifySkipForSmoke());
    }

    private System.Collections.IEnumerator VerifySkipForSmoke()
    {
        player!.Stop();
        var script = Script.FromScriptText("CupSkipSmoke", "@clearChoice * wait:false\n@wait i0.6\n" +
            string.Concat(Enumerable.Repeat("@wait i0.1\n", 24)) + "@stop", (string?)null);
        var playback = player.PreloadAndPlayAsync(script);
        while (!playback.IsCompleted && Run.Running) yield return null;
        playback.GetAwaiter().GetResult();
        double deadline = Time.realtimeSinceStartupAsDouble + 4;
        while ((player.PlaybackSpot.LineIndex != 1 || HasActiveControls()) &&
               Run.Running && Time.realtimeSinceStartupAsDouble < deadline)
            yield return null;
        player.SkipMode = PlayerSkipMode.Everything;
        var skipInput = Engine.GetService<IInputManager>().GetSampler(InputConfiguration.SkipName);
        deadline = Time.realtimeSinceStartupAsDouble + 2;
        bool sawDialogueSkip = false;
        int lineBeforeSkip = player.PlaybackSpot.LineIndex;
        while (Run.Running && player.Playing && Time.realtimeSinceStartupAsDouble < deadline)
        {
            skipInput.Activate(1);
            sawDialogueSkip |= pauseReason == CupPauseReason.Skip;
            if (sawDialogueSkip && player.PlaybackSpot.LineIndex > lineBeforeSkip) break;
            yield return null;
        }
        if (!sawDialogueSkip || player.PlaybackSpot.LineIndex <= lineBeforeSkip)
        {
            Fail(new InvalidOperationException($"Game fast-forward did not pause the timer " +
                $"(skip={player.SkipActive}, playing={player.Playing}, choices={HasActiveControls()}, " +
                $"line={player.PlaybackSpot.LineIndex}, reason={pauseReason})."));
            yield break;
        }
        TimeSpan frozen = Run.Elapsed;
        skipInput.Activate(0);
        player.SetSkipEnabled(false);
        player.Stop();
        yield return null;
        if (Run.Paused || Run.Elapsed <= frozen)
        {
            Fail(new InvalidOperationException("The timer did not resume after fast-forward."));
            yield break;
        }
        LoggerInstance.Msg("Cup smoke: game fast-forward paused and resumed the timer.");
        try { VerifyGoalForSmoke(); }
        catch (Exception ex) { Fail(ex); }
    }

    private bool HeroChoiceAvailable() => Target?.HeroChoice switch
    {
        2034 => Number("yValHeroDistrustPoint") <= 4,
        2037 => Number("yValHeroDistrustPoint") >= 2,
        _ => true
    };

    private bool PriestChoiceAvailable()
    {
        bool returned = Flag("xSwitchPriestLowSanEnded");
        int dependence = Number("PriestYoriPoint");
        return Target?.PriestChoice(returned) switch
        {
            1164 => dependence >= 2 && !Flag("xSwitchPriestLowSanEndedAfterMesorasiChoice"),
            1165 => dependence <= 4 || returned || Flag("xSwitchPriestLowSanEndedAfterMesorasiChoice"),
            1166 => dependence <= 4 && !Flag("xSwitchPriestLowSanEndedAfterTakeHandChoice"),
            _ => true
        };
    }

    internal void RecordMapHook() => mapHookCalls++;

    internal bool UsesMageDetour => Target?.Id is "mageNotMaryokuSuki" or "mageNotMaryokuIssyo";

    internal bool KeepMapChoice(MapSpaceType type, string? option)
    {
        if (!UsesMageDetour ||
            Number("dungeonId") != 9 || Number("sectionId") != 2) return true;
        if (type != MapSpaceType.boss && option != "902") return true;
        routeNoticeUntil = Time.realtimeSinceStartupAsDouble + 4;
        return false;
    }

    private void ObserveMap()
    {
        var mapService = Engine.GetService<MapService>();
        var map = mapService.map;
        if (map == null) return;
        string key = $"{map.dungeonId}/{map.sectionId}";
        if (lastMapVisit == key) return;
        lastMapVisit = key;
        visitedMaps++;
        var cells = new System.Text.StringBuilder();
        for (int x = 0; x < map.spaces.Count; x++)
        {
            var row = map.spaces[x];
            cells.Append('[');
            for (int y = 0; y < row.Count; y++)
            {
                var space = row[y];
                cells.Append((int)space.spaceType).Append(':').Append(space.option).Append(':').Append(space.enemyParams).Append(':');
                if (space.possibleY != null)
                    for (int i = 0; i < space.possibleY.Count; i++) cells.Append(space.possibleY[i]).Append(',');
                cells.Append('|');
            }
            cells.Append(']');
        }
        string hash = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(cells.ToString())));
        if (mapFingerprints.TryAdd(key, hash)) LoggerInstance.Msg($"Cup map {key} {hash}");
    }

    private void ObserveProgress(string script, int line)
    {
        if (!explorationStarted || Target == null) return;
        if (script == "Dungeon" && line == 338) ObserveFloorProgress(6);
        int boss = (script, line) switch
        {
            ("Scenario_Hero1", 571) or ("Scenario_Mage1", 837) or ("Scenario_Warrior1", 1306) or ("Scenario_Priest1", 689) => 1,
            ("Scenario_Hero2", 1344) or ("Scenario_Mage2", 2328) or ("Scenario_Warrior2", 3370) or ("Scenario_Priest2", 1863) => 2,
            ("Scenario_Hero3", 2362) or ("Scenario_Mage3", 970) or ("Scenario_Warrior3", 1591) or ("Scenario_Priest3", 1755) => 3,
            ("Scenario_Hero4", 1442) or ("Scenario_Mage4", 1330) or ("Scenario_Warrior4", 1362) or ("Scenario_Priest4", 2245) => 4,
            _ => 0
        };
        if (boss > 0) Split(boss * 100, $"已击败第 {boss} 层首领");
        if (script == "Battle" && line == 6555 && Number("dungeonId") >= 5 &&
            Number("xPriestFinalPhase") is >= 1 and <= 3)
        {
            int phase = Number("xPriestFinalPhase");
            if (Target.Id is not ("priestNod" or "priestNodAfterLowSan" or "mageNotMaryokuSpecialWin") &&
                !(Target.Id.StartsWith("mageNotMaryoku", StringComparison.Ordinal) && Flag("xSwitchRunAllowed")))
                Split(420 + phase, $"终战 · 完成第 {phase} 阶段");
        }
        if (script == "Scenario_Warrior5" && line is 1310 or 1550)
            Split(line == 1310 ? 431 : 432, line == 1310 ? "终战 · 第一场决斗胜利" : "终战 · 第二场决斗胜利");
        if (script == "Scenario_Priest5" && line == 301)
            Split(435, "僧侣终战胜利");
        if (script == "Scenario_Priest5" && line == 411)
            Split(520, "魔王终战胜利");
        if (script == "Scenario_Hero5" && line == 1803)
            Split(431, "勇者终击成功");
        if (script == "Scenario_Mage5" && line is 629 or 2535 or 2969)
            Split(431, "魔法师终战胜利");
    }

    private void ObserveBossProgress(BossGoal boss, string script, int line)
    {
        if (!explorationStarted) return;
        if (script == "Dungeon" && line == 338)
        {
            ObserveFloorProgress(boss.Floor);
            return;
        }

        if (script != "Battle" || line is not (4 or 6555 or 8210)) return;
        if (BossGoal.ForBattle(ReadBattleTarget(), Number("dungeonId")) != boss) return;
        int rank = (boss.Floor - 1) * 100 + 90;
        if (line == 4)
            Split(rank, "已进入目标 Boss 战");
        else
        {
            int phase = Number(line == 6555 ? "xPriestFinalPhase" : "xGlausFinalPhase");
            if (phase is >= 1 and <= 3 && (boss.Id != "priest" || !Flag("xSwitchRunAllowed")))
                Split(rank + phase, $"目标 Boss · 完成第 {phase} 阶段");
        }
    }

    private string ReadBattleTarget()
    {
        string target = Read("battleTarget");
        return target.Length > 0 ? target : bossBattle;
    }

    private void ObserveFloorProgress(int maxFloor)
    {
        int dungeon = Number("dungeonId"), section = Number("sectionId");
        int floor = dungeon == 10 ? 6 : Math.Min(dungeon, 5);
        if (floor > 0 && floor <= maxFloor && section > 0)
            Split((floor - 1) * 100 + section, $"第 {floor} 层 · 第 {section} 区");
    }

    private void Split(int rank, string label)
    {
        if (rank <= Run.Progress || !Run.Running) return;
        Run.Reach(rank);
        progressName = label;
        splits.Add(new { Rank = rank, Name = label, Milliseconds = (long)Run.Elapsed.TotalMilliseconds });
        audit!.Record(Run, "checkpoint", detail: label);
        Persist("running");
    }

    private bool TimeLimitReached() => Run.Running && Settings.LimitMinutes > 0 &&
        Run.Elapsed.TotalMinutes >= Settings.LimitMinutes;

    private void Finish(string result)
    {
        if (!Run.Running) return;
        Run.Finish(result);
        audit!.Record(Run, "finish", result, detail: result == "clear" ? GoalId : progressName);
        DungeonAiProductQualityRandom.Disable();
        Persist(result);
        LoggerInstance.Msg($"Cup result: {result} | {Run.Elapsed} | {progressName}");
    }

    private void Fail(Exception ex)
    {
        error = ex.Message;
        RestoreTitleInteraction();
        LoggerInstance.Error(ex.ToString());
        try { Finish("mod_error"); }
        catch (Exception writeError) { LoggerInstance.Error(writeError.ToString()); }
    }

    private void Persist(string state)
    {
        string dir = Path.Combine(root, smoke ? "verification" : "results");
        string path = Path.Combine(dir, runId + ".json");
        var data = JsonSerializer.SerializeToUtf8Bytes(new
        {
            Schema = 1, Mod = Version, GameBuild, MatchHash = matchHash, RunId = runId, audit!.RunCode,
            Settings, Status = state, ElapsedMilliseconds = (long)Run.Elapsed.TotalMilliseconds,
            RoomRetriesUsed = Run.RoomRetries, Deaths = Run.Deaths,
            RoomRetryPenaltyMilliseconds = (long)Run.RoomRetryPenalty.TotalMilliseconds,
            DeathPenaltyMilliseconds = (long)Run.DeathPenalty.TotalMilliseconds,
            PenaltyTotalMilliseconds = (long)Run.Penalty.TotalMilliseconds,
            Progress = Run.Progress, ProgressName = progressName, Splits = splits,
            Ending = Read("endingType"), ActualPartner = Read("partnerCharacter"),
            CompletedGoal = state == "clear" ? GoalId : null, MapFingerprints = mapFingerprints,
            Events = audit.Events, ObservedScriptCommands = scriptEvents, SeededTiles = seededTiles,
            VisitedMaps = visitedMaps, MapHookCalls = mapHookCalls, FilteredChoices = filteredChoices
        }, Json);
        if (state != "running") finalResult = data;
        WriteResult(path, data);
    }

    public override void OnApplicationQuit()
    {
        if (Active == this) Finish("quit");
    }

    public override void OnGUI()
    {
        if (Active != this && error.Length == 0) return;
        try
        {
            if (error.Length == 0 && !Run.Running && Run.Result == null)
            {
                if (launchRequestedAt >= 0)
                {
                    GUI.Box(LeftCenteredRect(260, 44), "Loading Cup...");
                    return;
                }
                if (setup != null && titleMenu != null && titleMenu.Visible)
                {
                    if (setupPanel == null)
                    {
                        LoggerInstance.Msg("Cup setup: creating controls.");
                        setupPanel = new CupSetupPanel();
                        LoggerInstance.Msg("Cup setup: controls created.");
                    }
                    setupPanel.Draw(setup, setupNotice, chosen => queuedSettings = chosen, () => Application.Quit());
                    if (!titleInteractionDisabled && Event.current.type == EventType.Repaint)
                    {
                        previousTitleInteraction = titleMenu.Interactable;
                        titleMenu.Interactable = false;
                        titleInteractionDisabled = true;
                        LoggerInstance.Msg("Cup setup: first frame drawn.");
                    }
                }
                return;
            }
            hudStyle ??= new GUIStyle()
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = Math.Clamp(Screen.height / 52, 15, 25),
                font = Font.CreateDynamicFontFromOSFont("Microsoft YaHei", 18)
            };
            hudStyle.normal.textColor = Color.white;
            float width = Math.Min(Screen.width - 24, 540);
            if (error.Length > 0)
            {
                if (finalResult == null)
                {
                    GUI.Box(LeftCenteredRect(width, 74), "Cup error\n" + error);
                    return;
                }
            }
            string status = Run.Result switch
            {
                "clear" => Boss != null ? "已击败目标 Boss" : "已达成目标结局", "defeat" => "死亡超出额度", "time_limit" => "时间到",
                "required_choice_locked" => "未达分支条件", "wrong_story_branch" or "wrong_ending" => Boss != null ? "剧情结束 · 未击败目标" : "偏离目标结局",
                "quit_to_title" => "已退出", "mod_error" => "Mod 异常 · 已停表",
                null when Run.Paused => pauseReason switch
                {
                    CupPauseReason.Save => "存档暂停", CupPauseReason.Input => "输入暂停",
                    CupPauseReason.Skip => "快进暂停", CupPauseReason.Preparing => "准备开赛",
                    CupPauseReason.RoomRetry => "SL 读档暂停", _ => "演出暂停"
                },
                null when Run.Running => progressName,
                null => "从标题选择新游戏", _ => Run.Result ?? ""
            };
            if (Run.Running && !Run.Paused && Time.realtimeSinceStartupAsDouble < routeNoticeUntil) status = "该格通往其他结局，请走剧情岔路";
            string clock = $"{(int)Run.Elapsed.TotalHours:00}:{Run.Elapsed.Minutes:00}:{Run.Elapsed.Seconds:00}.{Run.Elapsed.Milliseconds / 100}";
            var hud = GetHudRect();
            GUI.Box(hud, "");
            GUI.Label(hud,
                $"  {Settings.EventId}   {clock}   {status}\n  {GoalName}   {(ModeValue == "1" ? "概率" : "时机")}\n" +
                $"  SL {Run.RoomRetries}/{Settings.MaxRoomRetries}   死亡 {Run.Deaths}/{Settings.MaxDeaths}   罚时 +{Run.Penalty.TotalSeconds:0}s\n" +
                $"  本局编号 {audit?.RunCode}", hudStyle);
            if (finalResult != null) DrawResultExport(hud);
            if (Run.Running && Time.realtimeSinceStartupAsDouble < weaponCurseNoticeUntil)
            {
                var notice = new Rect(hud.x, hud.yMax, hud.width, 28);
                GUI.Box(notice, "");
                GUI.Label(notice, $"  {weaponCurseNotice}", hudStyle);
            }
        }
        catch (Exception ex) { Fail(ex); }
    }

    private Rect GetHudRect() => LeftCenteredRect(540, Math.Max(112, hudStyle!.fontSize * 5));

    private static Rect LeftCenteredRect(float width, float height) =>
        new(12, Math.Max(0, (Screen.height - height) / 2), Math.Min(Screen.width - 24, width), height);
}
