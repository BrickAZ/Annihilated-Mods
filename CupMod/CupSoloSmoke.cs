using Il2Cpp;
using Il2CppNaninovel;
using Il2CppNaninovel.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace CupCompetition;

public sealed partial class CupMod
{
    private static bool AttackForecastSmoke => HasSoloSmokeArgument("--annihilated-cup-attack-forecast-smoke");
    private static bool SoloBattleUiSmoke => HasSoloSmokeArgument("--annihilated-cup-solo-ui-smoke");
    private static bool SoloUiWarriorSmoke => SoloBattleUiSmoke && HasSoloSmokeArgument("--annihilated-cup-solo-ui-warrior");
    private bool SoloSmoke { get; } = AttackForecastSmoke || SoloBattleUiSmoke || HasSoloSmokeArgument("--annihilated-cup-solo-smoke");
    private string SoloSmokeDisabledCharacter => HasSoloSmokeArgument("--annihilated-cup-solo-ban-player") ? "player" :
        HasSoloSmokeArgument("--annihilated-cup-solo-unmatched") ? "warrior" : "mage";
    private bool SoloSmokeInherits => !AttackForecastSmoke && !HasSoloSmokeArgument("--annihilated-cup-solo-single-action");
    private bool SoloSmokeDefeat { get; } = HasSoloSmokeArgument("--annihilated-cup-solo-defeat");
    private bool SoloSmokeDreak { get; } = HasSoloSmokeArgument("--annihilated-cup-solo-dreak");
    private readonly List<(int Slot, string Actor, int Card)> soloSmokeActions = new();
    private readonly List<string> soloSmokeEnemyTargets = new();
    private bool soloSmokeEnemyPhase;
    private bool soloSmokeRoundDone;
    private int soloDreakVictoryExcludedHealth = -1;
    private int soloDreakStolenWeapons;
    private (float Base, float Intent, int Forecast, int Health, string Label) attackForecastSample;

    private static bool HasSoloSmokeArgument(string name) =>
        Environment.GetCommandLineArgs().Contains(name, StringComparer.Ordinal);

    private void RecordSoloSmokeCommand(string script, int line)
    {
        if (soloSmokeRoundDone) return;
        if (SoloUiWarriorSmoke && !Settings.InheritDisabledAction && (script, line) == ("Action", 776) &&
            Read("actionUserId") == "partner" && Read("actionTargetId") == "enemy1" &&
            IsDirectEntertainmentDamage(Read("damageType")))
        {
            // Reach the original victory sequence via a real weapon hit.
            vars!.SetVariableValue("enemy1Health", "1");
        }
        if (SoloSmokeDreak && script == "Battle" && line == 1966 && SoloActor == "partner" &&
            Number("actionCount") == (Settings.InheritDisabledAction ? 2 : 1))
        {
            // Exercise the real Dreak card after the ordinary card. An intact goblin's native
            // break gate hides Dreak, so model the state after its guard has been depleted.
            vars!.SetVariableValue("enemy1Break", "0");
            vars.SetVariableValue("enemy1BreakCount", "0");
        }
        if (SoloSmokeDreak && script == "Battle" && line == 3552 && Number("selectCommand") == -101 && SoloActor == "partner")
        {
            soloDreakVictoryExcludedHealth = Number("playerHealth");
            soloDreakStolenWeapons = Number("enemy1StolenWeaponCount");
            soloSmokeActions.Add((Number("actionCount"), Read("turnCharacter"), Number("selectCommand")));
            LoggerInstance.Msg($"Cup solo smoke: native Dreak victory | stolen={soloDreakStolenWeapons}");
        }
        if (script == "Battle" && line == 3458 && !soloSmokeEnemyPhase)
        {
            soloSmokeActions.Add((Number("actionCount"), Read("turnCharacter"), Number("selectCommand")));
            return;
        }
        if (script == "Battle" && line == 3859)
        {
            soloSmokeEnemyPhase = true;
            LoggerInstance.Msg($"Cup solo smoke: enemy phase after {string.Join(",", soloSmokeActions)}");
            if (SoloSmokeDefeat && SoloActor.Length > 0)
            {
                // Set the fixture HP after both player actions. The native enemy action must
                // deal the fatal damage and the game must reach its normal defeat accounting.
                vars!.SetVariableValue(SoloActor + "Health", "1");
                vars.SetVariableValue(SoloActor + "StateBlock", "0");
            }
            return;
        }
        if (script == "Action" && line == 284 && soloSmokeEnemyPhase &&
            Read("actionUserId").StartsWith("enemy", StringComparison.Ordinal) && Read("actionTargetId") is "player" or "partner")
            soloSmokeEnemyTargets.Add(Read("actionTargetId"));
        if (script == "Battle" && line == 4362) soloSmokeRoundDone = true;
    }

