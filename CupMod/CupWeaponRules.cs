using System.Globalization;
using HarmonyLib;
using Il2Cpp;
using Il2CppNaninovel;
using UnityEngine;

namespace CupCompetition;

public sealed partial class CupMod
{
    private bool WeaponRulesActive => Settings.MaxWeapons.HasValue || Settings.KickOnly;
    private bool stealAddPending;
    private bool stealAddActive;
    private bool playerKickSteal;
    private IntPtr drushSource;
    private IntPtr drushReplacement;
    private long openingWeaponCurse;
    private string weaponCurseNotice = "";
    private double weaponCurseNoticeUntil;

    private void InstallWeaponRulesHook()
    {
        HarmonyInstance.Patch(AccessTools.Method(typeof(Inventory), nameof(Inventory.AddItem), new[] { typeof(ItemData) }),
            prefix: new HarmonyMethod(typeof(CupMod), nameof(CheckWeaponAcquisition)),
            postfix: new HarmonyMethod(typeof(CupMod), nameof(TrackDrushReplacement)));
        HarmonyInstance.Patch(AccessTools.Method(typeof(ItemService), nameof(ItemService.MetamolizeItemBySlotNumber)),
            prefix: new HarmonyMethod(typeof(CupMod), nameof(CaptureMetamorphosisSource)),
            postfix: new HarmonyMethod(typeof(CupMod), nameof(CheckMetamorphosedWeapon)));
        HarmonyInstance.Patch(AccessTools.Method(typeof(ItemService), nameof(ItemService.CombineInventoryItems)),
            prefix: new HarmonyMethod(typeof(CupMod), nameof(CaptureCombinedWeapon)),
            postfix: new HarmonyMethod(typeof(CupMod), nameof(CheckCombinedWeapon)));
    }

    private void ObserveWeaponRules(string script, int line)
    {
        switch (script, line)
        {
            case ("First", 305) when !openingRulesApplied:
                // Opening invokes the same starter-inventory reset again before entering the dungeon.
                openingWeaponCurse = 0;
                break;
            case ("Action", 2246):
                stealAddPending = stealAddActive = false;
                int slot = Number("selectCommand");
                playerKickSteal = slot == -101 && Read("actionUserId") == "player" && Read("timing") == "battle";
                drushSource = IntPtr.Zero;
                if (Settings.MaxWeapons.HasValue && slot > 0 && Read("timing") == "battle" &&
                    Flag("battleBreakMode") && Number("ignoreBattleBreakModeDepth") == 0 &&
                    ItemCustomFunctions.GetItemDurabilityBySlotNumber(slot) > 0 &&
                    ItemCustomFunctions.GetItemBreakPreventionRemaining(slot) <= 0 &&
                    ItemCustomFunctions.GetItemTargetExEffectValue(slot, "priestFinalWeaponProtect") <= 0 &&
                    ItemCustomFunctions.IsItemBreakingForCurrentUse(slot))
                {
                    var source = Engine.GetService<ItemService>().GetItemBySlotNumber(slot);
                    if (IsWeapon(source)) drushSource = source!.Pointer;
                }
                break;
            case ("Action", 2287 or 2314):
                stealAddPending = true;
                break;
            case ("Func", 137):
                stealAddActive = stealAddPending && Read("addItem") == Read("enemyStealItem");
                stealAddPending = false;
                break;
            case ("Action", 2320):
                stealAddPending = stealAddActive = false;
                break;
            case ("Battle", 1610 or 3442 or 3458 or 3552):
            case ("Dungeon", 338):
            case ("GameOver", 0):
                SettleWeaponReplacement();
                break;
        }
    }

    private static bool IsWeapon(ItemData? item) => item != null && item.hasAttack();

    private static int CountWeapons(Inventory inventory)
    {
        int count = 0;
        foreach (var item in inventory.items)
            if (IsWeapon(item)) count++;
        return count;
    }

    private static bool CheckWeaponAcquisition(Inventory __instance, ItemData __0, ref int __result, out bool __state)
    {
        __state = false;
        if (Active is not { WeaponRulesActive: true } cup || !cup.Run.Running || !IsWeapon(__0))
            return true;

        if (!cup.Settings.KickOnly || cup.stealAddActive && cup.playerKickSteal)
        {
            if (cup.Settings.MaxWeapons is not int limit) return true;
            int weapons = 0;
            bool hasDrushSource = false;
            foreach (var item in __instance.items)
            {
                if (IsWeapon(item)) weapons++;
                if (cup.drushSource != IntPtr.Zero && item?.Pointer == cup.drushSource) hasDrushSource = true;
            }
            if (weapons < limit) return true;
            // The native action removes its old weapon after it processes the steal reward.
            if (weapons == limit && cup.stealAddActive && hasDrushSource && cup.drushReplacement == IntPtr.Zero)
            {
                __state = true;
                return true;
            }
        }

        cup.ConvertWeaponToCurse(__0);
        __result = -1;
        return false;
    }

