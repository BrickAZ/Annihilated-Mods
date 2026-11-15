using Il2Cpp;
using Il2CppNaninovel;
using Il2CppNaninovel.UI;
using UnityEngine;

namespace CupCompetition;

public sealed partial class CupMod
{
    private void VerifyWeaponOpeningForSmoke()
    {
        var items = Engine.GetService<ItemService>();
        if (CountWeapons(items.inventory) != 0 || items.GetItemBySlotNumber(1)?.resourceName != "potion" ||
            ReadCurse("curse") != 110 || ReadCurse("curseCount") != 110 || !Flag("xSwitchMageOutWay"))
        {
            Fail(new InvalidOperationException($"Kick-only opening: first item={items.GetItemBySlotNumber(1)?.resourceName}; curse={Read("curse")}/{Read("curseCount")}."));
            return;
        }
        LoggerInstance.Msg("Cup weapon opening smoke passed: starting iron sword converted, potion and companion skill kept, configured curse credited.");
        Finish("smoke_weapon_opening_pass");
    }

    private System.Collections.IEnumerator VerifyJumpForSmoke()
    {
        var maps = Engine.GetService<MapService>();
        double deadline = Time.realtimeSinceStartupAsDouble + 8;
        int dungeon = jumpSmokeFloor == 6 ? 10 : jumpSmokeFloor == 5 ? 8 : jumpSmokeFloor;
        var items = Engine.GetService<ItemService>();
        while (Run.Running && Time.realtimeSinceStartupAsDouble < deadline)
        {
            yield return null;
            if (maps.mapChoiceButtonContainer?.GetComponentsInChildren<UnityEngine.UI.Button>()
                .Any(button => button.isActiveAndEnabled && button.IsInteractable()) != true) continue;
            if (maps.map.dungeonId != dungeon || maps.map.sectionId != 1 ||
                items.GetItemBySlotNumber(1)?.resourceName != "ironSword" ||
                (Read("partnerCharacter") == "mage" && !Flag("xSwitchMageOutWay")))
            {
                Fail(new InvalidOperationException($"Jump opened the wrong map or lost native setup: {maps.map.dungeonId}/{maps.map.sectionId}, {Read("partnerCharacter")}."));
                yield break;
            }
            int level = 1 + (jumpSmokeFloor - 1) * 10;
            var party = new[] { "player", "partner" };
            if (party.Any(member => Number(member + "Lv") != level || Number(member + "Exp") != 0 ||
                    Number(member + "NextExp") <= 0 || Number(member + "NextExp") != Number(member + "MaxNextExp") ||
                    Number(member + "Health") <= 0 || Number(member + "Health") != Number(member + "MaxHealth")) ||
                Number("playerMaxHealthAdd") + Number("playerStrAdd") + Number("playerDexAdd") +
                    Number("playerAgiAdd") + Number("playerMagAdd") <= 0 ||
                Number($"partner{Read("partnerCharacter")}Lv") != level)
            {
                Fail(new InvalidOperationException($"Jump level mismatch: expected={level}; " +
                    $"player={Read("playerLv")}/{Read("playerNextExp")}/{Read("playerHealth")}; " +
                    $"partner={Read("partnerLv")}/{Read("partnerNextExp")}/{Read("partnerHealth")}."));
                yield break;
            }
            LoggerInstance.Msg($"Cup jump smoke passed | floor={jumpSmokeFloor} | playable map={dungeon}/1 | " +
                $"both level={level} | player HP={Read("playerMaxHealth")} | partner HP={Read("partnerMaxHealth")}");
            Finish($"smoke_floor_{jumpSmokeFloor}_pass");
            yield break;
        }
        Fail(new InvalidOperationException($"Jump map not playable at {player!.PlaybackSpot.ScriptName}:{player.PlaybackSpot.LineIndex}."));
    }

