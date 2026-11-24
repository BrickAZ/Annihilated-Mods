using Il2Cpp;
using Il2CppNaninovel;
using SkillEntries = Il2CppSystem.Collections.Generic.IReadOnlyList<Il2Cpp.IconTextListEntry>;

namespace PlayableRivals;

public sealed partial class RivalMod
{
    private void InstallLearnedSkillHooks()
    {
        Patch(typeof(LearnedSkillListSource), nameof(LearnedSkillListSource.BuildEntries),
            nameof(BuildRivalLearnedSkills));
        Patch(typeof(LearnedSkillListSource), nameof(LearnedSkillListSource.GetStateSignature),
            nameof(RivalLearnedSkillSignature));
        Patch(typeof(LearnedSkillStatusPanel), nameof(LearnedSkillStatusPanel.HandleVariableUpdated),
            nameof(RefreshRivalLearnedSkills), postfix: true);
        Patch(typeof(IconTextListRow), nameof(IconTextListRow.Bind),
            nameof(LoadRivalLearnedSkillIcon), postfix: true);
    }

    private static bool BuildRivalLearnedSkills(IconTextListContext __0, ref SkillEntries __result)
    {
        if (__0?.OwnerId != "partner" || active is not { Role: not Rival.None } mod) return true;

        var entries = new Il2CppSystem.Collections.Generic.List<IconTextListEntry>();
        foreach (var card in RivalRules.Skills(mod.Role))
        {
            if (RivalRules.GetSkill(card).Floor > mod.UnlockedFloor) continue;
            string id = CardPrefix + card;
            string icon = OriginalIcon(card) is { } original ? "Item/" + original :
                card == Card.Doll ? "Icon/summon" : "";
            var request = new ItemCardPreviewRequest(id, "partner", mod.GetCard(card));
            entries.Add(new IconTextListEntry(id, icon, CardName(card), HoverPreview.ItemCardPreview(request)));
        }
        __result = entries.Cast<SkillEntries>();
        return false;
    }

    private static bool RivalLearnedSkillSignature(IconTextListContext __0, ref int __result)
    {
        if (__0?.OwnerId != "partner" || active is not { Role: not Rival.None } mod) return true;
        __result = int.MinValue + 4 * (int)mod.Role + mod.UnlockedFloor;
        return false;
    }

    private static void RefreshRivalLearnedSkills(LearnedSkillStatusPanel __instance, CustomVariableUpdatedArgs __0)
    {
        if (__instance.statusOwner == "partner" &&
            __0?.Name is RoleKey or FloorKey or "dungeonId")
            __instance.refreshRequested = true;
    }

    private static void LoadRivalLearnedSkillIcon(IconTextListRow __instance, IconTextListEntry __0)
    {
        if (active is not { } mod || !TryCard(__0?.Id, out var card) ||
            NewIcon(card) is not { } artKey || mod.FindArt(artKey) is not { } sprite) return;
        var icon = __instance.icon;
        if (icon == null) return;
        icon.sprite = sprite;
        var color = icon.color;
        color.a = 1;
        icon.color = color;
    }
}
