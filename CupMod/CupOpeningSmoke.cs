using Il2CppNaninovel;
using Il2CppNaninovel.UI;
using Il2Cpp;
using UnityEngine;

namespace CupCompetition;

public sealed partial class CupMod
{
    private System.Collections.IEnumerator VerifyOpeningDeathForSmoke()
    {
        double deadline = Time.realtimeSinceStartupAsDouble + 8;
        while (Run.Running && Time.realtimeSinceStartupAsDouble < deadline)
        {
            yield return null;
            var choices = UnityEngine.Object.FindObjectsByType<ChoiceHandlerButton>(FindObjectsSortMode.None);
            var retry = choices.FirstOrDefault(choice => ActiveButton(choice) != null &&
                choice.ChoiceState.OnSelectScript?.TrimEnd().EndsWith(".GameOverEnd", StringComparison.Ordinal) == true);
            if (retry == null) continue;
            if (!choices.Any(choice => ActiveButton(choice) != null &&
                    choice.ChoiceState.OnSelectScript?.TrimEnd().EndsWith(".StatusAllocation", StringComparison.Ordinal) == true) ||
                Number("curse") != 100 || Number("playerAllocationPoint") != 6 || !Flag("playerDeath") ||
                Run.Deaths != 0 || Run.DeathPenalty != TimeSpan.Zero || Run.Paused)
                break;
            LoggerInstance.Msg("Cup opening smoke: native allocation and retry choices, curse=100, points=6, deaths=0, penalty=0.");
            var convert = choices.FirstOrDefault(choice => ActiveButton(choice) != null &&
                choice.ChoiceState.OnSelectScript?.TrimEnd().EndsWith(".BuyByCurse", StringComparison.Ordinal) == true);
            if (convert == null) break;
            ActiveButton(convert)!.onClick.Invoke();
            deadline = Time.realtimeSinceStartupAsDouble + 8;
            while (Run.Running && (cursePool == null || Read("curseGenerateRenderedVersion") != "0") &&
                   Time.realtimeSinceStartupAsDouble < deadline) yield return null;
            var items = Engine.GetService<ItemService>();
            int firstCount = items.GetCurseGenerateCandidateResourceNames()
                .Cast<Il2CppSystem.Collections.Generic.IReadOnlyCollection<string>>().Count;
            var next = BuildCursePool(2);
            var third = BuildCursePool(3);
            if (cursePool == null || !cursePool.Contains("tailWhip") || !cursePool.Contains("blackKnightSword") ||
                cursePool.Contains("katana") || !next.IsSupersetOf(cursePool) || !next.Contains("pikapikaSword") ||
                !next.Contains("doll") || !next.Contains("teaSet") || next.Contains("katana") ||
                !third.IsSupersetOf(next) || !third.Contains("seaDragonEye") || third.Contains("katana") ||
                firstCount != cursePool.Count)
            {
                Fail(new InvalidOperationException($"Curse pool floor checks failed: first=[{string.Join(",", cursePool ?? new HashSet<string>())}], " +
                    $"second=[{string.Join(",", next)}], native list={firstCount}."));
                yield break;
            }
            string reject = items.GetCurseGenerateSelectionRejectMessage("blackKnightSword", 0, 0, false);
            var purchase = new Il2CppSystem.Collections.Generic.List<string>();
            purchase.Add("blackKnightSword");
            if (!string.IsNullOrEmpty(reject) ||
                !items.GenerateItemsByCurse(purchase.Cast<Il2CppSystem.Collections.Generic.IReadOnlyList<string>>(), false) ||
                Number("curse") != 70 || !Enumerable.Range(1, 12).Any(
                    slot => ItemCustomFunctions.GetItemResourceNameBySlotNumber(slot) == "blackKnightSword"))
            {
                Fail(new InvalidOperationException($"The native black knight sword purchase failed: {reject}, curse={Read("curse")}."));
                yield break;
            }
            LoggerInstance.Msg($"Cup curse pool smoke: floor 1 list ({firstCount} items), cumulative floor 2 ({next.Count} items), " +
                $"cumulative floor 3 ({third.Count} items), including the black knight sword and sea dragon eye; " +
                "native purchase charged 30 and added the sword.");
            vars!.SetVariableValue("curseGenerateAction", "cancel");
            deadline = Time.realtimeSinceStartupAsDouble + 7;
            retry = null;
            while (Run.Running && retry == null && Time.realtimeSinceStartupAsDouble < deadline)
            {
                yield return null;
                retry = UnityEngine.Object.FindObjectsByType<ChoiceHandlerButton>(FindObjectsSortMode.None)
                    .FirstOrDefault(choice => ActiveButton(choice) != null &&
                        choice.ChoiceState.OnSelectScript?.TrimEnd().EndsWith(".GameOverEnd", StringComparison.Ordinal) == true);
            }
            if (retry == null) break;
            ActiveButton(retry)!.onClick.Invoke();
            var advance = Engine.GetService<IInputManager>().GetSampler(InputConfiguration.ContinueName);
            deadline = Time.realtimeSinceStartupAsDouble + 14;
            while (Run.Running && openingDeathInProgress && Time.realtimeSinceStartupAsDouble < deadline)
            {
                yield return null;
                if (player!.WaitingForInput && !HasActiveControls()) advance.Activate(1f);
            }
            if (!openingDeathInProgress && !Flag("playerDeath") && Number("playerHealth") > 0 &&
                Number("playerAllocationPoint") == 6 && Number("curse") == 70 && Run.Deaths == 0)
            {
                LoggerInstance.Msg("Cup opening smoke: native retry revived player and returned to floor 1.");
                yield break;
            }
            break;
        }
        Fail(new InvalidOperationException($"Opening death was not playable at {player?.PlaybackSpot.ScriptName}:{player?.PlaybackSpot.LineIndex}: curse={Read("curse")}, " +
            $"points={Read("playerAllocationPoint")}, death={Read("playerDeath")}, timer={pauseReason}, counted={Run.Deaths}."));
    }
}
