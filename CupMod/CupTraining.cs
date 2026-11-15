using System.Globalization;
using HarmonyLib;
using Il2Cpp;
using Il2CppNaninovel;
using Il2CppNaninovel.Commands;
using Il2CppNaninovel.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CupCompetition;

public sealed partial class CupMod
{
    private const int KickCommand = -101;
    private const string TrainingScriptName = "AnnihilatedCupHarshTraining";
    private Script? trainingScript;
    private bool companionHasStealSkill;
    private bool buildingTrainingStealBar;

    private const string TrainingScript = @"
# ShowCommand
@gosub Battle.PriestFinalAfterFormStartSetupText if:battleTarget==""priest""
@if cupShowTrainingKick==1
    @set cupNativeBattleCifText=battleCifText
    @set cupNativeActionNumber=actionNumber
    @set actionNumber=-101
    @gosub Battle.ShowMagic
    @set battleCifText=cupNativeBattleCifText
    @set actionNumber=cupNativeActionNumber
@endif
@return";

    private bool IsTrainingKickChoice => Number("actionNumber") == KickCommand;

    private void InstallTrainingEligibilityHooks()
    {
        HarmonyInstance.Patch(AccessTools.Method(typeof(AttackBar), nameof(AttackBar.Initialize)),
            prefix: new HarmonyMethod(typeof(CupMod), nameof(BeginTrainingStealBar)),
            postfix: new HarmonyMethod(typeof(CupMod), nameof(EndTrainingStealBar)));
        Hook(typeof(EnemyService), nameof(EnemyService.CanStealItemFromEnemy), nameof(AllowTrainingStealTarget),
            new[] { typeof(string) });
    }

    // Scope the eligibility change to the game's own steal-zone generation.
    private static void BeginTrainingStealBar(ItemData __0, int __2, out bool __state)
    {
        __state = false;
        if (Active is not { } cup) return;
        __state = cup.buildingTrainingStealBar;
        cup.buildingTrainingStealBar = cup.Run.Running && cup.Settings.HarshTraining && __0 != null &&
            cup.Read("turnCharacter") is "player" or "partner" &&
            (__0.resourceName == "kick" ? __2 == KickCommand ||
                (__2 == 0 && cup.Number("selectCommand") == KickCommand) : __0.canStealItem());
    }

    private static void EndTrainingStealBar(bool __state)
    {
        if (Active is { } cup) cup.buildingTrainingStealBar = __state;
    }

    private bool HasTrainingStealTarget(string target, out int id)
    {
        id = 0;
        var slot = target.StartsWith("enemy", StringComparison.Ordinal) ? target.AsSpan(5) : target.AsSpan();
        if (!int.TryParse(slot, NumberStyles.None, CultureInfo.InvariantCulture, out id) || id <= 0) return false;
        string enemy = "enemy" + id.ToString(CultureInfo.InvariantCulture);
        string resource = Read(enemy + "ResourceName");
        if (resource.Length == 0 || Number(enemy + "StolenWeaponCount") >= EnemyCustomFunctions.GetEnemyStealItemMaxCount(resource, id))
            return false;
        return EnemyCustomFunctions.GetEnemyStealItem(resource, Number(enemy + "FormNum"), id) is { Length: > 0 } item && item != "none";
    }

    private static bool AllowTrainingStealTarget(string __0, ref bool __result)
    {
        if (Active is not { Run.Running: true, Settings.HarshTraining: true } cup || string.IsNullOrEmpty(__0))
            return true;
        var spot = cup.player!.PlaybackSpot;
        bool nativeStealAttack = spot.ScriptName == "Action" && spot.LineIndex == 1307 &&
            cup.Read("actionType") == "stealAttack" && cup.Read("actionUserId") is "player" or "partner";
        if (!cup.buildingTrainingStealBar && !nativeStealAttack) return true;
        if (!cup.HasTrainingStealTarget(__0, out int id) || !EnemyCustomFunctions.IsEnemyTargetable(id))
            return true;
        __result = true;
        return false;
    }

