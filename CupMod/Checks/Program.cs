using CupCompetition;
static void Assert(bool value, string what)
{
    if (!value) throw new Exception(what);
}
foreach (string locale in new[] { "zh-CN", "zh-TW", "zh_Hant", "zh" })
    Assert(CupLanguage.ForLocale(locale).Text("跑刀", "Knife Run") == "跑刀", $"Chinese startup locale: {locale}");
foreach (string? locale in new[] { "en", "ja", "fr", "", null })
    Assert(CupLanguage.ForLocale(locale).Text("跑刀", "Knife Run") == "Knife Run", $"English fallback: {locale}");
var invalidSetup = new CupSetup(new CupSettings { EventId = "" });
Assert(!invalidSetup.TryGetSettings(out _, out string englishError, CupLanguage.ForLocale("ja")) &&
    englishError.StartsWith("Enter a Cup ID", StringComparison.Ordinal), "Validation uses the same English fallback as the setup screen");
var normalProfile = """
    {"objectJsonMap":{"keys":["AudioSettings","Naninovel.LocalizationManager+Settings, Elringus.Naninovel.Runtime"],
      "values":["{\"Volume\":0.5}","{\"SelectedLocale\":\"en\"}"]}}
    """u8.ToArray();
using (var profile = System.Text.Json.JsonDocument.Parse(CupLanguage.CopyLocaleSetting(normalProfile)!))
{
    var map = profile.RootElement.GetProperty("objectJsonMap");
    using var setting = System.Text.Json.JsonDocument.Parse(map.GetProperty("values")[0].GetString()!);
    Assert(map.GetProperty("keys").GetArrayLength() == 1 &&
        setting.RootElement.GetProperty("SelectedLocale").GetString() == "en",
        "Cup save isolation carries over only the native locale setting");
}
Assert(CupLanguage.CopyLocaleSetting("{\"objectJsonMap\":{\"keys\":[],\"values\":[]}}"u8.ToArray()) is null,
    "A fresh profile leaves the game's startup language at its native default");
var run = new CupRun();
run.Reach(1);
Assert(run.Progress == 0, "No progress before start");
run.Start();
run.Reach(8);
run.Reach(3);
Assert(run.Progress == 8, "Script rewind cannot reduce or increase the previous maximum");
run.Finish("defeat");
run.Reach(9);
run.Finish("clear");
Assert(run.Progress == 8 && run.Result == "defeat", "Result is immutable after the first terminal event");
var pauseRun = new CupRun();
pauseRun.SetPaused(true);
Assert(!pauseRun.Running && !pauseRun.Paused, "Pause cannot start a run");
pauseRun.Start();
pauseRun.SetPaused(true);
TimeSpan pausedAt = pauseRun.Elapsed;
Thread.Sleep(20);
pauseRun.Reach(1);
Assert(pauseRun.Running && pauseRun.Paused && pauseRun.Elapsed == pausedAt && pauseRun.Progress == 1,
    "Pausing freezes elapsed time while the run stays active");
pauseRun.SetPaused(false);
Thread.Sleep(20);
Assert(!pauseRun.Paused && pauseRun.Elapsed > pausedAt, "Leaving a save screen resumes the existing clock");
pauseRun.SetPaused(true);
pauseRun.Finish("quit_to_title");
pauseRun.SetPaused(false);
Assert(!pauseRun.Running && !pauseRun.Paused && pauseRun.Result == "quit_to_title", "A paused run can end and cannot resume after finishing");
Assert(CupTiming.IsFixedWait("1.5") && !CupTiming.IsFixedWait("i") && !CupTiming.IsFixedWait("i1.5") &&
    !CupTiming.IsFixedWait("0") && !CupTiming.IsFixedWait("Infinity"), "Only positive, non-skippable waits are fixed");
