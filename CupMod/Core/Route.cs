namespace CupCompetition;

internal sealed record Route(string Id, string Partner, string Name, string EndingType, bool Box = false, string Kind = "通关")
{
    public static readonly Route[] All =
    {
        new("hero", "hero", "并肩走出迷宫", "hero"),
        new("hero4Bad", "hero", "谁也不相信", "hero4Bad", Kind: "坏结局"),
        new("dropout_hero", "hero", "与落选者联手", "dropout", Kind: "坏结局"),
        new("mageMaryoku", "mage", "取回魔力并拉钩", "mageMaryoku"),
        new("mageNotMaryokuSuki", "mage", "未取回魔力 · 喜欢", "mageNotMaryokuSuki"),
        new("mageNotMaryokuIssyo", "mage", "未取回魔力 · 珍视", "mageNotMaryokuIssyo"),
        new("mageNotMaryokuSpecialWin", "mage", "未取回魔力 · 特殊胜利", "mageNotMaryokuSpecialWin"),
        new("mage5YakusokuSinaiBad", "mage", "取回魔力 · 没有拉钩", "mage5YakusokuSinaiBad", Kind: "坏结局"),
        new("mage-exit", "mage", "陷阱后折返", "", Kind: "提前收束"),
        new("warrior", "warrior", "偶像尾声", "warrior"),
        new("warrior_hakoniwa", "warrior", "箱庭尾声", "warrior", true),
        new("warriorKill", "warrior", "杀戮尾声", "warriorKill"),
        new("warriorAfterGlitch", "warrior", "故障后的偶像尾声", "warriorAfterGlitch"),
        new("warriorGlitchBad", "warrior", "接受战士 · 故障", "warriorGlitchBad", Kind: "回退结局"),
        new("dropout_warrior", "warrior", "放弃救援，与落选者联手", "dropout", Kind: "坏结局"),
        new("priestNod", "priest", "点头 · 慢生活", "priestNod"),
        new("priestNodAfterLowSan", "priest", "点头 · 低理智回退版", "priestNodAfterLowSan"),
        new("priestLowLoop", "priest", "摇头 · 低循环／魔王", "priestLowLoop"),
        new("priestHighLoop", "priest", "摇头 · 高循环", "priestHighLoop"),
        new("dropout_priest", "priest", "握住落选者的手", "dropout", Kind: "坏结局"),
        new("priest4KilledBad", "priest", "看向僧侣 · 低理智", "priest4KilledBad", Kind: "回退结局"),
        new("priest4NoReachBad", "priest", "袖手旁观", "priest4NoReachBad", Kind: "坏结局"),
        new("ritual", "common", "献祭仪式", "", Kind: "提前收束"),
    };

    public string PartnerName => Partner switch { "hero" => "勇者", "mage" => "魔法师", "warrior" => "战士", "priest" => "僧侣", _ => "序章" };

    public int LatestStartingFloor => Id switch
    {
        "ritual" or "mage-exit" => 1,
        "hero4Bad" or "dropout_hero" or "dropout_warrior" or "dropout_priest" or "priestNodAfterLowSan" or "priest4KilledBad" => 3,
        "priest4NoReachBad" or "mageNotMaryokuSpecialWin" => 4,
        "priestLowLoop" => 6,
        _ => 5
    };

    public bool IsEnding(string endingType, string partner, bool box) =>
        EndingType.Length > 0 && EndingType == endingType &&
        (endingType != "dropout" || Partner == partner) &&
        (endingType is not ("warrior" or "warriorAfterGlitch") || Box == box);

    public bool ContinuesAfter(string endingType) => (Id, endingType) is
        ("warriorAfterGlitch", "warriorGlitchBad") or ("priestNodAfterLowSan", "priest4KilledBad");

    public static string? EarlyEndingAt(string script, int line) => (script, line) switch
    {
        ("Opening3", 1509) => "ritual",
        ("Scenario_Mage1", 217 or 269) => "mage-exit",
        _ => null
    };

