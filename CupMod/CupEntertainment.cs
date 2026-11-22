using System.Globalization;
using Il2Cpp;
using Il2CppNaninovel;

namespace CupCompetition;

public sealed partial class CupMod
{
    private const string CompletedPartyRound = "cupEntertainmentCompletedPartyRound";
    private const string StolenWeaponFrom = "CupStolenWeaponFrom";
    private const string StolenWeaponUntil = "CupStolenWeaponUntil";
    private const string PendingStealActions = "CupPendingStealActions";
    private const string BaseEnemyMaxHealth = "CupBaseMaxHealth";
    private const string BonfireCombinationIndex = "cupEntertainmentBonfireCombinationIndex";
    private const string BonfireCombinationAvailable = "cupEntertainmentBonfireCombinationAvailable";
    private bool TracksStealDamage => Settings.StolenWeaponDamageMultiplier != 1;

    private void HandleEntertainmentCommand(Command command, string script, int line)
    {
        switch ((script, line))
        {
            case ("Dungeon", 928) when Settings.FixedWeaponCombination &&
                Flag("restReinforceActive") && !Flag("restReinforceReturnToCaller") &&
                Read(BonfireCombinationIndex) == Read("i") && !Flag(BonfireCombinationAvailable):
                vars!.SetVariableValue("cifText", "[cif=false]");
                break;

            // The action loop, including free actions and solo takeover, converges here.
            // The second entry covers native branches that jump straight to the enemy phase.
            case ("Battle", 3476) when Settings.ExtraActionAfterSteal:
                GrantPendingStealAction();
                break;

            case ("Battle", 3518 or 3658) when TracksStealDamage || Settings.ExtraActionAfterSteal:
                WriteEntertainmentNumber(CompletedPartyRound, Number("turnCount"));
                if (Settings.ExtraActionAfterSteal) ClearPendingStealActions();
                break;

            case ("Action", 778) when TracksStealDamage || Settings.EnemyAttackMultiplier != 1:
                ApplyEntertainmentDamage();
                break;

            case ("Battle", 14333) when Settings.EnemyHealthMultiplier != 1:
                MultiplyEntertainmentNumber("tempWarriorFinalHealthGrowth", Settings.EnemyHealthMultiplier);
                break;
        }
    }

    private void FinishEntertainmentCommand(Command command, string script, int line)
    {
        switch ((script, line))
        {
            case ("Dungeon", 667) when Settings.FixedWeaponCombination:
                if (!Flag("restReinforceTargetAvailable"))
                {
                    FindBonfireCombination(Engine.GetService<ItemService>(), out bool eligible);
                    if (eligible) vars!.SetVariableValue("restReinforceTargetAvailable", "true");
                }
                break;

            case ("Dungeon", 846 or 898) when Settings.FixedWeaponCombination &&
                Flag("restReinforceActive") && !Flag("restReinforceReturnToCaller"):
                AppendBonfireCombination();
                break;

            case ("Battle", 47) when TracksStealDamage || Settings.ExtraActionAfterSteal:
                WriteEntertainmentNumber(CompletedPartyRound, 0);
                if (Settings.ExtraActionAfterSteal) ClearPendingStealActions();
                break;

            case ("Battle", 5385) when TracksStealDamage || Settings.EnemyHealthMultiplier != 1:
                ApplyNewEnemyHealth();
                break;

            // This phase gives the existing Dropout slot a new pool of HP.
            case ("Battle", 4410) when Settings.EnemyHealthMultiplier != 1:
                MultiplyEntertainmentNumber("tempAdd", Settings.EnemyHealthMultiplier);
                break;

            // Both transformations add the former shape's maximum HP to the next shape.
            // Hand the native spawn calculation the former shape's original maximum.
            case ("Battle", 11166) when Settings.EnemyHealthMultiplier != 1:
                int carriedHealth = Number("tempSlimeTransitionSourceHealth");
                if (carriedHealth > 0)
                {
                    int sourceSlot = Number("tempSlimeTransitionSourceId");
                    int originalMaximum = Number($"enemy{sourceSlot}{BaseEnemyMaxHealth}");
                    WriteEntertainmentNumber("tempSlimeTransitionSourceHealth",
                        Math.Clamp(Math.Floor(carriedHealth / Settings.EnemyHealthMultiplier), 1,
                            originalMaximum > 0 ? originalMaximum : int.MaxValue));
                }
                break;

            case ("Battle", 11167) when Settings.EnemyHealthMultiplier != 1:
                RestoreSpawnMaxHealth("tempSlimeTransitionSourceId", "tempSlimeTransitionSourceMaxHealth");
                break;

            case ("Battle", 9113) when Settings.EnemyHealthMultiplier != 1:
                RestoreSpawnMaxHealth("xVillainessWithDollSlot", "enemyCustomMaxHealthAddValue");
                break;

            // The native subroutine sets this only after resource and steal-limit checks.
            case ("Action", 2262) when (TracksStealDamage || Settings.ExtraActionAfterSteal) && Read("timing") == "battle":
                RecordSuccessfulWeaponSteal();
                break;
        }
    }