Assert(CupTiming.PauseFor(false, true, false) == CupPauseReason.Cutscene, "A locked cutscene pauses the clock");
Assert(CupTiming.PauseFor(false, true, true) == CupPauseReason.None, "Active choices and battle controls keep counting");
Assert(CupTiming.PauseFor(false, false, false) == CupPauseReason.None, "A completed or skippable command resumes the clock");
Assert(CupTiming.PauseFor(true, true, true) == CupPauseReason.Save, "The save screen retains its existing pause rule");
Assert(CupTiming.PauseFor(false, false, false, preparing: true) == CupPauseReason.Preparing,
    "Initialization does not count toward the cup");
Assert(CupTiming.PauseFor(false, false, false, input: true) == CupPauseReason.Input,
    "A native text input dialog pauses the timer");
Assert(CupTiming.PauseFor(false, false, false, skipActive: true) == CupPauseReason.None,
    "The skip flag alone cannot pause an idle or unclassified command");
Assert(CupTiming.PauseFor(false, false, false, skipActive: true, advancingDialogue: true) == CupPauseReason.Skip &&
    CupTiming.PauseFor(false, false, true, skipActive: true, advancingDialogue: true) == CupPauseReason.None,
    "Native dialogue fast-forward pauses only while an eligible dialogue command is executing");
Assert(CupRandom.Seed(123, "map", 1, 1) == CupRandom.Seed(123, "map", 1, 1), "Same map key reproduces");
Assert(CupRandom.Seed(123, "map", 1, 1) != CupRandom.Seed(123, "battle", 1, 1), "Map and battle streams are separate");
Assert(CupRandom.Seed(123, "map", 1, 1) != CupRandom.Seed(123, "map", 1, 2), "Map regions use separate seeds");
Assert(Route.All.Select(x => x.Id).Distinct().Count() == 23, "All documented reachable ending IDs");
foreach (Route route in Route.All)
{
    Assert(Enumerable.Range(1986, 4).Count(line => route.KeepChoice("Opening3", line, false)) == (route.Partner == "common" ? 0 : 1), route.Id + " locks one partner");
    Assert(route.KeepChoice("Dungeon", 338, false), route.Id + " permits ordinary map movement");
}
var glitch = Route.All.Single(x => x.Id == "warriorAfterGlitch");
Assert(glitch.KeepChoice("Scenario_Warrior5", 1149, false) && !glitch.KeepChoice("Scenario_Warrior5", 1149, true),
    "Warrior recovery choice changes after story return");
var priest = Route.All.Single(x => x.Id == "priestNodAfterLowSan");
Assert(priest.KeepChoice("Scenario_Priest3", 1164, false) && priest.KeepChoice("Scenario_Priest3", 1165, true),
    "Priest recovery choice changes after story return");
var setup = new CupSetup(new CupSettings { EventId = "Spring", RouteId = "hero", Seed = 10 });
setup.SelectPartner("mage");
Assert(setup.Route.Partner == "mage" && setup.Endings.Count == 6, "Partner switches to compatible endings");
setup.SelectEnding("mageNotMaryokuIssyo");
setup.BattleMode = "timing";
setup.SeedText = "-2147483648";
setup.LimitText = "25";
Assert(setup.TryGetSettings(out var selected, out _) &&
       selected!.RouteId == "mageNotMaryokuIssyo" && selected.Seed == int.MinValue &&
       selected.LimitMinutes == 25 && selected.BattleMode == "timing", "Start uses the selected setup");
setup.SelectPartner("warrior");
Assert(selected!.RouteId == "mageNotMaryokuIssyo", "Editing a draft cannot change a captured run");
setup.SeedText = "2147483648";
Assert(!setup.TryGetSettings(out _, out _), "Seed outside the 32-bit range blocks start");
setup.SeedText = "20";
setup.LimitText = "-1";
Assert(!setup.TryGetSettings(out _, out _), "Negative limit blocks start");
setup.LimitText = "";
Assert(setup.TryGetSettings(out var noLimit, out _) && noLimit!.LimitMinutes == 0, "Blank limit means no limit");
setup.EventId = " ";
Assert(!setup.TryGetSettings(out _, out _), "Blank event ID blocks start");
Route ending(string id) => Route.All.Single(route => route.Id == id);
var warriorBad = ending("warriorGlitchBad");
Assert(warriorBad.KeepChoice("Scenario_Warrior5", 1149, false), "The glitch ending has a playable branch");
Assert(warriorBad.IsEnding("warriorGlitchBad", "warrior", false) &&
    !glitch.IsEnding("warriorGlitchBad", "warrior", false) && glitch.ContinuesAfter("warriorGlitchBad"),
    "The same scripted return can end one race and continue another");