    public bool KeepsEarlyEnding(string script, int line) => EarlyEndingAt(script, line) == Id;

    public int HeroChoice => Id switch { "hero4Bad" => 2034, "dropout_hero" => 2033, _ => 2032 };
    public int PriestChoice(bool returned) => Id switch
    {
        "dropout_priest" => 1162,
        "priest4KilledBad" => 1160,
        "priestNodAfterLowSan" when !returned => 1160,
        _ => 1161
    };

    public bool KeepChoice(string script, int line, bool returned)
    {
        if (script == "Opening" && line == 120) return false;
        if (script == "Opening3" && line == 1431) return Id == "ritual";
        if (Id == "ritual" && script == "Opening3")
        {
            if (line is >= 77 and <= 81) return line == 80;
            if (line is >= 1388 and <= 1393) return line == 1388;
            if (line == 1433) return false;
        }
        if (script == "Opening3" && line >= 1983 && line <= 1986)
            return line == (Partner switch { "hero" => 1983, "warrior" => 1984, "mage" => 1985, "priest" => 1986, _ => -1 });

        return script switch
        {
            "Scenario_Hero3" when line is >= 2032 and <= 2034 => line == HeroChoice,
            "Scenario_Mage1" when line is >= 196 and <= 200 => (line is 196 or 199) == (Id == "mage-exit"),
            "Scenario_Mage3" when line is 1166 or 1167 => line == (Id is "mageMaryoku" or "mage5YakusokuSinaiBad" ? 1166 : 1167),
            "Scenario_Mage4" when line is 1561 or 1562 => Id == "mageNotMaryokuSpecialWin" ||
                line == (Id == "mageNotMaryokuSuki" ? 1561 : 1562),
            "Scenario_Mage5" when line is 250 or 251 => line == (Id == "mage5YakusokuSinaiBad" ? 251 : 250),
            "Scenario_Warrior1" when line is >= 1538 and <= 1542 => (line == 1539) == (Id == "warriorKill"),
            "Scenario_Warrior2" when line is 3484 or 3485 => line == (Box ? 3484 : 3485),
            "Scenario_Warrior2" when line is 3515 or 3516 => line == (Box ? 3515 : 3516),
            "Scenario_Warrior3" when line is 1433 or 1434 => line == (Id == "dropout_warrior" ? 1434 : 1433),
            "Scenario_Warrior5" when line is 1137 or 1138 =>
                line == ((Id is "warriorAfterGlitch" or "warriorGlitchBad") && !returned ? 1138 : 1137),
            "Scenario_Priest3" when line is >= 1160 and <= 1162 =>
                line == PriestChoice(returned),
            "Scenario_Priest4" when line is 2907 or 2908 => line == (Id == "priest4NoReachBad" ? 2908 : 2907),
            "Scenario_Priest5" when line is 141 or 142 =>
                line == (Id is "priestLowLoop" or "priestHighLoop" ? 141 : 142),
            _ => true
        };
    }
}

internal sealed record CupSettings
{
    public string EventId { get; init; } = "Cup-001";
    public string RouteId { get; init; } = "hero";
    public string BossId { get; init; } = "";
    public int Seed { get; init; } = 20261113;
    public string BattleMode { get; init; } = "probability";
    public int LimitMinutes { get; init; }
    public int MaxRoomRetries { get; init; }
    public int RoomRetryPenaltySeconds { get; init; }
    public int MaxDeaths { get; init; }
    public int DeathPenaltySeconds { get; init; }
    public int StartingCurse { get; init; }
    public int StartingAllocationPoints { get; init; }
    public bool OpeningDeath { get; init; }
    public bool RunKnife { get; init; }
    public int StartingFloor { get; init; } = 1;
    public int? MaxWeapons { get; init; }
    public bool KickOnly { get; init; }
    public bool HarshTraining { get; init; }
}
