namespace CupCompetition;

internal sealed record BossGoal(string Id, string Name, string EnglishName, int Floor)
{
    public static readonly BossGoal[] All =
    {
        new("outcast", "第 1 层 · 放逐者", "Outcast", 1),
        new("dealer", "第 2 层 · 荷官人偶", "Dealer Pigeon", 2),
        new("villainess", "第 2 层 · 恶役千金", "Villainess", 2),
        new("seaDragon", "第 3 层 · 海龙", "Tide Reaver", 3),
        new("dropout", "第 3 层 · 落选者", "Dropout", 3),
        new("giant", "第 4 层 · 巨人", "Illusion Crane", 4),
        new("slime", "第 4 层 · 史莱姆", "Slime", 4),
        new("priest", "第 5 层 · 僧侣", "Priest", 5),
        new("warriorFinalDuel", "第 5 层 · 战士第一场决斗", "Warrior (First Duel)", 5),
        new("warriorLv1Duel", "第 5 层 · 战士第二场决斗", "Warrior (Second Duel)", 5),
        new("demonLord", "额外区域 · 魔王", "Demon King Graus", 6)
    };

    public string DisplayName(CupLanguage language) => language.Text(Name,
        Floor == 6 ? $"Extra Area · {EnglishName}" : $"F{Floor} · {EnglishName}");

    public static bool IsVictoryMarker(string script, int line) => (script, line) is
        ("Battle", 3565 or 4465 or 13849) or
        ("Scenario_Hero5", 1587) or ("Scenario_Priest5", 301);

    public static string BattleId(string target, string enemies) => target.Length > 0 ? target : enemies switch
    {
        "outcast" => "outcast",
        "seaDragon,seaDragonPart" => "seaDragon",
        _ => ""
    };

    public static BossGoal? ForBattle(string battle, int dungeon) => All.FirstOrDefault(goal =>
        goal.Id == battle && dungeon > 0 &&
        (battle switch
        {
            "priest" => dungeon is 5 or 7 or 8 or 9,
            "warriorFinalDuel" or "warriorLv1Duel" => dungeon == 6,
            "demonLord" => dungeon == 10,
            _ => dungeon == goal.Floor
        }));

    public static BossGoal? WinAt(string script, int line, string battle, int dungeon, bool heroFinisher = false)
    {
        return (script, line) switch
        {
            // The hero finisher reports a generic win signal even when it misses.
            ("Scenario_Hero5", 1587) when heroFinisher && dungeon == 5 => ForBattle("priest", dungeon),
            ("Scenario_Priest5", 301) when dungeon == 7 => ForBattle("priest", dungeon),
            ("Battle", 4465) when battle == "priest" && dungeon == 8 => ForBattle(battle, dungeon),
            ("Battle", 13849) when battle == "warriorFinalDuel" => ForBattle(battle, dungeon),
            ("Battle", 3565) when battle != "priest" || dungeon != 5 => ForBattle(battle, dungeon),
            _ => null
        };
    }
}
