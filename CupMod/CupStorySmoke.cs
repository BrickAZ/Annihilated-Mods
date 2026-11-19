using Il2Cpp;
using Il2CppNaninovel;
using Il2CppNaninovel.Commands;
using Il2CppNaninovel.UI;
using UnityEngine;
using UnityEngine.UI;

namespace CupCompetition;

public sealed partial class CupMod
{
    private bool StorySmokeRequested => Environment.GetCommandLineArgs()
        .Contains("--annihilated-cup-story-smoke", StringComparer.Ordinal);
    private bool ManualStorySmoke => StorySmokeRequested && Environment.GetCommandLineArgs()
        .Contains("--annihilated-cup-story-manual", StringComparer.Ordinal);

    private System.Collections.IEnumerator VerifyManualStoryOpeningForSmoke()
    {
        var advance = Engine.GetService<IInputManager>().GetSampler(InputConfiguration.ContinueName);
        double deadline = Time.realtimeSinceStartupAsDouble + 18;
        while (Run.Running && Time.realtimeSinceStartupAsDouble < deadline &&
               (!player!.WaitingForInput || player.PlaybackSpot.ScriptName != "Opening")) yield return null;
        var dialogue = player!.PlaybackSpot;
        yield return new WaitForSecondsRealtime(0.45f);
        if (!Run.Running || !player.WaitingForInput || dialogue.ScriptName != "Opening" ||
            player.PlaybackSpot.LineIndex != dialogue.LineIndex)
        {
            Fail(new InvalidOperationException("Manual story did not wait for input on the native opening dialogue."));
            yield break;
        }
        LoggerInstance.Msg($"Cup manual story opening waited for dialogue input | {dialogue.ScriptName}:{dialogue.LineIndex}");

        // Hold the game's own skip input during the test to advance the long prologue.
        // Even with that input held, actual story choices still need their native click.
        var skip = Engine.GetService<IInputManager>().GetSampler(InputConfiguration.SkipName);
        var previousSkipMode = player.SkipMode;
        player.SkipMode = PlayerSkipMode.Everything;
        deadline = Time.realtimeSinceStartupAsDouble + 160;
        try
        {
            while (Run.Running && !explorationStarted && Time.realtimeSinceStartupAsDouble < deadline)
            {
                yield return null;
                var spot = player.PlaybackSpot;
                if (spot.ScriptName == "Opening3" && player.PlayedCommand?.GetIl2CppType().Name == nameof(Stop))
                {
                    string? destination = spot.LineIndex switch
                    {
                        83 => Flag("xSwitchOpMageChoiceHanasiEnd") ? ".kangaeScene" : ".mageScene",
                        522 => Flag("xSwitchOpMageChoiceKorekara") ? ".mageChoiceHanasiOwari" : ".mageChoiceKorekara",
                        1822 => "",
                        _ => StoryDestination(Target!, spot.ScriptName, spot.LineIndex)
                    };
                    if (destination != null && FindStoryButtonForSmoke(destination) is { } button)
                    {
                        LoggerInstance.Msg($"Cup smoke manually clicked {spot.ScriptName}:{spot.LineIndex} -> {destination}");
                        button.onClick.Invoke();
                    }
                }
                else
                {
                    skip.Activate(1f);
                    if (player.WaitingForInput) advance.Activate(1f);
                }
            }
        }
        finally
        {
            skip.Activate(0f);
            player.SetSkipEnabled(false);
            player.SkipMode = previousSkipMode;
        }
        if (!explorationStarted && Run.Running)
            Fail(new InvalidOperationException($"Manual story opening stalled at {player.PlaybackSpot.ScriptName}:{player.PlaybackSpot.LineIndex}."));
    }

    private static Button? FindStoryButtonForSmoke(string destination)
    {
        foreach (var choice in UnityEngine.Object.FindObjectsByType<ChoiceHandlerButton>(FindObjectsSortMode.None))
        {
            string action = choice.ChoiceState?.OnSelectScript?.TrimEnd() ?? "";
            if ((destination.Length == 0 ? action.Length == 0 : action.EndsWith(destination, StringComparison.Ordinal)) &&
                ActiveButton(choice) is { } button) return button;
        }
        return null;
    }

