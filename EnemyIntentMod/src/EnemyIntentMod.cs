using HarmonyLib;
using Il2Cpp;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using MelonLoader;

[assembly: MelonInfo(typeof(EnemyIntent.EnemyIntentMod), "Annihilated Enemy Intent", "0.2.0", "Brick")]
[assembly: MelonGame("DeathKumiai", "Annihilated")]

namespace EnemyIntent;

public sealed partial class EnemyIntentMod : MelonMod
{
    [ThreadStatic] private static RenderContext? current;
    private static MelonLogger.Instance? logger;
    private static bool loggedRenderError;

    public override void OnInitializeMelon()
    {
        logger = LoggerInstance;
        Patch(typeof(EnemyService), "UpdateEnemyNextActionDisplayCore",
            prefix: nameof(BeginRender), finalizer: nameof(EndRender));
        Patch(typeof(ActionData), nameof(ActionData.GetIconAndValueText), postfix: nameof(CaptureRow));
        Patch(typeof(ActionOrStateFunctions), nameof(ActionOrStateFunctions.BuildEnemyNextActionTooltipContents),
            postfix: nameof(AddIntentCatalog));
        Patch(typeof(EnemyService), "ApplyEnemyNextActionTextWithAnimation", prefix: nameof(FormatIntent));
    }

    private void Patch(Type type, string name, string? prefix = null, string? postfix = null, string? finalizer = null)
    {
        var target = AccessTools.Method(type, name) ?? throw new MissingMethodException(type.FullName, name);
        HarmonyMethod? Hook(string? method) => method == null ? null : new HarmonyMethod(typeof(EnemyIntentMod), method);
        HarmonyInstance.Patch(target, prefix: Hook(prefix), postfix: Hook(postfix), finalizer: Hook(finalizer));
    }

    private static void BeginRender(EnemyService __instance, ActionSetData? __0, Il2CppReferenceArray<ActionData>? __1,
        EnemyController? __2, out RenderContext? __state)
    {
        __state = current;
        current = null;
        if (__0 != null && __1 != null && __1.Length > 0 && __2 != null)
            current = new RenderContext(__instance, __2.enemyId, __0.Pointer, new NativeIntentRow[__1.Length]);
    }

    private static void EndRender(RenderContext? __state) => current = __state;

    private static void CaptureRow(ActionData __instance, TargetType __3, ActionSetData? __6,
        int __7, int __8, string? __result)
    {
        if (current is not { } render || __8 != -1 || __result == null || __6?.Pointer != render.ActionSet ||
            __7 < 0 || __7 >= render.Rows.Length)
            return;

        var type = __instance.action;
        AttackKey? attack = ActionTypeUtility.IsAttackEffect(type) ? new((int)type, (int)__3) : null;
        render.Rows[__7] = new NativeIntentRow(__result, attack);
    }

    private static void FormatIntent(int __0, ref string __1,
        ref Il2CppSystem.Collections.Generic.IReadOnlyList<int>? __3, ref int __4)
    {
        if (current is not { } render || render.EnemyId != __0 || render.Rows.Length != __4 ||
            string.IsNullOrWhiteSpace(__1))
            return;

        try
        {
            int[] warnings = __3 == null ? Array.Empty<int>() :
                new int[__3.Cast<Il2CppSystem.Collections.Generic.IReadOnlyCollection<int>>().Count];
            for (int i = 0; i < warnings.Length; i++)
                warnings[i] = __3![i];

            var display = IntentRows.Reformat(__1, render.Rows, warnings);
            if (display.Text == __1)
                return;

            if (!display.Warnings.SequenceEqual(warnings))
            {
                var list = new Il2CppSystem.Collections.Generic.List<int>();
                foreach (int index in display.Warnings)
                    list.Add(index);
                __3 = list.Cast<Il2CppSystem.Collections.Generic.IReadOnlyList<int>>();
            }

            __1 = display.Text;
            __4 = display.ActionCount;
        }
        catch (Exception error)
        {
            if (loggedRenderError)
                return;
            loggedRenderError = true;
            logger?.Warning($"Using the original intent display after a formatting error: {error.Message}");
        }
    }

    private sealed record RenderContext(EnemyService Service, int EnemyId, IntPtr ActionSet, NativeIntentRow[] Rows);
}
