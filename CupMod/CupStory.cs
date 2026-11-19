using Il2CppNaninovel;
using Il2CppNaninovel.Commands;
using Il2CppNaninovel.UI;
using MelonLoader;
using UnityEngine;

namespace CupCompetition;

public sealed partial class CupMod
{
    private int storyChoicesSubmitted;

    private bool SkipStoryCommand(Command command)
    {
        var spot = command.PlaybackSpot;
        return Settings.FastStory && Target != null && IsStoryPresentation(spot.ScriptName, spot.LineIndex) &&
            command.GetIl2CppType().Name is nameof(PrintText) or nameof(AppendText) or nameof(Wait) or
                nameof(WaitForInput) or nameof(ImpulseDisplay) or nameof(Flash);
    }

    private static bool IsStoryPresentation(string script, int line)
    {
        // These sequences use native timing or battle inputs within a scenario script.
        if ((script, line) is ("Scenario_Warrior4", >= 570 and <= 714) or
            ("Scenario_Hero5", >= 1916 and <= 1958) or ("Scenario_Priest5", >= 1730 and <= 1750))
            return false;
        return script is "Opening" or "Opening2" or "Opening3" or "Scenario" or "Ending" ||
            script.StartsWith("Scenario_", StringComparison.Ordinal) ||
            script == "Func" && line is >= 1234 and <= 1375; // Skill acquisition.
    }

    private void HandleStoryCommand(Command command, string script, int line)
    {
        if (!Settings.FastStory || Target is not { } route || !IsStoryPresentation(script, line)) return;
        switch (command.GetIl2CppType().Name)
        {
            case nameof(Stop):
                if (StoryDestination(route, script, line) is { } destination)
                    MelonCoroutines.Start(SubmitStoryChoice(command.Pointer, script, line, destination));
                break;
            case nameof(ModifyCharacter): command.Cast<ModifyCharacter>().Duration = 0f; break;
            case nameof(ModifyBackground): command.Cast<ModifyBackground>().Duration = 0f; break;
            case nameof(VCamera): command.Cast<VCamera>().Duration = 0f; break;
            case nameof(HideActors): command.Cast<HideActors>().Duration = 0f; break;
            case nameof(HideAllCharacters): command.Cast<HideAllCharacters>().Duration = 0f; break;
            case nameof(HidePrinter): command.Cast<HidePrinter>().Duration = 0f; break;
            case nameof(AnimateActor): command.Wait = false; break;
            case nameof(ActorSpriteEffect): command.Wait = false; break;
            case nameof(ChangeVolume): command.Wait = false; break;
            case nameof(DisplayEffect): command.Wait = false; break;
            case nameof(PlayBgm): command.Wait = false; break;
            case "PlaySfx": command.Wait = false; break; // Both runtime and game define this command.
            case nameof(StopBgm): command.Wait = false; break;
            case nameof(StopSfx): command.Wait = false; break;
        }
    }

    private System.Collections.IEnumerator SubmitStoryChoice(IntPtr stop, string script, int line, string destination)
    {
        double deadline = Time.realtimeSinceStartupAsDouble + 8;
        while (Run.Running)
        {
            yield return null;
            if (!Run.Running || player!.PlayedCommand?.Pointer != stop) yield break;
            foreach (var choice in UnityEngine.Object.FindObjectsByType<ChoiceHandlerButton>(FindObjectsSortMode.None))
            {
                if (ActiveButton(choice) is not { } button ||
                    choice.ChoiceState.OnSelectScript?.TrimEnd().EndsWith(destination, StringComparison.Ordinal) != true)
                    continue;
                try
                {
                    storyChoicesSubmitted++;
                    LoggerInstance.Msg($"Cup story choice | goal={GoalId} | route={Target!.Id} | {script}:{line} -> {destination}");
                    button.onClick.Invoke();
                }
                catch (Exception ex) { Fail(ex); }
                yield break;
            }
            if (Time.realtimeSinceStartupAsDouble >= deadline)
            {
                LoggerInstance.Warning($"Cup story choice unavailable | route={Target!.Id} | {script}:{line} -> {destination}");
                Finish("required_choice_locked");
                yield break;
            }
        }
    }