    private void AllowTrainingStealAfterHit()
    {
        string actor = Read("actionUserId"), target = Read("actionTargetId"), actionTarget = Read("actionTarget");
        int click = Number("actionCountTemp");
        if (Flag("tempCanStealEnemyWeapon") || actor is not ("player" or "partner") || !Flag("sectionEffectEligibleAction") ||
            click < 0 || click >= ActionCustomFunctions.GetSectionDataLength() ||
            ActionCustomFunctions.GetSectionOptionValueAtPosByType(click, "StealWeapon") <= 0 ||
            !ActionCustomFunctions.CanUseAttackSectionAction(Read("actionType"), actionTarget, actor) ||
            ActionCustomFunctions.GetSectionEnemyTargetForActionAtPos(click, actionTarget) != target ||
            !HasTrainingStealTarget(target, out _))
            return;
        vars!.SetVariableValue("tempCanStealEnemyWeapon", "true");
    }

    private void TraceTrainingHitForSmoke()
    {
        int click = Number("actionCountTemp");
        if (Number("selectCommand") != KickCommand || click < 0 || click >= ActionCustomFunctions.GetSectionDataLength()) return;
        LoggerInstance.Msg($"Cup training smoke: native result | stop={Read("stopPositions")} | target={Read("actionTargetId")} | " +
            $"steal={ActionCustomFunctions.GetSectionOptionValueAtPosByType(click, "StealWeapon")} | result={Read("tempCanStealEnemyWeapon")}");
    }

    private static bool HasNativeSteal(int slot)
    {
        if (slot >= 0 || slot == KickCommand) return false;
        var item = Engine.GetService<ItemService>().GetItemBySlotNumber(slot);
        if (item == null) return false;
        if (item.resourceName == "warriorHand" || item.canStealItem() ||
            ItemCustomFunctions.GetItemTargetExEffectValueMax(slot, "canStealItem") > 0 ||
            ItemCustomFunctions.GetItemTargetExEffectValueMax(slot, "stealWeaponOnKill") > 0 ||
            ItemCustomFunctions.GetItemTargetExEffectValueMax(slot, "stealWeaponOnKillReduceLimit") > 0)
            return true;
        int count = ItemCustomFunctions.GetActionLengthBySlotNumber(slot);
        for (int i = 0; i < count; i++)
            if (ItemCustomFunctions.GetActionTypeBySlotNumberAndActionNumber(slot, i) == "stealAttack") return true;
        return false;
    }

    private void HandleTrainingCommand(Command command, string script, int line)
    {
        if (script != "Battle") return;
        switch (line)
        {
            case 1809:
                companionHasStealSkill = false;
                break;
            case 1910 when Read("turnCharacter") == "partner":
                companionHasStealSkill |= HasNativeSteal(Number("actionNumber"));
                break;
            case 2028 when trainingSmoke && Read("turnCharacter") == "partner" &&
                Environment.GetCommandLineArgs().Contains("--annihilated-cup-training-native-card-smoke", StringComparer.Ordinal):
                // Reproduce the native warrior-hand configuration at the point where the game chooses its basic card.
                vars!.SetVariableValue("partnerCharacter", "warrior");
                vars.SetVariableValue("xSwitchSlimeWarriorSpecialWeaponActive", "1");
                break;
            case 2044:
                string actor = Read("turnCharacter");
                bool show = actor == "player" || actor == "partner" &&
                    !companionHasStealSkill && !HasNativeSteal(Number("defaultActionNumber"));
                if (trainingSmoke) LoggerInstance.Msg($"Training card build | actor={actor} | native skill={companionHasStealSkill} | show={show}");
                vars!.SetVariableValue("cupShowTrainingKick", show ? "1" : "0");
                trainingScript ??= Script.FromScriptText(TrainingScriptName, TrainingScript, (string?)null);
                RegisterCupScript(TrainingScriptName, trainingScript);
                var call = command.Cast<Gosub>();
                call.Path = new NamedString(TrainingScriptName, "ShowCommand");
                break;
        }
    }

    private System.Collections.IEnumerator VerifyGiantTrainingForSmoke()
    {
        const string name = "AnnihilatedCupGiantTrainingSmoke";
        player!.Stop();
        var script = Script.FromScriptText(name, "@gosub Scenario.GiantBattleCore\n@stop", (string?)null);
        RegisterCupScript(name, script);
        var playback = player.PreloadAndPlayAsync(script);
        while (!playback.IsCompleted && Run.Running) yield return null;
        playback.GetAwaiter().GetResult();
        yield return VerifyTrainingForSmoke("virtualSword", 2);
    }

