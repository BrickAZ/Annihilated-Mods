using UnityEngine;

namespace CupCompetition;

internal sealed class CupSetupPanel
{
    private static readonly Color Ink = new(0.94f, 0.92f, 0.88f);
    private static readonly Color Muted = new(0.72f, 0.70f, 0.66f);
    private static readonly Color Gold = new(0.88f, 0.73f, 0.46f);
    private static readonly Color Dark = new(0.08f, 0.085f, 0.10f, 0.98f);
    private readonly GUIStyle title;
    private readonly GUIStyle ruleTitle;
    private readonly GUIStyle ruleText;
    private readonly GUIStyle label;
    private readonly GUIStyle hint;
    private readonly GUIStyle warning;
    private readonly GUIStyle button;
    private readonly GUIStyle selected;
    private readonly GUIStyle primary;
    private readonly GUIStyle input;
    private readonly GUIStyle fill = new();
    private bool showEntertainmentRules;

    public CupSetupPanel()
    {
        var font = Font.CreateDynamicFontFromOSFont("Microsoft YaHei", 24);
        title = Style(font, 32, Ink);
        title.fontStyle = FontStyle.Bold;
        ruleTitle = Style(font, 23, Ink);
        ruleTitle.fontStyle = FontStyle.Bold;
        ruleText = Style(font, 18, Ink);
        label = Style(font, 16, Ink);
        hint = Style(font, 14, Muted);
        hint.clipping = TextClipping.Clip;
        warning = Style(font, 14, new Color(1f, 0.68f, 0.53f));
        warning.clipping = TextClipping.Clip;
        button = ButtonStyle(font, Ink, new Color(0.16f, 0.17f, 0.20f), new Color(0.24f, 0.25f, 0.29f));
        selected = ButtonStyle(font, Gold, new Color(0.27f, 0.22f, 0.16f), new Color(0.37f, 0.30f, 0.20f));
        primary = ButtonStyle(font, new Color(0.11f, 0.09f, 0.06f), Gold, new Color(0.98f, 0.84f, 0.57f));
        primary.fontStyle = FontStyle.Bold;
        input = Style(font, 17, Ink);
        input.padding = new RectOffset(12, 10, 0, 0);
        input.normal.background = Texture(new Color(0.13f, 0.14f, 0.16f));
        input.focused.background = Texture(new Color(0.21f, 0.19f, 0.15f));
        fill.normal.background = Texture2D.whiteTexture;
    }

