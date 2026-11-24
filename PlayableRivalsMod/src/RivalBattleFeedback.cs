using Il2Cpp;
using Il2CppInterop.Runtime;
using Il2CppTMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PlayableRivals;

public sealed partial class RivalMod
{
    private TMP_FontAsset? battleFeedbackFont;
    private TMP_SpriteAsset? battleFeedbackSprites;
    private TextMeshProUGUI? battleFeedbackLabel;
    private Image? battleFeedbackBackground;
    private string battleFeedback = "";
    private float battleFeedbackUntil;
    private float nextBattleFeedbackRefresh;

    private void RememberBattleTextStyle(TMP_Text label)
    {
        battleFeedbackFont = label.font;
        battleFeedbackSprites = label.spriteAsset ?? TMP_Settings.defaultSpriteAsset;
    }

    private void ShowBattleFeedback(string message)
    {
        battleFeedback = message;
        battleFeedbackUntil = Time.unscaledTime + 3.2f;
        nextBattleFeedbackRefresh = 0;
    }

    private void ClearBattleFeedback()
    {
        battleFeedback = "";
        battleFeedbackUntil = 0;
        nextBattleFeedbackRefresh = 0;
    }

    private void ShowCoverFeedback(string summon, string target, double absorbed, double remaining, string destroyed)
    {
        string detail = remaining > 0 ? $"余 {remaining:0.##} 伤害" : "全部挡下";
        if (destroyed.Length > 0) detail += $" · {destroyed}";
        ShowBattleFeedback($"{summon}替 {CustomFunctions.TranslateName(target)} 挡下 {absorbed:0.##}\n{detail}");
    }

    private void UpdateBattleFeedback()
    {
        if (Time.unscaledTime < nextBattleFeedbackRefresh) return;
        nextBattleFeedbackRefresh = Time.unscaledTime + 0.12f;

        bool inBattle = IsRivalBattle && Flag(BattleKey);
        string message = inBattle && Time.unscaledTime < battleFeedbackUntil ? battleFeedback : "";
        if (inBattle && message.Length == 0)
        {
            int barrier = Number(BarrierHp);
            int doll = Number(DollHp);
            if (barrier > 0)
                message = $"屏障　{barrier}/{Number("rivalsBarrierMaxHp")} HP　格挡 {Number("rivalsBarrierBlock")}";
            else if (doll > 0)
                message = $"傀儡　{doll}/{Number("rivalsDollMaxHp")} HP　剩余攻击 {Number("rivalsDollCount")}";
        }

        if (message.Length == 0)
        {
            if (battleFeedbackBackground != null && battleFeedbackBackground.gameObject.activeSelf)
                battleFeedbackBackground.gameObject.SetActive(false);
            return;
        }

        if (battleFeedbackFont == null) return;
        if (battleFeedbackLabel == null) CreateBattleFeedback();
        var label = battleFeedbackLabel!;
        var background = battleFeedbackBackground!;
        if (label.text != message)
        {
            label.font = battleFeedbackFont;
            label.spriteAsset = battleFeedbackSprites;
            label.text = message;
            var size = label.GetPreferredValues(message, 396, 0);
            background.rectTransform.sizeDelta = new Vector2(Mathf.Min(416, size.x + 20), size.y + 10);
        }
        if (!background.gameObject.activeSelf) background.gameObject.SetActive(true);
    }

    private void CreateBattleFeedback()
    {
        var root = new GameObject("Playable Rivals Battle Feedback", Il2CppType.Of<RectTransform>(),
            Il2CppType.Of<Canvas>(), Il2CppType.Of<CanvasScaler>());
        UnityEngine.Object.DontDestroyOnLoad(root);
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 110;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = .5f;

        var panel = new GameObject("Battle feedback panel", Il2CppType.Of<RectTransform>(), Il2CppType.Of<Image>());
        panel.transform.SetParent(root.transform, false);
        battleFeedbackBackground = panel.GetComponent<Image>();
        battleFeedbackBackground.color = new Color(.04f, .09f, .15f, .87f);
        battleFeedbackBackground.raycastTarget = false;
        var frame = battleFeedbackBackground.rectTransform;
        frame.anchorMin = frame.anchorMax = frame.pivot = new Vector2(0, 1);
        frame.anchoredPosition = new Vector2(26, -64);

        var line = new GameObject("Battle feedback text", Il2CppType.Of<RectTransform>(), Il2CppType.Of<TextMeshProUGUI>());
        line.transform.SetParent(panel.transform, false);
        battleFeedbackLabel = line.GetComponent<TextMeshProUGUI>();
        battleFeedbackLabel.font = battleFeedbackFont;
        battleFeedbackLabel.fontSize = 20;
        battleFeedbackLabel.color = new Color(.96f, .97f, 1);
        battleFeedbackLabel.raycastTarget = false;
        var text = battleFeedbackLabel.rectTransform;
        text.anchorMin = Vector2.zero;
        text.anchorMax = Vector2.one;
        text.offsetMin = new Vector2(10, 5);
        text.offsetMax = new Vector2(-10, -5);
    }
}