Assert(ending("dropout_warrior").IsEnding("dropout", "warrior", false) &&
    !ending("dropout_warrior").IsEnding("dropout", "hero", false), "Shared ending type requires its actual partner");
Assert(ending("mage5YakusokuSinaiBad").KeepChoice("Scenario_Mage3", 1169, false) &&
    !ending("mage5YakusokuSinaiBad").KeepChoice("Scenario_Mage3", 1170, false) &&
    ending("mage5YakusokuSinaiBad").KeepChoice("Scenario_Mage5", 253, false) &&
    !ending("mage5YakusokuSinaiBad").KeepChoice("Scenario_Mage5", 251, false), "No-promise branch uses the recovered-magic route");
Assert(ending("hero4Bad").KeepChoice("Scenario_Hero3", 2037, false) &&
    !ending("hero4Bad").KeepChoice("Scenario_Hero3", 2034, false) &&
    !ending("hero4Bad").KeepChoice("Scenario_Hero3", 2035, false), "Hero's third-floor choices include the inserted save alias");
Assert(ending("ritual").KeepsEarlyEnding("Opening3", 1510) && ending("mage-exit").KeepsEarlyEnding("Scenario_Mage1", 271),
    "Pre-credits endings have explicit success markers");
Assert(BossGoal.WinAt("Battle", 3565, "outcast", 1)?.Id == "outcast", "Main first-floor boss recognized");
Assert(BossGoal.WinAt("Battle", 3565, "outcastRe", 5) == null, "A same-name rematch does not count");
Assert(BossGoal.WinAt("Battle", 3565, "priest", 5) == null &&
    BossGoal.WinAt("Scenario_Hero5", 1587, "priest", 5, true)?.Id == "priest", "Hero finisher requires success");
Assert(BossGoal.WinAt("Battle", 4972, "priest", 9) == null, "Leaving battle is not victory");
Assert(BossGoal.WinAt("Battle", 3565, BossGoal.BattleId("", "outcast"), 1)?.Id == "outcast" &&
    BossGoal.WinAt("Battle", 3565, BossGoal.BattleId("", "seaDragon,seaDragonPart"), 3)?.Id == "seaDragon",
    "Named fixed fights without battleTarget still require a victory");
Assert(BossGoal.WinAt("Battle", 3565, BossGoal.BattleId("outcastRe", "outcast"), 1) == null,
    "An explicit rematch ID takes precedence over the enemy list");
Assert(BossGoal.WinAt("Battle", 4465, "priest", 8)?.Id == "priest" &&
    BossGoal.WinAt("Scenario_Priest5", 301, "", 7)?.Id == "priest", "Scripted priest victories use their own markers");
Assert(BossGoal.WinAt("Battle", 13849, "warriorFinalDuel", 6)?.Id == "warriorFinalDuel" &&
    BossGoal.WinAt("Battle", 13849, "warriorLv1Duel", 6) == null, "Stop-card victory applies to the first duel only");
setup.EventId = "Boss Cup";
setup.BossMode = true;
setup.SelectBoss("demonLord");
Assert(!setup.TryGetSettings(out _, out _), "Boss setup asks for a compatible story branch");
setup.SelectBossRoute("hero");
Assert(!setup.TryGetSettings(out _, out _), "Demon King cannot use an unrelated hero route");
setup.SelectBossRoute("priestLowLoop");
Assert(setup.TryGetSettings(out var bossSettings, out _) &&
    bossSettings!.BossId == "demonLord" && bossSettings.RouteId == "priestLowLoop", "Boss and story branch save independently");
Assert(new CupSetup(bossSettings!).TryGetSettings(out var reloadedBoss, out _) && reloadedBoss == bossSettings,
    "Boss configuration and its story branch survive reloading");