    private System.Collections.IEnumerator VerifyTrainingForSmoke(string stolenWeapon = "club", int enemySlot = 1)
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();
        bool clicked = false, hit = false;
        string target = "enemy" + enemySlot;
        bool partnerTest = Environment.GetCommandLineArgs().Contains("--annihilated-cup-training-partner-smoke", StringComparer.Ordinal);
        bool nativeCardTest = Environment.GetCommandLineArgs().Contains("--annihilated-cup-training-native-card-smoke", StringComparer.Ordinal);
        bool basicAttackTest = Environment.GetCommandLineArgs().Contains("--annihilated-cup-training-basic-smoke", StringComparer.Ordinal);
        bool convert = partnerTest && Settings.KickOnly;
        string expectedActor = partnerTest ? "partner" : "player";
        string firstActor = "", firstPosition = "";
        double curseBefore = 0, curseCountBefore = 0;
        double commandReady = -1, stealBarReady = -1;
        var advance = Engine.GetService<IInputManager>().GetSampler(InputConfiguration.ContinueName);
        double deadline = Time.realtimeSinceStartupAsDouble + (enemySlot == 1 ? 21 : 34);
        try
        {
            while (Run.Running && Time.realtimeSinceStartupAsDouble < deadline)
            {
                yield return null;
                var bar = UnityEngine.Object.FindObjectsByType<AttackBar>(FindObjectsSortMode.None)
                    .FirstOrDefault(candidate => candidate.isActiveAndEnabled && candidate.IsTimingInputActive);
                if (basicAttackTest && clicked && bar != null && Number("selectCommand") == -1 &&
                    bar.initializedItemData?.resourceName == "default" && bar.colorSections.Count > 0 &&
                    bar.colorSections.ToArray().All(section => section.Options.ToArray().All(option => option.Type != ColorSectionOptionType.StealWeapon)))
                {
                    LoggerInstance.Msg("Cup training smoke passed | ordinary attack card opened its native non-steal attack bar");
                    Finish("smoke_training_pass");
                    yield break;
                }
                if (bar != null && clicked && Number("selectCommand") == KickCommand)
                {
                    if (hit) continue;
                    var section = bar.colorSections.ToArray().FirstOrDefault(c => c.TargetId == target &&
                        c.SectionEnd > c.SectionStart && c.Options.ToArray().Any(option => option.Type == ColorSectionOptionType.StealWeapon));
                    if (section == null) continue;
                    if (stealBarReady < 0) stealBarReady = Time.realtimeSinceStartupAsDouble;
                    if (Time.realtimeSinceStartupAsDouble - stealBarReady < 0.3) continue;
                    if (Number(target + "Break") <= 0) break;
                    float center = bar.GetAbsolutePosition((section.SectionStart + section.SectionEnd) / 2);
                    bar.SetGuidedCursorConstraint(center, center);
                    bar.SetPrimaryCursorPositionX(center);
                    LoggerInstance.Msg($"Cup training smoke: selecting {target} native steal section {section.SectionStart}..{section.SectionEnd} | probability={bar.BattleForecastHitProbability(section)} | break={Read(target + "Break")}");
                    InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState(Key.Enter));
                    hit = true;
                    continue;
                }
                if (clicked && hit && (!partnerTest || firstPosition == "back") && Number(target + "StolenWeaponCount") > 0 &&
                    (convert ? Number("itemAddedIndex") < 0 && ReadCurse("curse") == curseBefore + 8 &&
                        ReadCurse("curseCount") == curseCountBefore + 8 &&
                        !Enumerable.Range(1, 12).Any(slot => ItemCustomFunctions.GetItemResourceNameBySlotNumber(slot) == stolenWeapon) :
                        Number("itemAddedIndex") > 0 &&
                        ItemCustomFunctions.GetItemResourceNameBySlotNumber(Number("itemAddedIndex")) == stolenWeapon))
                {
                    LoggerInstance.Msg($"Cup training smoke passed | actor={firstActor} | position={firstPosition} | " +
                        (convert ? $"{stolenWeapon} converted into 8 curse without inventory gain" : $"native weapon steal succeeded | item={stolenWeapon}"));
                    Finish("smoke_training_pass");
                    yield break;
                }
                var choices = UnityEngine.Object.FindObjectsByType<ChoiceHandlerButton>(FindObjectsSortMode.None);
                if (!clicked && player!.PlaybackSpot.ScriptName == "Battle" && player.PlaybackSpot.LineIndex == 2123)
                {
                    var basic = choices.FirstOrDefault(choice => choice.ChoiceState?.Summary == Read("defaultActionNumber") &&
                        ActiveButton(choice) != null);
                    if (basic == null) continue;
                    if (commandReady < 0) commandReady = Time.realtimeSinceStartupAsDouble;
                    if (Time.realtimeSinceStartupAsDouble - commandReady < 0.55) continue;
                    if (enemySlot == 2 && !Flag("xSwitchGiantHandRevealEvent"))
                    {
                        if (HasTrainingStealTarget("enemy1", out _) || EnemyCustomFunctions.IsEnemyTargetable(2))
                            break;
                        const string name = "AnnihilatedCupGiantRevealSmoke";
                        var reveal = Script.FromScriptText(name, "@clearChoice *\n@set enemy1Health=Floor(enemy1MaxHealth/4)\n@gosub Battle.GiantPhaseCheck\n@set enemy1StateCovering=0\n@goto Battle.BattleTurnEnd", (string?)null);
                        RegisterCupScript(name, reveal);
                        var playback = player.PreloadAndPlayAsync(reveal);
                        while (!playback.IsCompleted && Run.Running) yield return null;
                        playback.GetAwaiter().GetResult();
                        commandReady = -1;
                        continue;
                    }
                    if (Number(target + "Break") <= 0) break;
                    if (ModeValue == "1")
                    {
                        // Exercise the real hit-rate calculation with a high-DEX test character.
                        vars!.SetVariableValue("playerDex", "1000");
                        vars.SetVariableValue("playerDexAdd", "1000");
                    }
                    if (Read("turnCharacter") != expectedActor)
                    {
                        vars!.SetVariableValue("actionCount", "2");
                        vars.SetVariableValue("xSwitchSkipTurnCharacter", "0");
                        vars.SetVariableValue("turnCharacterPosition", "back");
                        vars.SetVariableValue("notTurnCharacterPosition", "front");
                        var battle = Engine.GetService<IScriptManager>().Cast<ScriptManager>().scriptLoader.GetLoadedOrNull("Battle");
                        player.Play(battle, 1601);
                        commandReady = -1;
                        continue;
                    }
                    var kick = choices.FirstOrDefault(choice => choice.ChoiceState?.Summary == "-101" && ActiveButton(choice) != null);
                    LoggerInstance.Msg($"Cup training smoke: actor={expectedActor} basic={basic.ChoiceState.Summary} kick={kick != null} | " +
                        $"cards={string.Join(",", choices.Select(c => $"{c.ChoiceState?.Summary}:{ActiveButton(c) != null}"))}");
                    if (nativeCardTest && kick == null && HasNativeSteal(Number("defaultActionNumber")))
                    {
                        LoggerInstance.Msg("Cup training smoke passed | native companion attack retained; no extra kick card");
                        Finish("smoke_training_pass");
                        yield break;
                    }
                    if (kick == null || nativeCardTest) break;
                    firstActor = Read("turnCharacter");
                    firstPosition = Read("turnCharacterPosition");
                    curseBefore = ReadCurse("curse");
                    curseCountBefore = ReadCurse("curseCount");
                    LoggerInstance.Msg($"Cup training smoke: both choices present before break={Read(target + "Break")}");
                    clicked = true;
                    ActiveButton(basicAttackTest ? basic : kick)!.onClick.Invoke();
                }
                foreach (var choice in choices)
                    if (choice.ChoiceState?.OnSelectScript?.Contains("ShowStolenItemMessageSelected", StringComparison.Ordinal) == true &&
                        ActiveButton(choice) is { } button) { button.onClick.Invoke(); break; }
                if ((clicked || enemySlot == 2) && player!.WaitingForInput) advance.Activate(1f);
            }
        }
        finally { InputSystem.RemoveDevice(keyboard); }
        Fail(new InvalidOperationException($"Training smoke failed | clicked={clicked} hit={hit} | {player?.PlaybackSpot.ScriptName}:{player?.PlaybackSpot.LineIndex} | selected={Read("selectCommand")} steal={Read(target + "StolenWeaponCount")} item={Read("itemAddedIndex")} curse={Read("curse")}/{Read("curseCount")} before={curseBefore}/{curseCountBefore}"));
    }
}
