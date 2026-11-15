using System.Globalization;
using Il2Cpp;
using Il2CppNaninovel;
using Il2CppNaninovel.Commands;
using Il2CppNaninovel.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CupCompetition;

public sealed partial class CupMod
{
    private const string KnifeScriptName = "AnnihilatedCupKnife";
    private bool waitingForStolenItem;
    private int stolenSlot;
    private ItemData? stolenItem;
    private Script? knifeScriptAsset;
    private Dictionary<string, string>? knifeContext;

    // Action.PushActionContext saves the action fields. These belong to the surrounding hit or item use.
    private static readonly string[] KnifeContextKeys =
    {
        "selectCommand", "battleBreakMode", "ignoreBattleBreakModeDepth", "battleWarriorDestroyMode", "sectionEffectEligibleAction",
        "actionCountTemp", "damageBeforeText", "xSwitchDamageBeforeMessage", "hpSacrificeAtkBonus",
        "turnCharacter", "turnCharacterId", "turnCharacterPosition", "notTurnCharacterPosition",
        "selectionEnemyTargetId", "tempClicks", "tempSkipBattleAttackAnimation", "stopPositions",
        "xSwitchAttackBarProcessing", "xSwitchActionSetDoubleUpInitialized", "xSwitchActionSetDoubleUp",
        "xSwitchActionSetDoubleUpUsed", "actionSetDoubleUpUser",
        "breakItemNumber", "forceItemBreak", "itemBreakContext", "itemAddedIndex",
        "damageBeforeHealth", "damageBreakValue", "damageBeforeBlock", "tempWasFullyBlocked"
    };

    private const string KnifeScript = @"
# AfterSteal
@gosub Action.StealItemEvent
@if cupKnifeSlot>0
@gosub Action.PushActionContext
@set z=PushBattleItemActionSet(cupKnifeSlot)
@set selectCommand=cupKnifeSlot
@set battleWarriorDestroyMode=false
@set ignoreBattleBreakModeDepth=0
@set battleBreakMode=true
@set z=RecordBattleUsedItem(cupKnifeSlot)
@if cupKnifeAttackIndex>=0&&{cupKnifeTarget}Health>0&&{cupKnifeTarget}Death==0
    @set turnCharacter=cupKnifeActor
    @set turnCharacterId=cupKnifeActor
    @set turnCharacterPosition=GetTargetCharacterPosition(cupKnifeActor)
    @set notTurnCharacterPosition=""back""
    @set notTurnCharacterPosition=""front"" if:turnCharacterPosition==""back""
    @set actionUserId=cupKnifeActor
    @set selectionEnemyTargetId=cupKnifeTarget
    @set tempClicks=1
    @set tempSkipBattleAttackAnimation=false
    @set xSwitchAttackBarProcessing=1
    @hidePrinter wait:false
    @battleCharacterSprite itemUseIntro slot:{cupKnifeSlot} characterId:""{cupKnifeActor}""
    @summonAttackBar clickCount:1 stopPositionsVarName:""stopPositions""
    @set xSwitchAttackBarProcessing=0
    @set cupKnifePower=GetSectionDamageAtPos(0) if:GetSectionDataLength()>0
    @set cupKnifePower=-999 if:GetSectionDataLength()<=0
    @set cupKnifeHitTarget=GetSectionEnemyTargetForActionAtPos(0,""enemy"") if:cupKnifePower>-999
    @set selectionEnemyTargetId=cupKnifeHitTarget if:cupKnifePower>-999&&IsEnemyStr(cupKnifeHitTarget)
    @gosub .ResolveHit if:cupKnifePower>-999&&{selectionEnemyTargetId}Health>0&&{selectionEnemyTargetId}Death==0
    @print ""外れた！"" waitInput:false if:cupKnifePower<=-999
@endif
@if cupKnifeSlot>0
    @set breakItemNumber=cupKnifeSlot
    @set forceItemBreak=true
    @gosub BreakItem
@endif
@set z=PopBattleActionSetContext()
@gosub Action.PopActionContext
@endif
# Done
@return

# ResolveHit
    @set actionTarget=""choiceEnemy""
    @set actionUserId=cupKnifeActor
    @set actionType=GetActionTypeBySlotNumberAndActionNumber(cupKnifeSlot,cupKnifeAttackIndex)
    @set actionType=""attack"" if:actionType==""stealAttack""
    @set sectionEffectEligibleAction=false
    @set actionCountTemp=0
    @set actionExEffectIndex=-1
    @set actionTargetArray=""""
    @set damageBeforeText=""""
    @set xSwitchDamageBeforeMessage=0
    @set xSwitchActionSetDoubleUpInitialized=true
    @set xSwitchActionSetDoubleUp=false
    @set hpSacrificeAtkBonus=0
    @set cupKnifeBase=GetActionValueBySlotNumberAndActionNumber(cupKnifeSlot,cupKnifeAttackIndex,cupKnifeActor)
    @set cupKnifeStat=GetStatusCalcBySlotNumberAndActionNumber(cupKnifeSlot,cupKnifeAttackIndex)
    @set cupKnifeStat=CalculateStatusCalc(cupKnifeActor,cupKnifeStat)
    @set cupKnifeIntrinsic=GetIntrinsicActionPower(actionType,cupKnifeActor)
    @set actionValue=(cupKnifeBase+cupKnifeStat+cupKnifeIntrinsic)*cupKnifePower-cupKnifeIntrinsic
    @gosub Action
    @return";

