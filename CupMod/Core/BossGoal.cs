namespace CupCompetition;

internal sealed record BossGoal(string Id, string Name, int Floor)
{
    public static readonly BossGoal[] All =
    {
        new("outcast", "第 1 层 · 放逐者", 1),
        new("dealer", "第 2 层 · 荷官人偶", 2),
        new("villainess", "第 2 层 · 恶役千金", 2),
        new("seaDragon", "第 3 层 · 海龙", 3),
        new("dropout", "第 3 层 · 落选者", 3),
        new("giant", "第 4 层 · 巨人", 4),
        new("slime", "第 4 层 · 史莱姆", 4),
        new("priest", "第 5 层 · 僧侣", 5),
        new("warriorFinalDuel", "第 5 层 · 战士第一场决斗", 5),
        new("warriorLv1Duel", "第 5 层 · 战士第二场决斗", 5),
        new("demonLord", "额外区域 · 魔王", 6)
    };

    public static bool IsVictoryMarker(string script, int line) => (script, line) is
        ("Battle", 3509 or 4407 or 13761) or
        ("Scenario_Hero5", 1578) or ("Scenario_Priest5", 300);

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
            ("Scenario_Hero5", 1578) when heroFinisher && dungeon == 5 => ForBattle("priest", dungeon),
            ("Scenario_Priest5", 300) when dungeon == 7 => ForBattle("priest", dungeon),
            ("Battle", 4407) when battle == "priest" && dungeon == 8 => ForBattle(battle, dungeon),
            ("Battle", 13761) when battle == "warriorFinalDuel" => ForBattle(battle, dungeon),
            ("Battle", 3509) when battle != "priest" || dungeon != 5 => ForBattle(battle, dungeon),
            _ => null
        };
    }
}
