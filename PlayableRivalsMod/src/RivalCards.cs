using HarmonyLib;
using Il2Cpp;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppNaninovel;
using UnityEngine;

namespace PlayableRivals;

public sealed partial class RivalMod
{
    private const string CardPrefix = "rivals_";
    private readonly Dictionary<Card, ItemData> cards = new();

    private static string CardName(Card card) => card switch
    {
        Card.Sword => "毁灭魔剑", Card.Dagger => "匕首", Card.Thread => "人偶之线",
        Card.CodexWeapon => "魔力法典", Card.Punch => "物理攻击",
        Card.Steal => "抢夺准备", Card.Switch => "切换匕首", Card.Enchant => "元素附魔",
        Card.Fire => "火附魔", Card.Freeze => "冰附魔", Card.Thunder => "雷附魔", Card.Poison => "毒附魔",
        Card.Love => "对战士的爱", Card.Dominate => "傀儡", Card.Doll => "召唤傀儡",
        Card.Codex => "法典", Card.Barrier => "召唤屏障", Card.Armor => "覆甲",
        _ => ""
    };

    private static string? OriginalIcon(Card card) => card switch
    {
        Card.Sword => "destructionMagicSword", Card.Dagger or Card.Switch => "hunterDagger",
        Card.Thread => "doll", Card.CodexWeapon => "magiaTakt", Card.Punch => "default",
        Card.Steal => "hookRope", Card.Love => "warriorHead", Card.Dominate => "dominationWand",
        Card.Codex => "spellBook", _ => null
    };

    private static string? NewIcon(Card card) => card switch
    {
        Card.Enchant => "icons/elemental_enchant", Card.Barrier => "icons/summon_barrier",
        Card.Armor => "icons/armor_transfer", _ => null
    };

    private static bool TryCard(string? resource, out Card card)
    {
        card = default;
        return resource != null && resource.StartsWith(CardPrefix, StringComparison.Ordinal) &&
            Enum.TryParse(resource[CardPrefix.Length..], out card) && Enum.IsDefined(card);
    }

    private static bool TryCard(int number, out Card card)
    {
        card = (Card)number;
        return number is <= -7101 and >= -7144 && Enum.IsDefined(card);
    }

    private void InstallCardHooks()
    {
        Patch(typeof(ItemService), nameof(ItemService.GetItemBySlotNumber), nameof(ItemBySlot), types: new[] { typeof(int) });
        Patch(typeof(Inventory), nameof(Inventory.GetItemBySlotNumber), nameof(ItemBySlot), types: new[] { typeof(int) });
        Patch(typeof(ItemService), nameof(ItemService.FindItemDataById), nameof(ItemBySlot), types: new[] { typeof(int) });
        Patch(typeof(ItemService), nameof(ItemService.FindItemDataByResourceName), nameof(ItemByResource), types: new[] { typeof(string) });
        Patch(typeof(ItemService), nameof(ItemService.FindExactItemDataByResourceName), nameof(ItemByResource), types: new[] { typeof(string) });
        Patch(typeof(ItemData), nameof(ItemData.GetItemName), nameof(ItemName));
        Patch(typeof(ItemService), nameof(ItemService.GetItemName), nameof(ItemNameByResource));
        Patch(typeof(ItemData), nameof(ItemData.GetDisplayIconResourceName), nameof(DisplayIcon));
        Patch(typeof(ItemService), nameof(ItemService.GetItemIconSpriteAsync), nameof(LoadCardIcon));
        Patch(typeof(ItemData), nameof(ItemData.canStealItem), nameof(CanSteal));
        InstallRobberySectionHooks();
        Patch(typeof(NaniFunctions), "IsMagicUnavailable", nameof(MagicProfile), postfix: true);
        Patch(typeof(ItemCustomFunctions), nameof(ItemCustomFunctions.IsActionUnavailableForMagic),
            nameof(UnavailableMagic), postfix: true);
        Patch(typeof(ItemCustomFunctions), nameof(ItemCustomFunctions.HasUnavailableMagicEffects),
            nameof(MagicFailureMessage), postfix: true);
    }

    private static void MagicProfile(string __0, ref bool __result)
    {
        if (active is { BattleRival: Rival.Dropout } mod && __0 == "partner")
            __result = mod.NoMagic;
    }

    private static void MagicFailureMessage(int __0, string __1, ref bool __result)
    {
        if (active is { BattleRival: Rival.Dropout } && __1 == "partner")
            __result = false;
    }

    private static bool ItemBySlot(int __0, ref ItemData __result)
    {
        if (!TryCard(__0, out var card) || active is not { } mod) return true;
        __result = mod.GetCard(card);
        return false;
    }

    private static bool ItemByResource(string __0, ref ItemData __result)
    {
        if (active is not { } mod || !TryCard(__0, out var card)) return true;
        __result = mod.GetCard(card);
        return false;
    }

    private static bool ItemName(ItemData __instance, ref string __result) =>
        ItemNameByResource(__instance.resourceName, ref __result);

