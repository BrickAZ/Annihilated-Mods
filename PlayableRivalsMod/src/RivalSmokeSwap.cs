using Il2Cpp;
using Il2CppNaninovel;
using Il2CppNaninovel.UI;
using UnityEngine;

namespace PlayableRivals;

public sealed partial class RivalMod
{
    private bool smokeSwap;

    private System.Collections.IEnumerator VerifySwapHands()
    {
        Card weapon = RivalRules.FormFor(Role, Round, LastUsed(Card.Switch), NoMagic);
        var skills = RivalRules.Skills(Role);
        var items = Engine.GetService<ItemService>();
        if (OutcastDamageLimited && !BuildRivalPresentation(Card.Sword, GetCard(Card.Sword), "partner").Description.Contains(CapNotice))
        {
            LoggerInstance.Error("RIVALS SMOKE FAILED missing Outcast damage cap notice");
            yield break;
        }
        foreach (var card in Enum.GetValues<Card>().Where(card => CardOwner(card) == Role))
        {
            var request = new ItemPresentationRequest(GetCard(card), "rivalsSmoke", "partner", "battle", false);
            var presentation = items.GetOrBuildPresentation(request);
            if (string.IsNullOrWhiteSpace(presentation?.Description) ||
                presentation.Description.Contains("冷却时间：", StringComparison.Ordinal) ||
                !VerifyCardTooltips(card, presentation.TooltipContents))
            {
                LoggerInstance.Error($"RIVALS SMOKE FAILED {card} missing its native card description");
                yield break;
            }
        }

        // Inspect the actual hand card's label in addition to the presentation service.
        string firstSlot = ((int)skills[0]).ToString(System.Globalization.CultureInfo.InvariantCulture);
        var screenCard = UnityEngine.Object.FindObjectsByType<ChoiceHandlerButton>(FindObjectsSortMode.None)
            .FirstOrDefault(button => button.isActiveAndEnabled &&
                button.ChoiceState?.OnSelectScript?.Contains(firstSlot, StringComparison.Ordinal) == true);
        var visibleText = (screenCard?.GetComponentsInChildren<Il2CppTMPro.TMP_Text>(true))?
            .Where(t => t.gameObject.activeInHierarchy).Select(t => t.text).ToArray() ?? Array.Empty<string>();
        string text = string.Join(" | ", visibleText);
        var expectedFace = BuildRivalPresentation(skills[0], GetCard(skills[0]), "partner").Description;
        if (!visibleText.Contains(expectedFace) ||
            (Role == Rival.Outcast && Read("forwardCharacter") == "partner" &&
             expectedFace.Contains("处于后排时", StringComparison.Ordinal)))
        {
            LoggerInstance.Error($"RIVALS SMOKE FAILED missing rendered text for {skills[0]}: {text}");
            yield break;
        }
        LoggerInstance.Msg($"RIVALS RENDERED CARD | {text.Replace("\n", " ")}");
        if (Role == Rival.Villainess)
            VerifyLoveTiming(items);
        if (!VerifyLearnedSkillRows(items, skills))
            yield break;

        var face = UnityEngine.Object.FindObjectsByType<StatusFaceController>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(controller => controller.partnerFace?.sprite != null);
        string prefix = "rivals/profile/" + RivalRules.Id(Role) + "_";
        var partnerStatus = UnityEngine.Object.FindObjectsByType<DraggableStatusUI>(FindObjectsSortMode.None)
            .FirstOrDefault(status => status.statusOwner == "partner");
        if (face == null || face.partnerFace.overrideSprite?.name != prefix + "normal" ||
            partnerStatus?.characterName?.text != RivalRules.Name(Role))
        {
            LoggerInstance.Error($"RIVALS SMOKE FAILED profile | name={partnerStatus?.characterName?.text} stock={face?.partnerFace?.sprite?.name} shown={face?.partnerFace?.overrideSprite?.name}");
            yield break;
        }
        var portrait = face.partnerFace;
        var backing = portrait.transform.parent.Find("RivalsProfileBackdrop")?.GetComponent<UnityEngine.UI.Image>();
        if (backing == null || !backing.enabled || backing.sprite?.name != "rivals/profile_backdrops/normal" ||
            backing.transform.GetSiblingIndex() + 1 != portrait.transform.GetSiblingIndex() || backing.raycastTarget ||
            backing.rectTransform.rect.size != portrait.rectTransform.rect.size)
        {
            LoggerInstance.Error($"RIVALS SMOKE FAILED missing normal profile backdrop behind {prefix}normal");
            yield break;
        }
        var originalFace = portrait.sprite;
        foreach (string extraState in new[] { "happy", "extra" })
        {
            var testFace = Sprite.Create(originalFace.texture, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));
            testFace.name = "fixture_" + extraState;
            try
            {
                portrait.sprite = testFace;
                RefreshPartnerStatusFace(portrait, Role);
                if (portrait.overrideSprite?.name != prefix + extraState ||
                    backing.sprite?.name != "rivals/profile_backdrops/" + extraState)
                {
                    LoggerInstance.Error($"RIVALS SMOKE FAILED {extraState} profile and backdrop mismatch");
                    yield break;
                }
                LoggerInstance.Msg($"RIVALS PROFILE BACKDROP | {RivalRules.Name(Role)} | simulated {extraState} -> {backing.sprite.name}");
            }
            finally
            {
                portrait.sprite = originalFace;
                RefreshPartnerStatusFace(portrait, Role);
                UnityEngine.Object.Destroy(testFace);
            }
        }
        int health = Number("partnerHealth");
        try
        {
            Write("partnerHealth", 1);
            face.UpdateStatusFace();
            float deadline = Time.realtimeSinceStartup + 3;
            while (Time.realtimeSinceStartup < deadline &&
                   (portrait.overrideSprite?.name != prefix + "damaged" || backing.sprite?.name != "rivals/profile_backdrops/damaged"))
                yield return null;
            if (portrait.overrideSprite?.name != prefix + "damaged" || backing.sprite?.name != "rivals/profile_backdrops/damaged")
            {
                LoggerInstance.Error($"RIVALS SMOKE FAILED damaged profile | stock={portrait.sprite?.name} shown={portrait.overrideSprite?.name} backdrop={backing.sprite?.name}");
                yield break;
            }
            LoggerInstance.Msg($"RIVALS PROFILE STATE | {RivalRules.Name(Role)} | normal -> {portrait.overrideSprite.name} with {backing.sprite.name}");
        }
        finally
        {
            Write("partnerHealth", health);
            face.UpdateStatusFace();
        }

