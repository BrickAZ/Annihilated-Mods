using Il2Cpp;
using Il2CppInterop.Runtime;
using Il2CppNaninovel;

using Il2CppNaninovel.UI;
using Il2CppTMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PlayableRivals;

public sealed partial class RivalMod
{
    private readonly Dictionary<string, Sprite> art = new(StringComparer.Ordinal);
    private readonly Dictionary<IntPtr, (Image Image, Sprite Original, Sprite Changed)> replacedArt = new();
    private readonly Dictionary<IntPtr, (TMP_Text Label, string Original, string Changed, Color OriginalColor)> replacedText = new();
    private Dictionary<string, string>? artNames;
    private float nextArtRefresh;
    private bool choosing;
    private string? unmodifiedChoiceName;
    private string? lastChoiceName;
    private string? unmodifiedChoiceDescription;
    private string? lastChoiceDescription;

    private void InstallArtHooks()
    {
        Patch(typeof(BattleCharacterSpriteController), "ApplySpriteAndSize", nameof(ReplaceBattleSprite));
        Patch(typeof(CustomFunctions), nameof(CustomFunctions.TranslateName), nameof(PartnerDisplayName), postfix: true);
        Patch(typeof(PartnerSkillPreview), nameof(PartnerSkillPreview.NotifyUI), nameof(PresentSelectionCards));
        Patch(typeof(CustomFunctions), nameof(CustomFunctions.TranslateMessage), nameof(ChoiceDescription), postfix: true);
        Patch(typeof(PartnerSkillPreview), "LateUpdate", nameof(ChoiceHeader), postfix: true);
        Patch(typeof(CustomFunctions), nameof(CustomFunctions.TranslateCharacterName), nameof(ChoiceDisplayName), postfix: true);
    }

    private static void PartnerDisplayName(string __0, ref string __result)
    {
        if (__0 == "partner" && active is { Role: not Rival.None } mod)
            __result = RivalRules.Name(mod.Role);
    }

    private static void ChoiceDisplayName(string __0, ref string __result)
    {
        if (active is { choosing: true } && RivalRules.FromPartner(__0 ?? "") is { } role && role != Rival.None)
            __result = RivalRules.Name(role);
    }

