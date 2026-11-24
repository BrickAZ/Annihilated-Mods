using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using HarmonyLib;
using Il2Cpp;
using Il2CppNaninovel;
using Il2CppNaninovel.Commands;
using MelonLoader;

[assembly: MelonInfo(typeof(PlayableRivals.RivalMod), "Annihilated Playable Rivals", "0.1.16", "Brick")]

namespace PlayableRivals;

public sealed partial class RivalMod : MelonMod
{
    private const string GameHash = "AB27FAA27ABF2AE18431C1586662C0B4720F7F59DACEAF6399B6B5BB9F2C8E39";
    private const string RoleKey = "rivalsSelected";
    private const string FloorKey = "rivalsUnlockedFloor";
    private const string ScriptName = "PlayableRivals";
    private static RivalMod? active;
    private CustomVariableManager? vars;
    private CustomScriptPlayer? player;
    private StateManager? stateManager;
    private Il2CppSystem.Action<Command>? commandStart;
    private Il2CppSystem.Action<Command>? commandFinish;
    private Il2CppSystem.Action<GameSaveLoadArgs>? gameLoadFinish;
    private Script? scriptAsset;
    private bool reportedFailure;
    private bool refreshLoadedHand;

    private Rival Role => Read(RoleKey) == Read("partnerCharacter")
        ? RivalRules.FromPartner(Read(RoleKey)) : Rival.None;

    private Rival BattleRival => Read("timing") == "battle" ? Role : Rival.None;
    private bool IsRivalBattle => BattleRival != Rival.None;

    private string Read(string key) => vars?.GetVariableValue(key) ?? "";
    private bool Flag(string key) => Read(key) is "1" or "true" or "True";
    private int Number(string key) => int.TryParse(Read(key), NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) ? n :
        double.TryParse(Read(key), NumberStyles.Float,CultureInfo.InvariantCulture,out double v) && double.IsFinite(v) ? (int)v : 0;

    private void Write(string name, int value) => Write(name, value.ToString(CultureInfo.InvariantCulture));
    private void Write(string name, string value) => vars!.SetVariableValue(name, value);