var firstBossBranch = new CupSettings { BossId = "priest", RouteId = "hero" };
var secondBossBranch = firstBossBranch with { RouteId = "mageMaryoku" };
Assert(firstBossBranch.ForMatch() == secondBossBranch.ForMatch(),
    "Different player-selected branches can compete in the same Boss race");
Assert((firstBossBranch with { BossId = "" }).ForMatch() != (secondBossBranch with { BossId = "" }).ForMatch(),
    "An ending race remains keyed by its selected ending");
var legacySetup = System.Text.Json.JsonSerializer.Deserialize<CupSettings>("{}")!;
Assert(legacySetup.FastStory, "Configurations saved before the switch keep automatic story playback");
foreach (var settings in new[] { legacySetup, firstBossBranch })
{
    var manualSetup = new CupSetup(settings) { FastStory = false };
    Assert(manualSetup.TryGetSettings(out var chosen, out _) && chosen is { FastStory: false } &&
        chosen.BossId == settings.BossId && chosen.RouteId == settings.RouteId &&
        chosen.ForMatch() != settings.ForMatch(), "Manual story playback is a saved rule in either goal mode");
    var saved = System.Text.Json.JsonSerializer.Serialize(chosen);
    Assert(new CupSetup(System.Text.Json.JsonSerializer.Deserialize<CupSettings>(saved)!).FastStory == false,
        "Saved manual story playback stays off when reloaded");
}
var opening = new CupSetup(new CupSettings());
Assert(opening.TryGetSettings(out var defaults, out _) && defaults is
    { StartingCurse: 0, StartingAllocationPoints: 0, OpeningDeath: false, RunKnife: false },
    "Old configurations keep the standard start");
Assert(defaults is { FixedWeaponCombination: false, ExtraActionAfterSteal: false, StolenWeaponDamageMultiplier: 2,
    EnemyHealthMultiplier: 1, EnemyAttackMultiplier: 1, DisabledCharacter: "", InheritDisabledAction: false },
    "Old configurations default to native combat and a twofold optional vulnerability");
var expanded = new CupSetup(new CupSettings
{
    FixedWeaponCombination = true, ExtraActionAfterSteal = true, StolenWeaponDamageMultiplier = 1, EnemyHealthMultiplier = 1.5,
    EnemyAttackMultiplier = 0.75, DisabledCharacter = "player", InheritDisabledAction = true
});
Assert(expanded.TryGetSettings(out var expandedSettings, out _) && expandedSettings is
    { FixedWeaponCombination: true, ExtraActionAfterSteal: true, StolenWeaponDamageMultiplier: 1, EnemyHealthMultiplier: 1.5,
      EnemyAttackMultiplier: 0.75, DisabledCharacter: "player", InheritDisabledAction: true, RouteId: "hero" },
    "Entertainment settings survive editing and do not alter the chosen story");
expanded.EnemyAttackMultiplierText = "NaN";
Assert(!expanded.TryGetSettings(out _, out _), "A non-finite combat multiplier is rejected");
var previousStealRules = System.Text.Json.JsonSerializer.Deserialize<CupSettings>(
    "{\"StolenWeaponVulnerability\":true,\"StolenWeaponDamageMultiplier\":3}")!;
Assert(previousStealRules is { ExtraActionAfterSteal: false, StolenWeaponDamageMultiplier: 3 },
    "The previous damage toggle does not enable the new action rule; the configured multiplier is retained");
opening.StartingCurseText = "100";
opening.StartingAllocationText = "6";
opening.RunKnife = true;
Assert(opening.TryGetSettings(out var specialStart, out _) && specialStart is
    { StartingCurse: 100, StartingAllocationPoints: 6, OpeningDeath: true, RunKnife: true, MaxDeaths: 0 },
    "Allocation points imply an opening death even with zero regular deaths");
Assert(new CupSetup(specialStart!).TryGetSettings(out var reloadedStart, out _) && reloadedStart == specialStart,
    "Opening resources and run knife survive reloading");
opening.StartingAllocationText = "0";
Assert(opening.TryGetSettings(out var curseOnly, out _) && curseOnly is { StartingCurse: 100, OpeningDeath: false },
    "Starting curse can be granted without making the player die");
