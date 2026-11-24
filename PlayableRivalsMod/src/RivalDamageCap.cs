using Il2Cpp;
using Il2CppNaninovel;
using Il2CppNaninovel.Commands;

namespace PlayableRivals;

public sealed partial class RivalMod
{
    private const string CapUnlockedKey = "rivalsOutcastDamageUnlocked";
    private const string CapSpentKey = "rivalsOutcastDirectSpent";
    private bool OutcastDamageLimited => Role == Rival.Outcast && !Flag(CapUnlockedKey);
    private const string CapNotice = "直接伤害合计最多30";
    private const string CapDescription = "一次武器或技能使用中，所有目标、所有段数的直接伤害合计最多 30 点。燃烧、中毒等间接伤害不受限制。击败赫德夺舍的士兵后解除。";

    private bool IsLimitedOutcastHit => IsRivalBattle && OutcastDamageLimited &&
        Read("actionUserId") == "partner" &&
        RivalRules.IsDirectDamageType(Read("damageType")) &&
        ActionCustomFunctions.IsAttackEffectAction(Read("actionType"));

    private void HandleDamageCap(Command command, string script, int line)
    {
        if (Role != Rival.Outcast) return;
        if (script == "Battle")
        {
            // This is the native Win entry, after the complete multi-phase boss battle.
            if (line == 5208 && Read("battleTarget") == "slime") Write(CapUnlockedKey, 1);
            // Reset once per committed weapon use, never per target, hit, QTE or add-action.
            if (line == 2319) Write(CapSpentKey, 0);
            if (line is 47 or 4977) Write(CapSpentKey, 0);
        }
        if (script != "Action" || !IsLimitedOutcastHit) return;
        if (line == 955)
        {
            // Native critical/steal multipliers, protection and block have already resolved.
            // Clamp before sanity damage, HP subtraction, feedback and damage-derived effects.
            int amount = RivalRules.LimitDirectDamage(Number("actionValue"), Number(CapSpentKey));
            Write("actionValue", amount);
            Write(CapSpentKey, Number(CapSpentKey) + amount);
        }
        // Native successful robbery also executes a surviving ordinary enemy after Damage.
        // Keep the stolen item and its other effects, but prohibit this second HP write.
        if (line is 1343 or 1453)
            OverrideCondition(command, "false");
        if (line is 1340 or 1451)
        {
            var set = command.Cast<SetCustomVariable>();
            var original = set.Expression;
            commandRestore[command.Pointer] = () => set.Expression = original;
            string target = Read("actionTargetId");
            set.Expression = $"{target}IsShowKillEffect={target}Health<=0";
        }
    }

    private void RestoreOutcastMilestone()
    {
        // Pre-0.1.16 saves have no mod milestone. The native final-phase flag survives
        // battle cleanup; only migrate once outside that encounter (never at phase changes).
        if (Role == Rival.Outcast && Flag("xSwitchSlimePhase3EndBranchDone") &&
            (Read("timing") != "battle" || Read("battleTarget") != "slime"))
            Write(CapUnlockedKey, 1);
    }
}