    private static void RegisterCupScript(string name, Script script)
    {
        var loader = Engine.GetService<IScriptManager>().Cast<ScriptManager>().scriptLoader;
        if (loader.GetLoadedOrNull(name) != null) return;
        var source = loader.ProvisionSources[0];
        loader.LoadedResources.Add(new ResourceLoader<Script>.LoadedResource(
            new Resource<Script>(source.BuildFullPath(name), script), source));
        if (loader.GetLoadedOrNull(name) == null)
            throw new InvalidOperationException($"Could not register cup script {name}.");
    }

    private void HandleKnifeCommand(Command command, string script, int line)
    {
        if (script == "Action")
        {
            switch (line)
            {
                case 2232:
                    stolenSlot = 0;
                    stolenItem = null;
                    waitingForStolenItem = false;
                    break;
                case 2271:
                case 2298:
                    waitingForStolenItem = knifeContext == null && Read("timing") == "battle" &&
                        Read("actionUserId") is "player" or "partner";
                    break;
                case 2304:
                    PrepareKnifeFollowup(command.Cast<Gosub>());
                    break;
            }
        }
        else if (script == KnifeScriptName && knifeContext != null)
        {
            if (line == KnifeDoneLine)
            {
                foreach (var entry in knifeContext) vars!.SetVariableValue(entry.Key, entry.Value);
                knifeContext = null;
                stolenSlot = 0;
                stolenItem = null;
            }
            else if (Number("cupKnifeSlot") > 0 && stolenItem != null &&
                Engine.GetService<ItemService>().GetItemBySlotNumber(stolenSlot)?.Pointer != stolenItem.Pointer)
            {
                // Never let a slot reused by a story event destroy a different item.
                vars!.SetVariableValue("cupKnifeAttackIndex", "-1");
                vars.SetVariableValue("cupKnifeSlot", "0");
            }
        }
    }

    private static readonly int KnifeDoneLine = Array.IndexOf(KnifeScript.Split('\n'), "# Done") + 1;

    private void CaptureStolenItem()
    {
        waitingForStolenItem = false;
        int slot = Number("itemAddedIndex");
        if (slot <= 0) return;
        var item = Engine.GetService<ItemService>().GetItemBySlotNumber(slot);
        if (item == null || item.resourceName != Read("enemyStealItem")) return;
        stolenSlot = slot;
        stolenItem = item;
    }

    private void PrepareKnifeFollowup(Gosub command)
    {
        command.Path = new NamedString("", "StealItemEvent");
        if (stolenItem == null || knifeContext != null || !Flag("stealEnemyWeaponSucceeded")) return;
        string target = Read("stealTargetId");
        string actor = Read("actionUserId");
        if (!target.StartsWith("enemy", StringComparison.Ordinal) ||
            !int.TryParse(target.AsSpan(5), NumberStyles.None, CultureInfo.InvariantCulture, out int id) || id <= 0 ||
            actor is not ("player" or "partner")) return;
        int index = -1;
        int actions = ItemCustomFunctions.GetActionLengthBySlotNumber(stolenSlot);
        for (int i = 0; i < actions; i++)
        {
            string action = ItemCustomFunctions.GetActionTypeBySlotNumberAndActionNumber(stolenSlot, i);
            if (!ActionCustomFunctions.IsAttackEffectAction(action)) continue;
            index = i;
            break;
        }
        if (index < 0) return;
        if (knifeScriptAsset == null)
            knifeScriptAsset = Script.FromScriptText(KnifeScriptName, KnifeScript, (string?)null);
        RegisterCupScript(KnifeScriptName, knifeScriptAsset);
        knifeContext = KnifeContextKeys.ToDictionary(key => key, Read);
        vars!.SetVariableValue("cupKnifeSlot", stolenSlot.ToString(CultureInfo.InvariantCulture));
        vars.SetVariableValue("cupKnifeAttackIndex", index.ToString(CultureInfo.InvariantCulture));
        vars.SetVariableValue("cupKnifeTarget", target);
        vars.SetVariableValue("cupKnifeActor", actor);
        command.Path = new NamedString(KnifeScriptName, "AfterSteal");
    }

