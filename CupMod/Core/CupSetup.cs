using System.Globalization;

namespace CupCompetition;

internal sealed class CupSetup
{
    public static IReadOnlyList<Route> Partners { get; } = Route.All.DistinctBy(route => route.Partner).ToArray();
    public bool BossMode { get; set; }
    public BossGoal? Boss { get; private set; }
    public Route Route { get; private set; }
    public IReadOnlyList<Route> Endings { get; private set; }
    public string EventId { get; set; }
    public string SeedText { get; set; }
    public string LimitText { get; set; }
    public string RoomRetriesText { get; set; }
    public string RoomRetryPenaltyText { get; set; }
    public string DeathsText { get; set; }
    public string DeathPenaltyText { get; set; }
    public string StartingCurseText { get; set; }
    public string StartingAllocationText { get; set; }
    public bool OpeningDeath { get; set; }
    public bool RunKnife { get; set; }
    public string StartingFloorText { get; set; }
    public string MaxWeaponsText { get; set; }
    public bool KickOnly { get; set; }
    public bool HarshTraining { get; set; }
    public int LatestStartingFloor => BossMode ? Boss?.Floor ?? 1 : Route.LatestStartingFloor;
    public string BattleMode { get; set; }

    public CupSetup(CupSettings settings)
    {
        Route = Route.All.FirstOrDefault(route => route.Id == settings.RouteId) ?? Route.All[0];
        Endings = Route.All.Where(route => route.Partner == Route.Partner).ToArray();
        BossMode = !string.IsNullOrEmpty(settings.BossId);
        SelectBoss(BossMode ? settings.BossId : BossGoal.All[0].Id);
        EventId = settings.EventId ?? "";
        SeedText = settings.Seed.ToString(CultureInfo.InvariantCulture);
        LimitText = settings.LimitMinutes.ToString(CultureInfo.InvariantCulture);
        RoomRetriesText = settings.MaxRoomRetries.ToString(CultureInfo.InvariantCulture);
        RoomRetryPenaltyText = settings.RoomRetryPenaltySeconds.ToString(CultureInfo.InvariantCulture);
        DeathsText = settings.MaxDeaths.ToString(CultureInfo.InvariantCulture);
        DeathPenaltyText = settings.DeathPenaltySeconds.ToString(CultureInfo.InvariantCulture);
        StartingCurseText = settings.StartingCurse.ToString(CultureInfo.InvariantCulture);
        StartingAllocationText = settings.StartingAllocationPoints.ToString(CultureInfo.InvariantCulture);
        OpeningDeath = settings.OpeningDeath && settings.StartingAllocationPoints == 0;
        RunKnife = settings.RunKnife;
        StartingFloorText = settings.StartingFloor.ToString(CultureInfo.InvariantCulture);
        MaxWeaponsText = settings.MaxWeapons?.ToString(CultureInfo.InvariantCulture) ?? "";
        KickOnly = settings.KickOnly;
        HarshTraining = settings.HarshTraining;
        BattleMode = settings.BattleMode == "timing" ? "timing" : "probability";
    }

    public void SelectPartner(string partner)
    {
        if (Route.Partner == partner) return;
        var endings = Route.All.Where(route => route.Partner == partner).ToArray();
        if (endings.Length == 0) return;
        Endings = endings;
        Route = endings[0];
    }

    public void SelectEnding(string id)
    {
        Route = Endings.FirstOrDefault(route => route.Id == id) ?? Route;
    }

    public void SelectBoss(string id) => Boss = BossGoal.All.FirstOrDefault(goal => goal.Id == id);

    public bool TryGetSettings(out CupSettings? settings, out string error)
    {
        settings = null;
        error = "";
        string eventId = EventId.Trim();
        if (eventId.Length is < 1 or > 64 || eventId.Any(char.IsControl))
            error = "请输入 1 至 64 个字符的杯赛编号。";
        else if (!int.TryParse(SeedText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int seed))
            error = "种子须为 -2147483648 至 2147483647 的整数。";
        else if (!int.TryParse(string.IsNullOrWhiteSpace(LimitText) ? "0" : LimitText, NumberStyles.Integer,
                     CultureInfo.InvariantCulture, out int limit) || limit is < 0 or > 1440)
            error = "限时须为 0 至 1440 分钟，0 表示不限时。";
        else if (BossMode && Boss == null)
            error = "请选择目标 Boss。";
        else if (BattleMode is not ("timing" or "probability"))
            error = "请选择战斗方式。";
        else if (!TryReadNonnegative(RoomRetriesText, out int roomRetries) || !TryReadNonnegative(DeathsText, out int deaths))
            error = "SL 次数和死亡上限须为非负整数。";
        else if (!TryReadNonnegative(RoomRetryPenaltyText, out int retryPenalty) || !TryReadNonnegative(DeathPenaltyText, out int deathPenalty))
            error = "SL 和死亡的罚秒须为非负整数。";
        else if (!TryReadNonnegative(StartingCurseText, out int startingCurse) ||
                 !TryReadNonnegative(StartingAllocationText, out int allocationPoints))
            error = "开局诅咒和属性点须为非负整数。";
        else if (!BossMode && Route.Id == "ritual" && (startingCurse > 0 || allocationPoints > 0 || OpeningDeath))
            error = "献祭仪式在进入迷宫前结束。请将开局诅咒和属性点设为 0，并关闭开局死亡。";
        else if (!TryReadNonnegative(StartingFloorText, out int startingFloor) || startingFloor < 1 || startingFloor > LatestStartingFloor)
            error = LatestStartingFloor == 1 ? "当前目标须从第 1 层开始。" : $"当前目标可从第 1 至 {LatestStartingFloor} 层开始。";
        else if (!TryReadNonnegative(string.IsNullOrWhiteSpace(MaxWeaponsText) ? "0" : MaxWeaponsText, out int maxWeapons))
            error = "武器上限填非负整数，留空表示不限。";
        else
            settings = new CupSettings
            {
                EventId = eventId, RouteId = BossMode ? "" : Route.Id, BossId = BossMode ? Boss!.Id : "",
                Seed = seed, LimitMinutes = limit, BattleMode = BattleMode,
                MaxRoomRetries = roomRetries, RoomRetryPenaltySeconds = retryPenalty,
                MaxDeaths = deaths, DeathPenaltySeconds = deathPenalty,
                StartingCurse = startingCurse, StartingAllocationPoints = allocationPoints,
                OpeningDeath = OpeningDeath || allocationPoints > 0, RunKnife = RunKnife,
                StartingFloor = startingFloor, MaxWeapons = string.IsNullOrWhiteSpace(MaxWeaponsText) ? null : maxWeapons,
                KickOnly = KickOnly, HarshTraining = HarshTraining
            };
        return settings != null;
    }

    private static bool TryReadNonnegative(string value, out int number) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out number) && number >= 0;
}
