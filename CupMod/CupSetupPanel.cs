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
    internal bool ShowEntertainmentRules { get; set; }
    internal bool ShowCombatRules { get; set; }
    private bool showBossRoutes;
    private readonly CupLanguage language;
    private string T(string chinese, string english) => language.Text(chinese, english);

    public CupSetupPanel(CupLanguage language)
    {
        this.language = language;
        var font = Font.CreateDynamicFontFromOSFont("Microsoft YaHei", 24);
        title = Style(font, 32, Ink);
        title.fontStyle = FontStyle.Bold;
        ruleTitle = Style(font, language.English ? 21 : 23, Ink);
        ruleTitle.wordWrap = true;
        ruleTitle.fontStyle = FontStyle.Bold;
        ruleText = Style(font, 18, Ink);
        ruleText.wordWrap = true;
        label = Style(font, 16, Ink);
        hint = Style(font, 14, Muted);
        hint.clipping = TextClipping.Clip;
        warning = Style(font, 14, new Color(1f, 0.68f, 0.53f));
        warning.clipping = TextClipping.Clip;
        warning.wordWrap = true;
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
        float height = ShowEntertainmentRules ? 760 : 775;
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

            if (showBossRoutes)
            {
                DrawBossRoutes(setup);
                return;
            }
            if (ShowEntertainmentRules)
            {
                if (ShowCombatRules) DrawCombatRules(setup);
                else DrawEntertainmentRules(setup);
                return;
            }

            GUI.Label(new Rect(28, 20, 712, 45), T("杯赛配置", "Cup Setup"), title);
            GUI.Label(new Rect(28, 79, 92, 36), T("比赛模式", "Mode"), label);
            if (GUI.Button(new Rect(124, 79, 304, 36), T("指定结局", "Target Ending"), !setup.BossMode ? selected : button))
                setup.BossMode = false;
            if (GUI.Button(new Rect(436, 79, 304, 36), T("指定 Boss", "Target Boss"), setup.BossMode ? selected : button))
                setup.BossMode = true;

            DrawGoal(setup);

            GUI.Label(new Rect(28, 362, 136, 36), T("快速过剧情", "Story Auto-Skip"), label);
            GUI.Label(new Rect(172, 362, 336, 36), T("按所选分支自动推进对白、演出和剧情选项。", "Auto-play dialogue, cutscenes and choices."), hint);
            setup.FastStory = DrawRuleSwitch(362, setup.FastStory);

            GUI.Label(new Rect(28, 410, 92, 36), T("战斗方式", "Battle Mode"), label);
            if (GUI.Button(new Rect(124, 410, 304, 36), T("概率判定", "Probability"), setup.BattleMode == "probability" ? selected : button))
                setup.BattleMode = "probability";
            if (GUI.Button(new Rect(436, 410, 304, 36), T("时机判定", "Timing"), setup.BattleMode == "timing" ? selected : button))
                setup.BattleMode = "timing";

            GUI.Label(new Rect(28, 462, 228, 22), T("杯赛编号", "Cup ID"), label);
            GUI.Label(new Rect(268, 462, 270, 22), T("地图种子", "Map Seed"), label);
            GUI.Label(new Rect(570, 462, 170, 22), T("限时（分钟）", "Time Limit (min)"), label);
            setup.EventId = GUI.TextField(new Rect(28, 490, 228, 36), setup.EventId, input);
            setup.SeedText = GUI.TextField(new Rect(268, 490, 194, 36), setup.SeedText, input);
            if (GUI.Button(new Rect(470, 490, 88, 36), T("随机", "Random"), button))
                setup.SeedText = System.Security.Cryptography.RandomNumberGenerator.GetInt32(int.MinValue, int.MaxValue)
                    .ToString(System.Globalization.CultureInfo.InvariantCulture);
            setup.LimitText = GUI.TextField(new Rect(570, 490, 170, 36), setup.LimitText, input);

            GUI.Label(new Rect(28, 532, 169, 22), T("F9 重打上限（次）", "F9 Retry Limit"), label);
            GUI.Label(new Rect(209, 532, 169, 22), T("每次 SL 罚秒", "Penalty per Retry (s)"), label);
            GUI.Label(new Rect(390, 532, 169, 22), T("允许死亡（次）", "Deaths Allowed"), label);
            GUI.Label(new Rect(571, 532, 169, 22), T("每次死亡罚秒", "Penalty per Death (s)"), label);
            setup.RoomRetriesText = GUI.TextField(new Rect(28, 560, 169, 36), setup.RoomRetriesText, input);
            setup.RoomRetryPenaltyText = GUI.TextField(new Rect(209, 560, 169, 36), setup.RoomRetryPenaltyText, input);
            setup.DeathsText = GUI.TextField(new Rect(390, 560, 169, 36), setup.DeathsText, input);
            setup.DeathPenaltyText = GUI.TextField(new Rect(571, 560, 169, 36), setup.DeathPenaltyText, input);

            GUI.Label(new Rect(28, 602, 169, 22), T("开局诅咒", "Starting Curse"), label);
            GUI.Label(new Rect(209, 602, 169, 22), T("开局属性点", "Starting Stat Points"), label);
            GUI.Label(new Rect(390, 602, 169, 22), T("开局死亡（免罚）", "Opening Death (Free)"), label);
            GUI.Label(new Rect(571, 602, 169, 22), T($"起始楼层（1–{setup.LatestStartingFloor}）", $"Starting Floor (1–{setup.LatestStartingFloor})"), label);
            setup.StartingCurseText = GUI.TextField(new Rect(28, 630, 169, 36), setup.StartingCurseText, input);
            setup.StartingAllocationText = GUI.TextField(new Rect(209, 630, 169, 36), setup.StartingAllocationText, input);
            bool deathRequired = int.TryParse(setup.StartingAllocationText, out int points) && points > 0;
            if (GUI.Button(new Rect(390, 630, 169, 36),
                    deathRequired ? T("随属性点开启", "Required for Stat Points") : setup.OpeningDeath ? T("已开启", "On") : T("已关闭", "Off"),
                    (deathRequired || setup.OpeningDeath) ? selected : button) && !deathRequired)
                setup.OpeningDeath = !setup.OpeningDeath;
            setup.StartingFloorText = GUI.TextField(new Rect(571, 630, 169, 36), setup.StartingFloorText, input);

            bool valid = setup.TryGetSettings(out var settings, out string validation, language);
            string message = validation.Length > 0 ? validation : notice;
            GUI.Label(new Rect(28, 671, 712, 37),
                message.Length > 0 ? message : T("F9 需要重打当前房间 Mod。限时填 0 表示不限时。", "F9 requires the Room Retry Mod. Set the time limit to 0 for unlimited time."),
                message.Length > 0 ? warning : hint);

            if (GUI.Button(new Rect(28, 714, 132, 43), T("退出游戏", "Quit Game"), button)) exit();
            int ruleCount = (setup.RunKnife ? 1 : 0) + (setup.KickOnly ? 1 : 0) + (setup.HarshTraining ? 1 : 0) +
                (string.IsNullOrWhiteSpace(setup.MaxWeaponsText) ? 0 : 1) + (setup.FixedWeaponCombination ? 1 : 0) +
                (setup.ExtraActionAfterSteal ? 1 : 0) + (setup.DisabledCharacter.Length > 0 ? 1 : 0) +
                (settings is { StolenWeaponDamageMultiplier: not 1 } ? 1 : 0) +
                (settings is { EnemyHealthMultiplier: not 1 } ? 1 : 0) + (settings is { EnemyAttackMultiplier: not 1 } ? 1 : 0);
            if (GUI.Button(new Rect(172, 714, 200, 43),
                    ruleCount > 0 ? T($"娱乐规则 · {ruleCount} 项开启", $"Optional Rules · {ruleCount} On") : T("娱乐规则", "Optional Rules"), ruleCount > 0 ? selected : button))
            {
                GUI.FocusControl(null);
                ShowEntertainmentRules = true;
                ShowCombatRules = false;
            }
            var enabled = GUI.enabled;
            GUI.enabled = enabled && valid;
            try
            {
                if (GUI.Button(new Rect(551, 714, 189, 43), T("开始杯赛", "Start Cup"), primary)) start(settings!);
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
        GUI.Label(new Rect(28, 20, 712, 45), T("娱乐规则", "Optional Rules"), title);
        Fill(new Rect(28, 84, 712, 185), new Color(0.12f, 0.125f, 0.15f));
        GUI.Label(new Rect(48, 99, 392, 36), T("跑刀", "Knife Run"), ruleTitle);
        setup.RunKnife = DrawRuleSwitch(99, setup.RunKnife);
        GUI.Label(new Rect(48, 150, 672, 32), T("从敌人手里抢到武器后，自动进入这把武器的殉刃攻击条。", "Steal a usable weapon to immediately open its Drush attack bar."), ruleText);
        GUI.Label(new Rect(48, 184, 672, 32), T("玩家亲自完成一次攻击判定，命中或打空都会损坏武器。", "Attack once. The weapon breaks on a hit or miss."), ruleText);
        GUI.Label(new Rect(48, 218, 672, 32), T("如果被抢的敌人已经死亡，武器直接损坏。", "If that enemy is already dead, the weapon breaks immediately."), ruleText);
        Fill(new Rect(28, 281, 712, 138), new Color(0.12f, 0.125f, 0.15f));
        GUI.Label(new Rect(48, 296, 360, 36), T("背包武器上限", "Weapon Limit"), ruleTitle);
        GUI.Label(new Rect(360, 296, 224, 36), T("留空不限，0 不留", "Blank = unlimited; 0 = none"), hint);
        setup.MaxWeaponsText = GUI.TextField(new Rect(586, 296, 134, 36), setup.MaxWeaponsText, input);
        GUI.Label(new Rect(48, 340, 672, 32), T("超出的武器碎成诅咒，破坏价值为 0 时按兑换价折算。", "Extra weapons turn into Curse. At 0 break value, use the exchange price."), ruleText);
        GUI.Label(new Rect(48, 374, 672, 32), T("用殉刃夺取时，若旧武器同次行动损坏，新武器占用腾出的名额。", "A weapon stolen with Drush can take the slot of the one it breaks."), ruleText);

        Fill(new Rect(28, 431, 712, 122), new Color(0.12f, 0.125f, 0.15f));
        GUI.Label(new Rect(48, 446, 406, 36), T("仅主角命毁夺取武器", "Dreak-Only Weapon Loot"), ruleTitle);
        setup.KickOnly = DrawRuleSwitch(446, setup.KickOnly);
        GUI.Label(new Rect(48, 489, 672, 28), T("只保留主角用「命毁」从敌人手中夺来的武器。", "Keep only weapons the protagonist steals from enemies with Dreak."), ruleText);
        GUI.Label(new Rect(48, 517, 672, 28), T("开局、宝箱、事件、兑换等其他来源的武器自动碎成诅咒。", "All other weapons turn into Curse, including starting gear."), ruleText);
        Fill(new Rect(28, 565, 712, 122), new Color(0.12f, 0.125f, 0.15f));
        GUI.Label(new Rect(48, 572, 456, 51), T("严苛的训练几乎要了我的命", CupLanguage.TrainingTitle), ruleTitle);
        setup.HarshTraining = DrawRuleSwitch(580, setup.HarshTraining);
        GUI.Label(new Rect(48, 623, 672, 28), T("主角及原生没有夺取手段的同伴，可额外选择「命毁」。", "The protagonist and allies without a steal move gain Dreak in either row."), ruleText);
        GUI.Label(new Rect(48, 651, 672, 28), T("敌人无需破防即可被夺取，前后排都能选命毁，仍按原版判定。", "Steal before breaking guard. The usual attack check still applies."), ruleText);
        if (GUI.Button(new Rect(28, 705, 220, 43), T("战斗与火堆规则", "Combat and Bonfire Rules"), button))
            ShowCombatRules = true;
        if (GUI.Button(new Rect(552, 705, 188, 43), T("返回杯赛配置", "Back to Cup Setup"), primary))
            ShowEntertainmentRules = false;
    }

    private void DrawCombatRules(CupSetup setup)
    {
        GUI.Label(new Rect(28, 20, 712, 45), T("战斗与火堆规则", "Combat and Bonfire Rules"), title);

        GUI.Label(new Rect(48, 99, 450, 36), T("火堆固定合成武器", "Guaranteed Bonfire Fusion"), ruleTitle);
        setup.FixedWeaponCombination = DrawRuleSwitch(99, setup.FixedWeaponCombination);
        GUI.Label(new Rect(48, 142, 672, 32), T("在原版强化候选之外加上一项「合成武器」。", "Add a weapon fusion option alongside the normal bonfire upgrades."), ruleText);

        GUI.Label(new Rect(48, 195, 450, 36), T("夺取武器后多一个回合行动", "Extra Action After Stealing a Weapon"), ruleTitle);
        setup.ExtraActionAfterSteal = DrawRuleSwitch(195, setup.ExtraActionAfterSteal);
        GUI.Label(new Rect(48, 238, 672, 32), T("成功夺取后，夺取者在本轮额外行动一次。", "After a successful steal, the character takes one extra action this round."), ruleText);
        GUI.Label(new Rect(48, 275, 450, 32), T("怪物被夺取武器后下一回合受伤倍率", "Damage Taken Next Round After Weapon Steal"), label);
        GUI.Label(new Rect(516, 275, 22, 36), "×", label);
        setup.StolenWeaponDamageMultiplierText = GUI.TextField(new Rect(540, 275, 180, 36), setup.StolenWeaponDamageMultiplierText, input);
        setup.EnemyHealthMultiplierText = DrawMultiplier(48, 318, T("怪物血量倍率", "Enemy Health"), setup.EnemyHealthMultiplierText);
        setup.EnemyAttackMultiplierText = DrawMultiplier(508, 318, T("怪物攻击倍率", "Enemy Attack"), setup.EnemyAttackMultiplierText);

        GUI.Label(new Rect(48, 400, 672, 36), T("禁用一个角色", "Ban a Character"), ruleTitle);
        (string Id, string Name)[] characters =
        {
            ("", T("不禁用", "None")), ("player", T("士兵", "Soldier")), ("hero", T("勇者", "Hero")),
            ("warrior", T("战士", "Warrior")), ("mage", T("魔法师", "Mage")), ("priest", T("僧侣", "Priest"))
        };
        for (int i = 0; i < characters.Length; i++)
        {
            var (id, name) = characters[i];
            if (GUI.Button(new Rect(48 + i * 113, 452, 105, 38), name, setup.DisabledCharacter == id ? selected : button))
                setup.DisabledCharacter = id;
        }
        GUI.Label(new Rect(48, 505, 672, 32), T("禁用只影响战斗上场。士兵缺席时，同伴可按原版条件使用「命陨」。", "Bans apply in battle. A solo companion gains Dreak under its usual conditions."), ruleText);
        GUI.Label(new Rect(48, 556, 450, 36), T("接替被禁用角色的行动", "Take the Banned Character's Action"), ruleTitle);
        setup.InheritDisabledAction = DrawRuleSwitch(556, setup.InheritDisabledAction);
        GUI.Label(new Rect(48, 600, 672, 32), T("单人上场时，可以接替本轮空出的那次行动。", "A solo combatant can also take the vacant action each round."), ruleText);

        if (GUI.Button(new Rect(28, 705, 220, 43), T("武器规则", "Weapon Rules"), button))
            ShowCombatRules = false;
        if (GUI.Button(new Rect(552, 705, 188, 43), T("返回杯赛配置", "Back to Cup Setup"), primary))
            ShowEntertainmentRules = false;
    }

    private string DrawMultiplier(int x, int y, string name, string value)
    {
        GUI.Label(new Rect(x, y, 216, 22), name, label);
        GUI.Label(new Rect(x, y + 30, 22, 36), "×", label);
        return GUI.TextField(new Rect(x + 24, y + 30, 188, 36), value, input);
    }

    private bool DrawRuleSwitch(float y, bool on)
    {
        if (GUI.Button(new Rect(516, y, 96, 36), T("关闭", "Off"), on ? button : selected)) return false;
        if (GUI.Button(new Rect(620, y, 100, 36), T("开启", "On"), on ? selected : button)) return true;
        return on;
    }

    private void DrawGoal(CupSetup setup)
    {
        if (setup.BossMode)
        {
            GUI.Label(new Rect(28, 135, 92, 36), T("目标 Boss", "Boss"), label);
            for (int i = 0; i < BossGoal.All.Length; i++)
            {
                BossGoal boss = BossGoal.All[i];
                if (GUI.Button(GoalRect(i, 135), boss.DisplayName(language), boss == setup.Boss ? selected : button))
                    setup.SelectBoss(boss.Id);
            }
            GUI.Label(new Rect(28, 315, 92, 36), T("剧情分支", "Story Route"), label);
            string route = setup.BossRoute is { } chosen
                ? $"{chosen.DisplayPartner(language)} · {chosen.DisplayName(language)}"
                : T("选择可到达目标 Boss 的分支", "Choose a Route to the Target Boss");
            if (setup.Boss != null && GUI.Button(new Rect(124, 315, 616, 36), route, setup.BossRoute == null ? button : selected))
                showBossRoutes = true;
            return;
        }

        GUI.Label(new Rect(28, 135, 92, 36), T("同伴", "Companion"), label);
        for (int i = 0; i < CupSetup.Partners.Count; i++)
        {
            Route partner = CupSetup.Partners[i];
            if (GUI.Button(new Rect(124 + i * 124, 135, 120, 36), partner.DisplayPartner(language),
                    partner.Partner == setup.Route.Partner ? selected : button))
                setup.SelectPartner(partner.Partner);
        }
        GUI.Label(new Rect(28, 188, 92, 36), T("目标结局", "Ending"), label);
        for (int i = 0; i < setup.Endings.Count; i++)
        {
            Route route = setup.Endings[i];
            if (GUI.Button(GoalRect(i, 188), route.DisplayName(language), route.Id == setup.Route.Id ? selected : button))
                setup.SelectEnding(route.Id);
        }
        GUI.Label(new Rect(124, 323, 616, 24), T($"所选结局类型 · {setup.Route.Kind}　　地图岔路由你决定。",
            $"Ending type: {setup.Route.DisplayKind(language)}. You choose the path through the map."), hint);
    }

    private void DrawBossRoutes(CupSetup setup)
    {
        GUI.Label(new Rect(28, 20, 712, 45), T("选择剧情分支", "Choose a Story Route"), title);
        GUI.Label(new Rect(28, 70, 712, 30), setup.Boss?.DisplayName(language) ?? "", label);
        for (int i = 0; i < setup.BossRoutes.Count; i++)
        {
            Route route = setup.BossRoutes[i];
            var rect = new Rect(28 + i % 2 * 360, 108 + i / 2 * 46, 352, 40);
            string name = $"{route.DisplayPartner(language)} · {route.DisplayName(language)}";
            if (GUI.Button(rect, name, route == setup.BossRoute ? selected : button))
            {
                setup.SelectBossRoute(route.Id);
                showBossRoutes = false;
            }
        }
        if (GUI.Button(new Rect(552, 666, 188, 43), T("返回杯赛配置", "Back to Cup Setup"), primary))
            showBossRoutes = false;
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
        style.wordWrap = true;
        style.padding = new RectOffset(5, 5, 0, 0);
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