    private System.Collections.IEnumerator VerifyRunKnifeForSmoke()
    {
        const string testName = "AnnihilatedCupKnifeSmoke";
        player!.Stop();
        var script = Script.FromScriptText(testName, @"
@clearChoice * wait:false
@set actionUserId=""player""
@set stealTargetId=""enemy1""
@set stealUseExistingEnemyStealItem=false
@set stealUseMaxCount=false
@set stealSpecialOpacity=false
@set stealReduceLimitRate=0
@set enemy1StolenWeaponCount=0
@set enemy1Health=24
@gosub Action.TryStealEnemyWeapon
@set cupKnifeSmokeLiveHealth=enemy1Health
@set enemy1Health=0
@set enemy1Death=1
@set enemy1StolenWeaponCount=0
@set stealReduceLimitRate=0.5
@gosub Action.TryStealEnemyWeapon
@set cupKnifeSmokeCompleted=1
@stop", (string?)null);
        RegisterCupScript(testName, script);
        var playback = player.PreloadAndPlayAsync(script);
        while (!playback.IsCompleted && Run.Running) yield return null;
        playback.GetAwaiter().GetResult();
        var advance = Engine.GetService<IInputManager>().GetSampler(InputConfiguration.ContinueName);
        double deadline = Time.realtimeSinceStartupAsDouble + 25;
        var keyboard = InputSystem.AddDevice<Keyboard>();
        double barOpenedAt = -1;
        bool pressed = false;
        while (!Flag("cupKnifeSmokeCompleted") && Run.Running && Time.realtimeSinceStartupAsDouble < deadline)
        {
            yield return null;
            var bar = UnityEngine.Object.FindObjectsByType<AttackBar>(FindObjectsSortMode.None)
                .FirstOrDefault(candidate => candidate.isActiveAndEnabled && candidate.IsTimingInputActive);
            if (bar != null && Read("selectCommand") == Read("cupKnifeSlot") && knifeContext != null)
            {
                if (barOpenedAt < 0) barOpenedAt = Time.realtimeSinceStartupAsDouble;
                if (Time.realtimeSinceStartupAsDouble - barOpenedAt < 0.65) continue;
                if (!pressed)
                {
                    if (Number("enemy1Health") != 24 || !Flag("battleBreakMode") || Run.Paused)
                    {
                        Fail(new InvalidOperationException("Run knife resolved before an input, or the attack bar was paused."));
                        break;
                    }
                    var section = bar.colorSections.ToArray().FirstOrDefault(c => c.TargetId == "enemy1" && c.SectionEnd > c.SectionStart);
                    if (section == null) continue;
                    bar.SetPrimaryCursorPositionX(bar.GetAbsolutePosition((section.SectionStart + section.SectionEnd) / 2));
                    InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState(Key.Enter));
                    pressed = true;
                }
                continue;
            }
            foreach (var choice in UnityEngine.Object.FindObjectsByType<ChoiceHandlerButton>(FindObjectsSortMode.None))
            {
                if (ActiveButton(choice) is { } button &&
                    choice.ChoiceState?.OnSelectScript?.Contains("ShowStolenItemMessageSelected", StringComparison.Ordinal) == true)
                {
                    button.onClick.Invoke();
                    break;
                }
            }
            if (player.WaitingForInput) advance.Activate(1f);
        }
        InputSystem.RemoveDevice(keyboard);
        string resource = Read("enemyStealItem");
        if (!Flag("cupKnifeSmokeCompleted") || !pressed || Number("cupKnifeSmokeLiveHealth") >= 24 ||
            resource == "" || Enumerable.Range(1, 12).Any(slot => ItemCustomFunctions.GetItemResourceNameBySlotNumber(slot) == resource))
        {
            Fail(new InvalidOperationException($"Run knife smoke failed at {player.PlaybackSpot.ScriptName}:{player.PlaybackSpot.LineIndex}: " +
                $"item={resource}, live HP={Read("cupKnifeSmokeLiveHealth")}, completed={Read("cupKnifeSmokeCompleted")}."));
            yield break;
        }
        LoggerInstance.Msg($"Cup run knife smoke: attack bar waited for key input with HP=24; after input HP=" +
            $"{Read("cupKnifeSmokeLiveHealth")}; {resource} destroyed in live and dead branches.");
        Finish("smoke_knife_pass");
    }
}