    private System.Collections.IEnumerator VerifyWeaponRulesForSmoke()
    {
        const string name = "AnnihilatedCupWeaponSmoke";
        var items = Engine.GetService<ItemService>();
        double before = ReadCurse("curse");
        if (items.AddItem("club") > 0 || ReadCurse("curse") <= before || items.AddItem("potion") <= 0)
        {
            Fail(new InvalidOperationException("Overflow or non-weapon acquisition did not follow the cap."));
            yield break;
        }

        player!.Stop();
        // Use the real event and enemy-weapon rewards. Seed the two action contexts explicitly.
        var script = Script.FromScriptText(name, @"
@clearChoice * wait:false
@set addItem=""club""
@gosub Dungeon.GetEventItem
@set cupEventRejected=itemAddedIndex<=0
@set timing=""battle""
@set actionUserId=""player""
@set selectCommand=1
@set battleBreakMode=true
@set ignoreBattleBreakModeDepth=0
@set stealTargetId=""enemy1""
@set stealUseExistingEnemyStealItem=false
@set stealUseMaxCount=false
@set stealSpecialOpacity=false
@set stealReduceLimitRate=0
@set enemy1StolenWeaponCount=0
@gosub Action.TryStealEnemyWeapon
@set cupDrushKept=itemAddedIndex>0
@set breakItemNumber=1
@set forceItemBreak=true
@gosub BreakItem
@set cupDrushDone=1
@stop
# Kick
@clearChoice * wait:false
@set actionUserId=""partner""
@set selectCommand=-101
@set enemy1StolenWeaponCount=0
@gosub Action.TryStealEnemyWeapon
@set cupPartnerRejected=itemAddedIndex<=0
@set actionUserId=""player""
@set selectCommand=-101
@set enemy1StolenWeaponCount=0
@gosub Action.TryStealEnemyWeapon
@set cupKickKept=itemAddedIndex>0
@set cupKickDone=1
@stop", (string?)null);
        RegisterCupScript(name, script);
        var playback = player.PreloadAndPlayAsync(script);
        while (!playback.IsCompleted && Run.Running) yield return null;
        playback.GetAwaiter().GetResult();
        double deadline = Time.realtimeSinceStartupAsDouble + 20;
        while (Run.Running && !Flag("cupDrushDone") && Time.realtimeSinceStartupAsDouble < deadline)
        {
            yield return null;
            AdvanceWeaponSmoke();
        }
        SettleWeaponReplacement();
        // Removing the sword can shift the stolen club out of the slot recorded by AddItem.
        int keptSlot = Enumerable.Range(1, items.inventory.items.Count).FirstOrDefault(slot =>
            items.GetItemBySlotNumber(slot)?.resourceName == Read("enemyStealItem"));
        if (!Flag("cupDrushDone") || !Flag("cupEventRejected") || !Flag("cupDrushKept") ||
            keptSlot <= 0 || CountWeapons(items.inventory) != 1)
        {
            string contents = string.Join(",", Enumerable.Range(1, items.inventory.items.Count)
                .Select(slot => items.GetItemBySlotNumber(slot)?.resourceName));
            Fail(new InvalidOperationException($"Weapon cap smoke at {player.PlaybackSpot.ScriptName}:{player.PlaybackSpot.LineIndex}: event={Read("cupEventRejected")}; drush={Read("cupDrushKept")}; done={Read("cupDrushDone")}; kept={keptSlot}; items={contents}."));
            yield break;
        }

        items.RemoveItemBySlotNumber(keptSlot);
        Settings = Settings with { KickOnly = true };
        before = ReadCurse("curse");
        if (items.AddItem("ironSword") > 0 || ReadCurse("curse") <= before)
        {
            Fail(new InvalidOperationException("Kick-only allowed a weapon from an ordinary source despite a free slot."));
            yield break;
        }
        var kick = player.PreloadAndPlayAsync(script, label: "Kick");
        while (!kick.IsCompleted && Run.Running) yield return null;
        kick.GetAwaiter().GetResult();
        deadline = Time.realtimeSinceStartupAsDouble + 20;
        while (Run.Running && !Flag("cupKickDone") && Time.realtimeSinceStartupAsDouble < deadline)
        {
            yield return null;
            AdvanceWeaponSmoke();
        }
        if (!Flag("cupKickDone") || !Flag("cupPartnerRejected") || !Flag("cupKickKept") ||
            !IsWeapon(items.GetItemBySlotNumber(Number("itemAddedIndex"))))
        {
            Fail(new InvalidOperationException($"Kick smoke at {player.PlaybackSpot.ScriptName}:{player.PlaybackSpot.LineIndex}: partner={Read("cupPartnerRejected")}; kick={Read("cupKickKept")}; done={Read("cupKickDone")}."));
            yield break;
        }
        LoggerInstance.Msg("Cup weapon rules smoke passed: cap overflow, native event, potion, Drush replacement, kick-only ordinary source, partner rejection, player kick.");
        Finish("smoke_weapon_rules_pass");
    }

    private void AdvanceWeaponSmoke()
    {
        foreach (var choice in UnityEngine.Object.FindObjectsByType<ChoiceHandlerButton>(FindObjectsSortMode.None))
        {
            if (ActiveButton(choice) is not { } button ||
                choice.ChoiceState?.OnSelectScript?.Contains("ShowStolenItemMessageSelected", StringComparison.Ordinal) != true)
                continue;
            button.onClick.Invoke();
            break;
        }
        if (player!.WaitingForInput)
            Engine.GetService<IInputManager>().GetSampler(InputConfiguration.ContinueName).Activate(1f);
    }
}
