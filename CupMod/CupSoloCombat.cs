using Il2Cpp;
using Il2CppNaninovel;
using Il2CppNaninovel.Commands;
using UnityEngine;

namespace CupCompetition;

public sealed partial class CupMod
{
    private const string SoloScriptName = "AnnihilatedCupSoloCombat";
    private string soloExcluded = "";
    private string soloSavedFormation = "";
    private string soloSavedStatus = "";
    private string soloSavedOrder = "";
    private bool soloNativeDreakOffered;
    private readonly List<GameObject> soloHiddenStatusPanels = new();
    private readonly Dictionary<IntPtr, Action> soloCommandRestore = new();
    private Script? soloScript;

    private string SoloActor => soloExcluded switch { "player" => "partner", "partner" => "player", _ => "" };
    private string SoloStatusVariable => soloExcluded == "player" ? "showPlayerStatus" : "showPartnerStatus";

    private const string SoloDefeatScript = @"
# Defeat
@gosub Func.Death
@stopBgm fade:3 wait:false
@battleCharacterSprite mini state:""taore"" position:""front"" characterId:""{GetCharacterLookId(tempTarget)}""
@print ""{TranslateName(tempTarget)}{cupSoloDefeatSuffix}"" isTrans:false
@resetBeforeGosub
@gosub Battle.FinishBattleStatus
@goto GameOver";

    private void InstallSoloHooks()
    {
        Hook(typeof(CustomVariableManager), nameof(CustomVariableManager.SetVariableValue), nameof(KeepSoloFormation),
            new[] { typeof(string), typeof(string) });
        Hook(typeof(ActionTargetResolver), nameof(ActionTargetResolver.IsPartyTargetAvailable), nameof(KeepSoloTarget),
            new[] { typeof(string) });
        Hook(typeof(ActionTargetResolver), nameof(ActionTargetResolver.ResolveIncomingPartyActionTarget),
            nameof(ResolveSoloIncomingTarget), new[] { typeof(string), typeof(string), typeof(string) });
    }

    private static void KeepSoloFormation(string __0, ref string __1)
    {
        if (Active is not { Run.Running: true, soloExcluded.Length: > 0 } cup || cup.Run.AllowsRoomRetryLoad) return;
        if (__0 == cup.SoloStatusVariable)
            __1 = "false";
        else if (__0 is "forwardCharacter" or "beforeForwardCharacter" or "turnCharacter" or "turnCharacterId")
            __1 = cup.SoloActor;
        else if (__0 == "turnCharacterPosition")
            __1 = "front";
        else if (__0 == "notTurnCharacterPosition")
            __1 = "back";
        else if (__0 == "isOrderChangeable")
            __1 = "False";
        else if (__0 == "cupShowTrainingKick" && cup.soloNativeDreakOffered)
            __1 = "0";
        else if (__0 == "selectionForwardTargetId" && __1 == cup.soloExcluded)
            __1 = cup.SoloActor;
    }

    private static bool KeepSoloTarget(string __0, ref bool __result)
    {
        if (Active is not { Run.Running: true, soloExcluded.Length: > 0 } cup || __0 != cup.soloExcluded) return true;
        __result = false;
        return false;
    }

    private static bool ResolveSoloIncomingTarget(string __0, string __1, ref string __result)
    {
        if (Active is not { Run.Running: true, soloExcluded.Length: > 0 } cup ||
            __0?.StartsWith("enemy", StringComparison.Ordinal) != true || __1 is not ("player" or "partner")) return true;
        // The other party member cannot cover the sole participant.
        __result = cup.SoloActor;
        return false;
    }

    private void EnterSoloBattle()
    {
        string partner = Read("partnerCharacter");
        // A battle that already starts with one combatant has no missing second slot.
        if (soloExcluded.Length > 0 || !Flag("showPartnerStatus") || Number("partnerDeath") > 0 ||
            partner is not ("hero" or "mage" or "warrior" or "priest")) return;
        string disabled = Settings.DisabledCharacter == "player" ? "player" :
            Settings.DisabledCharacter == partner ? "partner" : "";
        if (disabled.Length == 0) return;
        soloSavedFormation = Read("forwardCharacter");
        soloSavedStatus = Read(disabled == "player" ? "showPlayerStatus" : "showPartnerStatus");
        soloSavedOrder = Read("isOrderChangeable");
        soloExcluded = disabled;
        vars!.SetVariableValue(SoloStatusVariable, "false");
        vars.SetVariableValue("forwardCharacter", SoloActor);
        vars.SetVariableValue("beforeForwardCharacter", SoloActor);
        vars.SetVariableValue("isOrderChangeable", "False");
        // The draggable profile is owned by a character independently of the drop zone.
        foreach (var panel in Resources.FindObjectsOfTypeAll<DraggableStatusUI>())
        {
            var status = panel.gameObject;
            if (!status.scene.IsValid() || !status.activeSelf || panel.statusOwner != soloExcluded) continue;
            soloHiddenStatusPanels.Add(status);
            status.SetActive(false);
        }
    }

    private void RestoreSoloBattle()
    {
        if (soloExcluded.Length == 0) return;
        string status = SoloStatusVariable;
        ResetSoloAfterRetry();
        vars!.SetVariableValue(status, soloSavedStatus);
        vars.SetVariableValue("forwardCharacter", soloSavedFormation);
        vars.SetVariableValue("beforeForwardCharacter", soloSavedFormation);
        vars.SetVariableValue("isOrderChangeable", soloSavedOrder);
    }

