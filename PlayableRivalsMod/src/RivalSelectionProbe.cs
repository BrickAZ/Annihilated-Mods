using Il2Cpp;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppNaninovel.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PlayableRivals;

public sealed partial class RivalMod
{
    private System.Collections.IEnumerator VerifySelection()
    {
        var preview = Resources.FindObjectsOfTypeAll<PartnerSkillPreview>()
            .First(p => p.gameObject.scene.IsValid() && p.entries.Count >= 3);
        var printer = preview.GetComponentInParent<RevealableTextPrinterPanel>();
        var portrait = printer != null ? SelectionPortrait(printer) : null;
        var header = printer != null ? SelectionName(printer) : null;
        if (printer == null || portrait == null || header == null)
        {
            LoggerInstance.Error($"RIVALS SMOKE FAILED could not find selection printer or portrait: {printer?.name}");
            yield break;
        }

        var cases = new[]
        {
            (Route: "hero", Text: "每次出手直接伤害合计最多 30", StockSkill: "-201", Frame: "card_hero"),
            (Route: "warrior", Text: "用人偶之线", StockSkill: "-401", Frame: "card_warrior"),
            (Route: "mage", Text: "用魔力法典", StockSkill: "-301", Frame: "card_mage")
        };

        foreach (var test in cases)
        {
            var role = RivalRules.FromPartner(test.Route);
            string name = RivalRules.Name(role);
            string dot = "rivals/dot/dot_" + RivalRules.Id(role);
            var expectedCard = RivalRules.Skills(role)[0];
            var entry = preview.entries[test.Route];
            var choice = UnityEngine.Object.FindObjectsByType<ChoiceHandlerButton>(FindObjectsSortMode.None)
                .First(c => c.ChoiceState?.OnSelectScript?.TrimEnd().EndsWith(".PartnerChoice" + test.Route, StringComparison.OrdinalIgnoreCase) == true);
            var trigger = choice.GetComponent<CustomEventTrigger>();

            trigger.InvokePointerEnter();

            float deadline = Time.realtimeSinceStartup + 4;
            while (Time.realtimeSinceStartup < deadline &&
                   (preview.description.text?.Contains(test.Text, StringComparison.Ordinal) != true ||
                    preview.presenter?.cardDisplay?.itemData?.id != (int)expectedCard))
                yield return null;
            yield return new WaitForSecondsRealtime(0.25f);
            if (test.Route == "hero")
            {
                // An older native text reveal can complete after the preview has changed.
                preview.description.text = "捉摸不透的僧侣！";
                yield return null;
            }

            var card = preview.presenter?.cardDisplay;
            string? frame = card?.CardImage?.sprite?.name;
            if (header.text != name || portrait.overrideSprite?.name != dot ||
                preview.description.text?.Contains(test.Text, StringComparison.Ordinal) != true ||
                card?.itemData?.id != (int)expectedCard || frame != test.Frame || card.useLimitTMP?.text != "∞")
            {
                LoggerInstance.Error($"RIVALS SMOKE FAILED selection {test.Route}: name={header.text}, dot={portrait.overrideSprite?.name}, description={preview.description.text}, card={card?.itemData?.id}, frame={frame}, stock={card?.useLimitTMP?.text}");
                yield break;
            }
            if (!VerifyFaceSprites(card, card.itemDescriptionTextMesh.text))
                yield break;

            string face = card.itemDescriptionTextMesh.text;
            bool nativePresentation = expectedCard switch
            {
                Card.Steal => face.Contains("下次武器攻击可抢夺", StringComparison.Ordinal) &&
                    !face.Contains("处于后排时", StringComparison.Ordinal),
                Card.Love => !face.Contains("target_choice", StringComparison.Ordinal) &&
                    face.Contains("<sprite name=\"fire\"", StringComparison.Ordinal) &&
                    face.Contains("<sprite name=\"poison\"", StringComparison.Ordinal) &&
                    face.Contains($"<sprite=\"{TargetSpriteAssetName}\" name=\"villainess\">", StringComparison.Ordinal),
                Card.Codex => face.Contains("<sprite name=\"block\" color=\"#3366FF\">2", StringComparison.Ordinal) &&
                    face.Contains("<sprite name=\"target_choice\">　<sprite name=\"target_player\">　" +
                        $"<sprite=\"{TargetSpriteAssetName}\" name=\"dropout\">", StringComparison.Ordinal),
                _ => false
            };
            if (!nativePresentation || face.Contains("第 2 回合起", StringComparison.Ordinal))
            {
                LoggerInstance.Error($"RIVALS SMOKE FAILED missing original condition or effect presentation for {expectedCard}: {face}");
                yield break;
            }

            LoggerInstance.Msg($"RIVALS SELECTION | {test.Route} | {name} | {dot} | {preview.description.text} | {frame} | {card.itemDescriptionTextMesh.text}");

            preview.next.onClick.Invoke();
            deadline = Time.realtimeSinceStartup + 4;
            int weapon = (int)RivalRules.FormFor(role, 1, 0, false);
            while (Time.realtimeSinceStartup < deadline && card.itemData?.id != weapon)
                yield return null;
            if (card.itemData?.id != weapon || header.text != name ||
                portrait.overrideSprite?.name != dot || preview.description.text?.Contains(test.Text, StringComparison.Ordinal) != true)
            {
                LoggerInstance.Error($"RIVALS SMOKE FAILED selection {test.Route} weapon page or unstable header: card={card.itemData?.id}");
                yield break;
            }
            if (!VerifyFaceSprites(card, card.itemDescriptionTextMesh.text))
                yield break;
            LoggerInstance.Msg($"RIVALS SELECTION WEAPON | {test.Route} | {card.CardImage.sprite.name} | {card.itemDescriptionTextMesh.text}");

            // Rebind the original page through the same native renderer for a direct comparison.
            var modified = entry.Cards;
            entry.Cards = new Il2CppStringArray(new[] { test.StockSkill });
            preview.page = 0;
            preview.ShowPage();
            deadline = Time.realtimeSinceStartup + 4;
            int stockSkill = int.Parse(test.StockSkill);
            while (Time.realtimeSinceStartup < deadline && card.itemData?.id != stockSkill)
                yield return null;
            yield return new WaitForSecondsRealtime(0.2f);
            string? stockFrame = card.CardImage.sprite?.name;
            string stockText = card.itemDescriptionTextMesh.text;
            entry.Cards = modified;
            if (card.itemData?.id != stockSkill || stockFrame != test.Frame || string.IsNullOrWhiteSpace(stockText))
            {
                LoggerInstance.Error($"RIVALS SMOKE FAILED original comparison {test.Route}: item={card.itemData?.id}, frame={stockFrame}, face={stockText}");
                yield break;
            }
            LoggerInstance.Msg($"RIVALS STOCK SELECTION | {test.Route} | {card.itemNameTextMesh.text} | {stockFrame} | {card.useLimitTMP.text} | {stockText}");
            preview.ShowPage();
            trigger.InvokePointerExit();
            EventSystem.current.SetSelectedGameObject(null);
            yield return null;
        }

        var priest = UnityEngine.Object.FindObjectsByType<ChoiceHandlerButton>(FindObjectsSortMode.None)
            .First(c => c.ChoiceState?.OnSelectScript?.TrimEnd().EndsWith(".PartnerChoicePriest", StringComparison.Ordinal) == true);
        var priestTrigger = priest.GetComponent<CustomEventTrigger>();
        priestTrigger.InvokePointerEnter();
        float priestDeadline = Time.realtimeSinceStartup + 4;
        while (Time.realtimeSinceStartup < priestDeadline &&
            (portrait.overrideSprite?.name != "dot_priest" || preview.presenter?.cardDisplay?.itemData?.id != -501))
            yield return null;
        if (header.text != "僧侣" || portrait.overrideSprite?.name != "dot_priest" ||
            preview.presenter?.cardDisplay?.itemData?.id != -501)
        {
            LoggerInstance.Error($"RIVALS SMOKE FAILED priest profile: {header.text} | {portrait.overrideSprite?.name} | {preview.presenter?.cardDisplay?.itemData?.id}");
            yield break;
        }
        LoggerInstance.Msg($"RIVALS SELECTION PRIEST | {header.text} | {portrait.overrideSprite.name}");
        priestTrigger.InvokePointerExit();
    }

