using Il2CppTMPro;
using UnityEngine;
using UnityEngine.TextCore;

namespace PlayableRivals;

public sealed partial class RivalMod
{
    private const string TargetSpriteAssetName = "RivalsCardTargets";
    private const string TargetGap = "<space=0.24em>";
    private TMP_SpriteAsset? targetSprites;

    private void EnsureTargetSprites(TMP_SpriteAsset? original)
    {
        if (targetSprites != null || original == null || original.material == null) return;
        int referenceIndex = original.GetSpriteIndexFromName("target_player");
        if (referenceIndex < 0 || FindArt("icons/card_targets") is not { } art) return;

        var reference = original.spriteCharacterTable[referenceIndex].glyph;
        var atlas = art.texture;
        atlas.hideFlags = HideFlags.DontUnloadUnusedAsset;
        var material = new Material(original.material)
        {
            name = TargetSpriteAssetName,
            mainTexture = atlas,
            hideFlags = HideFlags.DontUnloadUnusedAsset
        };
        var asset = ScriptableObject.CreateInstance<TMP_SpriteAsset>();
        asset.name = TargetSpriteAssetName;
        asset.version = original.version;
        asset.faceInfo = original.faceInfo;
        asset.hideFlags = HideFlags.DontUnloadUnusedAsset;
        asset.spriteSheet = atlas;
        asset.material = material;
        asset.spriteGlyphTable = new();
        asset.spriteCharacterTable = new();

        // Use the same inline size and baseline as the game's soldier portrait.
        var names = new[] { "villainess", "dropout" };
        for (uint index = 0; index < names.Length; index++)
        {
            var glyph = new TMP_SpriteGlyph(index, reference.metrics,
                new GlyphRect(2 + 28 * (int)index, 2, 24, 24), reference.scale, 0);
            asset.spriteGlyphTable.Add(glyph);
            asset.spriteCharacterTable.Add(new TMP_SpriteCharacter(0xE000 + index, asset, glyph)
            {
                name = names[index]
            });
        }
        asset.UpdateLookupTables();
        MaterialReferenceManager.AddSpriteAsset(asset);
        targetSprites = asset;
    }

    private string ApplyRivalTargetArt(string text, Card card)
    {
        var role = CardOwner(card);
        if (role is not (Rival.Villainess or Rival.Dropout) || !text.Contains("target_", StringComparison.Ordinal))
            return text;

        string portrait = targetSprites != null
            ? $"<sprite=\"{TargetSpriteAssetName}\" name=\"{RivalRules.Id(role)}\">"
            : role == Rival.Villainess ? "千金" : "落选者";
        string original = role == Rival.Villainess ? "target_warrior" : "target_mage";
        text = text.Replace($"<sprite name=\"{original}\">", portrait, StringComparison.Ordinal);
        if (!IsRivalBattle)
        {
            // The selection screen still carries the previous partner until a choice is made.
            text = text.Replace("<sprite name=\"target_hero\">", portrait, StringComparison.Ordinal)
                .Replace("<sprite name=\"target_warrior\">", portrait, StringComparison.Ordinal)
                .Replace("<sprite name=\"target_mage\">", portrait, StringComparison.Ordinal);
            if (card is Card.Codex or Card.CodexWeapon)
                text = text.Replace("<sprite name=\"target_choice\">　" + portrait,
                    "<sprite name=\"target_choice\">　<sprite name=\"target_player\">　" + portrait,
                    StringComparison.Ordinal);
        }
        return text;
    }
}