    private static bool ItemNameByResource(string __0, ref string __result)
    {
        if (!TryCard(__0, out var card)) return true;
        __result = CardName(card);
        return false;
    }

    private static bool DisplayIcon(ItemData __instance, ref string __result)
    {
        if (!TryCard(__instance.resourceName, out var card)) return true;
        __result = OriginalIcon(card) ?? __instance.resourceName;
        return false;
    }

    private static bool CanSteal(ItemData __instance, ref bool __result)
    {
        if (active is { } mod && mod.PreparedWeaponCanSteal(__instance))
        {
            __result = true;
            return false;
        }
        if (!TryCard(__instance.resourceName, out _)) return true;
        __result = false;
        return false;
    }

    // Do not add effects to shared inventory items. Native forbidding effects take precedence.
    private bool PreparedWeaponCanSteal(ItemData? item) =>
        BattleRival == Rival.Outcast && StealPrepared &&
        (robberySectionActor ?? Read("turnCharacter")) == "partner" && IsWeaponAttack(item) &&
        (TryCard(item!.resourceName, out var card) && card is Card.Sword or Card.Dagger ||
         Engine.GetService<ItemService>().GetItemTargetExEffectValue(item!, nameof(ExEffect.cantStealItem)) <= 0);

    private static bool IsWeaponAttack(ItemData? item) => item != null &&
        (!TryCard(item.resourceName, out var card) || card is Card.Sword or Card.Dagger) &&
        item.HasDirectEnemyAttackForCurrentUse(null, false);

    [ThreadStatic]
    private static bool buildingRobberySections;
    [ThreadStatic]
    private static string? robberySectionActor;
    private readonly record struct RobberyContext(bool Building, string? Actor);

    private void InstallRobberySectionHooks()
    {
        var initialize = AccessTools.Method(typeof(AttackBar), nameof(AttackBar.Initialize),
            new[] { typeof(ItemData), typeof(bool), typeof(int), typeof(string), typeof(AttackBarPreviewOptions) });
        HarmonyInstance.Patch(initialize, prefix: new HarmonyMethod(typeof(RivalMod), nameof(BeginRobberySections)),
            finalizer: new HarmonyMethod(typeof(RivalMod), nameof(EndRobberySections)));
        Patch(typeof(EnemyService), nameof(EnemyService.CanStealItemFromEnemy),
            nameof(AllowPreparedRobberyTarget), postfix: true, types: new[] { typeof(string) });
    }

    private static void BeginRobberySections(ItemData __0, string __3, out RobberyContext __state)
    {
        __state = new(buildingRobberySections, robberySectionActor);
        robberySectionActor = string.IsNullOrEmpty(__3) ? active?.Read("turnCharacter") : __3;
        buildingRobberySections = active is { BattleRival: Rival.Outcast, StealPrepared: true } mod &&
            (string.IsNullOrEmpty(__3) ? mod.Read("turnCharacter") : __3) == "partner" &&
            mod.PreparedWeaponCanSteal(__0);
    }

    private static void EndRobberySections(RobberyContext __state)
    {
        buildingRobberySections = __state.Building;
        robberySectionActor = __state.Actor;
    }

    private static void AllowPreparedRobberyTarget(EnemyService __instance, string __0, ref bool __result)
    {
        if (__result || !buildingRobberySections || active is not { } mod ||
            !int.TryParse(__0, out int number) || number < 1) return;
        string enemy = "enemy" + number;
        if (mod.Number(enemy + "Break") <= 0 || mod.Number(enemy + "MaxBreak") <= 0 ||
            mod.Flag(enemy + "Death") || __instance.GetEnemyControllerById(number) == null ||
            __instance.GetEnemyData(number) is not { } data) return;
        __result = EnemyStealItemResolver.HasRemaining(data, mod.Number(enemy + "FormNum"),
            mod.Number(enemy + "StolenWeaponCount"));
    }

    private static void UnavailableMagic(int __0, int __1, string __2, ref bool __result)
    {
        if (active is not { BattleRival: Rival.Dropout } mod || __2 != "partner") return;
        if (TryCard(__0, out var card))
        {
            __result = mod.NoMagic && (card == Card.CodexWeapon || RivalRules.GetSkill(card).Magic);
            return;
        }
        if (mod.NoMagic)
        {
            var item = Engine.GetService<ItemService>().GetItemBySlotNumber(__0);
            var set = item?.battleActionSetData;
            if (set != null && __1 >= 0 && __1 < set.actionStatusCalcs.Length)
                __result = set.IsMagic(__1);
        }
    }

    private static bool LoadCardIcon(string __0, ref UniTask<Sprite> __result)
    {
        if (active is not { } mod || !TryCard(__0, out var card)) return true;
        var local = NewIcon(card);
        local ??= card switch
        {
            Card.Doll => "icons/summon", Card.Fire => "icons/fire", Card.Freeze => "icons/freeze",
            Card.Thunder => "icons/paralysis", Card.Poison => "icons/poison", _ => null
        };
        if (local != null && mod.FindArt(local) is { } icon)
        {
            __result = UniTask.FromResult(icon);
            return false;
        }
        var vanilla = card switch
        {
            Card.Doll => "summon", Card.Fire => "fire", Card.Freeze => "freeze",
            Card.Thunder => "paralysis", Card.Poison => "poison", _ => OriginalIcon(card)
        };
        if (vanilla == null) return true;
        __result = Engine.GetService<ItemService>().GetItemIconSpriteAsync(vanilla);
        return false;
    }