opening.OpeningDeath = true;
Assert(opening.TryGetSettings(out var zeroPointDeath, out _) && zeroPointDeath is { OpeningDeath: true },
    "A starter death can be selected with zero allocation points");
opening.StartingCurseText = "-1";
Assert(!opening.TryGetSettings(out _, out _), "Negative starting curse is invalid");
opening.StartingCurseText = "1";
opening.SelectPartner("common");
opening.SelectEnding("ritual");
Assert(!opening.TryGetSettings(out _, out _), "The pre-dungeon ritual does not accept dungeon opening rules");
Console.WriteLine("Cup core checks passed");

var limits = new CupSettings { MaxRoomRetries = 2, RoomRetryPenaltySeconds = 7, MaxDeaths = 1, DeathPenaltySeconds = 30 };
Assert(CupTiming.PauseFor(false, false, true, roomRetry: true) == CupPauseReason.RoomRetry,
    "SL stays paused even while restored battle controls are active");
Assert(CupTiming.PauseFor(true, false, true, roomRetry: false) == CupPauseReason.Save &&
    CupTiming.PauseFor(false, false, true, roomRetry: false) == CupPauseReason.None,
    "After SL the game state decides whether to resume");
var cupRun = new CupRun();
Assert(!cupRun.TryBeginRoomRetry(limits.MaxRoomRetries), "Retry requires an active run");
cupRun.Start();
cupRun.SetPaused(true);
Assert(!cupRun.TryBeginRoomRetry(0), "Zero SL quota blocks F9");
Assert(cupRun.TryBeginRoomRetry(limits.MaxRoomRetries) && !cupRun.TryBeginRoomRetry(limits.MaxRoomRetries), "Reserve one retry at a time");
TimeSpan beforeRetry = cupRun.Elapsed;
Assert(!cupRun.CompleteRoomRetry(false, 7) && cupRun.RoomRetries == 0 && cupRun.Elapsed == beforeRetry, "A failed load does not use SL or add time");
Assert(cupRun.TryBeginRoomRetry(limits.MaxRoomRetries) && cupRun.CompleteRoomRetry(true, 7) && cupRun.Elapsed - beforeRetry == TimeSpan.FromSeconds(7), "Successful retry charges one SL and its penalty");
Assert(!cupRun.CompleteRoomRetry(true, 7) && cupRun.RoomRetries == 1 && cupRun.RoomRetryPenalty == TimeSpan.FromSeconds(7), "Duplicate completion cannot charge again");
Assert(cupRun.TryBeginRoomRetry(limits.MaxRoomRetries) && cupRun.CompleteRoomRetry(true, 7) && !cupRun.TryBeginRoomRetry(limits.MaxRoomRetries), "Exhausted SL cannot be reused");
TimeSpan beforePenalty = cupRun.Elapsed;
Assert(cupRun.RecordDeath(limits.MaxDeaths, limits.DeathPenaltySeconds) && cupRun.Deaths == 1, "First allowed death keeps the run alive");
Assert(cupRun.Elapsed - beforePenalty == TimeSpan.FromSeconds(30) && cupRun.RoomRetries == 2, "Penalty adds to paused clock without consuming SL");
Assert(!cupRun.RecordDeath(limits.MaxDeaths, limits.DeathPenaltySeconds) && cupRun.Deaths == 2 && cupRun.DeathPenalty == TimeSpan.FromSeconds(60) && cupRun.Penalty == TimeSpan.FromSeconds(74), "SL and death penalties add together and remain separately auditable");
var zeroDeath = new CupRun();
zeroDeath.Start();
Assert(!zeroDeath.RecordDeath(0, 0), "Zero deaths ends the run on the first death");
var savedLimits = new CupSetup(limits);
Assert(savedLimits.TryGetSettings(out var parsedLimits, out _) && parsedLimits!.MaxRoomRetries == 2 && parsedLimits.RoomRetryPenaltySeconds == 7 && parsedLimits.MaxDeaths == 1 && parsedLimits.DeathPenaltySeconds == 30, "Setup round-trips both quotas and both penalties");
savedLimits.RoomRetryPenaltyText = "-1";
Assert(!savedLimits.TryGetSettings(out _, out _), "Negative SL penalties are rejected");
savedLimits.RoomRetryPenaltyText = "0";
savedLimits.DeathsText = "-1";
Assert(!savedLimits.TryGetSettings(out _, out _), "Negative death limits are rejected");
var legacyLimits = System.Text.Json.JsonSerializer.Deserialize<CupSettings>("{\"EventId\":\"Old\"}")!;
Assert(legacyLimits.MaxRoomRetries == 0 && legacyLimits.RoomRetryPenaltySeconds == 0 && legacyLimits.MaxDeaths == 0 && legacyLimits.DeathPenaltySeconds == 0, "Existing configurations keep zero-budget behavior");
Assert(legacyLimits is { StartingFloor: 1, MaxWeapons: null, KickOnly: false, HarshTraining: false }, "Old config keeps the vanilla floor and weapon rules");
var jump = new CupSetup(legacyLimits) { StartingFloorText = "4", MaxWeaponsText = "0", KickOnly = true, HarshTraining = true };
Assert(jump.TryGetSettings(out var jumpSettings, out _) && jumpSettings is { StartingFloor: 4, MaxWeapons: 0, KickOnly: true, HarshTraining: true },
    "Jump and independent entertainment rules round-trip, including a zero weapon cap");
