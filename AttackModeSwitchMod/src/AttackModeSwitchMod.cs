using HarmonyLib;
using Il2CppNaninovel.UI;
using Il2CppTMPro;
using MelonLoader;

[assembly: MelonInfo(typeof(AnnihilatedAttackModeSwitch.AttackModeSwitchMod),
    "Annihilated Attack Mode Switch", "1.1.0", "Brick")]

namespace AnnihilatedAttackModeSwitch;

public sealed class AttackModeSwitchMod : MelonMod
{
    public override void OnInitializeMelon()
    {
        if (Environment.GetCommandLineArgs().Contains("--annihilated-cup", StringComparer.Ordinal))
            return;

        var refresh = AccessTools.Method(typeof(CustomGameSettingsFreeCursorOperationModeDropdown),
            nameof(CustomGameSettingsFreeCursorOperationModeDropdown.RefreshInteractable));
        HarmonyInstance.Patch(refresh,
            postfix: new HarmonyMethod(typeof(AttackModeSwitchMod), nameof(EnableDuringBattle)));
    }

    private static void EnableDuringBattle(CustomGameSettingsFreeCursorOperationModeDropdown __instance)
    {
        var variables = __instance.variableManager;
        if (variables is null || variables.GetVariableValue("timing") != "battle" ||
            variables.GetVariableValue("gameStarted") is not ("1" or "true" or "True") ||
            variables.GetVariableValue("xSwitchAttackBarProcessing") == "1")
            return;

        var dropdown = __instance.GetComponent<TMP_Dropdown>();
        if (dropdown == null)
            return;

        // The native control owns the selected value, localization and settings callbacks.
        dropdown.interactable = true;
        var row = __instance.rowCanvasGroup;
        if (row != null)
        {
            row.interactable = true;
            row.blocksRaycasts = true;
            row.alpha = 1f;
        }
    }
}
