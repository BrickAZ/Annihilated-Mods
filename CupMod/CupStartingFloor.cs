using System.Globalization;
using Il2CppNaninovel;
using Il2CppNaninovel.Commands;

namespace CupCompetition;

public sealed partial class CupMod
{
    private bool startFloorApplied;
    private const string StartingLevelScriptName = "AnnihilatedCupStartingLevel";

    // Use the original per-level stat allocation and status calculation without the level-up presentation.
    private const string StartingLevelScript = @"
# Apply
@set tempCharacter=""player""
# PlayerLevelLoop
@if playerLv<cupStartingLevel
    @gosub StatusFunc.ApplyPlayerAutoStatusAddOneLevel
    @set playerLv++
    @gosub StatusFunc.StatusSetting
    @goto .PlayerLevelLoop
@endif
@set playerExp=0
@set playerNextExp=CalcNextExp(playerLv)
@set playerMaxNextExp=playerNextExp
@set playerHealth=playerMaxHealth
@if partnerCharacter!=""""
    @set tempCharacter=""partner""
    @set partnerLv=cupStartingLevel
    @set partnerExp=0
    @set partnerNextExp=CalcNextExp(partnerLv)
    @set partnerMaxNextExp=partnerNextExp
    @gosub StatusFunc.StatusSetting
    @set partnerHealth=partnerMaxHealth
    @gosub Func.SavePartnerCharacterState
@endif
@gosub Func.HideMenu
@return";

    private void ApplyStartingFloor(Gosub command)
    {
        if (startFloorApplied)
        {
            if (Settings.StartingFloor > 1) command.Path = new NamedString("Func", "HideMenu");
            return;
        }
        startFloorApplied = true;
        int floor = Settings.StartingFloor;
        if (floor <= 1) return;

        string partner = Read("partnerCharacter");
        bool mageWithoutMagic = Target?.Id.StartsWith("mageNotMaryoku", StringComparison.Ordinal) == true;
        if (floor >= 4 && partner == "mage")
        {
            vars!.SetVariableValue("xSwitchMageChoiceGetMaryoku", mageWithoutMagic ? "0" : "1");
            vars.SetVariableValue("xSwitchMageChoiceNotMaryoku", mageWithoutMagic ? "1" : "0");
            vars.SetVariableValue("mageType", mageWithoutMagic ? "" : "Maryoku");
            if (mageWithoutMagic) vars.SetVariableValue("xSwitchMageSuperBuff", "1");
            if (floor >= 5 && Target?.Id == "mageNotMaryokuSuki")
                vars.SetVariableValue("xSwitchMageChoiceKokuhakuSuki", "1");
        }
        if (partner == "warrior")
        {
            // Previous choices affect the warrior's fifth-floor battle and ending.
            if (Target?.Id == "warriorKill") vars!.SetVariableValue("xSwitchChoiceOutcastFinalB", "1");
            if (floor >= 3 && Target?.Box == true)
            {
                vars!.SetVariableValue("xSwitchWarriorBattleAfterKutsugaesu", "1");
                vars.SetVariableValue("xSwitchChoiceWarriorBattleAfterKutsugaesuS", "1");
            }
        }

        int dungeon = floor switch
        {
            6 => 10,
            5 when partner == "warrior" => 6,
            5 when partner == "priest" => 7,
            5 when partner == "mage" => mageWithoutMagic ? 9 : 8,
            _ => floor
        };
        vars!.SetVariableValue("dungeonId", dungeon.ToString(CultureInfo.InvariantCulture));
        vars.SetVariableValue("sectionId", "1");
        vars.SetVariableValue("dungeonX", "0");
        int startingLevel = 1 + (floor - 1) * 10;
        vars.SetVariableValue("cupStartingLevel", startingLevel.ToString(CultureInfo.InvariantCulture));
        RegisterCupScript(StartingLevelScriptName, Script.FromScriptText(StartingLevelScriptName, StartingLevelScript, (string?)null));
        command.Path = new NamedString(StartingLevelScriptName, "Apply");
        LoggerInstance.Msg($"Cup jump start | floor={floor} | dungeon={dungeon} | partner={partner} | starting level={startingLevel}");
    }
}