Assert(new CupSetup(jumpSettings!).TryGetSettings(out var reloadedJump, out _) && reloadedJump == jumpSettings,
    "Jump and rules survive reloading");
jump.BossMode = true;
jump.SelectBoss("dealer");
jump.SelectBossRoute("hero");
Assert(!jump.TryGetSettings(out _, out _), "Cannot jump past the target boss");
jump.SelectBoss("demonLord");
jump.SelectBossRoute("priestLowLoop");
jump.StartingFloorText = "6";
jump.MaxWeaponsText = "";
Assert(jump.TryGetSettings(out var extra, out _) && extra is { StartingFloor: 6, MaxWeapons: null }, "The Demon Lord supports its extra area and a blank cap");

const string auditRunId = "20261114-123456-deadbeef";
var audited = new CupRun();
audited.Start();
audited.SetPaused(true);
var audit = new CupAudit(auditRunId);
Assert(audit.RunCode == "DEADBEEF", "The HUD code is derived from the full attempt ID");
audit.Record(audited, "start");
audit.Record(audited, "opening_death");
Assert(audited.TryBeginRoomRetry(1) && !audited.CompleteRoomRetry(false, 7), "Failed SL uses no allowance");
audit.Record(audited, "sl_failed");
Assert(audited.TryBeginRoomRetry(1) && audited.CompleteRoomRetry(true, 7), "Successful SL charges 7 seconds");
audit.Record(audited, "sl_success", penaltySeconds: 7);
Assert(!audited.RecordDeath(0, 17), "The fatal death still charges 17 seconds");
audit.Record(audited, "death", "over_limit", penaltySeconds: 17);
audited.Finish("defeat");
audit.Record(audited, "finish", "defeat");
var events = audit.Events;
Assert(events.Select(evt => evt.Sequence).SequenceEqual(Enumerable.Range(1, 6)) &&
    events.Select(evt => evt.PenaltyAddedMilliseconds).SequenceEqual(new long[] { 0, 0, 0, 7000, 17000, 0 }),
    "The timeline records each penalty once, before the terminal event");
Assert(events[^1].Result == "defeat" && events[^1].PenaltyTotalMilliseconds == 24000 &&
    events[^1].RoomRetriesUsed == 1 && events[^1].Deaths == 1 &&
    events[^1].ElapsedMilliseconds == (long)audited.Elapsed.TotalMilliseconds,
    "Terminal event and aggregate standings agree");
Assert(events.Zip(events.Skip(1)).All(pair => pair.First.WallMilliseconds <= pair.Second.WallMilliseconds),
    "The recording offset advances monotonically even while gameplay is paused");