    private static void TrackDrushReplacement(Inventory __instance, int __result, bool __state)
    {
        if (__state && __result > 0 && Active is { } cup)
            cup.drushReplacement = __instance.GetItemBySlotNumber(__result)?.Pointer ?? IntPtr.Zero;
    }

    // These native transformations write an inventory slot directly instead of calling Inventory.AddItem.
    private static void CaptureMetamorphosisSource(ItemService __instance, int __0, out IntPtr __state) =>
        __state = Active is { WeaponRulesActive: true, Run.Running: true }
            ? __instance.GetItemBySlotNumber(__0)?.Pointer ?? IntPtr.Zero : IntPtr.Zero;

    private static void CheckMetamorphosedWeapon(ItemService __instance, int __0, IntPtr __state)
    {
        if (__state == IntPtr.Zero || Active is not { } cup) return;
        var weapon = __instance.GetItemBySlotNumber(__0);
        if (IsWeapon(weapon) && weapon!.Pointer != __state &&
            (cup.Settings.KickOnly || cup.Settings.MaxWeapons is int cap && CountWeapons(__instance.inventory) > cap))
            cup.DestroyAcquiredWeapon(__instance, __0, weapon);
    }

    private static void CaptureCombinedWeapon(ItemService __instance, int __0, int __1, out bool __state) =>
        __state = Active is { WeaponRulesActive: true, Run.Running: true } &&
            (IsWeapon(__instance.GetItemBySlotNumber(__0)) || IsWeapon(__instance.GetItemBySlotNumber(__1)));

    private static void CheckCombinedWeapon(ItemService __instance, int __0, int __1, int __result, bool __state)
    {
        if (__result <= 0 || Active is not { WeaponRulesActive: true, Run.Running: true } cup) return;
        int slot = __1 < __0 ? __0 - 1 : __0;
        var weapon = __instance.GetItemBySlotNumber(slot);
        if (IsWeapon(weapon) && (cup.Settings.KickOnly && !__state ||
            cup.Settings.MaxWeapons is int cap && CountWeapons(__instance.inventory) > cap))
            cup.DestroyAcquiredWeapon(__instance, slot, weapon!);
    }

    private void SettleWeaponReplacement()
    {
        if (drushReplacement == IntPtr.Zero) return;
        IntPtr replacement = drushReplacement;
        drushReplacement = IntPtr.Zero;
        var service = Engine.GetService<ItemService>();
        int replacementSlot = 0, weapons = 0;
        for (int slot = 1; slot <= service.inventory.items.Count; slot++)
        {
            var item = service.GetItemBySlotNumber(slot);
            if (!IsWeapon(item)) continue;
            weapons++;
            if (item!.Pointer == replacement) replacementSlot = slot;
        }
        if (replacementSlot == 0 || Settings.MaxWeapons is not int limit || weapons <= limit) return;
        DestroyAcquiredWeapon(service, replacementSlot, service.GetItemBySlotNumber(replacementSlot));
    }

    private void DestroyAcquiredWeapon(ItemService service, int slot, ItemData item)
    {
        service.RemoveItemBySlotNumber(slot);
        ConvertWeaponToCurse(item);
    }

    private void ConvertWeaponToCurse(ItemData item)
    {
        long value = ItemService.GetItemGainCurse(item);
        if (item.resourceName == "warriorHead")
            value += Read("timing") == "battle" && Read("battleTarget") == "villainess" ? 50 : 30;
        // Many ordinary weapons have zero native break value until reinforced or worn down.
        if (value <= 0) value = Math.Max(1, item.price);
        if (!openingRulesApplied && Target?.Id != "ritual")
            openingWeaponCurse += value;
        else
        {
            AddWeaponCurse("curse", value);
            AddWeaponCurse("curseCount", value);
        }
        weaponCurseNotice = $"{item.GetItemName()}碎成诅咒 +{value}";
        weaponCurseNoticeUntil = Time.realtimeSinceStartupAsDouble + 3.5;
        LoggerInstance.Msg($"Cup weapon converted | {item.resourceName} | curse=+{value}");
    }

    private void AddWeaponCurse(string key, long value) =>
        vars!.SetVariableValue(key, (ReadCurse(key) + value).ToString("R", CultureInfo.InvariantCulture));

    private double ReadCurse(string key) =>
        double.TryParse(Read(key), NumberStyles.Float, CultureInfo.InvariantCulture, out double value) ? value : 0;
}
