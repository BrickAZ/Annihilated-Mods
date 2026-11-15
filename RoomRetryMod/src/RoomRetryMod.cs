using Il2CppNaninovel;
using Il2CppNaninovel.Commands;
using MelonLoader;
using UnityEngine;
using UnityEngine.InputSystem;

[assembly: MelonInfo(typeof(AnnihilatedRoomRetry.RoomRetryMod), "Annihilated Room Retry", "1.0.1", "Brick")]

namespace AnnihilatedRoomRetry;

public sealed class RoomRetryMod : MelonMod
{
    private const string Slot = "AnnihilatedRoomRetryCheckpoint";
    private const string BattleScript = "Battle";

    private readonly RetryCheckpoint checkpoint = new();
    private CustomScriptPlayer player;
    private StateManager state;
    private Il2CppSystem.Action<Command> captureBattle;
    private Il2CppSystem.Action<GameSaveLoadArgs> onLoad;
    private Il2CppSystem.Action onReset;
    private Il2CppSystem.Action onRollback;
    private UniTask.Awaiter save;
    private UniTask<GameStateMap>.Awaiter load;
    private bool enabled;
    private bool cupMode;
    private Func<bool> canCaptureCupRetry;
    private Func<bool> beginCupRetry;
    private Action<bool> completeCupRetry;

    public override void OnInitializeMelon()
    {
        cupMode = Environment.GetCommandLineArgs().Contains("--annihilated-cup");
        enabled = true;
    }

    public override void OnUpdate()
    {
        if (!enabled)
            return;

        try
        {
            if (!Engine.Initialized)
                return;

            if (player is null && !Attach())
                return;

            if (save is not null)
                PollCapture();
            if (load is not null)
                PollRetry();

            if (!Application.isFocused || Keyboard.current?.f9Key.wasPressedThisFrame != true)
                return;

            RetryBattle();
        }
        catch (Exception error)
        {
            LoggerInstance.Error(error.ToString());
            if (checkpoint.Loading) completeCupRetry?.Invoke(false);
            enabled = false;
            CompleteCapture(false);
            checkpoint.Invalidate();
        }
    }

    private bool Attach()
    {
        var scriptPlayer = Engine.GetService<IScriptPlayer>()?.TryCast<CustomScriptPlayer>();
        var stateManager = Engine.GetService<IStateManager>()?.TryCast<StateManager>();
        if (scriptPlayer is null || stateManager is null)
            return false;
        if (cupMode && !BindCupRules())
        {
            LoggerInstance.Warning("Cup mode requires a Cup mod with F9 quota support. F9 is disabled.");
            enabled = false;
            return false;
        }

        captureBattle = (Il2CppSystem.Action<Command>)CaptureBattle;
        onLoad = (Il2CppSystem.Action<GameSaveLoadArgs>)HandleGameLoad;
        onReset = (Il2CppSystem.Action)InvalidateUnlessRetrying;
        onRollback = (Il2CppSystem.Action)InvalidateUnlessRetrying;

        scriptPlayer.OnCommandExecutionStart += captureBattle;
        stateManager.OnGameLoadStarted += onLoad;
        stateManager.OnResetStarted += onReset;
        stateManager.OnRollbackStarted += onRollback;
        player = scriptPlayer;
        state = stateManager;
        LoggerInstance.Msg("Battle entry checkpoint registered.");
        return true;
    }

    private bool BindCupRules()
    {
        var type = RegisteredMelons.Select(mod => mod.GetType())
            .FirstOrDefault(candidate => candidate.FullName == "CupCompetition.CupMod");
        if (type is null) return false;
        canCaptureCupRetry = type.GetMethod("CanCaptureRoomRetry")?.CreateDelegate<Func<bool>>();
        beginCupRetry = type.GetMethod("TryBeginRoomRetry")?.CreateDelegate<Func<bool>>();
        completeCupRetry = type.GetMethod("CompleteRoomRetry")?.CreateDelegate<Action<bool>>();
        return canCaptureCupRetry is not null && beginCupRetry is not null && completeCupRetry is not null;
    }