    private ItemData GetCard(Card card)
    {
        if (cards.TryGetValue(card, out var found)) return found;
        var id = CardPrefix + card;
        var actionSet = new ActionSetData
        {
            name = id,
            actionNames = new Il2CppStructArray<ActionType>(0),
            actionTargets = new Il2CppStructArray<TargetType>(0),
            actionValues = new Il2CppStructArray<float>(0),
            actionStatusCalcs = new Il2CppStringArray(0),
            exEffects = new Il2CppStringArray(0),
            exEffectOptions = new Il2CppStringArray(0),
            exEffectStatusCalcs = new Il2CppStringArray(0),
            exConditions = new Il2CppStructArray<Condition>(0),
            exConditionStatusCalcs = new Il2CppStringArray(0),
            exConditionOptions = new Il2CppStringArray(0),
            customProperties = new()
        };
        var item = new ItemData
        {
            resourceName = id,
            id = (int)card,
            actions = new Il2CppStringArray(0),
            actionValues = new Il2CppStructArray<float>(0),
            battleActionSet = id,
            battleActionSetData = actionSet,
            useLimit = -1,
            barSetting = "",
            easing = "",
            attackCount = card is Card.Sword or Card.Dagger or Card.Thread or Card.Punch ? 1 : 0,
            cursorWidth = card == Card.Thread ? 3000 : 100,
            dungeonMinMax = new Il2CppStructArray<int>(0),
            reinforces = new(),
            reinforcedExEffectIndices = new()
        };
        actionSet.parentItem = item;
        void A(ActionType action, TargetType target = TargetType.myself, int value = 0, string formula = "0") =>
            actionSet.AddActionProperties(action, value, target, formula);
        switch (card)
        {
            case Card.Sword: A(ActionType.attack, TargetType.enemy, value: 18, formula: "str"); break;
            case Card.Dagger: A(ActionType.attack, TargetType.enemy); break;
            case Card.Thread: A(ActionType.rangeAttack, TargetType.enemy); break;
            case Card.Punch: A(ActionType.attack, TargetType.enemy); break;
            case Card.CodexWeapon: A(ActionType.block, TargetType.choiceAlly); break;
            case Card.Love:
                A(ActionType.fire, TargetType.enemyAll);
                A(ActionType.poison, TargetType.enemyAll, 1);
                A(ActionType.heal);
                // The native battle script snapshots dontUseTurn before resolving the healing.
                actionSet.AddExProperties(ExEffect.dontUseTurn, "1", "", Condition.HpBelowPer, "70");
                break;
            case Card.Dominate: A(ActionType.domination, TargetType.choiceEnemy, 1); break;
            case Card.Codex:
                A(ActionType.barrier, TargetType.choiceAlly, 2);
                A(ActionType.vulnerable, value: 1);
                break;
            case Card.Armor:
                A(ActionType.block, TargetType.player);
                A(ActionType.blockProtect, TargetType.player, 1);
                break;
            default: A(ActionType.heal); break;
        }
        if (RivalRules.GetSkill(card).Free) actionSet.AddExProperties(ExEffect.dontUseTurn);
        cards.Add(card, item);
        return item;
    }

    private void RefreshCards()
    {
        var stats = CurrentStats();
        int lv = Number("partnerLv");
        GetCard(Card.Sword).battleActionSetData.actionValues[0] = 18;
        foreach (var card in new[] { Card.Dagger, Card.Thread, Card.CodexWeapon, Card.Punch, Card.Armor })
            GetCard(card).battleActionSetData.actionValues[0] = RivalRules.Amount(card, lv, stats);
        var love = GetCard(Card.Love).battleActionSetData.actionValues;
        love[0] = RivalRules.ElementStacks("fire", stats.Mag);
        love[2] = RivalRules.Amount(Card.Love, lv, stats);
    }

    private void UpdateStealEffects()
    {
        foreach (var card in new[] { Card.Sword, Card.Dagger })
        {
            var set = GetCard(card).battleActionSetData;
            set.exEffects = new Il2CppStringArray(1);
            set.exEffectOptions = new Il2CppStringArray(1);
            set.exEffectStatusCalcs = new Il2CppStringArray(1);
            set.exConditions = new Il2CppStructArray<Condition>(1);
            set.exConditionStatusCalcs = new Il2CppStringArray(1);
            set.exConditionOptions = new Il2CppStringArray(1);
            set.exEffects[0] = StealPrepared ? nameof(ExEffect.canStealWithoutBreaking) : nameof(ExEffect.cantStealItem);
            set.exEffectOptions[0] = "";
            set.exEffectStatusCalcs[0] = "1";
            set.exConditionStatusCalcs[0] = "0";
            set.exConditionOptions[0] = "";
        }
    }
}