    private System.Collections.IEnumerator VerifyStoryForSmoke()
    {
        double deadline = Time.realtimeSinceStartupAsDouble + 14;
        var map = Engine.GetService<MapService>();
        Button? tile = null;
        while (Run.Running && Time.realtimeSinceStartupAsDouble < deadline)
        {
            yield return null;
            tile = map.mapChoiceButtonContainer?.GetComponentsInChildren<Button>()
                .FirstOrDefault(button => button.isActiveAndEnabled && button.IsInteractable());
            if (tile != null && (player!.PlaybackSpot.ScriptName, player.PlaybackSpot.LineIndex) == ("Dungeon", 338)) break;
            tile = null;
        }
        if (tile == null || Target?.Id != "mageMaryoku" || Read("partnerCharacter") != "mage" ||
            !Flag("xSwitchMageOutWay") || Flag("xSwitchMageChoiceGetMaryoku"))
        {
            Fail(new InvalidOperationException($"Story smoke could not reach the Mage's opening map at {player?.PlaybackSpot.ScriptName}:{player?.PlaybackSpot.LineIndex}."));
            yield break;
        }

        int selectedTiles = seededTiles;
        int submittedChoices = storyChoicesSubmitted;
        int column = Number("dungeonX");
        yield return new WaitForSecondsRealtime(0.5f);
        if (!Run.Running || seededTiles != selectedTiles || !tile.IsInteractable())
        {
            Fail(new InvalidOperationException("Story fast-forward consumed the first native map choice."));
            yield break;
        }
        // Resolve the outstanding native map input before replacing playback for this test.
        tile.onClick.Invoke();
        deadline = Time.realtimeSinceStartupAsDouble + 5;
        while (Run.Running && seededTiles == selectedTiles && Time.realtimeSinceStartupAsDouble < deadline)
            yield return null;
        if (!Run.Running || seededTiles != selectedTiles + 1)
        {
            Fail(new InvalidOperationException("The test click did not resolve the first native map choice."));
            yield break;
        }
        selectedTiles = seededTiles;

        // Start the unmodified story at the save-alias label before its actual F3 branch.
        // The wrapper only provides the return from the scene to the playable map.
        const string name = "CupStoryRuntimeSmoke";
        player!.Stop();
        var script = Script.FromScriptText(name, "@clearChoice * wait:false\n" +
            "@hideUI MapUI time:0\n@set showMap=false\n" +
            "@set switchShowMenu=0\n@set xSwitchForceDisableSave=1\n" +
            "@gosub Scenario_Mage3.Test5\n" +
            "@goto Dungeon.ExploreStart", (string?)null);
        RegisterCupScript(name, script);
        var playback = player.PreloadAndPlayAsync(script);
        deadline = Time.realtimeSinceStartupAsDouble + (ManualStorySmoke ? 105 : 25);
        while (!playback.IsCompleted && Run.Running && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
        if (!playback.IsCompleted)
        {
            Fail(new InvalidOperationException("Story smoke could not load the native Mage branch."));
            yield break;
        }
        playback.GetAwaiter().GetResult();

        bool manuallySelected = false;
        var advance = Engine.GetService<IInputManager>().GetSampler(InputConfiguration.ContinueName);
        while (Run.Running && Time.realtimeSinceStartupAsDouble < deadline)
        {
            yield return null;
            if (ManualStorySmoke && (player.PlaybackSpot.ScriptName, player.PlaybackSpot.LineIndex) == ("Scenario_Mage3", 1172) &&
                !manuallySelected && FindStoryButtonForSmoke(".MageChoiceGetMaryoku") != null)
            {
                yield return new WaitForSecondsRealtime(0.65f);
                if (storyChoicesSubmitted != submittedChoices ||
                    (player.PlaybackSpot.ScriptName, player.PlaybackSpot.LineIndex) != ("Scenario_Mage3", 1172) ||
                    FindStoryButtonForSmoke(".MageChoiceGetMaryoku") is not { } manualChoice) break;
                manuallySelected = true;
                LoggerInstance.Msg("Cup manual story waited for the native branch button; smoke will click it now.");
                manualChoice.onClick.Invoke();
            }
            else if (ManualStorySmoke && (player.WaitingForInput ||
                player.PlayedCommand?.GetIl2CppType().Name is nameof(PrintText) or nameof(AppendText)))
                advance.Activate(1f);
            if ((player.PlaybackSpot.ScriptName, player.PlaybackSpot.LineIndex) != ("Dungeon", 338)) continue;
            var nextTile = map.mapChoiceButtonContainer?.GetComponentsInChildren<Button>()
                .FirstOrDefault(button => button.isActiveAndEnabled && button.IsInteractable());
            if (nextTile == null || (ManualStorySmoke ? !manuallySelected || storyChoicesSubmitted != submittedChoices
                                                    : storyChoicesSubmitted != submittedChoices + 1)) continue;
            if (seededTiles != selectedTiles || Number("dungeonX") != column ||
                !Flag("xSwitchMageChoiceGetMaryoku") || Flag("xSwitchMageChoiceNotMaryoku") ||
                Read("mageType") != "Maryoku" || Read("partnerLooksType") != "Maryoku") break;
            LoggerInstance.Msg($"Cup story smoke passed | mode={(Boss == null ? "ending" : "boss")} | fast={Settings.FastStory} | goal={GoalId} | route={Target.Id} | native magic branch, skill and map handoff verified");
            Finish("story_smoke_verified");
            yield break;
        }
        Fail(new InvalidOperationException($"Story smoke stalled at {player.PlaybackSpot.ScriptName}:{player.PlaybackSpot.LineIndex}: " +
            $"choices={storyChoicesSubmitted - submittedChoices}, magic={Read("mageType")}, appearance={Read("partnerLooksType")}, " +
            $"playing={player.Playing}, waiting={player.WaitingForInput}, command={player.PlayedCommand?.GetIl2CppType().Name}, " +
            $"map={Read("showMap")}, column={Number("dungeonX")}, tiles={seededTiles - selectedTiles}."));
    }
}
