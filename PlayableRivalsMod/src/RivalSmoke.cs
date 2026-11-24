using System.Diagnostics;
using System.Runtime.InteropServices;
using Il2Cpp;
using Il2CppNaninovel;
using Il2CppNaninovel.UI;
using MelonLoader;
using MelonLoader.NativeUtils;
using UnityEngine;
using UnityEngine.UI;

namespace PlayableRivals;

public sealed partial class RivalMod
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void StateConstructor(IntPtr instance, IntPtr config, IntPtr engineConfig, IntPtr method);

    private NativeHook<StateConstructor>? smokeSaveHook;
    private StateConstructor? smokeSaveCallback;
    private string smokeRoute = "";
    private bool smokeStarted;
    private bool smokeIsolated;
    private bool smokeRobbery;
    private bool smokePreparationHealed;

    private void InstallSmokeHooks()
    {
        const string argument = "--playable-rivals-smoke=";
        smokeRoute = Environment.GetCommandLineArgs()
            .FirstOrDefault(value => value.StartsWith(argument, StringComparison.Ordinal))?[argument.Length..] ?? "";
        if (RivalRules.FromPartner(smokeRoute) == Rival.None) return;
        smokeSkills = Environment.GetCommandLineArgs().Contains("--playable-rivals-smoke-skills");
        smokeSwap = Environment.GetCommandLineArgs().Contains("--playable-rivals-smoke-swap");
        smokeRobbery = smokeRoute == "hero" && Environment.GetCommandLineArgs().Contains("--playable-rivals-smoke-robbery");
        Application.runInBackground = true;
        var module = Process.GetCurrentProcess().Modules.Cast<ProcessModule>()
            .Single(m => string.Equals(m.ModuleName, "GameAssembly.dll", StringComparison.OrdinalIgnoreCase));
        smokeSaveCallback = ConfigureSmokeSaves;
        LoggerInstance.Msg($"RIVALS SMOKE PROFILE {smokeRoute}");
        var address = module!.BaseAddress + 0xB22680;
        smokeSaveHook = new NativeHook<StateConstructor>(address, Marshal.GetFunctionPointerForDelegate(smokeSaveCallback));
        smokeSaveHook.Attach();
        if (smokeSaveHook!.TrampolineHandle == address) throw new InvalidOperationException("Smoke save profile hook failed.");
    }

    private void ConfigureSmokeSaves(IntPtr instance, IntPtr config, IntPtr engineConfig, IntPtr method)
    {
        if (config != IntPtr.Zero)
        {
            var settings = new StateConfiguration(config);
            string root = Path.Combine(Application.persistentDataPath, "NaninovelData");
            string original = Path.Combine(root, settings.SaveFolderName, "Settings.json");
            string targetProfile = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "annihilated-playable-rivals-smoke", smokeRoute));
            string relative = Path.GetRelativePath(root, targetProfile);
            if (Path.IsPathRooted(relative))
                throw new InvalidOperationException("Could not isolate the smoke save profile on the same drive.");
            Directory.CreateDirectory(targetProfile);
            settings.SaveFolderName = relative;
            string profile = Path.Combine(root, settings.SaveFolderName);
            string settingsFile = Path.Combine(profile, "Settings.json");
            if (!File.Exists(settingsFile) && File.Exists(original) &&
                CupCompetition.CupLanguage.CopyLocaleSetting(File.ReadAllBytes(original)) is { } locale)
            {
                Directory.CreateDirectory(profile);
                File.WriteAllBytes(settingsFile, locale);
            }
            smokeIsolated = true;
            LoggerInstance.Msg("RIVALS SMOKE SAVES " + settings.SaveFolderName);
        }
        smokeSaveHook!.Trampoline(instance, config, engineConfig, method);
    }

    private void UpdateSmoke()
    {
        if (smokeStarted || smokeRoute.Length == 0 || !smokeIsolated) return;
        var title = Engine.GetService<IUIManager>().GetUI<ITitleUI>()?.TryCast<TitleMenu>()
            ?? Resources.FindObjectsOfTypeAll<TitleMenu>().FirstOrDefault(menu => menu != null && menu.gameObject.activeInHierarchy);
        if (title?.Visible != true) return;
        smokeStarted = true;
        MelonCoroutines.Start(Smoke());
    }

    private System.Collections.IEnumerator Smoke()
    {
        const string name = "PlayableRivalsSmoke";
        LoggerInstance.Msg("RIVALS SMOKE START " + smokeRoute);
        var script = Script.FromScriptText(name, """
@gosub First.InitialSettings
@hideUI TitleUI time:0
@set playerType=2
@set forwardCharacter="partner"
@set showPlayerStatus=true
@set G_xSwitchFirstDeadOnPriestEventEnd=1
@goto Opening3.PartnerChoiceCore
""", (string?)null);
        var loader = Engine.GetService<IScriptManager>().Cast<ScriptManager>().scriptLoader;
        var source = loader.ProvisionSources[0];
        loader.LoadedResources.Add(new ResourceLoader<Script>.LoadedResource(
            new Resource<Script>(source.BuildFullPath(name), script), source));
        player!.Stop();
        var play = player.PreloadAndPlayAsync(script);
        float deadline = Time.realtimeSinceStartup + 18;
        while (!play.IsCompleted && Time.realtimeSinceStartup < deadline) yield return null;
        if (!play.IsCompleted)
        {
            LoggerInstance.Error("RIVALS SMOKE FAILED starting the selection script");
            yield break;
        }
        play.GetAwaiter().GetResult();
        var continueInput = Engine.GetService<IInputManager>().GetSampler(InputConfiguration.ContinueName);
        string title = char.ToUpperInvariant(smokeRoute[0]) + smokeRoute[1..];
        foreach (string target in new[] { ".PartnerChoice" + title, ".PartnerChoiceKakunin", ".PartnerChoiceEnd" })
        {
            deadline = Time.realtimeSinceStartup + 8;
            Button? button = null;
            while (button == null && Time.realtimeSinceStartup < deadline)
            {
                foreach (var choice in UnityEngine.Object.FindObjectsByType<ChoiceHandlerButton>(FindObjectsSortMode.None))
                {
                    if (choice.ChoiceState?.OnSelectScript?.TrimEnd().EndsWith(target, StringComparison.Ordinal) != true) continue;
                    var candidate = choice.GetComponent<Button>();
                    if (candidate != null && candidate.isActiveAndEnabled && candidate.IsInteractable()) button = candidate;
                }
                if (player.WaitingForInput && button == null) continueInput.Activate(1f);
                yield return null;
            }
            if (button == null)
            {
                LoggerInstance.Error($"RIVALS SMOKE FAILED {target} at {player.PlaybackSpot.ScriptName}:{player.PlaybackSpot.LineIndex}");
                yield break;
            }
            if (target == ".PartnerChoice" + title)
            {
                yield return new WaitForSecondsRealtime(0.35f);
                yield return null;
                LoggerInstance.Msg("RIVALS SELECTION ART " + replacedArt.Count);
                if (smokeSwap)
                {
                    yield return VerifySelection();
                    var preview = Resources.FindObjectsOfTypeAll<PartnerSkillPreview>()
                        .FirstOrDefault(p => p.gameObject.scene.IsValid() && p.entries != null && p.entries.Count >= 3);
                    bool all = preview != null;
                    foreach (var route in new[] { "hero", "warrior", "mage" })
                    {
                        var first = RivalRules.Skills(RivalRules.FromPartner(route))[0];
                        if (preview == null || !preview.entries.TryGetValue(route, out var entry) ||
                            entry.Cards.Length != 2 || entry.Cards[0] != ((int)first).ToString(System.Globalization.CultureInfo.InvariantCulture))
                            all = false;
                    }
                    if (!all || replacedArt.Count != 3)
                    {
                        LoggerInstance.Error($"RIVALS SMOKE FAILED selection art or preview entries: art={replacedArt.Count}, entries={preview?.entries?.Count}");
                        yield break;
                    }
                }
            }
            button.onClick.Invoke();
        }

        deadline = Time.realtimeSinceStartup + 12;
        while (Role == Rival.None && Time.realtimeSinceStartup < deadline)
        {
            if (player.WaitingForInput) continueInput.Activate(1f);
            yield return null;
        }
        if (Role == Rival.None)
        {
            LoggerInstance.Error("RIVALS SMOKE FAILED selecting the role marker");
            yield break;
        }

        // Synthetic battle begins after a real click through all three native selection buttons.
        player.Stop();
        var battle = Script.FromScriptText(name+"Battle", """
@clearChoice * wait:false
@set playerType=2
@set playerCharacter="player"
@set showPlayerStatus=true;showPartnerStatus=true
@set playerLv=1;partnerLv=1
@set partnerHealth=0;partnerMaxHealth=0;partnerDeath=0;playerDeath=0
@set tempCharacter="partner"
@gosub StatusFunc.StatusSetting
@set playerHealth=100;playerMaxHealth=100;playerStr=10;playerDex=10;playerAgi=10;playerMag=10
@set tempCharacter="player"
@set dungeonId=3;sectionId=1
@set forwardCharacter="partner"
@set battleTarget=""
@set enemyResourceName="goblin"
@set enemyNumber="goblin"
@set addEnemyResourceNames="goblin"
@set addEnemyPositions="0.5"
@gosub Battle.Start
@stop
""", (string?)null);
        loader.LoadedResources.Add(new ResourceLoader<Script>.LoadedResource(
            new Resource<Script>(source.BuildFullPath(name+"Battle"), battle), source));
        var start = player.PreloadAndPlayAsync(battle);
        deadline = Time.realtimeSinceStartup + 12;
        while (!start.IsCompleted && Time.realtimeSinceStartup < deadline) yield return null;
        if (!start.IsCompleted)
        {
            LoggerInstance.Error("RIVALS SMOKE FAILED loading the battle script");
            yield break;
        }
        start.GetAwaiter().GetResult();
        deadline = Time.realtimeSinceStartup + 22;
        var weapon = RivalRules.FormFor(Role, 1, 0, false);
        string slot = ((int)weapon).ToString(System.Globalization.CultureInfo.InvariantCulture);
        Button? command = null;
        while (Time.realtimeSinceStartup < deadline)
        {
            foreach (var choice in UnityEngine.Object.FindObjectsByType<ChoiceHandlerButton>(FindObjectsSortMode.None))
            {
                var state = choice.ChoiceState;
                if (state?.Id?.TrimEnd().EndsWith(slot, StringComparison.Ordinal) != true &&
                    state?.OnSelectScript?.Contains(slot, StringComparison.Ordinal) != true) continue;
                var btn = choice.GetComponent<Button>();
                if (btn != null && btn.isActiveAndEnabled && btn.IsInteractable()) command = btn;
            }
            if (command != null) break;
            if (player.WaitingForInput) continueInput.Activate(1f);
            yield return null;
        }

        var expected = RivalRules.GetStats(Role, 1);
        if (command == null || CurrentStats() != expected)
        {
            LoggerInstance.Msg($"Battle choices role={Read("rivalsRole")}, weapon={Read("rivalsWeapon")}: " +
                string.Join(" | ", UnityEngine.Object.FindObjectsByType<ChoiceHandlerButton>(FindObjectsSortMode.None)
                    .Where(c => c.isActiveAndEnabled).Take(14)
                    .Select(c => $"{c.name} / {c.ChoiceState?.Id} / {c.ChoiceState?.OnSelectScript}")));
            LoggerInstance.Error($"RIVALS SMOKE FAILED battle card {slot} ({command != null}) at {player.PlaybackSpot.ScriptName}:{player.PlaybackSpot.LineIndex}; stats={CurrentStats()}; timing={Read("timing")}; actor={Read("turnCharacter")}; enemy={Read("enemyMaxCount")}; role={Role}");
            yield break;
        }
        yield return new WaitForSecondsRealtime(0.3f);
        yield return null;
        LoggerInstance.Msg("RIVALS ART IN USE " + string.Join(", ", art.Keys));
        var item = Engine.GetService<ItemService>().GetItemBySlotNumber((int)weapon);
        if (item?.battleActionSetData?.actionNames.Length != 1)
        {
            LoggerInstance.Error("RIVALS SMOKE FAILED missing native action data");
            yield break;
        }
        if (smokeSwap)
        {
            MelonCoroutines.Start(VerifySwapHands());
            yield break;
        }
        if (smokeRobbery)
        {
            yield return VerifyNativeRobbery(continueInput);
            yield break;
        }
        if (smokeSkills)
        {
            var cast = Role switch
            {
                Rival.Outcast => Card.Switch, Rival.Villainess => Card.Doll, Rival.Dropout => Card.Armor,
                _ => throw new InvalidOperationException()
            };
            if (Role == Rival.Outcast)
            {
                int before = Round;
                var enchant = FindSmokeCard(Card.Enchant);
                if (enchant == null)
                {
                    LoggerInstance.Error("RIVALS SMOKE FAILED Enchant card unavailable");
                    yield break;
                }
                enchant.onClick.Invoke();
                deadline = Time.realtimeSinceStartup + 8;
                Button? fire = null;
                while (fire == null && Time.realtimeSinceStartup < deadline)
                {
                    fire = FindSmokeCard(Card.Fire);
                    yield return null;
                }
                if (fire == null)
                {
                    LoggerInstance.Error($"RIVALS SMOKE FAILED Fire selection did not open at {player.PlaybackSpot.ScriptName}:{player.PlaybackSpot.LineIndex}; command={Read("selectCommand")} | " + string.Join(", ", UnityEngine.Object.FindObjectsByType<ChoiceHandlerButton>(FindObjectsSortMode.None).Where(c => c.isActiveAndEnabled).Take(12).Select(c => $"{c.ChoiceState?.Id}:{c.ChoiceState?.OnSelectScript}")));
                    yield break;
                }
                fire.onClick.Invoke();
                deadline = Time.realtimeSinceStartup + 12;
                while (Time.realtimeSinceStartup < deadline &&
                       (LastUsed(Card.Enchant) <= 0 || FindSmokeCard(cast) == null))
                {
                    if (player.WaitingForInput) continueInput.Activate(1f);
                    yield return null;
                }
                if (Round != before || Read("turnCharacter") != "partner" || Read(Enchantment) != "fire")
                {
                    LoggerInstance.Error("RIVALS SMOKE FAILED Enchant consumed this action or did not apply Fire");
                    yield break;
                }
            }
            var skillButton = FindSmokeCard(cast);
            if (skillButton == null)
            {
                LoggerInstance.Error($"RIVALS SMOKE FAILED {cast} unavailable in native hand on round {Round}");
                yield break;
            }
            skillButton.onClick.Invoke();
            deadline = Time.realtimeSinceStartup + 16;
            while (Time.realtimeSinceStartup < deadline && LastUsed(cast) <= 0)
            {
                if (player.WaitingForInput) continueInput.Activate(1f);
                yield return null;
            }
            player.Stop();
            try
            {
                VerifySmokeEffects(cast);
                LoggerInstance.Msg($"RIVALS SMOKE PASSED {smokeRoute} | native card {cast} resolved | runtime effects passed");
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"RIVALS SMOKE FAILED {cast} at {player.PlaybackSpot.ScriptName}:{player.PlaybackSpot.LineIndex}: {ex}");
            }
            yield break;
        }
        command.onClick.Invoke();
        deadline = Time.realtimeSinceStartup + 10;
        while (Time.realtimeSinceStartup < deadline && Number("rivalsCommittedCard") != (int)weapon)
        {
            if (player.WaitingForInput) continueInput.Activate(1f);
            yield return null;
        }
        if (Number("rivalsCommittedCard") != (int)weapon)
        {
            LoggerInstance.Error($"RIVALS SMOKE FAILED weapon click at {player.PlaybackSpot.ScriptName}:{player.PlaybackSpot.LineIndex}");
            yield break;
        }
        if (weapon is Card.Sword or Card.Thread)
        {
            deadline = Time.realtimeSinceStartup + 10;
            while (!Flag("xSwitchAttackBarProcessing") && Time.realtimeSinceStartup < deadline)
            {
                if (player.WaitingForInput) continueInput.Activate(1f);
                yield return null;
            }
            if (!Flag("xSwitchAttackBarProcessing"))
            {
                LoggerInstance.Error($"RIVALS SMOKE FAILED QTE did not open at {player.PlaybackSpot.ScriptName}:{player.PlaybackSpot.LineIndex}");
                yield break;
            }
        }
        LoggerInstance.Msg($"RIVALS SMOKE PASSED {smokeRoute} | selected native button | {CurrentStats()} | weapon {item.resourceName}");
    }

    private static Button? FindSmokeCard(Card card)
    {
        string slot = ((int)card).ToString(System.Globalization.CultureInfo.InvariantCulture);
        foreach (var choice in UnityEngine.Object.FindObjectsByType<ChoiceHandlerButton>(FindObjectsSortMode.None))
        {
            if (!choice.isActiveAndEnabled) continue;
            var state = choice.ChoiceState;
            if (state?.Id?.TrimEnd().EndsWith(slot, StringComparison.Ordinal) != true &&
                state?.OnSelectScript?.Contains(slot, StringComparison.Ordinal) != true) continue;
            var button = choice.GetComponent<Button>();
            if (button != null && button.IsInteractable()) return button;
        }
        return null;
    }
}
