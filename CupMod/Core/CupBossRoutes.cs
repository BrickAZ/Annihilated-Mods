namespace CupCompetition;

internal static class CupBossRoutes
{
    // The three alliances leave on floor 3 without fighting Dropout.
    private static bool EntersDungeon(Route route) => route.Id is not ("ritual" or "mage-exit");
    private static bool FightsDropout(Route route) =>
        EntersDungeon(route) && route.EndingType != "dropout";

    internal static IReadOnlyList<Route> For(BossGoal boss) => Route.All.Where(route => boss.Id switch
    {
        "outcast" or "dealer" or "villainess" or "seaDragon" => EntersDungeon(route),
        "dropout" or "giant" or "slime" => FightsDropout(route),
        "priest" => route.Id is "hero" or "mageMaryoku" or "mage5YakusokuSinaiBad"
            or "mageNotMaryokuSuki" or "mageNotMaryokuIssyo" or "mageNotMaryokuSpecialWin"
            or "priestLowLoop" or "priestHighLoop",
        "warriorFinalDuel" or "warriorLv1Duel" => route.Id is "warrior" or "warrior_hakoniwa"
            or "warriorKill" or "warriorAfterGlitch",
        "demonLord" => route.Id == "priestLowLoop",
        _ => false
    }).ToArray();
}
