using Il2Cpp;
using Il2CppTMPro;
using System.Globalization;
using Tooltips = Il2CppSystem.Collections.Generic.List<Il2Cpp.TooltipContent>;
using ReadOnlyTooltips = Il2CppSystem.Collections.Generic.IReadOnlyList<Il2Cpp.TooltipContent>;

namespace PlayableRivals;

public sealed partial class RivalMod
{
    private void InstallPresentationHook()
    {
        Patch(typeof(ItemPresentationProvider), nameof(ItemPresentationProvider.GetOrBuild), nameof(DescribeRivalCard));
        Patch(typeof(BattleItemCommandDisplay), "RenderCharacterDependentTexts", nameof(DescribeLiveHandCard), postfix: true);
    }

    private static bool DescribeRivalCard(ItemPresentationRequest __0, ref ItemPresentationResult __result)
    {
        if (active is not { } mod || __0?.Source is not { } item || !TryCard(item.resourceName, out var card)) return true;
        if (buildingNativePresentation) return true;
        __result = mod.BuildRivalPresentation(card, item, __0.ActorId, __0.Purpose);
        return false;
    }

    private static void DescribeLiveHandCard(BattleItemCommandDisplay __instance, ItemData __0, string __1)
    {
        if (active is not { } mod || __0 == null) return;
        if (!TryCard(__0.resourceName, out var card))
        {
            if (mod.OutcastDamageLimited && __1 == "partner" && IsWeaponAttack(__0) &&
                __instance.itemDescriptionTextMesh is { } capLabel)
                capLabel.text = AppendCardText(capLabel.text, CapNotice);
            if (mod.BattleRival == Rival.Outcast && mod.StealPrepared && __1 == "partner" &&
                IsWeaponAttack(__0) && __instance.itemDescriptionTextMesh is { } label)
            {
                string notice = mod.PreparedWeaponCanSteal(__0) ? PreparedRobberyNotice : "本次出手将消耗抢夺准备";
                label.text = AppendCardText(label.text, $"<link=\"effectOutline:activeConditional\"><color=#FFF2A6>{notice}</color></link>");
            }
            return;
        }
        if (card is Card.Love or Card.Codex or Card.CodexWeapon && __instance.itemDescriptionTextMesh != null)
            mod.EnsureTargetSprites(__instance.itemDescriptionTextMesh.spriteAsset ?? TMP_Settings.defaultSpriteAsset);
        var presentation = mod.BuildRivalPresentation(card, __0, __1);
        if (__instance.itemDescriptionTextMesh != null)
        {
            mod.RememberBattleTextStyle(__instance.itemDescriptionTextMesh);
            __instance.itemDescriptionTextMesh.text = presentation.Description;
        }
        if (__instance.itemTooltipDisplay != null)
            __instance.itemTooltipDisplay.ReplaceContents(presentation.TooltipContents);
        var skill = RivalRules.GetSkill(card);
        if (skill.Floor == 0) return;
        var frame = CardOwner(card) switch
        {
            Rival.Outcast => __instance.HeroCardSprite,
            Rival.Villainess => __instance.WarriorCardSprite,
            Rival.Dropout => __instance.MageCardSprite,
            _ => null
        };
        if (__instance.CardImage != null && frame != null) __instance.CardImage.sprite = frame;
        string remaining = skill.Cooldown >= 0 ? "∞" : mod.IsRivalBattle && mod.LastUsed(card) > 0 ? "0" : "1";
        if (__instance.useLimitTMP != null)
            __instance.useLimitTMP.text = remaining;
        if (__instance.itemLimitTooltipDisplay != null)
        {
            if (skill.Cooldown >= 0)
            {
                __instance.itemLimitTooltipDisplay.ReplaceContents(presentation.TooltipContents);
            }
            else
            {
                var limits = new Tooltips();
                limits.Add(new TooltipContent($"剩余使用次数：{remaining}", "每场战斗限用 1 次。"));
                __instance.itemLimitTooltipDisplay.ReplaceContents(limits.Cast<ReadOnlyTooltips>());
            }
        }
    }