    public override void OnInitializeMelon()
    {
        using var game = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "GameAssembly.dll"));
        using var sha256 = SHA256.Create();
        if (Convert.ToHexString(sha256.ComputeHash(game)) != GameHash)
        {
            LoggerInstance.Warning("Playable Rivals requires Steam Build 26845264.");
            return;
        }

        try
        {
            active = this;
            InstallCardHooks();
            InstallPresentationHook();
            InstallArtHooks();
            InstallLearnedSkillHooks();
            InstallSmokeHooks();
            LoggerInstance.Msg("Playable Rivals initialized for Steam Build 26845264.");
        }
        catch (Exception ex)
        {
            active = null;
            HarmonyInstance.UnpatchSelf();
            LoggerInstance.Error(ex.ToString());
        }
    }

    private void Patch(Type owner, string method, string hook, bool postfix = false, Type[]? types = null)
    {
        MethodBase target = AccessTools.Method(owner, method, types) ?? throw new MissingMethodException(owner.FullName, method);
        var patch = new HarmonyMethod(typeof(RivalMod), hook);
        HarmonyInstance.Patch(target, prefix: postfix ? null : patch, postfix: postfix ? patch : null);
    }

    public override void OnUpdate()
    {
        if (active != this || !Engine.Initialized) return;
        if (player == null)
        {
            vars = Engine.GetService<ICustomVariableManager>().Cast<CustomVariableManager>();
            player = Engine.GetService<IScriptPlayer>().Cast<CustomScriptPlayer>();
            commandStart = (Il2CppSystem.Action<Command>)BeforeCommand;
            commandFinish = (Il2CppSystem.Action<Command>)AfterCommand;
            player.add_OnCommandExecutionStart(commandStart);
            player.add_OnCommandExecutionFinish(commandFinish);
            stateManager = Engine.GetService<IStateManager>()?.TryCast<StateManager>();
            if (stateManager != null)
            {
                gameLoadFinish = (Il2CppSystem.Action<GameSaveLoadArgs>)OnGameLoaded;
                stateManager.OnGameLoadFinished += gameLoadFinish;
            }
            if (smokeRoute.Length > 0) LoggerInstance.Msg("RIVALS SMOKE ENGINE READY");
        }
        if (refreshLoadedHand && stateManager?.inFlightLoad != true)
        {
            refreshLoadedHand = false;
            // Saved choice objects retain their hidden/noninteractive entrance state.
            // Re-enter the native hand builder only at the idle hand, never during an action.
            if (IsRivalBattle && player.PlaybackSpot.ScriptName == "Battle" && player.PlaybackSpot.LineIndex == 2140)
                player.Play(player.PlayedScript, 1818);
        }
        UpdateArt();
        UpdateBattleFeedback();
        UpdateSmoke();
    }

    private void OnGameLoaded(GameSaveLoadArgs args)
    {
        ClearBattleFeedback();
        RestoreOutcastMilestone();
        if (BattleRival == Rival.Villainess && !Flag("battleWarriorDestroyModeCommitted"))
            ClearWarriorCombatState();
        RepairFinaleArmor();
        refreshLoadedHand = true;
    }

    public override void OnDeinitializeMelon()
    {
        if (!Engine.Initialized) return;
        if (stateManager != null && gameLoadFinish != null)
            stateManager.OnGameLoadFinished -= gameLoadFinish;
        if (player != null && commandStart != null && commandFinish != null)
        {
            player.remove_OnCommandExecutionStart(commandStart);
            player.remove_OnCommandExecutionFinish(commandFinish);
        }
    }

    private void BeforeCommand(Command command)
    {
        try
        {
            var spot = command.PlaybackSpot;
            if (smokeRobbery && spot.ScriptName == "Action" && spot.LineIndex == 1699 &&
                Number("rivalsCommittedCard") == (int)Card.Steal)
                smokePreparationHealed = true;
            if (spot.ScriptName == "Opening3")
            {
                if (spot.LineIndex == 2048)
                {
                    var route = Read("partnerCharacter");
                    if (RivalRules.FromPartner(route) != Rival.None) Write(RoleKey, route);
                }
                ObserveOpening(spot.LineIndex);
            }
            if (spot.ScriptName == "Battle" && spot.LineIndex == 1903)
            {
                RouteBattleHand(command.Cast<Goto>());
                return;
            }
            if (spot.ScriptName == "Battle" && spot.LineIndex == 3526)
            {
                RouteDollTurnEnd(command.Cast<Gosub>());
                return;
            }
            if (spot.ScriptName == ScriptName && spot.LineIndex == 1)
            {
                Write("rivalsCardAllowed", CanCommitRivalCard() ? 1 : 0);
                return;
            }
            if (spot.ScriptName == "Battle" && spot.LineIndex == 2286 &&
                TryCard(Number("selectCommand"), out _) && !CanCommitRivalCard())
            {
                // Saved 0.1.0 choice buttons can still jump straight to PlayerAttack.
                OverrideCondition(command, "true");
                return;
            }
            if (Role == Rival.None) return;
            if (spot.ScriptName == "StatusFunc" && Read("tempCharacter") == "partner")
                ReplaceStats(spot.LineIndex);
            if (spot.ScriptName is "Battle" or "Action" or ScriptName)
                HandleBattleCommand(command, spot.ScriptName, spot.LineIndex);
        }
        catch (Exception ex) { ReportCommandError(command, ex); }
    }

    private void AfterCommand(Command command)
    {
        try
        {
            var spot = command.PlaybackSpot;
            RestoreCommand(command);
            if (spot.ScriptName == "Battle" && spot.LineIndex == 2074 && BattleRival == Rival.Villainess)
                ClearWarriorCombatState();
            if (Role != Rival.None && spot.ScriptName == "Action" && spot.LineIndex == 958)
                OnAttackContact();
        }
        catch (Exception ex) { ReportCommandError(command, ex); }
    }

    private void ReportCommandError(Command command, Exception ex)
    {
        if (reportedFailure) return;
        reportedFailure = true;
        LoggerInstance.Error($"Playable Rivals command {command.PlaybackSpot.ScriptName}:{command.PlaybackSpot.LineIndex}: {ex}");
    }

    private Stats CurrentStats() => new(Number("partnerMaxHealth"),
        Number("partnerStr") + Number("partnerStateStrUp"), Number("partnerDex") + Number("partnerStateDexUp"),
        Number("partnerAgi") + Number("partnerStateAgiUp"), NoMagic ? 0 : Number("partnerMag") + Number("partnerStateMagUp"));

    private void ReplaceStats(int line)
    {
        var stats = RivalRules.GetStats(Role, Number("partnerLv"));
        if (line == 115)
        {
            var stat = Read("tempStatus") switch
            {
                "Str" => stats.Str, "Dex" => stats.Dex, "Agi" => stats.Agi,
                "Mag" => NoMagic ? 0 : stats.Mag, _ => 0
            };
            Write("yValStatusCalcValue", stat);
        }
        else if (line == 136)
        {
            // Keep the native HP delta and death rules when regenerating the panel.
            Write("tempHpBase", stats.Hp);
            Write("tempHpRate", 0);
        }
    }

    private void RegisterScript()
    {
        var loader = Engine.GetService<IScriptManager>().Cast<ScriptManager>().scriptLoader;
        if (loader.GetLoadedOrNull(ScriptName) != null) return;
        scriptAsset ??= Script.FromScriptText(ScriptName, BattleScript(), (string?)null);
        var source = loader.ProvisionSources[0];
        loader.LoadedResources.Add(new ResourceLoader<Script>.LoadedResource(
            new Resource<Script>(source.BuildFullPath(ScriptName), scriptAsset), source));
        if (loader.GetLoadedOrNull(ScriptName) == null)
            throw new InvalidOperationException("The playable rivals battle script could not be registered.");
    }
}
