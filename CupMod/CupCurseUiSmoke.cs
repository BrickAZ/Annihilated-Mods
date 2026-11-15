using Il2Cpp;
using Il2CppDemo;
using Il2CppNaninovel;
using Il2CppNaninovel.UI;
using UnityEngine;

namespace CupCompetition;

public sealed partial class CupMod
{
    private System.Collections.IEnumerator VerifyCurseUiForSmoke()
    {
        // Exercise the scroll list's button callbacks and the native confirmation flow.
        HashSet<string>? firstFloor = null;
        foreach (int floor in new[] { 1, 2 })
        {
            vars!.SetVariableValue("dungeonId", floor.ToString(System.Globalization.CultureInfo.InvariantCulture));
            double deadline = Time.realtimeSinceStartupAsDouble + 10;
            ChoiceHandlerButton? convert = null;
            while (Run.Running && convert == null && Time.realtimeSinceStartupAsDouble < deadline)
            {
                yield return null;
                convert = FindCupSmokeChoice(".BuyByCurse");
            }
            if (convert == null) { Fail(new InvalidOperationException("Curse menu entry missing.")); yield break; }
            ActiveButton(convert)!.onClick.Invoke();
            ItemScrollList? list = null;
            deadline = Time.realtimeSinceStartupAsDouble + 12;
            while (Run.Running && Time.realtimeSinceStartupAsDouble < deadline)
            {
                yield return null;
                list = ItemScrollList.GetActiveList();
                if (list != null && list.InitialViewportPrepared && list.curseGenerateCacheInitialized && cursePool != null) break;
            }
            if (list == null || !list.InitialViewportPrepared || cursePool == null)
            {
                Fail(new InvalidOperationException("Curse scroll list was not prepared."));
                yield break;
            }
            int balance = Number("curse");
            if (floor == 1) firstFloor = new HashSet<string>(cursePool);
            if (!list.CanSelectNextCurseGenerateItem("tailWhip") || list.CanSelectNextCurseGenerateItem("katana"))
            {
                Fail(new InvalidOperationException($"The curse list rejected a floor {floor} item or accepted a future item."));
                yield break;
            }
            string item = "";
            UnityEngine.UI.Button? card = null;
            deadline = Time.realtimeSinceStartupAsDouble + 4;
            while (card == null && Run.Running && Time.realtimeSinceStartupAsDouble < deadline)
            {
                yield return null;
                foreach (var cell in list.visibleCellsByIndex)
                {
                    string name = ItemScrollList.GetSelectionResourceName(cell.Value);
                    if (floor == 1 ? name != "blackKnightSword" : firstFloor!.Contains(name)) continue;
                    if (!list.CanSelectNextCurseGenerateItem(name) ||
                        cell.Value.GetComponent<UnityEngine.UI.Button>() is not { isActiveAndEnabled: true } button ||
                        !button.IsInteractable()) continue;
                    item = name;
                    card = button;
                    break;
                }
            }
            if (card == null)
            {
                Fail(new InvalidOperationException($"No selectable curse card appeared for floor {floor}."));
                yield break;
            }
            card.onClick.Invoke();
            yield return null;
            int total = list.SelectedCost;
            if (list.SelectedCount != 1 || total <= 0 || total >= balance || Number("curse") != balance)
            {
                Fail(new InvalidOperationException($"Clicking {item} did not select it at the expected cost."));
                yield break;
            }
            foreach (string action in new[] { "complete", "confirm" })
            {
                ChoiceHandlerButton? button = null;
                deadline = Time.realtimeSinceStartupAsDouble + 8;
                while (Run.Running && button == null && Time.realtimeSinceStartupAsDouble < deadline)
                {
                    yield return null;
                    button = UnityEngine.Object.FindObjectsByType<ChoiceHandlerButton>(FindObjectsSortMode.None)
                        .FirstOrDefault(choice => ActiveButton(choice) != null &&
                            choice.ChoiceState.OnSelectScript is { } script &&
                            script.Contains("curseGenerateAction", StringComparison.Ordinal) &&
                            script.Contains(action, StringComparison.Ordinal));
                }
                if (button == null)
                {
                    Fail(new InvalidOperationException($"Curse {action} choice missing."));
                    yield break;
                }
                ActiveButton(button)!.onClick.Invoke();
            }
            deadline = Time.realtimeSinceStartupAsDouble + 8;
            var advance = Engine.GetService<IInputManager>().GetSampler(InputConfiguration.ContinueName);
            while (Run.Running && FindCupSmokeChoice(".BuyByCurse") == null && Time.realtimeSinceStartupAsDouble < deadline)
            {
                yield return null;
                if (player!.WaitingForInput && !HasActiveControls()) advance.Activate(1f);
            }
            var inventory = Enumerable.Range(1, 24).Select(ItemCustomFunctions.GetItemResourceNameBySlotNumber).ToArray();
            if (Number("curse") != balance - total || !inventory.Contains(item) ||
                FindCupSmokeChoice(".BuyByCurse") == null)
            {
                Fail(new InvalidOperationException($"Curse UI purchase failed: floor={floor}, " +
                    $"balance={balance}->{Read("curse")}, cost={total}, inventory={string.Join(",", inventory)}."));
                yield break;
            }
            LoggerInstance.Msg($"Cup curse UI smoke: floor {floor} clicked {item}, " +
                $"confirmed native purchase, curse {balance}->{Read("curse")}, item in inventory.");
        }
        Finish("smoke_curse_ui_pass");
    }

    private static ChoiceHandlerButton? FindCupSmokeChoice(string destination) =>
        UnityEngine.Object.FindObjectsByType<ChoiceHandlerButton>(FindObjectsSortMode.None)
            .FirstOrDefault(choice => ActiveButton(choice) != null &&
                choice.ChoiceState.OnSelectScript?.TrimEnd().EndsWith(destination, StringComparison.Ordinal) == true);
}