    private Sprite? FindArt(string name)
    {
        if (art.TryGetValue(name, out var loaded))
        {
            if (loaded != null && loaded.texture != null) return loaded;
            if (smokeSwap) LoggerInstance.Msg("RIVALS ART RELOAD " + name);
        }
        var assembly = typeof(RivalMod).Assembly;
        artNames ??= assembly.GetManifestResourceNames().ToDictionary(n => n.Replace('\\', '/'), n => n,
            StringComparer.Ordinal);
        if (!artNames.TryGetValue("rivals/" + name + ".png", out var originalName)) return null;
        using var file = assembly.GetManifestResourceStream(originalName)!;
        using var stream = new MemoryStream();
        file.CopyTo(stream);
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
        {
            name = "rivals/" + name,
            filterMode = name.StartsWith("dot/", StringComparison.Ordinal) ||
                         name.StartsWith("icons/", StringComparison.Ordinal) ? FilterMode.Point : FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        if (!ImageConversion.LoadImage(texture, stream.ToArray(), true))
        {
            UnityEngine.Object.Destroy(texture);
            return null;
        }
        // The stock selection buttons mask and tilt the portrait. Frame the eyes
        // within the painted background before letting the native mask crop it.
        var frame = name switch
        {
            "select/outcast" => new Rect(98, 181, 720, 480),
            "select/villainess" => new Rect(320, 168, 720, 480),
            "select/dropout" => new Rect(188, 145, 720, 480),
            _ when name.StartsWith("profile/outcast_", StringComparison.Ordinal) => new Rect(45, 699, 550, 185),
            "profile/villainess_happy" or "profile/villainess_damaged" => new Rect(12, 739, 550, 185),
            _ when name.StartsWith("profile/villainess_", StringComparison.Ordinal) => new Rect(147, 739, 550, 185),
            _ when name.StartsWith("profile/dropout_", StringComparison.Ordinal) => new Rect(104, 724, 550, 185),
            _ => new Rect(0, 0, texture.width, texture.height)
        };
        var sprite = Sprite.Create(texture, frame, new Vector2(0.5f, 0.5f), 100f);
        sprite.name = texture.name;
        art[name] = sprite;
        return sprite;
    }

    private void ObserveOpening(int line)
    {
        if (line == 1973) choosing = true;
        if (line == 2034) choosing = false;
    }

    private void UpdateArt()
    {
        if (choosing && player?.PlaybackSpot.ScriptName != "Opening3") choosing = false;
        if (Time.unscaledTime < nextArtRefresh) return;
        nextArtRefresh = Time.unscaledTime + 0.25f;
        RefreshPartnerHud();

        if (choosing)
        {
            foreach (var button in UnityEngine.Object.FindObjectsByType<ChoiceHandlerButton>(FindObjectsSortMode.None))
            {
                var state = button.ChoiceState;
                var script = state?.OnSelectScript?.Trim();
                if (script == null) continue;
                Rival role = script.EndsWith(".PartnerChoiceHero", StringComparison.Ordinal) ? Rival.Outcast :
                    script.EndsWith(".PartnerChoiceWarrior", StringComparison.Ordinal) ? Rival.Villainess :
                    script.EndsWith(".PartnerChoiceMage", StringComparison.Ordinal) ? Rival.Dropout : Rival.None;
                if (role == Rival.None) continue;
                var pic = FindArt("select/" + RivalRules.Id(role));
                if (pic != null)
                {
                    foreach (var image in button.GetComponentsInChildren<Image>(true))
                    {
                        if (image.sprite == null || image.sprite.name.StartsWith("rivals/", StringComparison.Ordinal)) continue;
                        Vector2 rect = image.rectTransform.sizeDelta;
                        if (rect.x < 1000 || rect.y < 650) continue;
                        ReplaceImage(image, pic);
                    }
                }
                foreach (var label in button.GetComponentsInChildren<TMP_Text>(true))
                    if ((LooksLikeOriginalName(role, label.text) || label.text == RivalRules.Name(role)) &&
                        !replacedText.ContainsKey(label.Pointer)) ReplaceLabel(label, role);
            }
        }

        if (!choosing && Role == Rival.None)
        {
            foreach (var item in replacedArt.Values)
                if (item.Image != null && item.Image.sprite == item.Changed) item.Image.sprite = item.Original;
            foreach (var item in replacedText.Values)
                if (item.Label != null && item.Label.text == item.Changed) { item.Label.text = item.Original; item.Label.color = item.OriginalColor; }
            replacedArt.Clear();
            replacedText.Clear();
        }
        replacedArt.Where(p => p.Value.Image == null).Select(p => p.Key).ToList().ForEach(p => replacedArt.Remove(p));
        replacedText.Where(p => p.Value.Label == null).Select(p => p.Key).ToList().ForEach(p => replacedText.Remove(p));
    }

    private static bool LooksLikeOriginalName(Rival role, string name)
    {
        return role switch
        {
            Rival.Outcast => name is "勇者" or "Hero",
            Rival.Villainess => name is "战士" or "戰士" or "戦士" or "Warrior",
            Rival.Dropout => name is "魔法师" or "魔法師" or "魔法使い" or "Mage",
            _ => false
        };
    }

    private void ReplaceImage(Image image, Sprite replacement)
    {
        replacedArt.TryAdd(image.Pointer, (image, image.sprite, replacement));
        image.sprite = replacement;
    }

    private void ReplaceLabel(TMP_Text label, Rival role)
    {
        string replacement = RivalRules.Name(role);
        replacedText.TryAdd(label.Pointer, (label, label.text, replacement, label.color));
        label.text = replacement;
        label.color = role switch
        {
            Rival.Outcast => new Color32(249, 231, 235, 255),
            Rival.Villainess => new Color32(255, 242, 224, 255),
            _ => new Color32(233, 241, 255, 255)
        };
    }

    private void RefreshPartnerHud()
    {
        // Status widgets keep their owner when the formation re-parents their transforms.
        var role = Role;
        Sprite? replacement = role == Rival.None ? null :
            FindArt("dot/dot_" + RivalRules.Id(role) + (Flag("partnerDeath") ? "_dead" : ""));
        foreach (var status in UnityEngine.Object.FindObjectsByType<DraggableStatusUI>(FindObjectsSortMode.None))
        {
            if (status.statusOwner == "partner" && status.characterName is { } name)
            {
                if (role != Rival.None && name.text != RivalRules.Name(role))
                    name.text = RivalRules.Name(role);
                else if (role == Rival.None && (name.text is "放逐者" or "反派千金" or "落选者"))
                    status.ChangeNameAndIcon();
            }
            var image = status.characterImage;
            if (image == null) continue;
            var displayed = image.overrideSprite;
            if (status.statusOwner == "partner" && replacement != null)
            {
                if (displayed != replacement) image.overrideSprite = replacement;
            }
            else if (displayed != null && displayed.name.StartsWith("rivals/dot/", StringComparison.Ordinal))
                image.overrideSprite = null;
        }
        foreach (var controller in UnityEngine.Object.FindObjectsByType<StatusFaceController>(
            FindObjectsInactive.Include, FindObjectsSortMode.None))
            RefreshPartnerStatusFace(controller.partnerFace, role);
    }

    private void RefreshPartnerStatusFace(Image image, Rival role)
    {
        if (image == null) return;
        var displayed = image.overrideSprite;
        var parent = image.rectTransform.parent;
        if (parent == null) return;
        var backdrop = parent.Find("RivalsProfileBackdrop")?.GetComponent<Image>();
        var original = image.sprite;
        if (role == Rival.None || original == null)
        {
            if (backdrop != null) backdrop.enabled = false;
            if (role == Rival.None && displayed != null && displayed.name.StartsWith("rivals/profile/", StringComparison.Ordinal))
                image.overrideSprite = null;
            return;
        }

        // The stock controller chooses the face state. Keep portrait and backing in sync.
        string name = original.name;
        string state = name[(name.LastIndexOf('_') + 1)..];
        if (state is not ("happy" or "damaged" or "extra")) state = "normal";
        if (FindArt("profile/" + RivalRules.Id(role) + "_" + state) is not { } face) return;
        if (displayed != face) image.overrideSprite = face;
        if (FindProfileBackdrop(state) is { } background)
            ShowProfileBackdrop(image, backdrop, background);
    }

    private Sprite? FindProfileBackdrop(string state)
    {
        string key = "profile_backdrops/" + state;
        if (art.TryGetValue(key, out var sprite) && sprite != null && sprite.texture != null)
            return sprite;
        var atlas = FindArt("profile_backdrops/atlas");
        if (atlas == null) return null;

        const float aspect = 550f / 185f;
        float tileWidth = atlas.texture.width / 2f;
        float tileHeight = atlas.texture.height / 2f;
        float width = Math.Min(tileWidth, tileHeight * aspect) * 0.92f;
        float height = width / aspect;
        float centerX = tileWidth * (state is "happy" or "extra" ? 1.5f : 0.5f);
        float centerY = tileHeight * (state is "normal" or "happy" ? 1.5f : 0.5f);
        sprite = Sprite.Create(atlas.texture, new Rect(centerX - width / 2, centerY - height / 2, width, height),
            new Vector2(0.5f, 0.5f), 100f);
        sprite.name = "rivals/" + key;
        art[key] = sprite;
        return sprite;
    }

    private static void ShowProfileBackdrop(Image portrait, Image? backdrop, Sprite background)
    {
        var face = portrait.rectTransform;
        if (backdrop == null)
        {
            var node = new GameObject("RivalsProfileBackdrop", Il2CppType.Of<RectTransform>())
            {
                layer = portrait.gameObject.layer
            };
            node.transform.SetParent(face.parent, false);
            node.AddComponent<LayoutElement>().ignoreLayout = true;
            backdrop = node.AddComponent<Image>();
            backdrop.raycastTarget = false;
        }

        // A sibling shares the stock mask and fades with the portrait's panel.
        var rect = backdrop.rectTransform;
        rect.anchorMin = face.anchorMin;
        rect.anchorMax = face.anchorMax;
        rect.pivot = face.pivot;
        rect.sizeDelta = face.sizeDelta;
        rect.anchoredPosition3D = face.anchoredPosition3D;
        rect.localScale = face.localScale;
        rect.localRotation = face.localRotation;
        int faceIndex = face.GetSiblingIndex();
        int backgroundIndex = rect.GetSiblingIndex();
        if (backgroundIndex != faceIndex - 1)
            rect.SetSiblingIndex(backgroundIndex < faceIndex ? faceIndex - 1 : faceIndex);
        backdrop.maskable = portrait.maskable;
        backdrop.preserveAspect = portrait.preserveAspect;
        backdrop.color = portrait.color;
        if (backdrop.sprite != background) backdrop.sprite = background;
        backdrop.enabled = portrait.enabled;
    }

    private static void PresentSelectionCards(ref string __0)
    {
        if (active is not { choosing: true } mod || string.IsNullOrEmpty(__0) ||
            !__0.StartsWith("store|", StringComparison.Ordinal)) return;
        var args = __0.Split('|');
        if (args.Length != 8) return;
        var role = RivalRules.FromPartner(args[1]);
        if (role == Rival.None) return;
        var stats = RivalRules.GetStats(role, 1);
        var first = RivalRules.Skills(role)[0];
        var weapon = RivalRules.FormFor(role, 1, 0, false);
        mod.GetCard(weapon).battleActionSetData.actionValues[0] =
            weapon == Card.Sword ? 18 : RivalRules.Amount(weapon, 1, stats);
        if (role == Rival.Villainess)
        {
            var values = mod.GetCard(Card.Love).battleActionSetData.actionValues;
            values[0] = RivalRules.ElementStacks("fire", stats.Mag);
            values[2] = RivalRules.Amount(Card.Love, 1, stats);
        }
        __0 = $"store|{args[1]}|{(int)first},{(int)weapon}|{stats.Str}|{stats.Dex}|{stats.Agi}|{stats.Mag}|1";
    }

    private static void ChoiceDescription(string __0, ref string __result)
    {
        if (active is not { choosing: true }) return;
        var role = __0 switch
        {
            "PartnerHeroExplain" => Rival.Outcast,
            "PartnerWarriorExplain" => Rival.Villainess,
            "PartnerMageExplain" => Rival.Dropout,
            _ => Rival.None
        };
        if (role != Rival.None) __result = SelectionDescription(role);
    }

    private static string SelectionDescription(Rival role) => role switch
    {
        Rival.Outcast => "魔剑命中时夺取强化。前排可准备抢夺，让下次武器攻击夺取敌人武器，仍受不可夺取限制。<BR><BR>每次出手直接伤害合计最多 30，间接伤害不限。击败赫德夺舍的士兵后解除。",
        Rival.Villainess => "用人偶之线从远处攻击。<BR><BR>「对战士的爱」让全体敌人燃烧、中毒，同时恢复自己的生命。施放前生命低于 70% 时不消耗行动。",
        Rival.Dropout => "用魔力法典为同伴提供格挡。<BR><BR>「法典」能为指定友方施加结界，施法后自己会变得脆弱。",
        _ => ""
    };

    private static void ChoiceHeader(PartnerSkillPreview __instance)
    {
        if (active is not { } mod || (!mod.choosing && mod.lastChoiceName == null)) return;
        var printer = __instance.GetComponentInParent<RevealableTextPrinterPanel>();
        if (printer == null || printer.name != "PrinterFullscreenFromLeft") return;
        var portrait = SelectionPortrait(printer);
        var label = SelectionName(printer);
        var description = __instance.description;
        if (portrait == null || label == null) return;
        var role = mod.choosing && __instance.current != null
            ? RivalRules.FromPartner(mod.Read("choicePartnerCharacter")) : Rival.None;
        if (role != Rival.None)
        {
            if (label.text != mod.lastChoiceName) mod.unmodifiedChoiceName = label.text;
            mod.lastChoiceName = RivalRules.Name(role);
            if (label.text != mod.lastChoiceName) label.text = mod.lastChoiceName;
            if (description != null)
            {
                string text = SelectionDescription(role);
                if (description.text != text)
                {
                    if (description.text != mod.lastChoiceDescription)
                        mod.unmodifiedChoiceDescription = description.text;
                    description.text = text;
                }
                mod.lastChoiceDescription = text;
            }
            var sprite = mod.FindArt("dot/dot_" + RivalRules.Id(role));
            if (sprite != null && portrait.overrideSprite != sprite) portrait.overrideSprite = sprite;
            return;
        }
        if (portrait.overrideSprite?.name.StartsWith("rivals/dot/", StringComparison.Ordinal) == true)
            portrait.overrideSprite = null;
        if (mod.lastChoiceName != null && label.text == mod.lastChoiceName)
            label.text = mod.unmodifiedChoiceName ?? "";
        if (description != null && mod.lastChoiceDescription != null && description.text == mod.lastChoiceDescription)
            description.text = mod.unmodifiedChoiceDescription ?? "";
        mod.lastChoiceName = null;
        mod.unmodifiedChoiceName = null;
        mod.lastChoiceDescription = null;
        mod.unmodifiedChoiceDescription = null;
    }

    private static Image? SelectionPortrait(RevealableTextPrinterPanel printer) =>
        printer.transform.Find("Content/PrinterPanel/Printer/GameObject/Image")?.GetComponent<Image>();

    private static TMP_Text? SelectionName(RevealableTextPrinterPanel printer) =>
        printer.transform.Find("Content/PrinterPanel/Printer/GameObject/NameContainer/Name")?.GetComponent<TMP_Text>();

    private static void ReplaceBattleSprite(BattleCharacterSpriteController __instance, Image __0, ref Sprite __1)
    {
        if (active is not { IsRivalBattle: true } mod || __0 == null || __1 == null ||
            __0 != __instance.partnerCharacterImage) return;
        __1 = mod.PartnerBattleArt(__1.name) ?? __1;
    }

    private Sprite? PartnerBattleArt(string spriteName)
    {


        string route = Read("partnerCharacter");
        string id = RivalRules.Id(Role);
        string artKey = spriteName;
        if (artKey is "stepFront" or "stepBack" or "guwa" or "taore")
        {
            return FindArt($"mini/{id}/{artKey}");
        }
        if (!artKey.Contains(route + "_attack_", StringComparison.OrdinalIgnoreCase) ||
            artKey.Contains("_effect", StringComparison.OrdinalIgnoreCase)) return null;
        bool useAttack = artKey.Contains("_attack_attack", StringComparison.OrdinalIgnoreCase) ||
                         artKey.Contains("Attack1", StringComparison.Ordinal);
        if (useAttack && TryCard(Number("selectCommand"), out var card))
        {
            if (card == Card.Dagger) return FindArt("battle/outcast_dagger_attack1");
            if (card == Card.Punch) return FindArt("battle/dropout_physical_attack1");
        }
        return FindArt("battle/" + id + (useAttack ? "_attack_attack1" : "_attack_prepare1"));
    }

}
