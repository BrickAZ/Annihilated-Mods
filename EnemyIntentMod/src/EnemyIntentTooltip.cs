using Il2Cpp;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppNaninovel;
using LocalizationManager = Il2CppI2.Loc.LocalizationManager;

namespace EnemyIntent;

public sealed partial class EnemyIntentMod
{
    private static readonly string[] WarriorDuelMoves =
    {
        "warriorFinalSummonMagicSwords", "warriorFinalBerserkStrength",
        "warriorFinalDoubleAttack", "warriorFinalDoubleAttackForm2",
        "warriorFinalDoubleAttackForm3", "warriorFinalAttackForm3",
    };

    private static void AddIntentCatalog(Il2CppReferenceArray<ActionData>? __0, ActionSetData? __1,
        Il2CppSystem.Collections.Generic.IReadOnlyList<TooltipContent>? __result)
    {
        if (current is not { } render || __0 == null || __1?.Pointer != render.ActionSet || __result == null)
            return;

        try
        {
            var contents = __result.TryCast<Il2CppSystem.Collections.Generic.List<TooltipContent>>();
            if (contents == null)
                return;

            var actionService = Engine.GetService<ActionService>();
            string actor = FormattableString.Invariant($"enemy{render.EnemyId}");
            var planned = new IntentMove(__1.name ?? "", DescribeMove(__1, __0, actor));
            var seen = new HashSet<string>(StringComparer.Ordinal) { planned.Id };
            var candidates = new List<IntentMove>();
            foreach (string name in GetMoveNames(render))
            {
                if (string.IsNullOrWhiteSpace(name) || !seen.Add(name))
                    continue;
                var move = actionService.FindActionSetAndActionsByName(name);
                if (move.Item1 == null)
                    continue;
                candidates.Add(new IntentMove(name, DescribeMove(move.Item1, move.Item2, actor)));
            }

            var catalog = IntentCatalog.Format(candidates, planned, LocalizationManager.CurrentLanguageCode);
            contents.Insert(0, new TooltipContent(catalog.Title, catalog.Description));
        }
        catch (Exception error)
        {
            // Keep the stock tooltip if an encounter supplies an unsupported action table.
            if (loggedTooltipError)
                return;
            loggedTooltipError = true;
            logger?.Warning($"Using the original enemy tooltip: {error.Message}");
        }
    }

    private static bool loggedTooltipError;

    private static IEnumerable<string> GetMoveNames(RenderContext render)
    {
        var names = render.Service.GetEnemyData(render.EnemyId)?.actionSets;
        if (names != null)
            foreach (string name in names)
                yield return name;

        string battle = Helper.GetValStr("battleTarget");
        if (battle == "warriorFinalDuel" && Helper.GetVal<int>("yValWarriorFinalSlot") == render.EnemyId)
        {
            foreach (string move in WarriorDuelMoves)
                yield return move;
        }
        else if (battle == "dropout" && Helper.GetValStr("partnerCharacter") == "mage" &&
                 names?.Any(name => name == "dropoutGrandAttack") == true)
        {
            yield return "dropoutGrandAttackVsMage";
        }
    }

    private static string DescribeMove(ActionSetData set, Il2CppReferenceArray<ActionData>? actions, string actor)
    {
        var effects = new List<string>();
        if (actions != null)
        {
            var rows = actions.Where(action => action != null).ToArray();
            for (int i = 0; i < rows.Length;)
            {
                string title = rows[i].GetTitle().Trim();
                int end = i + 1;
                while (end < rows.Length && rows[end].action == rows[i].action)
                    end++;
                if (!string.IsNullOrWhiteSpace(title))
                    effects.Add(end - i > 1 ? FormattableString.Invariant($"{title} ×{end - i}") : title);
                i = end;
            }
        }

        if (set.exEffects is { } extra)
        {
            foreach (string effect in extra.Distinct(StringComparer.Ordinal))
            {
                if (!string.IsNullOrWhiteSpace(effect) && ActionOrStateFunctions.ShouldShowEnemyNextActionExEffect(effect))
                    effects.Add(ActionOrStateFunctions.GetEnemyNextActionExEffectTitle(effect, actor));
            }
        }

        return effects.Count == 0 ? set.name ?? "…" : string.Join(" + ", effects);
    }
}
