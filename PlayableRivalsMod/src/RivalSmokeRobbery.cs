using Il2Cpp;
using Il2CppNaninovel;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

namespace PlayableRivals;

public sealed partial class RivalMod
{
    private System.Collections.IEnumerator VerifyNativeRobbery(IInputSampler advance)
    {
        var preparation = FindSmokeCard(Card.Steal);
        if (preparation == null)
        {
            LoggerInstance.Error($"RIVALS SMOKE FAILED no usable preparation on round {Round}");
            yield break;
        }

        // Give the fixture a second action in the same round so its actual next input is the weapon.
        Write("partnerStateActionPlus", 1);
        Write("xSwitchGameCursorOperationMode", 0);
        int stolen = Number("enemy1StolenWeaponCount");
        preparation.onClick.Invoke();
        float deadline = Time.realtimeSinceStartup + 16;
        Button? sword = null;
        while (Time.realtimeSinceStartup < deadline)
        {
            if (StealPrepared && (sword = FindSmokeCard(Card.Sword)) != null) break;
            if (player!.WaitingForInput) advance.Activate(1f);
            yield return null;
        }

        if (sword == null || smokePreparationHealed ||
            LastUsed(Card.Steal) != Round || Number("partnerStateActionPlus") != 0)
        {
            LoggerInstance.Error($"RIVALS SMOKE FAILED preparation state | round={Round} used={LastUsed(Card.Steal)} ready={StealPrepared} heal={smokePreparationHealed} actor={Read("turnCharacter")} choice={sword != null} at {player!.PlaybackSpot.ScriptName}:{player.PlaybackSpot.LineIndex}");
            yield break;
        }
        LoggerInstance.Msg("RIVALS ROBBERY PREPARED | native card resolved without heal and consumed one action");
        sword.onClick.Invoke();

        var keyboard = InputSystem.AddDevice<Keyboard>();
        try
        {
            deadline = Time.realtimeSinceStartup + 21;
            float barReady = -1;
            bool pressed = false;
            bool released = false;
            var items = Engine.GetService<ItemService>();
            string stolenItem = "";
            while (Time.realtimeSinceStartup < deadline)
            {
                if (Number("enemy1StolenWeaponCount") > stolen && stolenItem.Length == 0)
                    stolenItem = Read("enemyStealItem");
                if (stolenItem.Length > 0 && !StealPrepared &&
                    Enumerable.Range(1, Math.Max(Number("nowItemCount"), 8)).Any(
                        slot => items.GetItemBySlotNumber(slot)?.resourceName == stolenItem))
                {
                    LoggerInstance.Msg($"RIVALS SMOKE PASSED hero | native steal sector added {stolenItem} to inventory and consumed preparation");
                    yield break;
                }
                if (pressed && !released)
                {
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                    released = true;
                }
                var bar = UnityEngine.Object.FindObjectsByType<AttackBar>(FindObjectsSortMode.None)
                    .FirstOrDefault(candidate => candidate.isActiveAndEnabled && Flag("xSwitchAttackBarProcessing"));
                if (bar != null && !pressed && bar.IsTimingInputActive &&
                    bar.initializedItemData?.resourceName == CardPrefix + Card.Sword)
                {
                    var sections = bar.colorSections?.ToArray() ?? Array.Empty<ColorSection>();
                    var section = sections.FirstOrDefault(candidate =>
                        candidate.TargetId == "enemy1" && candidate.SectionEnd > candidate.SectionStart &&
                        candidate.Options.ToArray().Any(option => option.Type == ColorSectionOptionType.StealWeapon));
                    if (section != null)
                    {
                        if (barReady < 0)
                        {
                            barReady = Time.realtimeSinceStartup;
                            int breakGauge = Number("enemy1Break");
                            if (breakGauge <= 0)
                            {
                                LoggerInstance.Error("RIVALS SMOKE FAILED theft fixture enemy already broken");
                                yield break;
                            }
                            LoggerInstance.Msg($"RIVALS ROBBERY SECTOR | goblin break gauge={breakGauge}");
                        }
                        if (Time.realtimeSinceStartup - barReady > 0.3f)
                        {
                            float center = bar.GetAbsolutePosition((section.SectionStart + section.SectionEnd) / 2);
                            var guaranteed = new Il2CppSystem.Collections.Generic.List<ColorSection>();
                            guaranteed.Add(section);
                            bar.SetGuidedGuaranteedHitSections(
                                guaranteed.Cast<Il2CppSystem.Collections.Generic.IEnumerable<ColorSection>>());
                            bar.ActivateGuidedCursorConstraint(section, 0, center);
                            LoggerInstance.Msg($"RIVALS ROBBERY INPUT | sector={section.SectionStart:F3}–{section.SectionEnd:F3} cursor={bar.cursorRectTransform.anchoredPosition.x:F3}");
                            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Enter));
                            pressed = true;
                        }
                    }
                }
                else if (!Flag("xSwitchAttackBarProcessing") && player!.WaitingForInput) advance.Activate(1f);
                yield return null;
            }
            LoggerInstance.Error($"RIVALS SMOKE FAILED native theft | sector={barReady >= 0} pressed={pressed} item={stolenItem} count={Number("enemy1StolenWeaponCount")} prepared={StealPrepared} at {player!.PlaybackSpot.ScriptName}:{player.PlaybackSpot.LineIndex} inventory={string.Join(",", Enumerable.Range(1, 8).Select(slot => items.GetItemBySlotNumber(slot)?.resourceName))}");
        }
        finally { InputSystem.RemoveDevice(keyboard); }
    }
}