    // Native @stop positions and their @choice destinations in Steam build 26705080.
    // Early choices deliberately prepare the existing distrust and dependence checks.
    private string? StoryDestination(Route route, string script, int line)
    {
        bool heroBad = route.Id == "hero4Bad";
        bool priestLow = route.Id is "priest4KilledBad" or "priestNodAfterLowSan";
        return (script, line) switch
        {
            ("Opening3", 83) when route.Id == "ritual" => ".priestScene",
            ("Opening3", 1395) when route.Id == "ritual" => ".priestChoiceKokokara",
            ("Opening3", 1436) => route.Id == "ritual" ? ".priestChoiceGisikiYes" : ".priestChoiceGisikiNo",
            ("Opening3", 1990) => ".PartnerChoice" + char.ToUpperInvariant(route.Partner[0]) + route.Partner[1..],
            ("Opening3", 2022) => ".PartnerChoiceKakunin",
            ("Opening3", 2030) => ".PartnerChoiceEnd",
            ("Opening3", 2404) => ".MageChoiceConfirm",
            ("Opening3", 2472) => ".MageChoiceConfirm2",
            ("Opening3", 2654) => ".MageChoiceNotYakutatazu",
            ("Opening3", 2866) => ".PriestChoiceConfirm",

            ("Scenario_Hero1", 736) => heroBad ? ".OutcastChoiceCantTrustHero" : ".OutcastChoiceBelieveHero",
            ("Scenario_Hero2", 95) => ".ChoiceHeroKoke2Tabe",
            ("Scenario_Hero2", 635) => ".ChoiceVillainessTeaNoru",
            ("Scenario_Hero2", 1099) => ".ChoiceVillainessKansouSabisii",
            ("Scenario_Hero2", 1496) => heroBad ? ".ChoiceVillainessLastKaibutsu" : ".ChoiceVillainessLastTsurai",
            ("Scenario_Hero3", 925) => ".Dropout2HeroChoice1Hai",
            ("Scenario_Hero3", 939) => ".Dropout2HeroChoice2",
            ("Scenario_Hero3", 1052) => heroBad ? ".Dropout2HeroChoiceTekiHai" : ".Dropout2HeroChoiceTekiIie",
            ("Scenario_Hero3", 2038) => route.HeroChoice switch
            {
                2035 => ".ChoiceDropoutWithoutHero",
                2037 => ".ChoiceDropoutNobody",
                _ => ".ChoiceDropoutWithHero"
            },
            ("Scenario_Hero5", 100) => ".ChoiceHeroWillDefeatForWorld",
            ("Scenario_Hero5", 1449) => ".ChoiceHeroDisagreePriest",

            ("Scenario_Mage1", 96) => ".ChoiceMageSearchFloor",
            ("Scenario_Mage1", 203) => route.Id == "mage-exit" ? ".ChoiceMageReturn" : ".ChoiceMageProtectNext",
            ("Scenario_Mage1", 307) => ".ChoiceMageQuestionTrapYouAreKind",
            ("Scenario_Mage1", 489) => ".ChoiceMageRational",
            ("Scenario_Mage1", 1101) => ".MageChoiceRiyuuHuyou",
            ("Scenario_Mage2", 84) => ".MageEncourage",
            ("Scenario_Mage2", 160) => ".HoldHandsSuggestion",
            ("Scenario_Mage2", 299) => ".DollComplementary",
            ("Scenario_Mage2", 492) => ".AgreedTraitor",
            ("Scenario_Mage2", 557) => ".RefuseAbandon",
            ("Scenario_Mage2", 850) => ".MagePokerDraw5",
            ("Scenario_Mage2", 931) => ".MageShowdownWin",
            ("Scenario_Mage2", 1029) => ".MageWishFuture",
            ("Scenario_Mage2", 1943) => ".VChoiceAtama",
            ("Scenario_Mage2", 2051) => ".VMeChoiceSyubou",
            ("Scenario_Mage2", 2470) => ".TokubetsuChoiceAru",
            ("Scenario_Mage3", 427) => ".MageChoiceKoukaiSitenai",
            ("Scenario_Mage3", 1172) => route.Id is "mageMaryoku" or "mage5YakusokuSinaiBad"
                ? ".MageChoiceGetMaryoku" : ".MageChoiceNotMaryoku",
            ("Scenario_Mage4", 385) => ".MageChoiceKodoku",
            ("Scenario_Mage4", 438) => ".MageJogenAdvice",
            ("Scenario_Mage4", 621) => ".MageChoiceUramiNai",
            ("Scenario_Mage4", 1566) => route.Id == "mageNotMaryokuSuki" ? ".MageChoicekokuhakusuki" : ".MageChoicekokuhakuflat",
            ("Scenario_Mage5", 254) => route.Id == "mage5YakusokuSinaiBad" ? ".MageChoiceYakusokuSinai" : ".MageChoiceYakusokuSuru",
            ("Scenario_Mage5", 2229) => ".RouletteDevour1",
            ("Scenario_Mage5", 2242) => ".RouletteDevour2",
            ("Scenario_Mage5", 2256) => ".RouletteDevour3",

            ("Scenario_Warrior1", 616) => ".ChoiceOutcastA",
            ("Scenario_Warrior1", 1545) => route.Id == "warriorKill" ? ".ChoiceOutcastFinalB" : ".ChoiceOutcastFinalA",
            ("Scenario_Warrior2", 2618) => ".ChoiceWarriorMamaiBelievePossibility",
            ("Scenario_Warrior2", 2960) => ".ChoiceWarriorTeianKotowaru",
            ("Scenario_Warrior2", 3487) => route.Box ? ".ChoiceWarriorBattleAfterKutsugaesu" : ".ChoiceWarriorBattleAfterTsuranuku",
            ("Scenario_Warrior2", 3518) => route.Box ? ".ChoiceWarriorBattleAfterKutsugaesuS" : ".ChoiceWarriorBattleAfterTsuranukuS",
            ("Scenario_Warrior2", 3549) => ".ChoiceWarriorBattleAfterKorosu",
            ("Scenario_Warrior3", 774) => ".WarriorChoiceUkenaiConfirmed",
            ("Scenario_Warrior3", 1438) => route.Id == "dropout_warrior" ? ".ChoiceWarriorRakudaiWith" : ".ChoiceWarriorRakudaiKotowaru",
            ("Scenario_Warrior5", 1102) => ".WarriorTomeru",
            ("Scenario_Warrior5", 1150) => (route.Id is "warriorGlitchBad" or "warriorAfterGlitch") &&
                !Flag("xSwitchWarriorGlitchEnd") ? ".WarriorGlitchEnd" : ".WarriorTomeru2",

            ("Scenario_Priest1", 86) => ".PriestChoiceNoroiKaerareta",
            ("Scenario_Priest1", 351) => priestLow ? ".PriestChoicePanKiseki" : ".PriestChoicePanNotKiseki",
            ("Scenario_Priest1", 885) => priestLow ? ".PriestChoiceNotSlash" : ".PriestChoiceSlash",
            ("Scenario_Priest1", 1161) => priestLow ? ".PriestChoiceTrust" : ".PriestChoiceNotTrust",
            ("Scenario_Priest2", 253) => priestLow ? ".ChoicePriestSoupKutiwoakeru" : ".ChoicePriestSoupKotowaru",
            ("Scenario_Priest2", 477) => priestLow ? ".PriestChoiceOtonasikuKiku" : ".PriestChoiceImiganai",
            ("Scenario_Priest2", 542) => ".PriestChoiceAtamaNagerenai",
            ("Scenario_Priest2", 822) => ".SentakusiIkiruhouga",
            ("Scenario_Priest2", 1403) => ".PriestChoiceAtamaNagerenai2",
            ("Scenario_Priest2", 1951) => ".ChoiceImportantToYou",
            ("Scenario_Priest3", 263) => priestLow ? ".ChoicePriestBelieve" : ".ChoicePriestDeny",
            ("Scenario_Priest3", 1167) => route.PriestChoice(Flag("xSwitchPriestLowSanEnded")) switch
            {
                1164 => ".ChoicePriestLook",
                1166 => ".ChoiceTakeHand",
                _ => ".ChoiceRefuseHand"
            },
            ("Scenario_Priest3", 1432) => ".ChoiceEllipsis",
            ("Scenario_Priest3", 1534) => ".ChoiceUnderstand",
            ("Scenario_Priest3", 1827) => ".ChoiceHeroRefuse",
            ("Scenario_Priest4", 1658) => ".ChoicePriestBladeOk",
            ("Scenario_Priest4", 2914) => route.Id == "priest4NoReachBad" ? ".ChoicePriestJustLookedDown" : ".ChoicePriestDontWantToDie",
            ("Scenario_Priest5", 53) => ".ChoicePriestUnderstand",
            ("Scenario_Priest5", 143) => route.Id is "priestLowLoop" or "priestHighLoop" ? ".ChoicePriestShakeHead" : ".ChoicePriestNod",
            ("Scenario_Priest5", 1141) => ".ChoicePriestSuki",
            _ => null
        };
    }
}