    public void Draw(CupSetup setup, string notice, Action<CupSettings> start, Action exit)
    {
        const float width = 768;
        float height = showEntertainmentRules ? 760 : 727;
        var previous = GUI.matrix;
        var guiColor = GUI.color;
        var contentColor = GUI.contentColor;
        var backgroundColor = GUI.backgroundColor;
        float scale = Math.Min(1.25f, Math.Min((Screen.width - 24f) / width, (Screen.height - 24f) / height));
        if (scale <= 0) return;

        try
        {
            GUI.color = GUI.contentColor = GUI.backgroundColor = Color.white;
            Fill(new Rect(0, 0, Screen.width, Screen.height), new Color(0.025f, 0.025f, 0.035f, 0.62f));
            GUI.matrix = Matrix4x4.TRS(
                new Vector3((Screen.width - width * scale) / 2, (Screen.height - height * scale) / 2, 0),
                Quaternion.identity, new Vector3(scale, scale, 1));
            Fill(new Rect(0, 0, width, height), Dark);
            Fill(new Rect(0, 0, width, 3), Gold);

            if (showEntertainmentRules)
            {
                DrawEntertainmentRules(setup);
                return;
            }

            GUI.Label(new Rect(28, 20, 712, 45), "杯赛配置", title);
            GUI.Label(new Rect(28, 79, 92, 36), "比赛模式", label);
            if (GUI.Button(new Rect(124, 79, 304, 36), "指定结局", !setup.BossMode ? selected : button))
                setup.BossMode = false;
            if (GUI.Button(new Rect(436, 79, 304, 36), "指定 Boss", setup.BossMode ? selected : button))
                setup.BossMode = true;

            DrawGoal(setup);

            GUI.Label(new Rect(28, 362, 92, 36), "战斗方式", label);
            if (GUI.Button(new Rect(124, 362, 304, 36), "概率判定", setup.BattleMode == "probability" ? selected : button))
                setup.BattleMode = "probability";
            if (GUI.Button(new Rect(436, 362, 304, 36), "时机判定", setup.BattleMode == "timing" ? selected : button))
                setup.BattleMode = "timing";

            GUI.Label(new Rect(28, 414, 228, 22), "杯赛编号", label);
            GUI.Label(new Rect(268, 414, 270, 22), "地图种子", label);
            GUI.Label(new Rect(570, 414, 170, 22), "限时（分钟）", label);
            setup.EventId = GUI.TextField(new Rect(28, 442, 228, 36), setup.EventId, input);
            setup.SeedText = GUI.TextField(new Rect(268, 442, 208, 36), setup.SeedText, input);
            if (GUI.Button(new Rect(484, 442, 74, 36), "随机", button))
                setup.SeedText = System.Security.Cryptography.RandomNumberGenerator.GetInt32(int.MinValue, int.MaxValue)
                    .ToString(System.Globalization.CultureInfo.InvariantCulture);
            setup.LimitText = GUI.TextField(new Rect(570, 442, 170, 36), setup.LimitText, input);

            GUI.Label(new Rect(28, 484, 169, 22), "F9 重打上限（次）", label);
            GUI.Label(new Rect(209, 484, 169, 22), "每次 SL 罚秒", label);
            GUI.Label(new Rect(390, 484, 169, 22), "允许死亡（次）", label);
            GUI.Label(new Rect(571, 484, 169, 22), "每次死亡罚秒", label);
            setup.RoomRetriesText = GUI.TextField(new Rect(28, 512, 169, 36), setup.RoomRetriesText, input);
            setup.RoomRetryPenaltyText = GUI.TextField(new Rect(209, 512, 169, 36), setup.RoomRetryPenaltyText, input);
            setup.DeathsText = GUI.TextField(new Rect(390, 512, 169, 36), setup.DeathsText, input);
            setup.DeathPenaltyText = GUI.TextField(new Rect(571, 512, 169, 36), setup.DeathPenaltyText, input);

            GUI.Label(new Rect(28, 554, 169, 22), "开局诅咒", label);
            GUI.Label(new Rect(209, 554, 169, 22), "开局属性点", label);
            GUI.Label(new Rect(390, 554, 169, 22), "开局死亡（免罚）", label);
            GUI.Label(new Rect(571, 554, 169, 22), $"起始楼层（1–{setup.LatestStartingFloor}）", label);
            setup.StartingCurseText = GUI.TextField(new Rect(28, 582, 169, 36), setup.StartingCurseText, input);
            setup.StartingAllocationText = GUI.TextField(new Rect(209, 582, 169, 36), setup.StartingAllocationText, input);
            bool deathRequired = int.TryParse(setup.StartingAllocationText, out int points) && points > 0;
            if (GUI.Button(new Rect(390, 582, 169, 36),
                    deathRequired ? "随属性点开启" : setup.OpeningDeath ? "已开启" : "已关闭",
                    (deathRequired || setup.OpeningDeath) ? selected : button) && !deathRequired)
                setup.OpeningDeath = !setup.OpeningDeath;
            setup.StartingFloorText = GUI.TextField(new Rect(571, 582, 169, 36), setup.StartingFloorText, input);

            bool valid = setup.TryGetSettings(out var settings, out string validation);
            string message = validation.Length > 0 ? validation : notice;
            GUI.Label(new Rect(28, 628, 712, 24),
                message.Length > 0 ? message : "F9 需要重打当前房间 Mod。限时填 0 表示不限时。",
                message.Length > 0 ? warning : hint);

            if (GUI.Button(new Rect(28, 666, 132, 43), "退出游戏", button)) exit();
            int ruleCount = (setup.RunKnife ? 1 : 0) + (setup.KickOnly ? 1 : 0) + (setup.HarshTraining ? 1 : 0) +
                (string.IsNullOrWhiteSpace(setup.MaxWeaponsText) ? 0 : 1);
            if (GUI.Button(new Rect(172, 666, 200, 43),
                    ruleCount > 0 ? $"娱乐规则 · {ruleCount} 项开启" : "娱乐规则", ruleCount > 0 ? selected : button))
            {
                GUI.FocusControl(null);
                showEntertainmentRules = true;
            }
            var enabled = GUI.enabled;
            GUI.enabled = enabled && valid;
            try
            {
                if (GUI.Button(new Rect(551, 666, 189, 43), "开始杯赛", primary)) start(settings!);
            }
            finally { GUI.enabled = enabled; }
        }
        finally
        {
            GUI.matrix = previous;
            GUI.color = guiColor;
            GUI.contentColor = contentColor;
            GUI.backgroundColor = backgroundColor;
        }
    }

