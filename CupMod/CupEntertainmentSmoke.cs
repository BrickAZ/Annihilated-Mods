using Il2Cpp;
using Il2CppNaninovel;
using Il2CppNaninovel.Commands;
using Il2CppNaninovel.UI;
using UnityEngine;

namespace CupCompetition;

public sealed partial class CupMod
{
    private static bool EntertainmentSmoke => Environment.GetCommandLineArgs().Contains(
        "--annihilated-cup-entertainment-smoke", StringComparer.Ordinal);

    private System.Collections.IEnumerator VerifyEntertainmentForSmoke()
    {
        const string name = "AnnihilatedCupEntertainmentSmoke";
        bool combatOnly = Environment.GetCommandLineArgs().Contains(
            "--annihilated-cup-entertainment-combat-only", StringComparer.Ordinal);
        bool fireOnly = Environment.GetCommandLineArgs().Contains(
            "--annihilated-cup-entertainment-fire-only", StringComparer.Ordinal);
        var items = Engine.GetService<ItemService>();
        if (items.GetItemBySlotNumber(1)?.resourceName != "ironSword")
        {
            Fail(new InvalidOperationException("Could not stock the fire smoke with a legal pair."));
            yield break;
        }
        Settings = Settings with
        {
            FixedWeaponCombination = true,
            StolenWeaponDamageMultiplier = 2,
            EnemyHealthMultiplier = 2,
            EnemyAttackMultiplier = 1.5,
        };
        if (!combatOnly)
        {
            yield return OpenEntertainmentBonfireForSmoke(items);
            if (!Run.Running) yield break;
            yield return VerifyFireCombinationForSmoke(items);
            if (!Run.Running) yield break;
            if (!Flag("cupSmokeFireVerified"))
            {
                Fail(new InvalidOperationException("The fire verification did not reach its final assertion."));
                yield break;
            }
            if (fireOnly)
            {
                Finish("smoke_entertainment_fire_pass");
                yield break;
            }
        }
        player!.Stop();
        var script = Script.FromScriptText(name, @"
# Combat
@clearChoice *
@set backgroundName=""dungeon_blue""
@gosub Func.ShowBackground
@set battleTarget=""""
@set addEnemyPositions=""0.25,0.75""
@set addEnemyResourceNames=""goblin,outcast""
@gosub Battle
@stop

# Summon
@clearChoice *
@set addEnemyPositions=""0.50""
@set addEnemySummonSide=""enemy""
@set addEnemyResourceNames=""goblin""
@gosub Battle.AddEnemies
@set cupSmokeSummonHealth=enemy3Health
@set cupSmokeCombatReady=1
@stop

# Damage
@set cupSmokeDamageReady=0
@set z=PushEmptyBattleActionSetContext()
@set actionExEffectIndex=-1
@set i=0
@set actionCountTemp=-1
@set sectionEffectEligibleAction=false
@set xSwitchActionDoubleUp=false
@set actionValue=10
@set cupSmokeDamageBefore={actionTargetId}Health
@gosub Action.Damage
@set cupSmokeDamageResult=cupSmokeDamageBefore-{actionTargetId}Health
@set z=PopBattleActionSetContext()
@set cupSmokeDamageReady=1
@stop

# Steal
@set cupSmokeStealReady=0
@set actionUserId=""player""
@set stealTargetId=""enemy2""
@set stealUseExistingEnemyStealItem=false
@set stealUseMaxCount=true
@set stealSpecialOpacity=false
@set stealReduceLimitRate=0
@gosub Action.TryStealEnemyWeapon
@set cupSmokeStealReady=1
@stop", (string?)null);
        RegisterCupScript(name, script);
        var task = player.PreloadAndPlayAsync(script, label: "Combat");
        while (!task.IsCompleted && Run.Running) yield return null;
        task.GetAwaiter().GetResult();
        yield return VerifyCombatRulesForSmoke(script);
    }

    private System.Collections.IEnumerator OpenEntertainmentBonfireForSmoke(ItemService items)
    {
        var scriptPlayer = player!;
        var map = Engine.GetService<MapService>();
        if (items.AddItem("club") <= 0)
        {
            Fail(new InvalidOperationException("Could not add the campfire test material."));
            yield break;
        }
        WriteEntertainmentNumber("nowItemCount", items.inventory.items.Count);
        WriteEntertainmentNumber("curse", 200);
        WriteEntertainmentNumber("curseCount", 200);
        System.Action? restore = null;
        void ForceCampfireTile(Command command)
        {
            if ((command.PlaybackSpot.ScriptName, command.PlaybackSpot.LineIndex) != ("Dungeon", 356)) return;
            var assignment = command.Cast<SetCustomVariable>();
            var original = assignment.Expression;
            restore = () => assignment.Expression = original;
            assignment.Expression = "dungeonSpaceType=\"rest\"";
        }
        var redirect = (Il2CppSystem.Action<Command>)ForceCampfireTile;
        scriptPlayer.add_OnCommandExecutionStart(redirect);
        try
        {
            double deadline = Time.realtimeSinceStartupAsDouble + 5;
            UnityEngine.UI.Button? tile = null;
            while (Run.Running && tile == null && Time.realtimeSinceStartupAsDouble < deadline)
            {
                yield return null;
                tile = map.mapChoiceButtonContainer?.GetComponentsInChildren<UnityEngine.UI.Button>()
                    .FirstOrDefault(candidate => candidate.isActiveAndEnabled && candidate.IsInteractable());
            }
            tile?.onClick.Invoke();
            deadline = Time.realtimeSinceStartupAsDouble + 10;
            while (Run.Running && Time.realtimeSinceStartupAsDouble < deadline &&
                   (scriptPlayer.PlaybackSpot.ScriptName, scriptPlayer.PlaybackSpot.LineIndex) != ("Dungeon", 687))
                yield return null;
            if ((scriptPlayer.PlaybackSpot.ScriptName, scriptPlayer.PlaybackSpot.LineIndex) != ("Dungeon", 687))
                Fail(new InvalidOperationException("The native map tile did not open the fire menu."));
        }
        finally
        {
            scriptPlayer.remove_OnCommandExecutionStart(redirect);
            restore?.Invoke();
        }
    }

    private System.Collections.IEnumerator VerifyFireCombinationForSmoke(ItemService items)
    {
        var scriptPlayer = player!;
        double deadline = Time.realtimeSinceStartupAsDouble + 12;
        while (Run.Running && Time.realtimeSinceStartupAsDouble < deadline &&
               !(scriptPlayer.PlaybackSpot.ScriptName == "Dungeon" && scriptPlayer.PlaybackSpot.LineIndex == 966))
        {
            yield return null;
            if (scriptPlayer.PlaybackSpot.ScriptName == "Dungeon" && scriptPlayer.PlaybackSpot.LineIndex == 687)
                ClickEntertainmentSmokeChoice("RestReinforce");
        }
        var offers = items.ExtractedReinforceData;
        if (scriptPlayer.PlaybackSpot.ScriptName != "Dungeon" || scriptPlayer.PlaybackSpot.LineIndex != 966 ||
            offers.Count < 2 || Number("getReinforceCount") != offers.Count ||
            offers[offers.Count - 1].name != ReinforceType.combineWithItem || ReadCurse("curse") != 195)
        {
            Fail(new InvalidOperationException($"Fire offer missing at {scriptPlayer.PlaybackSpot.ScriptName}:{scriptPlayer.PlaybackSpot.LineIndex}: " +
                $"count={offers.Count}, script count={Read("getReinforceCount")}, curse={Read("curse")}."));
            yield break;
        }
        int beforeCombination = items.inventory.items.Count;
        int offerCount = offers.Count;
        string combineText = ItemCustomFunctions.GetTranslatedReinforceExplain(offers.Count - 1);
        deadline = Time.realtimeSinceStartupAsDouble + 15;
        double nextClick = 0;
        while (Run.Running && Time.realtimeSinceStartupAsDouble < deadline && Number("restReinforceUseCount") == 0)
        {
            yield return null;
            double now = Time.realtimeSinceStartupAsDouble;
            if (now < nextClick) continue;
            var spot = scriptPlayer.PlaybackSpot;
            if (spot.ScriptName == "Dungeon" && spot.LineIndex is 1013 or 1014 && scriptPlayer.WaitingForInput)
                Engine.GetService<IInputManager>().GetSampler(InputConfiguration.ContinueName).Activate(1f);
            string? callback = (spot.ScriptName, spot.LineIndex) switch
            {
                ("Dungeon", 966) => $"selectReinforce={offerCount - 1}",
                ("Func", 319) => "itemCardSelectionNext",
                ("Dungeon", 1017) => "ExecuteReinforceCombine",
                _ => null,
            };
            if (callback != null && ClickEntertainmentSmokeChoice(callback, invokeNativeHandler: true))
                nextClick = now + 0.25;
        }
        if (Number("restReinforceUseCount") != 1 || items.inventory.items.Count != beforeCombination - 1 ||
            ReadCurse("curse") != 195)
        {
            var spot = scriptPlayer.PlaybackSpot;
            var choices = UnityEngine.Object.FindObjectsByType<ChoiceHandlerButton>(FindObjectsSortMode.None);
            string choiceDetails = string.Join(" | ", choices.Where(c => c.ChoiceState is not null)
                .Select(c => $"{ActiveButton(c) != null}:{c.ChoiceState.Summary}:{c.ChoiceState.OnSelectScript}"));
            Fail(new InvalidOperationException($"Native combine did not settle at {spot.ScriptName}:{spot.LineIndex}: " +
                $"selection={Read("selectReinforce")}/{Read("combinePrimarySlot")}/{Read("combineMaterialSlot")}; " +
                $"uses={Read("restReinforceUseCount")}; items {beforeCombination}->{items.inventory.items.Count}; curse={Read("curse")}. " +
                $"Eligible={Read(BonfireCombinationAvailable)}. Count={Read("nowItemCount")}. Expect {combineText}; found {choiceDetails}."));
            yield break;
        }
        LoggerInstance.Msg($"Cup entertainment smoke: native fire menu rendered {offerCount} choices; " +
            "the selected combination consumed one material and one fire use for 5 curse.");
        WriteEntertainmentNumber("cupSmokeFireVerified", 1);
    }

    private System.Collections.IEnumerator VerifyCombatRulesForSmoke(Script script)
    {
        double deadline = Time.realtimeSinceStartupAsDouble + 15;
        var scriptPlayer = player!;
        while (Run.Running && Time.realtimeSinceStartupAsDouble < deadline &&
               !(scriptPlayer.PlaybackSpot.ScriptName == "Battle" && scriptPlayer.PlaybackSpot.LineIndex == 2140))
        {
            yield return null;
            AdvanceWeaponSmoke();
        }
        int goblin = EnemyCustomFunctions.GetEnemyBaseHp("goblin");
        int outcast = EnemyCustomFunctions.GetEnemyBaseHp("outcast");
        if (scriptPlayer.PlaybackSpot.ScriptName != "Battle" || scriptPlayer.PlaybackSpot.LineIndex != 2140 ||
            Number("enemy1Health") != goblin * 2 || Number("enemy1MaxHealth") != goblin * 2 ||
            Number("enemy2Health") != outcast * 2 || Number("enemy2MaxHealth") != outcast * 2)
        {
            Fail(new InvalidOperationException($"Enemy starting HP at {scriptPlayer.PlaybackSpot.ScriptName}:{scriptPlayer.PlaybackSpot.LineIndex}: " +
                $"goblin {goblin}/{Read("enemy1Health")}/{Read("enemy1MaxHealth")}, " +
                $"outcast {outcast}/{Read("enemy2Health")}/{Read("enemy2MaxHealth")}."));
            yield break;
        }
        LoggerInstance.Msg($"Cup entertainment smoke: native opening created goblin HP {goblin * 2} and Outcast HP {outcast * 2}.");
        scriptPlayer.Stop();
        var task = scriptPlayer.PreloadAndPlayAsync(script, label: "Summon");
        while (!task.IsCompleted && Run.Running) yield return null;
        task.GetAwaiter().GetResult();
        yield return WaitForEntertainmentSmoke("cupSmokeCombatReady");
        if (!Run.Running) yield break;
        if (Number("cupSmokeSummonHealth") != goblin * 2 || Number("enemy3MaxHealth") != goblin * 2)
        {
            Fail(new InvalidOperationException($"Summoned goblin HP: {goblin}/{Read("cupSmokeSummonHealth")}/{Read("enemy3MaxHealth")}."));
            yield break;
        }

        WriteEntertainmentNumber("playerHealth", 100);
        WriteEntertainmentNumber("playerMaxHealth", 100);
        yield return VerifyEntertainmentDamageForSmoke(script, "enemy1", "player", "attack", "attack", 15);
        if (!Run.Running) yield break;
        yield return VerifyEntertainmentDamageForSmoke(script, "enemy1", "enemy1", "attack", "fire", 10);
        if (!Run.Running) yield break;

        WriteEntertainmentNumber("turnCount", 3);
        WriteEntertainmentNumber("enemy2StateEvading", 0);
        player!.Stop();
        task = player.PreloadAndPlayAsync(script, label: "Steal");
        while (!task.IsCompleted && Run.Running) yield return null;
        task.GetAwaiter().GetResult();
        yield return WaitForEntertainmentSmoke("cupSmokeStealReady");
        if (!Run.Running) yield break;
        if (Number("enemy2StolenWeaponCount") != 1 || Number("enemy2" + StolenWeaponFrom) != 4 ||
            Number("enemy2" + StolenWeaponUntil) != 4 || Number("player" + PendingStealActions) != 0)
        {
            Fail(new InvalidOperationException($"Native steal did not mark the correct target: {Read("stealEnemyWeaponSucceeded")}, " +
                $"{Read("enemy2StolenWeaponCount")}, expiry {Read("enemy2" + StolenWeaponUntil)}."));
            yield break;
        }
        yield return VerifyEntertainmentDamageForSmoke(script, "player", "enemy2", "attack", "attack", 10);
        if (!Run.Running) yield break;
        WriteEntertainmentNumber("turnCount", 4);
        yield return VerifyEntertainmentDamageForSmoke(script, "player", "enemy2", "attack", "attack", 20);
        if (!Run.Running) yield break;
        yield return VerifyEntertainmentDamageForSmoke(script, "player", "enemy1", "attack", "attack", 10);
        if (!Run.Running) yield break;

        // Give the fixture a second native steal on the same target to exercise the refresh.
        WriteEntertainmentNumber("enemy2StolenWeaponCount", 0);
        player!.Stop();
        task = player.PreloadAndPlayAsync(script, label: "Steal");
        while (!task.IsCompleted && Run.Running) yield return null;
        task.GetAwaiter().GetResult();
        yield return WaitForEntertainmentSmoke("cupSmokeStealReady");
        if (!Run.Running) yield break;
        if (Number("enemy2StolenWeaponCount") != 1 || Number("enemy2" + StolenWeaponFrom) != 4 ||
            Number("enemy2" + StolenWeaponUntil) != 5)
        {
            Fail(new InvalidOperationException("Repeating a native steal did not refresh the same target."));
            yield break;
        }
        yield return VerifyEntertainmentDamageForSmoke(script, "partner", "enemy2", "attack", "attack", 20);
        if (!Run.Running) yield break;
        HandleEntertainmentCommand(null!, "Battle", 3518);
        yield return VerifyEntertainmentDamageForSmoke(script, "player", "enemy2", "attack", "attack", 10);
        if (!Run.Running) yield break;
        WriteEntertainmentNumber("turnCount", 5);
        yield return VerifyEntertainmentDamageForSmoke(script, "player", "enemy2", "attack", "attack", 20);
        if (!Run.Running) yield break;
        HandleEntertainmentCommand(null!, "Battle", 3658);
        yield return VerifyEntertainmentDamageForSmoke(script, "player", "enemy2", "attack", "attack", 10);
        if (!Run.Running) yield break;
        LoggerInstance.Msg("Cup entertainment smoke passed: steal damage with extra actions off | current round = 10, next round = 20, other enemy = 10, refresh = 20, after party phase = 10.");
        Finish("smoke_entertainment_pass");
    }

    private bool ClickEntertainmentSmokeChoice(string callbackPart, bool invokeNativeHandler = false)
    {
        foreach (var choice in UnityEngine.Object.FindObjectsByType<ChoiceHandlerButton>(FindObjectsSortMode.None))
        {
            if (ActiveButton(choice) is not { } button || choice.ChoiceState is not { } state ||
                state.OnSelectScript is not { } callback ||
                !callback.Contains(callbackPart, StringComparison.Ordinal)) continue;
            if (invokeNativeHandler && choice.GetComponentInParent<ChoiceHandlerPanel>() is { } panel)
                _ = panel.HandleChoiceButtonClickedAsync(choice, state);
            else
                button.onClick.Invoke();
            return true;
        }
        return false;
    }

    private System.Collections.IEnumerator VerifyEntertainmentDamageForSmoke(
        Script script, string attacker, string target, string action, string damage, int expected)
    {
        player!.Stop();
        vars!.SetVariableValue("actionUserId", attacker);
        vars.SetVariableValue("actionTargetId", target);
        vars.SetVariableValue("actionTarget", "enemy");
        vars.SetVariableValue("actionType", action);
        vars.SetVariableValue("damageType", damage);
        WriteEntertainmentNumber("cupSmokeDamageReady", 0);
        var task = player.PreloadAndPlayAsync(script, label: "Damage");
        while (!task.IsCompleted && Run.Running) yield return null;
        task.GetAwaiter().GetResult();
        yield return WaitForEntertainmentSmoke("cupSmokeDamageReady");
        if (Run.Running && Number("cupSmokeDamageResult") != expected)
            Fail(new InvalidOperationException($"{attacker} -> {target} ({damage}), round {Read("turnCount")}: " +
                $"expected {expected} damage; observed {Read("cupSmokeDamageResult")}."));
    }

    private System.Collections.IEnumerator WaitForEntertainmentSmoke(string variable)
    {
        double deadline = Time.realtimeSinceStartupAsDouble + 12;
        while (Run.Running && !Flag(variable) && Time.realtimeSinceStartupAsDouble < deadline)
        {
            yield return null;
            AdvanceWeaponSmoke();
        }
        if (Run.Running && !Flag(variable))
        {
            var spot = player!.PlaybackSpot;
            Fail(new InvalidOperationException($"Timed out waiting for {variable} at {spot.ScriptName}:{spot.LineIndex}; " +
                $"playing={player.Playing}, waiting={player.WaitingForInput}, enemies={Read("enemyMaxCount")}."));
        }
    }
}