    private System.Collections.IEnumerator VerifySoloForSmoke()
    {
        string disabled = Settings.DisabledCharacter;
        bool unmatched = disabled == "warrior";
        bool lonePartner = disabled == "player";
        int expectedActions = unmatched || Settings.InheritDisabledAction ? 2 : 1;
        string[] expectedActors = unmatched ? new[] { "player", "partner" } :
            Enumerable.Repeat(lonePartner ? "partner" : "player", expectedActions).ToArray();
        string excludedId = lonePartner ? "player" : "partner";
        int excludedHealth = Number(excludedId + "Health");
        int clicked = 0;
        bool keyDown = false, hit = false, sawDreak = false, sawVictoryUi = false;
        double cardReady = -1, barReady = -1;
        var keyboard = InputSystem.AddDevice<Keyboard>();
        var advance = Engine.GetService<IInputManager>().GetSampler(InputConfiguration.ContinueName);
        double deadline = Time.realtimeSinceStartupAsDouble + (SoloUiWarriorSmoke ? 65 : 32);
        try
        {
            while (Run.Running && Time.realtimeSinceStartupAsDouble < deadline)
            {
                yield return null;
                if (keyDown)
                {
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                    keyDown = false;
                }
                if (SoloUiWarriorSmoke && !sawVictoryUi && (player!.PlaybackSpot.ScriptName, player.PlaybackSpot.LineIndex) == ("Battle", 4838))
                {
                    yield return new WaitForSecondsRealtime(0.35f);
                    yield return new WaitForEndOfFrame();
                    SaveSmokeScreenshot(Path.ChangeExtension(SmokeScreenshot, null) + "-solo-victory.png");
                    bool display = VerifySoloBattleDisplayForSmoke();
                    if (clicked != expectedActions || Read("livingEnemyCount") != "0" || !display)
                    {
                        Fail(new InvalidOperationException("The disabled character's status UI leaked into the native victory message."));
                        yield break;
                    }
                    sawVictoryUi = true;
                }
                if (sawVictoryUi && player!.PlaybackSpot.ScriptName == WarriorSoloUiScript &&
                    player.PlaybackSpot.LineIndex == 6)
                {
                    if (SoloActor.Length != 0 || !Flag("showPlayerStatus") || !Flag("showPartnerStatus") ||
                        !BattleStatusPanelsForSmoke().Any(panel => panel.statusOwner == excludedId && panel.gameObject.activeSelf))
                        break;
                    LoggerInstance.Msg("Cup solo UI smoke passed | native victory kept the excluded profile hidden; the roster and profile restored on battle exit.");
                    Finish("smoke_solo_victory_ui_pass");
                    yield break;
                }
                if (SoloSmokeDreak && soloDreakVictoryExcludedHealth >= 0 && player!.PlaybackSpot.ScriptName == "Dungeon")
                {
                    if (clicked != expectedActions || !sawDreak ||
                        !soloSmokeActions.Select(action => action.Actor).SequenceEqual(expectedActors) ||
                        soloSmokeActions.LastOrDefault().Card != -101 || soloDreakVictoryExcludedHealth != excludedHealth ||
                        SoloActor.Length != 0 || !Flag("showPlayerStatus") || Read("partnerCharacter") != "mage")
                        break;
                    bool weaponInInventory = Enumerable.Range(1, 12)
                        .Any(slot => ItemCustomFunctions.GetItemResourceNameBySlotNumber(slot) == "club");
                    LoggerInstance.Msg($"Cup solo Dreak smoke passed | actions={string.Join(",", soloSmokeActions)} " +
                        $"| native victory | stolen={soloDreakStolenWeapons} club in inventory={weaponInInventory} | roster restored");
                    Finish("smoke_solo_dreak_pass");
                    yield break;
                }
                if ((!SoloSmokeDefeat && soloSmokeRoundDone) || (SoloSmokeDefeat && Run.Deaths > 0))
                {
                    var actions = soloSmokeActions.Select(action => action.Actor).ToArray();
                    string alive = lonePartner ? "partner" : "player";
                    if (clicked != expectedActions || !actions.SequenceEqual(expectedActors) ||
                        !soloSmokeActions.Select(action => action.Slot).SequenceEqual(Enumerable.Range(1, expectedActions)) ||
                        !soloSmokeEnemyPhase || soloSmokeEnemyTargets.Count == 0 ||
                        !unmatched && (Number(excludedId + "Health") != excludedHealth ||
                            soloSmokeEnemyTargets.Any(target => target != alive)) ||
                        SoloSmokeDreak && (!sawDreak || soloSmokeActions.LastOrDefault().Card != -101) ||
                        SoloSmokeDefeat && (Run.Deaths != 1 ||
                            Run.DeathPenalty != TimeSpan.FromSeconds(Settings.DeathPenaltySeconds) ||
                            player!.PlaybackSpot.ScriptName != "GameOver"))
                        break;
                    if (AttackForecastSmoke)
                    {
                        var (baseline, intent, forecast, health, label) = attackForecastSample;
                        int actual = health - Number("playerHealth");
                        int expected = (int)Math.Ceiling(baseline * Settings.EnemyAttackMultiplier);
                        string number = expected.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        if (baseline <= 0 || intent != expected || forecast != actual ||
                            !System.Text.RegularExpressions.Regex.IsMatch(label, $@"(?<!\d){number}(?!\d)"))
                        {
                            Fail(new InvalidOperationException($"Enemy attack forecast: base={baseline}, intent={intent}, " +
                                $"label='{label}', forecast={forecast}, actual={actual}, expected intent={expected}."));
                            yield break;
                        }
                        LoggerInstance.Msg($"Cup attack forecast smoke passed | base={baseline} | label='{label}' | " +
                            $"intent={intent} | forecast={forecast} | actual={actual}.");
                        Finish("smoke_attack_forecast_pass");
                        yield break;
                    }
                    LoggerInstance.Msg($"Cup solo smoke passed | disabled={disabled} inherit={Settings.InheritDisabledAction} " +
                        $"| actions={string.Join(",", soloSmokeActions)} | enemy targets={string.Join(",", soloSmokeEnemyTargets)} " +
                        $"| Dreak={sawDreak} | deaths={Run.Deaths} penalty={Run.DeathPenalty.TotalSeconds}");
                    Finish("smoke_solo_pass");
                    yield break;
                }
                if (!soloSmokeEnemyPhase && clicked < expectedActions && soloSmokeActions.Count == clicked &&
                    player!.PlaybackSpot.ScriptName == "Battle" && player.PlaybackSpot.LineIndex == 2137)
                {
                    var cards = UnityEngine.Object.FindObjectsByType<ChoiceHandlerButton>(FindObjectsSortMode.None);
                    var basic = cards.FirstOrDefault(card => card.ChoiceState?.Summary == Read("defaultActionNumber") &&
                        ActiveButton(card) != null);
                    if (basic != null)
                    {
                        if (cardReady < 0) cardReady = Time.realtimeSinceStartupAsDouble;
                        if (Time.realtimeSinceStartupAsDouble - cardReady < 0.5) continue;
                        if (Read("turnCharacter") != expectedActors[clicked] || Number("actionCount") != clicked + 1 ||
                            !unmatched && (SoloActor != expectedActors[clicked] || Read("turnCharacterPosition") != "front" ||
                                Flag("isOrderChangeable") || Flag(lonePartner ? "showPlayerStatus" : "showPartnerStatus")))
                        {
                            LoggerInstance.Msg($"Cup solo smoke: unexpected slot state | active={SoloActor}, slot={Read("actionCount")}, " +
                                $"position={Read("turnCharacterPosition")}, swap={Read("isOrderChangeable")}, " +
                                $"show player={Read("showPlayerStatus")}, show partner={Read("showPartnerStatus")}");
                            break;
                        }
                        var dreak = cards.FirstOrDefault(card => card.ChoiceState?.Summary == "-101" && ActiveButton(card) != null);
                        sawDreak |= dreak != null;
                        if (lonePartner && !Settings.HarshTraining && Number("enemy1Break") > 0 && dreak != null) break;
                        var selected = SoloSmokeDreak && clicked == expectedActions - 1 ? dreak : basic;
                        if (SoloUiWarriorSmoke)
                        {
                            int itemSlot = Enumerable.Range(1, Number("nowItemCount"))
                                .FirstOrDefault(slot => ItemCustomFunctions.GetItemResourceNameBySlotNumber(slot) == "ironSword");
                            var sword = cards.FirstOrDefault(card => ActiveButton(card) != null &&
                                int.TryParse(card.ChoiceState?.Summary, out int slot) && slot == itemSlot && itemSlot > 0);
                            if (sword == null)
                            {
                                LoggerInstance.Msg($"Cup solo weapon unavailable | iron sword slot={itemSlot} | " +
                                    string.Join(" | ", cards.Select(card => $"{card.ChoiceState?.Summary}:{ActiveButton(card) != null}")));
                                break;
                            }
                            LoggerInstance.Msg($"Cup solo weapon UI | action={Read("actionCount")} weapon=ironSword | actor={Read("turnCharacter")} | row={Read("turnCharacterPosition")}");
                            if (clicked == expectedActions - 1) selected = sword;
                        }
                        if (selected == null)
                        {
                            LoggerInstance.Msg($"Cup solo smoke: native cards {string.Join(",", cards.Select(card => $"{card.ChoiceState?.Summary}/{ActiveButton(card) != null}"))}");
                            break;
                        }
                        if (AttackForecastSmoke) CaptureAttackForecastForSmoke();
                        if (SoloBattleUiSmoke)
                        {
                            yield return new WaitForEndOfFrame();
                            SaveSmokeScreenshot(Path.ChangeExtension(SmokeScreenshot, null) + $"-solo-{clicked + 1}.png");
                            if (!VerifySoloBattleDisplayForSmoke())
                            {
                                Fail(new InvalidOperationException("The sole battle participant did not own the visible status UI."));
                                yield break;
                            }
                        }
                        if (SoloUiWarriorSmoke && Settings.InheritDisabledAction && clicked == 1)
                        {
                            LoggerInstance.Msg("Cup solo UI smoke passed | floor 4 warrior, both actions have the native front-row weapon choices and the soldier profile is inactive.");
                            Finish("smoke_solo_ui_front_pass");
                            yield break;
                        }
                        clicked++;
                        hit = false;
                        cardReady = barReady = -1;
                        LoggerInstance.Msg($"Cup solo smoke: slot {clicked} {Read("turnCharacter")} selected native card {selected.ChoiceState.Summary}");
                        ActiveButton(selected)!.onClick.Invoke();
                        continue;
                    }
                }
                if (clicked > 0 && !hit)
                {
                    var bar = UnityEngine.Object.FindObjectsByType<AttackBar>(FindObjectsSortMode.None)
                        .FirstOrDefault(candidate => candidate.isActiveAndEnabled && candidate.IsTimingInputActive &&
                            candidate.initializedItemData != null);
                    if (bar != null)
                    {
                        if (SoloUiWarriorSmoke && clicked == expectedActions &&
                            (bar.initializedItemData.resourceName != "ironSword" || Read("turnCharacterPosition") != "front")) break;
                        var sections = bar.colorSections.ToArray().Where(candidate => candidate.TargetId == "enemy1" &&
                            candidate.SectionEnd > candidate.SectionStart);
                        var section = SoloSmokeDreak && clicked == expectedActions
                            ? sections.FirstOrDefault(candidate => candidate.Options.ToArray().Any(option =>
                                option.Type == ColorSectionOptionType.StealWeapon)) ?? sections.FirstOrDefault()
                            : sections.FirstOrDefault();
                        if (section == null) continue;
                        if (barReady < 0) barReady = Time.realtimeSinceStartupAsDouble;
                        if (Time.realtimeSinceStartupAsDouble - barReady < 0.3) continue;
                        if (SoloUiWarriorSmoke && clicked == expectedActions)
                            LoggerInstance.Msg("Cup solo weapon UI | ironSword opened its native front-row attack bar.");
                        float center = bar.GetAbsolutePosition((section.SectionStart + section.SectionEnd) / 2);
                        bar.SetGuidedCursorConstraint(center, center);
                        bar.SetPrimaryCursorPositionX(center);
                        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Enter));
                        hit = keyDown = true;
                        continue;
                    }
                }
                if (clicked > 0)
                {
                    if (SoloSmokeDreak) AdvanceWeaponSmoke();
                    else if (player!.WaitingForInput) advance.Activate(1f);
                }
            }
        }
        finally { InputSystem.RemoveDevice(keyboard); }
        Fail(new InvalidOperationException($"Cup solo smoke failed | {player?.PlaybackSpot.ScriptName}:{player?.PlaybackSpot.LineIndex} " +
            $"| disabled={disabled} inherit={Settings.InheritDisabledAction} clicked={clicked} hit={hit} | actor={Read("turnCharacter")} " +
            $"| actions={string.Join(",", soloSmokeActions)} | enemy phase={soloSmokeEnemyPhase} round done={soloSmokeRoundDone} " +
            $"| targets={string.Join(",", soloSmokeEnemyTargets)} | dreak={sawDreak} deaths={Run.Deaths} " +
            $"| excluded HP={excludedHealth}->{Read(excludedId + "Health")}"));
    }

    private const string WarriorSoloUiScript = "CupWarriorSoloUiSmoke";

    private System.Collections.IEnumerator VerifyWarriorSoloUiForSmoke()
    {
        player!.Stop();
        var script = Script.FromScriptText(WarriorSoloUiScript, "@clearChoice * wait:false\n@hideUI MapUI time:0\n" +
            "@set showMap=false\n@set addEnemyResourceNames=\"copyPaster\"\n@set enemyNumber=1\n@gosub Battle\n@stop", (string?)null);
        RegisterCupScript(WarriorSoloUiScript, script);
        var play = player.PreloadAndPlayAsync(script);
        while (!play.IsCompleted && Run.Running) yield return null;
        play.GetAwaiter().GetResult();
        yield return VerifySoloForSmoke();
    }

    private bool VerifySoloBattleDisplayForSmoke()
    {
        var panels = BattleStatusPanelsForSmoke();
        var battle = UnityEngine.Object.FindObjectsByType<BattleCharacterSpriteController>(FindObjectsSortMode.None).First();
        var soldierHealth = battle.GetHealthDisplayGroup("player");
        var companionHealth = battle.GetHealthDisplayGroup("partner");
        LoggerInstance.Msg($"Cup solo UI smoke | actor={SoloActor} slot={Read("actionCount")} " +
            $"front={Read("forwardCharacter")} row={Read("turnCharacterPosition")} native front={battle.GetFrontCharacterId()} | " +
            $"show player={Read("showPlayerStatus")} partner={Read("showPartnerStatus")} | " +
            $"world health player={soldierHealth?.alpha} companion={companionHealth?.alpha} | " +
            string.Join(" | ", panels.Select(panel => $"{panel.statusOwner} active={panel.gameObject.activeSelf} {panel._rectTransform?.position} " +
                string.Join("/", panel.GetComponentsInParent<CanvasGroup>(true).Select(group => $"{group.name}:{group.alpha}/{group.ignoreParentGroups}")))));
        return Read("forwardCharacter") == SoloActor && battle.GetFrontCharacterId() == SoloActor &&
            Read("turnCharacterPosition") == "front" &&
            panels.Any(panel => panel.statusOwner == SoloActor && IsVisibleStatusPanel(panel)) &&
            panels.Any(panel => panel.statusOwner == soloExcluded) &&
            !panels.Any(panel => panel.statusOwner == soloExcluded && panel.gameObject.activeSelf);
    }

    private static DraggableStatusUI[] BattleStatusPanelsForSmoke() => Resources.FindObjectsOfTypeAll<DraggableStatusUI>()
        .Where(panel => panel.gameObject.scene.IsValid() && panel.statusOwner is "player" or "partner").ToArray();

    private static bool IsVisibleStatusPanel(DraggableStatusUI panel) =>
        panel.gameObject.activeInHierarchy && panel.GetComponentsInParent<CanvasGroup>(true).All(group => group.alpha > 0.01f);

    private void CaptureAttackForecastForSmoke()
    {
        var enemy = UnityEngine.Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None)
            .First(controller => controller.enemyId == 1);
        if (HasSoloSmokeArgument("--annihilated-cup-attack-forecast-block"))
            vars!.SetVariableValue("playerStateBlock", "3");
        var preview = Engine.GetService<PartyHealthPreviewService>().CalculateEnemyPhasePreview(0, "player");
        float intent = EnemyCustomFunctions.GetEnemyNextActionDisplayedValueSum(1);
        var settings = Settings;
        float baseline;
        try
        {
            Settings = settings with { EnemyAttackMultiplier = 1 };
            baseline = EnemyCustomFunctions.GetEnemyNextActionDisplayedValueSum(1);
        }
        finally { Settings = settings; }
        string label = System.Text.RegularExpressions.Regex.Replace(enemy.NextActionText.text ?? "", "<[^>]*>", "");
        attackForecastSample = (baseline, intent, preview.Player.Damage, Number("playerHealth"), label);
    }
}