    private void DrawEntertainmentRules(CupSetup setup)
    {
        GUI.Label(new Rect(28, 20, 712, 45), "娱乐规则", title);
        Fill(new Rect(28, 84, 712, 185), new Color(0.12f, 0.125f, 0.15f));
        GUI.Label(new Rect(48, 99, 392, 36), "跑刀", ruleTitle);
        setup.RunKnife = DrawRuleSwitch(99, setup.RunKnife);
        GUI.Label(new Rect(48, 150, 672, 32), "从敌人手里抢到武器后，自动进入这把武器的殉刃攻击条。", ruleText);
        GUI.Label(new Rect(48, 184, 672, 32), "玩家亲自完成一次攻击判定，命中或打空都会损坏武器。", ruleText);
        GUI.Label(new Rect(48, 218, 672, 32), "如果被抢的敌人已经死亡，武器直接损坏。", ruleText);
        Fill(new Rect(28, 281, 712, 138), new Color(0.12f, 0.125f, 0.15f));
        GUI.Label(new Rect(48, 296, 360, 36), "背包武器上限", ruleTitle);
        GUI.Label(new Rect(442, 296, 142, 36), "留空不限，0 不留", hint);
        setup.MaxWeaponsText = GUI.TextField(new Rect(586, 296, 134, 36), setup.MaxWeaponsText, input);
        GUI.Label(new Rect(48, 340, 672, 32), "超出的武器碎成诅咒，破坏价值为 0 时按兑换价折算。", ruleText);
        GUI.Label(new Rect(48, 374, 672, 32), "用殉刃夺取时，若旧武器同次行动损坏，新武器占用腾出的名额。", ruleText);

        Fill(new Rect(28, 431, 712, 122), new Color(0.12f, 0.125f, 0.15f));
        GUI.Label(new Rect(48, 446, 406, 36), "仅主角命毁夺取武器", ruleTitle);
        setup.KickOnly = DrawRuleSwitch(446, setup.KickOnly);
        GUI.Label(new Rect(48, 489, 672, 28), "只保留主角用「命毁」从敌人手中夺来的武器。", ruleText);
        GUI.Label(new Rect(48, 517, 672, 28), "开局、宝箱、事件、兑换等其他来源的武器自动碎成诅咒。", ruleText);
        Fill(new Rect(28, 565, 712, 122), new Color(0.12f, 0.125f, 0.15f));
        GUI.Label(new Rect(48, 580, 462, 36), "严苛的训练几乎要了我的命", ruleTitle);
        setup.HarshTraining = DrawRuleSwitch(580, setup.HarshTraining);
        GUI.Label(new Rect(48, 623, 672, 28), "主角及原生没有夺取手段的同伴，可额外选择「命毁」。", ruleText);
        GUI.Label(new Rect(48, 651, 672, 28), "敌人无需破防即可被夺取，前后排都能选命毁，仍按原版判定。", ruleText);
        if (GUI.Button(new Rect(552, 705, 188, 43), "返回杯赛配置", primary))
            showEntertainmentRules = false;
    }

    private bool DrawRuleSwitch(float y, bool on)
    {
        if (GUI.Button(new Rect(516, y, 96, 36), "关闭", on ? button : selected)) return false;
        if (GUI.Button(new Rect(620, y, 100, 36), "开启", on ? selected : button)) return true;
        return on;
    }

    private void DrawGoal(CupSetup setup)
    {
        if (setup.BossMode)
        {
            GUI.Label(new Rect(28, 135, 92, 36), "目标 Boss", label);
            for (int i = 0; i < BossGoal.All.Length; i++)
            {
                BossGoal boss = BossGoal.All[i];
                if (GUI.Button(GoalRect(i, 135), boss.Name, boss == setup.Boss ? selected : button))
                    setup.SelectBoss(boss.Id);
            }
            GUI.Label(new Rect(124, 323, 616, 24), "同伴和路线自由选择。击败指定 Boss 时停表。", hint);
            return;
        }

        GUI.Label(new Rect(28, 135, 92, 36), "同伴", label);
        for (int i = 0; i < CupSetup.Partners.Count; i++)
        {
            Route partner = CupSetup.Partners[i];
            if (GUI.Button(new Rect(124 + i * 124, 135, 120, 36), partner.PartnerName,
                    partner.Partner == setup.Route.Partner ? selected : button))
                setup.SelectPartner(partner.Partner);
        }
        GUI.Label(new Rect(28, 188, 92, 36), "目标结局", label);
        for (int i = 0; i < setup.Endings.Count; i++)
        {
            Route route = setup.Endings[i];
            if (GUI.Button(GoalRect(i, 188), route.Name, route.Id == setup.Route.Id ? selected : button))
                setup.SelectEnding(route.Id);
        }
        GUI.Label(new Rect(124, 323, 616, 24), $"所选结局类型 · {setup.Route.Kind}　　地图岔路由你决定。", hint);
    }

    private static Rect GoalRect(int index, int top) => new(124 + index % 3 * 207, top + index / 3 * 44, 202, 36);

    private void Fill(Rect rect, Color color)
    {
        GUI.backgroundColor = color;
        GUI.Box(rect, new GUIContent(""), fill);
        GUI.backgroundColor = Color.white;
    }

    private static GUIStyle Style(Font font, int size, Color color)
    {
        var style = new GUIStyle
        {
            font = font,
            fontSize = size,
            alignment = TextAnchor.MiddleLeft,
            clipping = TextClipping.Clip
        };
        style.normal.textColor = style.focused.textColor = color;
        return style;
    }

    private static GUIStyle ButtonStyle(Font font, Color text, Color normal, Color hover)
    {
        var style = Style(font, 16, text);
        style.alignment = TextAnchor.MiddleCenter;
        style.normal.background = Texture(normal);
        style.hover.background = style.active.background = Texture(hover);
        style.hover.textColor = style.active.textColor = text;
        return style;
    }

    private static Texture2D Texture(Color color)
    {
        var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }
}
