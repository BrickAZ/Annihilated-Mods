using Il2Cpp;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppNaninovel;
using System.Globalization;

namespace PlayableRivals;

public sealed partial class RivalMod
{
    [ThreadStatic]
    private static bool buildingNativePresentation;

    private ItemPresentationResult GetNativeCardPresentation(Card card, ItemData item, string actor, string purpose)
    {
        // Display-only effects let the game describe the cards without adding extra combat actions.
        var display = item.Clone().Cast<ItemData>();
        var set = item.battleActionSetData.Clone();
        display.battleActionSetData = set;
        set.parentItem = display;
        if (UsesModAction(card))
        {
            set.actionNames = new Il2CppStructArray<ActionType>(0);
            set.actionTargets = new Il2CppStructArray<TargetType>(0);
            set.actionValues = new Il2CppStructArray<float>(0);
            set.actionStatusCalcs = new Il2CppStringArray(0);
        }
        else if (!IsRivalBattle)
        {
            var values = set.actionValues.ToArray();
            var formulas = set.actionStatusCalcs.ToArray();
            var stats = RivalRules.GetStats(CardOwner(card), 1);
            if (card is Card.Sword or Card.Dagger or Card.Thread or Card.CodexWeapon or Card.Punch or Card.Armor)
            {
                values[0] = RivalRules.Amount(card, 1, stats);
                formulas[0] = "0";
            }
            if (card == Card.Love)
            {
                values[0] = RivalRules.ElementStacks("fire", stats.Mag);
                values[2] = RivalRules.Amount(card, 1, stats);
            }
            set.actionValues = new Il2CppStructArray<float>(values);
            set.actionStatusCalcs = new Il2CppStringArray(formulas);
        }

        var skill = RivalRules.GetSkill(card);
        if (skill.Cooldown > 0 && !IsRivalBattle)
            set.AddExProperties(ExEffect.magicCooldown, skill.Cooldown.ToString(CultureInfo.InvariantCulture));
        if (skill.Front && IsRivalBattle && Read("forwardCharacter") != actor)
            set.AddExProperties(ExEffect.cantUse, "1", "", Condition.IsBackwardPosition, "1");
        if (card is Card.Sword or Card.Dagger)
            set.AddExProperties(ExEffect.stealTargetStatesEach, "2", "Group_Buff");

        var items = Engine.GetService<ItemService>();
        var request = new ItemPresentationRequest(display, "rivals-native:" + card, actor, purpose, false);
        buildingNativePresentation = true;
        try
        {
            return items.GetOrBuildPresentation(request);
        }
        finally
        {
            buildingNativePresentation = false;
        }
    }

    private static bool UsesModAction(Card card) => card is Card.Steal or Card.Switch or Card.Enchant or
        Card.Fire or Card.Freeze or Card.Thunder or Card.Poison or Card.Doll or Card.Barrier;
}