        foreach (var expected in new[] { "player", "partner", "player" })
        {
            float deadline = Time.realtimeSinceStartup + 5;
            while (Time.realtimeSinceStartup < deadline && !Flag("isOrderChangeable")) yield return null;
            var swap = UnityEngine.Object.FindObjectsByType<CharacterSwapButton>(FindObjectsSortMode.None)
                .FirstOrDefault(b => b.isActiveAndEnabled && b.CanSwapBetweenZones());
            if (swap != null)
                swap.SwapBetweenZones();
            else
            {
                var manager = Resources.FindObjectsOfTypeAll<PartyStatusManager>()
                    .FirstOrDefault(m => m.gameObject.scene.IsValid());
                if (manager == null)
                {
                    LoggerInstance.Error($"RIVALS SMOKE FAILED no formation manager at {player!.PlaybackSpot.ScriptName}:{player.PlaybackSpot.LineIndex}");
                    yield break;
                }
                manager.UpdateForwardCharacterByZoneNumber(1, expected);
            }
            deadline = Time.realtimeSinceStartup + 9;
            while (Time.realtimeSinceStartup < deadline &&
                   (Read("turnCharacter") != expected || player!.PlaybackSpot.ScriptName != "Battle" ||
                    player.PlaybackSpot.LineIndex != 2140 || !Flag("isOrderChangeable")))
                yield return null;
            yield return new WaitForSecondsRealtime(0.35f);

            var buttons = UnityEngine.Object.FindObjectsByType<ChoiceHandlerButton>(FindObjectsSortMode.None)
                .Where(c => c.isActiveAndEnabled).ToArray();
            var privateCards = Enum.GetValues<Card>().Where(card => buttons.Any(button =>
                button.ChoiceState?.OnSelectScript?.Contains(((int)card).ToString(System.Globalization.CultureInfo.InvariantCulture),
                    StringComparison.Ordinal) == true)).ToArray();
            if (Read("turnCharacter") != expected || (expected == "player" && privateCards.Length != 0) ||
                (expected == "partner" && (!privateCards.Contains(weapon) || skills.Any(card => !privateCards.Contains(card)))))
            {
                LoggerInstance.Error($"RIVALS SMOKE FAILED swap to {expected}, actor={Read("turnCharacter")}, " +
                    $"private hand=[{string.Join(", ", privateCards)}] at {player!.PlaybackSpot.ScriptName}:{player.PlaybackSpot.LineIndex}, forward={Read("forwardCharacter")}");
                yield break;
            }
            if (expected == "player")
            {
                string previous = Read("selectCommand");
                Write("selectCommand", (int)weapon);
                bool leaked = CanCommitRivalCard();
                Write("selectCommand", previous);
                if (leaked)
                {
                    LoggerInstance.Error($"RIVALS SMOKE FAILED soldier could commit {weapon}");
                    yield break;
                }
                if (Role == Rival.Outcast && Read("forwardCharacter") == "player")
                {
                    var rearFace = BuildRivalPresentation(Card.Steal, GetCard(Card.Steal), "partner").Description;
                    if (!rearFace.Contains("<link=\"effectOutline:activeNegative\"><color=#FF8080>处于后排时：<BR>无法使用", StringComparison.Ordinal))
                    {
                        LoggerInstance.Error($"RIVALS SMOKE FAILED rear-row condition not marked unavailable: {rearFace}");
                        yield break;
                    }
                    LoggerInstance.Msg("RIVALS REAR RESTRICTION | " + rearFace);
                }
            }

            string dot = "rivals/dot/dot_" + RivalRules.Id(Role);
            var hud = UnityEngine.Object.FindObjectsByType<DraggableStatusUI>(FindObjectsSortMode.None);
            deadline = Time.realtimeSinceStartup + 1.2f;
            while (Time.realtimeSinceStartup < deadline &&
                !hud.Any(s => s.statusOwner == "partner" && s.characterImage?.overrideSprite?.name == dot))
            {
                yield return null;
                hud = UnityEngine.Object.FindObjectsByType<DraggableStatusUI>(FindObjectsSortMode.None);
            }
            if (!hud.Any(s => s.statusOwner == "partner" && s.characterImage?.overrideSprite?.name == dot) ||
                hud.Any(s => s.statusOwner == "player" && s.characterImage?.overrideSprite?.name == dot))
            {
                LoggerInstance.Error($"RIVALS SMOKE FAILED wrong HUD dot after swap to {expected} | role={Role}, partner={Read("partnerCharacter")} | {string.Join(", ", hud.Select(s => s.statusOwner + "=" + s.characterImage?.overrideSprite?.name))}");
                yield break;
            }
            LoggerInstance.Msg($"RIVALS SWAP HAND | {expected} | [{string.Join(", ",privateCards)}] | dot={dot}");
        }
        LoggerInstance.Msg($"RIVALS SMOKE PASSED {smokeRoute} | native swap across both actors three times");
    }

    private bool VerifyLearnedSkillRows(ItemService items, Card[] skills)
    {
        string role = Read("partnerCharacter");
        string learnFlag = role switch
        {
            "hero" => "xSwitchHeroMagicFire",
            "warrior" => "xSwitchWarriorBerserk",
            _ => "xSwitchMageSuperBuff"
        };
        string previousFlag = Read(learnFlag);
        string previousFloor = Read("dungeonId");
        string previousUnlocked = Read(FloorKey);
        string previousRole = Read(RoleKey);
        var source = new LearnedSkillListSource();
        var variableManager = Engine.GetService<ICustomVariableManager>();
        var context = new IconTextListContext("partner", role, "partner", variableManager, items);
        var panels = UnityEngine.Object.FindObjectsByType<LearnedSkillStatusPanel>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        var panel = panels.FirstOrDefault(candidate => candidate.statusOwner == "partner");
        string Names(IconTextListContext owner)
        {
            var rows = source.BuildEntries(owner);
            int count = rows.Cast<Il2CppSystem.Collections.Generic.IReadOnlyCollection<IconTextListEntry>>().Count;
            return string.Join(",", Enumerable.Range(0, count).Select(i => rows[i].DisplayName));
        }
        try
        {
            Write(learnFlag, 1);
            Write(RoleKey, "");
            string stock = Names(context);
            string expectedOriginal = role switch { "hero" => "法玛", "warrior" => "狂暴", _ => "倾力支援" };
            if (!stock.Contains(expectedOriginal, StringComparison.Ordinal))
            {
                LoggerInstance.Error($"RIVALS SMOKE FAILED stock skill seed | wanted={expectedOriginal} actual={stock}");
                return false;
            }
            var soldier = new IconTextListContext("player", "player", "player", variableManager, items);
            string originalSoldier = Names(soldier);
            Write(RoleKey, previousRole);
            for (int floor = 1; floor <= 3; floor++)
            {
                Write("dungeonId", floor);
                Write(FloorKey, floor);
                string names = Names(context);
                string expected = string.Join(",", skills.Take(floor).Select(CardName));
                if (names != expected || Names(soldier) != originalSoldier)
                {
                    LoggerInstance.Error($"RIVALS SMOKE FAILED learned rows | floor={floor} wanted={expected} actual={names} stock={stock}");
                    return false;
                }
                if (panel == null || !panel.TryInitializeServices())
                {
                    LoggerInstance.Error("RIVALS SMOKE FAILED native companion learned-skill panel unavailable");
                    return false;
                }
                panel.RefreshEntries();
                string bound = string.Join(",", panel.listView.rows.ToArray()
                    .Where(row => row != null && row.gameObject.activeSelf)
                    .Select(row => row.label.text));
                if (bound != expected)
                {
                    LoggerInstance.Error($"RIVALS SMOKE FAILED rendered learned rows | floor={floor} wanted={expected} actual={bound}");
                    return false;
                }
                LoggerInstance.Msg($"RIVALS LEARNED ROWS | {role} | floor {floor} | {bound}");
            }
            return true;
        }
        finally
        {
            Write(learnFlag, previousFlag);
            Write("dungeonId", previousFloor);
            Write(FloorKey, previousUnlocked);
            Write(RoleKey, previousRole);
        }
    }

    private bool VerifyCardTooltips(Card card, Il2CppSystem.Collections.Generic.IReadOnlyList<TooltipContent> tooltips)
    {
        int count = tooltips.Cast<Il2CppSystem.Collections.Generic.IReadOnlyCollection<TooltipContent>>().Count;
        if (count == 0) return false;
        for (int index = 0; index < count; index++)
        {
            if (tooltips[index].Title == "使用条件") return false;
        }
        if (card == Card.Steal)
            return tooltips[0].Description.Contains("无需破防", StringComparison.Ordinal) &&
                tooltips[0].Description.Contains("不可夺取", StringComparison.Ordinal);
        if (RivalRules.Element(card).Length > 0)
            return count >= 2 && !string.IsNullOrWhiteSpace(tooltips[count - 1].Description);
        if (card is not (Card.CodexWeapon or Card.Codex or Card.Armor or Card.Sword)) return true;

        string details = string.Join(" | ", Enumerable.Range(0, count)
            .Select(index => $"{tooltips[index].Title}：{tooltips[index].Description}"));
        LoggerInstance.Msg($"RIVALS EFFECT TOOLTIPS | {card} | {details}");
        return card switch
        {
            Card.CodexWeapon => tooltips[0].Title.Contains("格挡", StringComparison.Ordinal) &&
                details.Contains("为指定友方添加", StringComparison.Ordinal) &&
                !details.Contains('⌈') && !details.Contains("为自己添加", StringComparison.Ordinal),
            Card.Codex => tooltips[0].Title.Contains("结界", StringComparison.Ordinal) &&
                details.Contains("脆弱", StringComparison.Ordinal),
            Card.Armor => details.Contains("为主角添加", StringComparison.Ordinal) &&
                !details.Contains("为自己添加", StringComparison.Ordinal),
            Card.Sword => details.Contains("造成23", StringComparison.Ordinal) &&
                details.Contains("<uppercase>str</uppercase>+18", StringComparison.Ordinal),
            _ => true
        };
    }

    private void VerifyLoveTiming(ItemService items)
    {
        var request = new ItemPresentationRequest(GetCard(Card.Love), "rivalsSmokeLove", "partner", "battle", false);
        int previousRound = Round;
        int previousUse = LastUsed(Card.Love);
        string previousCard = Read("selectCommand");
        try
        {
            Write("selectCommand", (int)Card.Love);
            CheckRound(1, false, "还需等待 1 回合");
            CheckRound(2, true, "使用后冷却 3 回合");
            Write(UsedKey(Card.Love), 2);
            CheckRound(2, false, "本回合已用");
            CheckRound(3, false, "冷却剩余 3 回合");
            CheckRound(5, false, "冷却剩余 1 回合");
            CheckRound(6, true, "使用后冷却 3 回合");
            LoggerInstance.Msg("RIVALS LOVE TIMING | first use 2 | after casting on 2 locked 3–5 | next use 6");
        }
        finally
        {
            Write("turnCount", previousRound);
            Write(UsedKey(Card.Love), previousUse);
            Write("selectCommand", previousCard);
        }

        void CheckRound(int round, bool ready, string timing)
        {
            Write("turnCount", round);
            string text = items.GetOrBuildPresentation(request).Description;
            if (CanCommitRivalCard() != ready || !text.Contains(timing, StringComparison.Ordinal) ||
                text.Contains("冷却时间：", StringComparison.Ordinal) ||
                (!ready && text.Contains("使用后冷却", StringComparison.Ordinal)))
                throw new InvalidOperationException($"Love timing on round {round}: {text}");
        }
    }
}