    private void ResetSoloAfterRetry()
    {
        soloExcluded = "";
        soloNativeDreakOffered = false;
        foreach (var restore in soloCommandRestore.Values) restore();
        soloCommandRestore.Clear();
        foreach (var status in soloHiddenStatusPanels)
            if (status != null) status.SetActive(true);
        soloHiddenStatusPanels.Clear();
    }

    private bool SoloParticipantAliveForRetry()
    {
        string member = SoloActor.Length > 0 ? SoloActor : "player";
        return Number(member + "Health") > 0 && !Flag(member + "Death");
    }

    private bool SkipSoloCommand(Command command)
    {
        if (soloExcluded.Length == 0) return false;
        var spot = command.PlaybackSpot;
        if (spot.ScriptName is not ("Battle" or "Action")) return false;
        if (command.TryCast<CharacterPositionSwap>() != null) return true;
        if (command.TryCast<BattleCharacterSprite>() is { } sprite &&
            (string?)sprite.Type is "show" or "showAndWait" or "mini" && (string?)sprite.Position == "back")
            return true;
        if (spot.ScriptName != "Battle") return false;
        // The back slot is empty. The synchronized hero skill cannot hand off to an excluded soldier.
        if (spot.LineIndex is 1665 or 1666 || soloExcluded == "player" && spot.LineIndex is 1498 or 2567)
            return true;
        return spot.LineIndex == 4737 && Read("actionTargetId") == soloExcluded;
    }

    private bool ForceSoloCommand(Command command)
    {
        if (soloExcluded.Length == 0) return false;
        var spot = command.PlaybackSpot;
        if (spot.ScriptName == "Battle" && spot.LineIndex == 1973 && soloExcluded == "player" && Settings.HarshTraining)
            return true;
        return spot.ScriptName == "Action" && spot.LineIndex is 280 or 281 &&
            Read("actionTargetId") == soloExcluded && ((spot.LineIndex == 280) == (Read("actionTargetArray").Length > 0));
    }

    private void ReplaceSoloCondition(Command command, string expression)
    {
        if (soloCommandRestore.ContainsKey(command.Pointer)) return;
        var condition = command.Cast<BeginIf>();
        var original = condition.Expression;
        soloCommandRestore.Add(command.Pointer, () => condition.Expression = original);
        condition.Expression = expression;
    }

    private void HandleSoloCommand(Command command, string script, int line)
    {
        if (Settings.DisabledCharacter.Length == 0) return;
        if (SoloSmoke) RecordSoloSmokeCommand(script, line);
        if (script == "Battle")
        {
            if (line == 1128) EnterSoloBattle();
            // Restore the narrative roster before the native victory experience distribution.
            if (line is 5038 or 5258) RestoreSoloBattle();
            if (soloExcluded.Length == 0) return;
            if (line == 4816 && soloExcluded == "player" && (Number("partnerHealth") <= 0 || Number("partnerDeath") > 0))
            {
                var gosub = command.Cast<Gosub>();
                var original = gosub.Path;
                soloCommandRestore.Add(command.Pointer, () => gosub.Path = original);
                soloScript ??= Script.FromScriptText(SoloScriptName, SoloDefeatScript, (string?)null);
                RegisterCupScript(SoloScriptName, soloScript);
                vars!.SetVariableValue("tempTarget", SoloActor);
                vars.SetVariableValue("cupSoloDefeatSuffix", T("倒下了。", " fell."));
                gosub.Path = new NamedString(SoloScriptName, "Defeat");
            }
            switch (line)
            {
                case 1820:
                    soloNativeDreakOffered = false;
                    break;
                case 1973 when soloExcluded == "player":
                    soloNativeDreakOffered = true;
                    break;
                case 1489 when soloExcluded == "player":
                    ReplaceSoloCondition(command, "false");
                    break;
                case 1624:
                    ReplaceSoloCondition(command, "false");
                    break;
                case 4872 when soloExcluded == "partner":
                    // An excluded priest cannot resurrect the sole soldier.
                    ReplaceSoloCondition(command, "false");
                    break;
                case 1966 when soloExcluded == "player":
                    ReplaceSoloCondition(command, "turnCharacter==\"partner\"");
                    break;
                case 2707 or 2722 or 3992:
                    ReplaceSoloCondition(command, "true");
                    break;
                case 3484:
                    // The native block increments actionCount and goes to NextAction. The native
                    // action-free and phase-transition branches above it still take precedence.
                    ReplaceSoloCondition(command, Settings.InheritDisabledAction ? "actionCount==1" : "false");
                    break;
                case 1377 or 3511 when soloExcluded == "player":
                    if (soloCommandRestore.ContainsKey(command.Pointer)) break;
                    var target = command.Cast<SetCustomVariable>();
                    var original = target.Expression;
                    soloCommandRestore.Add(command.Pointer, () => target.Expression = original);
                    target.Expression = "tempTarget=\"partner\"";
                    break;
            }
            return;
        }
        if (script == "Action")
        {
            // The game builds group targets before it calls the incoming single-target resolver.
            if (line == 2220 && soloExcluded.Length > 0)
                vars!.SetVariableValue("aliveAllyList", Number(SoloActor + "Death") > 0 ? "" : SoloActor);
            if (line == 250 && soloExcluded.Length > 0)
            {
                string targets = Read("actionTargetArray");
                if (targets.Length > 0)
                    vars!.SetVariableValue("actionTargetArray", string.Join(",", targets.Split(',').Where(target => target != soloExcluded)));
                else if (Read("actionTargetId") == soloExcluded && Read("actionUserId").StartsWith("enemy", StringComparison.Ordinal))
                    vars!.SetVariableValue("actionTargetId", SoloActor);
            }
        }
        else if (script == "GameOver" && line == 0 || script == "Dungeon" && Read("timing") != "battle")
            RestoreSoloBattle();
    }
}