    private void RecordSuccessfulWeaponSteal()
    {
        string actor = Read("actionUserId"), target = Read("stealTargetId");
        int round = Number("turnCount");
        if (actor is not ("player" or "partner") || !IsEntertainmentEnemy(target) ||
            Read(target + "BombSummonSide") == "ally" || round < 1) return;

        if (TracksStealDamage)
        {
            // A fresh steal applies next round. A refresh keeps any already active window.
            WriteEntertainmentNumber(target + StolenWeaponFrom, IsStolenWeaponVulnerable(target) ? round : round + 1);
            WriteEntertainmentNumber(target + StolenWeaponUntil, round + 1);
        }
        if (Settings.ExtraActionAfterSteal && Number(CompletedPartyRound) < round)
            WriteEntertainmentNumber(actor + PendingStealActions, Number(actor + PendingStealActions) + 1);
    }

    private void GrantPendingStealAction()
    {
        string actor = Read("turnCharacter");
        if (actor is not ("player" or "partner") || actor == soloExcluded || Number(actor + "Health") <= 0 ||
            Flag(actor + "Death") || Flag("dontUseTurn") || Flag("autoPlay") || Flag("returnAction")) return;
        string pending = actor + PendingStealActions;
        int count = Number(pending);
        if (count <= 0) return;
        WriteEntertainmentNumber(pending, count - 1);
        // The native extra-action branch consumes one charge and reopens this actor's hand.
        WriteEntertainmentNumber(actor + "StateActionPlus", Number(actor + "StateActionPlus") + 1);
    }

    private void ClearPendingStealActions()
    {
        WriteEntertainmentNumber("player" + PendingStealActions, 0);
        WriteEntertainmentNumber("partner" + PendingStealActions, 0);
    }

    private ReinforceData? FindBonfireCombination(ItemService items, out bool eligible)
    {
        eligible = false;
        if (!items.EnsureReinforceDataBaseLoaded()) return null;
        var reinforcements = items.reinforceDataBase?.datas;
        if (reinforcements == null) return null;
        ReinforceData? firstCombination = null;
        int itemCount = Number("nowItemCount");
        for (int index = 0; index < reinforcements.Length; index++)
        {
            var candidate = reinforcements[index];
            if (candidate == null || candidate.name != ReinforceType.combineWithItem) continue;
            firstCombination ??= candidate;
            if (itemCount < 2) return firstCombination;
            for (int slot = 1; slot <= itemCount; slot++)
            {
                var item = items.GetItemBySlotNumber(slot);
                if (item != null && items.HasItemBattleActionBySlotNumber(slot) &&
                    ItemService.IsValidCombinationPrimary(item) && candidate.IsMatchingReinforceTarget(item) &&
                    Enumerable.Range(1, itemCount).Any(material => material != slot &&
                        items.HasItemBattleActionBySlotNumber(material) &&
                        ItemCustomFunctions.CanUseItemAsCombinationMaterial(slot, material)))
                {
                    eligible = true;
                    return candidate;
                }
            }
        }
        return firstCombination;
    }