    private ItemPresentationResult BuildRivalPresentation(Card card, ItemData item, string actor, string purpose = "battle")
    {
        var native = GetNativeCardPresentation(card, item, actor, purpose);
        string name = CardName(card);
        string compact = ApplyRivalTargetArt(native.Description ?? "", card);
        string rules = ExtraCardRules(card);
        compact = UsesModAction(card) ? AppendCardText(rules, compact) : AppendCardText(compact, rules);
        var contents = new Tooltips();
        if (card is Card.Sword or Card.Dagger && OutcastDamageLimited)
        {
            compact = AppendCardText(compact, CapNotice);
            contents.Add(new TooltipContent("伤害限制", CapDescription));
        }
        string note = SpecialEffectDescription(card);
        if (note.Length > 0)
            contents.Add(new TooltipContent(name, note));
        var skill = RivalRules.GetSkill(card);
        if (skill.Floor > 0)
        {
            string availability = SkillAvailability(card, skill);
            if (availability.Length > 0)
                compact = AppendCardText(compact, FormatUnavailable(availability));
            else if (IsRivalBattle && skill.Cooldown > 0)
                compact = AppendCardText(compact, $"使用后冷却 {skill.Cooldown} 回合");
            if (!IsRivalBattle && card is Card.Love or Card.Steal)
                compact = FitPreviewCooldown(compact, card);
        }
        var nativeTooltips = native.TooltipContents;
        int nativeTooltipCount = nativeTooltips.Cast<Il2CppSystem.Collections.Generic.IReadOnlyCollection<TooltipContent>>().Count;
        for (int index = 0; index < nativeTooltipCount; index++)
        {
            var tooltip = nativeTooltips[index];
            string title = tooltip.Title ?? "";
            string description = tooltip.Description ?? "";
            description = card switch
            {
                Card.CodexWeapon => description.Replace("为自己添加", "为指定友方添加", StringComparison.Ordinal),
                Card.Armor => description.Replace("为自己添加", "为主角添加", StringComparison.Ordinal),
                Card.Sword => description.Replace("<size=75%>(<uppercase>str</uppercase>)</size>", "", StringComparison.Ordinal),
                _ => description
            };
            contents.Add(new TooltipContent(ApplyRivalTargetArt(title, card), ApplyRivalTargetArt(description, card)));
        }
        string element = RivalRules.Element(card);
        if (element.Length > 0)
        {
            int stacks = RivalRules.ElementStacks(element, CardDisplayStats(card).Mag);
            string description = ActionOrStateFunctions.GetStateDescriptionText(element, stacks.ToString(CultureInfo.InvariantCulture), true, "enemy1");
            if (!string.IsNullOrEmpty(description))
                contents.Add(new TooltipContent(ActionOrStateFunctions.GetStateReferenceText(element), description));
        }
        return new ItemPresentationResult(name, compact, contents.Cast<ReadOnlyTooltips>(), false);
    }

    private string SkillAvailability(Card card, Skill skill)
    {
        if (!IsRivalBattle) return "";
        int last = LastUsed(card);
        if (last > 0 && skill.Cooldown < 0) return "本场已用";
        if (card == Card.Armor && ArmorLockedInFinale)
            return "剧情施法结束后可用<BR>最终战第 4 阶段";
        if (last > 0 && Round == last) return "本回合已用";
        if (last > 0 && Round <= last + skill.Cooldown)
            return $"冷却剩余 {last + skill.Cooldown + 1 - Round} 回合";
        return Round < skill.First ? $"还需等待 {skill.First - Round} 回合" : "";
    }

    private static string FormatUnavailable(string text) =>
        $"<link=\"effectOutline:activeNegative\"><color=#FF8080>{text}</color></link>";

    private static string FitPreviewCooldown(string text, Card card)
    {
        const string label = "冷却时间：";
        int cooldown = text.IndexOf(label, StringComparison.Ordinal);
        if (cooldown < 0) return text;
        text = text.Insert(cooldown + label.Length, "<BR>");
        if (card == Card.Steal) return text;

        // The warrior frame's diagonal decoration covers the lower right of the preview.
        const string outline = "<link=\"effectOutline:activeNegative\">";
        int start = text.LastIndexOf(outline, cooldown, StringComparison.Ordinal);
        return start < 0 ? text : text.Insert(start, "<margin-right=36%><size=92%>") + "</size></margin>";
    }

    private static string AppendCardText(string original, string extra) =>
        extra.Length == 0 ? original : original.Length == 0 || original.EndsWith("<BR>", StringComparison.Ordinal)
            ? original + extra : original + "<BR>" + extra;