    private bool VerifyFaceSprites(BattleItemCommandDisplay card, string markup)
    {
        var asset = card.itemDescriptionTextMesh.spriteAsset;
        foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(markup, "<sprite name=\"([^\"]+)\""))
        {
            string name = match.Groups[1].Value;
            if (asset != null && asset.GetSpriteIndexFromName(name) >= 0) continue;
            LoggerInstance.Error($"RIVALS SMOKE FAILED sprite {name} missing in {asset?.name} on {card.itemData?.resourceName}");
            return false;
        }
        int wanted = System.Text.RegularExpressions.Regex.Matches(markup, $"<sprite=\"{TargetSpriteAssetName}\" name=\"[^\"]+\">").Count;
        if (wanted == 0) return true;
        var label = card.itemDescriptionTextMesh;
        label.ForceMeshUpdate();
        var mesh = label.textInfo;
        int rendered = mesh.characterInfo.Take(mesh.characterCount)
            .Count(c => c.isVisible && c.textElement?.textAsset?.Pointer == targetSprites?.Pointer);
        if (rendered != wanted)
        {
            LoggerInstance.Error($"RIVALS SMOKE FAILED target portraits on {card.itemData?.resourceName}: expected {wanted}, rendered {rendered}");
            return false;
        }
        return true;
    }
}
