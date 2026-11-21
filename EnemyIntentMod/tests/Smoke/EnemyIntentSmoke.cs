using System.Collections;
using System.Text.RegularExpressions;
using Il2Cpp;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppNaninovel;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(EnemyIntentSmoke), "Enemy intent smoke", "0.1.0", "Local test")]

public sealed class EnemyIntentSmoke : MelonMod
{
    private bool started;

    public override void OnUpdate()
    {
        if (started || !Environment.GetCommandLineArgs().Contains("--enemy-intent-smoke"))
            return;
        if (Time.realtimeSinceStartup > 55)
        {
            started = true;
            LoggerInstance.Error("Timed out before the isolated goblin fight displayed its first intent.");
            Application.Quit(19);
            return;
        }
        if (!Engine.Initialized)
            return;

        var script = Engine.GetService<IScriptPlayer>();
        if (script.PlaybackSpot.ScriptName != "Battle")
            return;
        var variables = Engine.GetService<ICustomVariableManager>();
        if (variables.GetVariableValue("showBattleModeChanger") is not ("1" or "true" or "True"))
            return;

        var enemy = UnityEngine.Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None)
            .FirstOrDefault(controller => controller.enemyId == 1);
        if (enemy == null || !enemy.isActiveAndEnabled || string.IsNullOrWhiteSpace(enemy.NextActionText?.text))
            return;

        started = true;
        MelonCoroutines.Start(Verify(script, enemy));
    }

    private IEnumerator Verify(IScriptPlayer script, EnemyController enemy)
    {
        bool passed = false;
        try
        {
            var profile = Engine.GetService<IStateManager>().Cast<StateManager>().Configuration.SaveFolderName;
            Require(profile.StartsWith("CupSaves/", StringComparison.Ordinal), "Missing test save isolation.");
            var service = Engine.GetService<EnemyService>();
            var original = service.GetEnemyNextActionInfo(1);
            var firstHit = original.Item2.First(row => row.action == ActionType.attack);
            var block = Engine.GetService<ActionService>().FindActionByName("block");
            string initial = Readout(enemy);
            LoggerInstance.Msg($"Native goblin intent: {initial}");
            Require(initial.Contains("×1"), "Native battle did not display a one-hit count.");
            script.Stop();

            var mixed = original.Item1.Clone();
            mixed.actionNames = new Il2CppStructArray<ActionType>(new[] { ActionType.attack, ActionType.attack, ActionType.block });
            mixed.actionTargets = new Il2CppStructArray<TargetType>(new[] { TargetType.enemy, TargetType.enemy, TargetType.myself });
            mixed.actionValues = new Il2CppStructArray<float>(new[] { 6f, 6f, 4f });
            mixed.actionStatusCalcs = new Il2CppStringArray(new[] { "", "", "" });
            var effects = new Il2CppReferenceArray<ActionData>(new[] { firstHit, firstHit, block });
            service.SetEnemyNextActionInfo(1, mixed, effects, enemy.NextActionText, "intent smoke mixed action");
            service.RefreshAllEnemyNextActionDisplays();
            string combined = Readout(enemy);
            LoggerInstance.Msg($"Mixed attack and block: {combined} | {enemy.NextActionText.text}");
            Require(combined.Contains("×2") && Regex.IsMatch(combined, @"(?<!\d)4(?!\d)"), "Expected a two-hit attack and 4 block.");

            mixed.actionValues[0] = 8f;
            mixed.actionValues[1] = 8f;
            service.RefreshAllEnemyNextActionDisplays();
            string updated = Readout(enemy);
            LoggerInstance.Msg($"Damage updated: {combined} -> {updated}");
            Require(updated != combined && updated.Contains("×2") &&
                Regex.IsMatch(updated, @"(?<!\d)(8|16) ×2"), "Expected the same two-hit attack at updated power.");
            passed = true;
        }
        catch (Exception error)
        {
            LoggerInstance.Error(error.ToString());
        }
        if (!passed)
        {
            Application.Quit(18);
            yield break;
        }

        yield return new WaitForSecondsRealtime(0.6f);

        try
        {
            var text = enemy.NextActionText;
            text.ForceMeshUpdate();
            var mesh = text.textInfo;
            var characters = Enumerable.Range(0, mesh.characterCount)
                .Select(i => mesh.characterInfo[i]).ToArray();
            bool countsRendered = characters.Any(info => info.character == '×' && info.isVisible);
            // This build emits sprite characters in TMP's private-use range without populating spriteCount.
            int iconGlyphs = characters.Count(info => info.isVisible && info.character is >= '\uE000' and <= '\uF8FF');
            Require(text.isActiveAndEnabled && enemy.NextActionAlpha > 0.8f && countsRendered && iconGlyphs >= 2,
                "The live enemy label did not generate the hit count and effect glyphs.");
            LoggerInstance.Msg($"Native text mesh contains the hit count and {iconGlyphs} action or target glyphs.");

            var service = Engine.GetService<EnemyService>();
            var warned = Engine.GetService<ActionService>().FindActionByName("stealAttack");
            var installed = service.GetEnemyNextActionInfo(1);
            installed.Item1.actionNames[2] = warned.action;
            installed.Item1.actionTargets[2] = TargetType.enemy;
            installed.Item2[2] = warned;
            service.RefreshAllEnemyNextActionDisplays();
            var warnings = enemy.nextActionWarningActionIndices;
            Require(warnings.Contains(1) && !warnings.Contains(2), "The original warning did not follow its grouped row.");
            LoggerInstance.Msg("Native warning index followed its action after the preceding hits merged.");

            Require(service.StoreEnemyNextActionByName(1, "warriorFinalBerserkStrength", 0), "Native replacement action was rejected.");
            var replacement = Readout(enemy);
            LoggerInstance.Msg($"Native action replacement: {replacement} | {enemy.NextActionText.text}");
            Require(!string.IsNullOrWhiteSpace(replacement) && !replacement.Contains("×"), "The old attack stayed visible after a non-attack move replaced it.");
            service.StoreEnemyNextAction(1, 0, true, "normal", true);
            Require(string.IsNullOrWhiteSpace(Readout(enemy)), "The badge kept a stale move after the game cleared the plan.");
            LoggerInstance.Msg("ENEMY INTENT SMOKE PASSED");
            Application.Quit(0);
        }
        catch (Exception error)
        {
            LoggerInstance.Error(error.ToString());
            Application.Quit(18);
        }
    }

    private static string Readout(EnemyController enemy) =>
        Regex.Replace(Regex.Replace(enemy.NextActionText?.text ?? "", "<[^>]+>", " "), @"\s+", " ").Trim();

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