    private void AppendBonfireCombination()
    {
        var items = Engine.GetService<ItemService>();
        var offers = items.ExtractedReinforceData;
        var combine = FindBonfireCombination(items, out bool eligible);
        WriteEntertainmentNumber(BonfireCombinationIndex, -1);
        if (offers == null || combine == null) return;
        WriteEntertainmentNumber(BonfireCombinationIndex, offers.Count);
        vars!.SetVariableValue(BonfireCombinationAvailable, eligible ? "true" : "false");
        offers.Add(combine);
        WriteEntertainmentNumber("getReinforceCount", offers.Count);
    }

    private void ApplyNewEnemyHealth()
    {
        int before = Number("enemyMaxCountBefore"), count = Number("enemyMaxCount");
        bool enemySummon = Read("addEnemySummonSide") != "ally";
        for (int slot = before + 1; slot <= count; slot++)
        {
            string enemy = "enemy" + slot.ToString(CultureInfo.InvariantCulture);
            if (TracksStealDamage)
                WriteEntertainmentNumber(enemy + StolenWeaponUntil, 0);
            if (!enemySummon || Read(enemy + "ResourceName") == "bom" || Settings.EnemyHealthMultiplier == 1) continue;
            WriteEntertainmentNumber(enemy + BaseEnemyMaxHealth, Number(enemy + "MaxHealth"));
            MultiplyEntertainmentNumber(enemy + "MaxHealth", Settings.EnemyHealthMultiplier);
            MultiplyEntertainmentNumber(enemy + "Health", Settings.EnemyHealthMultiplier);
        }
    }

    private void RestoreSpawnMaxHealth(string sourceSlot, string maxHealth)
    {
        int original = Number($"enemy{Number(sourceSlot)}{BaseEnemyMaxHealth}");
        if (original > 0) WriteEntertainmentNumber(maxHealth, original);
    }

    private bool IsStolenWeaponVulnerable(string target)
    {
        if (!IsEntertainmentEnemy(target)) return false;
        int round = Number("turnCount"), from = Number(target + StolenWeaponFrom);
        return from > 0 && from <= round && round <= Number(target + StolenWeaponUntil) && Number(CompletedPartyRound) < round;
    }

    private static bool IsEntertainmentEnemy(string actor) =>
        actor.StartsWith("enemy", StringComparison.Ordinal) &&
        int.TryParse(actor.AsSpan(5), NumberStyles.None, CultureInfo.InvariantCulture, out int slot) && slot > 0;

    private static bool IsDirectEntertainmentDamage(string type) =>
        type is "" or "attack" or "rangeAttack" or "killAttack";

    private static bool IsEnemyAttackSource(string bombSide, string resourceName) =>
        bombSide != "ally" && resourceName != "bom";

    private void ApplyEntertainmentDamage()
    {
        if (Read("timing") != "battle") return;
        string attacker = Read("actionUserId");
        string action = Read("actionType"), damage = Read("damageType");
        bool attack = (IsDirectEntertainmentDamage(damage) &&
            ActionCustomFunctions.IsAttackEffectAction(action)) ||
            (damage == "bomb" && action == "bomb" && !Flag(attacker + "Death"));
        if (attack && Settings.EnemyAttackMultiplier != 1 && IsEntertainmentEnemy(attacker) &&
            IsEnemyAttackSource(Read(attacker + "BombSummonSide"), Read(attacker + "ResourceName")))
            MultiplyEntertainmentNumber("actionValue", Settings.EnemyAttackMultiplier, roundUp: false);

        if (TracksStealDamage && IsStolenWeaponVulnerable(Read("actionTargetId")))
            MultiplyEntertainmentNumber("actionValue", Settings.StolenWeaponDamageMultiplier, roundUp: false);
    }

    private void MultiplyEntertainmentNumber(string key, double multiplier, bool roundUp = true)
    {
        if (multiplier == 1 || !double.TryParse(Read(key), NumberStyles.Float, CultureInfo.InvariantCulture, out double value) || value <= 0)
            return;
        double scaled = ScaleEntertainmentAmount(value, multiplier);
        WriteEntertainmentNumber(key, roundUp ? Math.Ceiling(scaled) : scaled);
    }

    private void WriteEntertainmentNumber(string key, double value) =>
        vars!.SetVariableValue(key, value.ToString("R", CultureInfo.InvariantCulture));

    private static double ScaleEntertainmentAmount(double value, double multiplier) =>
        Math.Min(int.MaxValue, value * multiplier);
}