    private string ExtraCardRules(Card card)
    {
        var stats = CardDisplayStats(card);
        int Amount() => RivalRules.Amount(card, IsRivalBattle ? Number("partnerLv") : 1, stats);

        return card switch
        {
            Card.Sword or Card.Dagger => "命中时结算 2 次" + PreparedRobberyLine(),
            Card.Steal => "下次武器攻击可抢夺<BR>无需破防<BR>不可夺取武器除外",
            Card.Switch => "下回合切换匕首<BR>每回合行动 2 次<BR>每刀为魔剑伤害一半<BR>每场限用 1 次",
            Card.Enchant => "选择火／冰／雷／毒<BR>每轮首次专武命中触发，持续 3 回合",
            Card.Fire or Card.Freeze or Card.Thunder or Card.Poison => EnchantmentText(card, stats.Mag),
            Card.Doll => $"召唤 {Amount()} HP 的傀儡<BR>下回合起每轮攻击<BR>最多攻击 3 次",
            Card.Barrier => $"召唤 {Amount()} HP 的屏障<BR>自带 {(stats.Mag + 1) / 2} 点格挡<BR>替全队挡伤，最多两轮",
            Card.Armor => "魔力清零，改用物攻",
            _ => ""
        };
    }

    private Stats CardDisplayStats(Card card) => IsRivalBattle ? CurrentStats() : RivalRules.GetStats(CardOwner(card), 1);

    private const string PreparedRobberyNotice = "本次攻击可抢夺武器";

    private string PreparedRobberyLine() => BattleRival == Rival.Outcast && StealPrepared
        ? $"<BR><link=\"effectOutline:activeConditional\"><color=#FFF2A6>{PreparedRobberyNotice}</color></link>"
        : "";

    private static string EnchantmentText(Card card, int mag)
    {
        string element = RivalRules.Element(card);
        string icon = ActionOrStateFunctions.GetStateValueReferenceText(element, RivalRules.ElementStacks(element, mag));
        return $"每轮首个专武命中附加{icon}<BR>持续 3 回合";
    }


    private static Rival CardOwner(Card card) => card switch
    {
        Card.Sword or Card.Dagger or Card.Steal or Card.Switch or Card.Enchant or Card.Fire or Card.Freeze or Card.Thunder or Card.Poison => Rival.Outcast,
        Card.Thread or Card.Love or Card.Dominate or Card.Doll => Rival.Villainess,
        Card.CodexWeapon or Card.Punch or Card.Codex or Card.Barrier or Card.Armor => Rival.Dropout,
        _ => throw new ArgumentOutOfRangeException(nameof(card))
    };

    private static string SpecialEffectDescription(Card card) => card switch
    {
        Card.Sword or Card.Dagger => "命中时，逐项夺取强化的效果结算两次。",
        Card.Steal => "发动时必须在前排。下一次武器攻击开放抢夺 QTE，普通武器也可生效，目标无需破防。武器自带不可夺取时不会开放抢夺 QTE。武器出手即消耗准备，落空或不可夺取也会消耗。治疗与非攻击技能不会消耗准备。",
        Card.Switch => "本次行动用于切换。下一回合起每轮行动两次并使用匕首，持续本场。每刀为魔剑基础伤害的一半，向下取整，命中仍逐项转移两次强化。",
        Card.Enchant => "从火、冰、雷、毒选一种。持续 3 回合，含施放回合，每轮首次专武命中触发。",
        Card.Fire or Card.Freeze or Card.Thunder or Card.Poison => "附魔持续 3 回合，计入施放回合。由每轮首次专武命中触发。",
        Card.Love => "对全体存活敌人施加燃烧和中毒，并治疗自己。施放前生命低于 70% 时不消耗行动，治疗后超过阈值也不会扣行动，冷却照常计算。",
        Card.Doll => "傀儡登场带 1 层掩护，从下一回合起每轮攻击一次。伤害为技巧除以 3 后向上取整，最多攻击 3 次。",
        Card.Barrier => "屏障替全队承伤，最多覆盖接下来的两个敌方行动阶段。打穿后，剩余伤害由原目标承受。",
        Card.Armor => "覆甲后本场魔力清零，专武切换为单段物理攻击。",
        _ => ""
    };
}
