using HarmonyLib;
using Il2Cpp;

namespace CupCompetition;

public sealed partial class CupMod
{
    [ThreadStatic] private static double enemyIntentMultiplier;
    [ThreadStatic] private static double enemyHealthForecastMultiplier;

    private void InstallEnemyAttackPreviewHooks()
    {
        var restoreIntent = new HarmonyMethod(typeof(CupMod), nameof(RestoreEnemyIntent));
        HarmonyInstance.Patch(AccessTools.Method(typeof(ActionOrStateFunctions), nameof(ActionOrStateFunctions.BuildEnemyNextActionTooltipContents)),
            prefix: new HarmonyMethod(typeof(CupMod), nameof(BeginEnemyIntent)), finalizer: restoreIntent);
        HarmonyInstance.Patch(AccessTools.Method(typeof(EnemyCustomFunctions), nameof(EnemyCustomFunctions.GetEnemyNextActionDisplayedValueSum)),
            prefix: new HarmonyMethod(typeof(CupMod), nameof(BeginEnemyIntentSum)), finalizer: restoreIntent);
        HarmonyInstance.Patch(AccessTools.Method(typeof(ActionSetData), nameof(ActionSetData.ApplyPlannedActionScale)),
            postfix: new HarmonyMethod(typeof(CupMod), nameof(ScaleEnemyIntent)));

        HarmonyInstance.Patch(AccessTools.Method(typeof(PartyHealthPreviewService), nameof(PartyHealthPreviewService.ForecastDamage)),
            prefix: new HarmonyMethod(typeof(CupMod), nameof(BeginEnemyHealthForecast)),
            finalizer: new HarmonyMethod(typeof(CupMod), nameof(RestoreEnemyHealthForecast)));
        Hook(typeof(BattleDamageRules), nameof(BattleDamageRules.Resolve), nameof(ScaleEnemyHealthForecast));
    }

    private static void BeginEnemyIntent(string __2, out double __state) =>
        BeginEnemyIntentScope(__2, out __state);

    private static void BeginEnemyIntentSum(int __0, out double __state) =>
        BeginEnemyIntentScope("enemy" + __0.ToString(System.Globalization.CultureInfo.InvariantCulture), out __state);

    private static void BeginEnemyIntentScope(string source, out double previousMultiplier)
    {
        previousMultiplier = enemyIntentMultiplier;
        enemyIntentMultiplier = Active is { Run.Running: true } cup && IsEntertainmentEnemy(source) &&
            IsEnemyAttackSource(cup.Read(source + "BombSummonSide"), cup.Read(source + "ResourceName"))
            ? cup.Settings.EnemyAttackMultiplier : 1;
    }

    private static void RestoreEnemyIntent(double __state) => enemyIntentMultiplier = __state;

    // The native intent renderer has already resolved stat bonuses and planned action power here.
    // Keep the stored enemy action untouched, since it is also used by actual battle resolution.
    private static void ScaleEnemyIntent(ActionSetData __instance, int __0, int __1, ref float __result)
    {
        if (enemyIntentMultiplier is 0 or 1 || __result <= 0 || __1 >= 0 ||
            __instance.actionNames is not { } actions || __0 < 0 || __0 >= actions.Length ||
            !ActionTypeUtility.IsAttackEffect(actions[__0])) return;
        __result = (float)ScaleEntertainmentAmount(__result, enemyIntentMultiplier);
    }

    private static void BeginEnemyHealthForecast(PartyHealthPreviewService.ForecastState __0, string __1, string __4,
        out double __state)
    {
        __state = enemyHealthForecastMultiplier;
        enemyHealthForecastMultiplier = Active is { Run.Running: true } cup && IsEntertainmentEnemy(__1) &&
            IsEnemyAttackSource(__0.Read(__1 + "BombSummonSide"), __0.Read(__1 + "ResourceName")) &&
            (IsDirectEntertainmentDamage(__4) || __4 == "bomb" && !__0.Flag(__1 + "Death"))
            ? cup.Settings.EnemyAttackMultiplier : 1;
    }

    private static void RestoreEnemyHealthForecast(double __state) => enemyHealthForecastMultiplier = __state;

    // Match the battle script's point of scaling, before the native defense calculation.
    private static void ScaleEnemyHealthForecast(ref float __0)
    {
        if (enemyHealthForecastMultiplier is not (0 or 1) && __0 > 0)
            __0 = (float)ScaleEntertainmentAmount(__0, enemyHealthForecastMultiplier);
    }
}
