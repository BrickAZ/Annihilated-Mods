namespace CupCompetition;

internal sealed record Route(string Id, string Partner, string Name, string EnglishName, string EndingType, bool Box = false, string Kind = "通关")
{
    public static readonly Route[] All =
    {
        new("hero", "hero", "并肩走出迷宫", "Leave the Dungeon Together", "hero"),
        new("hero4Bad", "hero", "谁也不相信", "Trust No One", "hero4Bad", Kind: "坏结局"),
        new("dropout_hero", "hero", "与落选者联手", "Side with Dropout", "dropout", Kind: "坏结局"),
        new("mageMaryoku", "mage", "取回魔力并拉钩", "Regain Magic and Pinky Promise", "mageMaryoku"),
        new("mageNotMaryokuSuki", "mage", "未取回魔力 · 喜欢", "No Magic · I Like You", "mageNotMaryokuSuki"),
        new("mageNotMaryokuIssyo", "mage", "未取回魔力 · 珍视", "No Magic · You Matter to Me", "mageNotMaryokuIssyo"),
        new("mageNotMaryokuSpecialWin", "mage", "未取回魔力 · 特殊胜利", "No Magic · Special Victory", "mageNotMaryokuSpecialWin"),
        new("mage5YakusokuSinaiBad", "mage", "取回魔力 · 没有拉钩", "Regain Magic · No Pinky Promise", "mage5YakusokuSinaiBad", Kind: "坏结局"),
        new("mage-exit", "mage", "陷阱后折返", "Turn Back After the Trap", "", Kind: "提前收束"),
        new("warrior", "warrior", "偶像尾声", "Idol Epilogue", "warrior"),
        new("warrior_hakoniwa", "warrior", "箱庭尾声", "Miniature Garden Epilogue", "warrior", true),
        new("warriorKill", "warrior", "杀戮尾声", "Slaughter Epilogue", "warriorKill"),
        new("warriorAfterGlitch", "warrior", "故障后的偶像尾声", "Idol Epilogue After the Glitch", "warriorAfterGlitch"),
        new("warriorGlitchBad", "warrior", "接受战士 · 故障", "Accept the Warrior · Glitch", "warriorGlitchBad", Kind: "回退结局"),
        new("dropout_warrior", "warrior", "放弃救援，与落选者联手", "Abandon the Rescue and Side with Dropout", "dropout", Kind: "坏结局"),
        new("priestNod", "priest", "点头 · 慢生活", "Nod · Slow Life", "priestNod"),
        new("priestNodAfterLowSan", "priest", "点头 · 低理智回退版", "Nod · Low-Sanity Rollback", "priestNodAfterLowSan"),
        new("priestLowLoop", "priest", "摇头 · 低循环／魔王", "Shake Head · Low Loop / Demon King", "priestLowLoop"),
        new("priestHighLoop", "priest", "摇头 · 高循环", "Shake Head · High Loop", "priestHighLoop"),
        new("dropout_priest", "priest", "握住落选者的手", "Take Dropout's Hand", "dropout", Kind: "坏结局"),
        new("priest4KilledBad", "priest", "看向僧侣 · 低理智", "Look at the Priest · Low Sanity", "priest4KilledBad", Kind: "回退结局"),
        new("priest4NoReachBad", "priest", "袖手旁观", "Stand By and Watch", "priest4NoReachBad", Kind: "坏结局"),
        new("ritual", "common", "献祭仪式", "Sacrifice Ritual", "", Kind: "提前收束"),
    };

    public string DisplayName(CupLanguage language) => language.Text(Name, EnglishName);
    public string DisplayPartner(CupLanguage language) => language.Text(PartnerName, Partner switch
    {
        "hero" => "Hero", "mage" => "Mage", "warrior" => "Warrior", "priest" => "Priest", _ => "Prologue"
    });

    public string DisplayKind(CupLanguage language) => language.Text(Kind, Kind switch
    {
        "坏结局" => "Bad Ending", "回退结局" => "Rollback Ending", "提前收束" => "Early Finish", _ => "Clear"
    });

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
        ("Opening3", 1510) => "ritual",
        ("Scenario_Mage1", 219 or 271) => "mage-exit",
        _ => null
    };

    public bool KeepsEarlyEnding(string script, int line) => EarlyEndingAt(script, line) == Id;

    public int HeroChoice => Id switch { "hero4Bad" => 2037, "dropout_hero" => 2035, _ => 2034 };
    public int PriestChoice(bool returned) => Id switch
    {
        "dropout_priest" => 1166,
        "priest4KilledBad" => 1164,
        "priestNodAfterLowSan" when !returned => 1164,
        _ => 1165
    };

    public bool KeepChoice(string script, int line, bool returned)
    {
        if (script == "Opening" && line == 120) return false;
        if (script == "Opening3" && line == 1432) return Id == "ritual";
        if (Id == "ritual" && script == "Opening3")
        {
            if (line is >= 77 and <= 81) return line == 80;
            if (line is >= 1389 and <= 1394) return line == 1389;
            if (line == 1434) return false;
        }
        if (script == "Opening3" && line is >= 1986 and <= 1989)
            return line == (Partner switch { "hero" => 1986, "warrior" => 1987, "mage" => 1988, "priest" => 1989, _ => -1 });

        return script switch
        {
            "Scenario_Hero3" when line is 2034 or 2035 or 2037 => line == HeroChoice,
            "Scenario_Mage1" when line is 197 or 198 or 199 or 201 or 202 => (line is 197 or 201) == (Id == "mage-exit"),
            "Scenario_Mage3" when line is 1169 or 1170 => line == (Id is "mageMaryoku" or "mage5YakusokuSinaiBad" ? 1169 : 1170),
            "Scenario_Mage4" when line is 1564 or 1565 => Id == "mageNotMaryokuSpecialWin" ||
                line == (Id == "mageNotMaryokuSuki" ? 1564 : 1565),
            "Scenario_Mage5" when line is 251 or 253 => line == (Id == "mage5YakusokuSinaiBad" ? 253 : 251),
            "Scenario_Warrior1" when line is >= 1540 and <= 1544 => (line == 1541) == (Id == "warriorKill"),
            "Scenario_Warrior2" when line is 3485 or 3486 => line == (Box ? 3485 : 3486),
            "Scenario_Warrior2" when line is 3516 or 3517 => line == (Box ? 3516 : 3517),
            "Scenario_Warrior3" when line is 1436 or 1437 => line == (Id == "dropout_warrior" ? 1437 : 1436),
            "Scenario_Warrior5" when line is 1148 or 1149 =>
                line == ((Id is "warriorAfterGlitch" or "warriorGlitchBad") && !returned ? 1149 : 1148),
            "Scenario_Priest3" when line is >= 1164 and <= 1166 =>
                line == PriestChoice(returned),
            "Scenario_Priest4" when line is 2912 or 2913 => line == (Id == "priest4NoReachBad" ? 2913 : 2912),
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
    public bool FastStory { get; init; } = true;
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
    public bool FixedWeaponCombination { get; init; }
    public bool ExtraActionAfterSteal { get; init; }
    public double StolenWeaponDamageMultiplier { get; init; } = 2;
    public double EnemyHealthMultiplier { get; init; } = 1;
    public double EnemyAttackMultiplier { get; init; } = 1;
    public string DisabledCharacter { get; init; } = "";
    public bool InheritDisabledAction { get; init; }

    public CupSettings ForMatch() => string.IsNullOrEmpty(BossId) ? this : this with { RouteId = "" };
}