    private static bool IsBattleEntry(Command command)
    {
        var spot = command.PlaybackSpot;
        return spot.ScriptName == BattleScript && spot.LineIndex == 0 &&
            command.TryCast<OnNovelTopForDontReleaseNovel>() is not null;
    }

    private void CaptureBattle(Command command)
    {
        try
        {
            if (!enabled || !IsBattleEntry(command) || canCaptureCupRetry?.Invoke() == false || !checkpoint.BeginCapture())
                return;

            // Serialize the complete snapshot here, before the first battle command runs.
            // Let the game write the frozen snapshot asynchronously and enable F9 when it finishes.
            var snapshot = new GameStateMap
            {
                SaveDateTime = Il2CppSystem.DateTime.Now,
                BuildVariant = SaveBuildVariant.Product
            };
            state.SaveAllServicesToState<IStatefulService<GameStateMap>, GameStateMap>(snapshot);
            state.PerformOnGameSerializeTasks(snapshot);
            var spot = snapshot.PlaybackSpot;
            if (spot is null || spot.ScriptName != BattleScript || spot.LineIndex != 0)
                throw new InvalidOperationException("The game did not capture the battle entry.");
            snapshot.ForceSerialize();
            save = state.GameSlotManager.SaveAsync(Slot, snapshot).GetAwaiter();
        }
        catch (Exception error)
        {
            CompleteCapture(false);
            LoggerInstance.Warning($"Could not save battle entry at {command.PlaybackSpot.ScriptName}: {error}");
        }
    }

    private void PollCapture()
    {
        try
        {
            if (!save.IsCompleted)
                return;

            save.GetResult();
            CompleteCapture(true);
            LoggerInstance.Msg("Battle entry saved (Battle:0). F9 is ready.");
        }
        catch (Exception error)
        {
            CompleteCapture(false);
            LoggerInstance.Warning($"Battle checkpoint could not be written: {error}");
        }
    }

    private void CompleteCapture(bool success)
    {
        save = null;
        checkpoint.FinishCapture(success);
    }

    private bool InBattle()
    {
        if (player.PlayedScript?.Name == BattleScript)
            return true;
        foreach (var spot in player.GosubReturnSpots.ToArray())
            if (spot.ScriptName == BattleScript)
                return true;
        return false;
    }

    private void RetryBattle()
    {
        if (state.inFlightSave || state.inFlightLoad || state.RollbackInProgress ||
            !checkpoint.BeginRetry(InBattle()))
            return;

        if (beginCupRetry?.Invoke() == false)
        {
            checkpoint.FinishRetry();
            LoggerInstance.Msg("F9: no cup retry is available.");
            return;
        }

        try
        {
            load = state.LoadGameAsync(Slot).GetAwaiter();
            LoggerInstance.Msg("F9: restoring the current battle entry.");
        }
        catch (Exception error)
        {
            completeCupRetry?.Invoke(false);
            checkpoint.FinishRetry();
            LoggerInstance.Warning($"Could not start the battle retry: {error}");
        }
    }

    private void PollRetry()
    {
        bool success = false;
        try
        {
            if (!load.IsCompleted)
                return;
            var result = load.GetResult();
            success = result is not null;
            LoggerInstance.Msg(result is null ? "The game did not load the battle checkpoint." : "Battle entry restored.");
        }
        catch (Exception error)
        {
            LoggerInstance.Warning($"Battle checkpoint could not be loaded: {error}");
        }
        load = null;
        checkpoint.FinishRetry();
        completeCupRetry?.Invoke(success);
    }

    private void HandleGameLoad(GameSaveLoadArgs args)
    {
        if (!checkpoint.Loading || args.SlotId != Slot)
            checkpoint.Invalidate();
    }

    private void InvalidateUnlessRetrying()
    {
        if (!checkpoint.Loading)
            checkpoint.Invalidate();
    }

    public override void OnDeinitializeMelon()
    {
        enabled = false;
        if (player is null || state is null || !Engine.Initialized)
            return;

        player.OnCommandExecutionStart -= captureBattle;
        state.OnGameLoadStarted -= onLoad;
        state.OnResetStarted -= onReset;
        state.OnRollbackStarted -= onRollback;
        CompleteCapture(false);
    }
}
