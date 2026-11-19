using System.Globalization;

namespace CupCompetition;

internal sealed class CupSetup
{
    public static IReadOnlyList<Route> Partners { get; } = Route.All.DistinctBy(route => route.Partner).ToArray();
    public bool BossMode { get; set; }
    public bool FastStory { get; set; }
    public BossGoal? Boss { get; private set; }
    public Route? BossRoute { get; private set; }
    public IReadOnlyList<Route> BossRoutes { get; private set; } = Array.Empty<Route>();
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
    public bool FixedWeaponCombination { get; set; }
    public bool ExtraActionAfterSteal { get; set; }
    public string StolenWeaponDamageMultiplierText { get; set; }
    public string EnemyHealthMultiplierText { get; set; }
    public string EnemyAttackMultiplierText { get; set; }
    public string DisabledCharacter { get; set; }
    public bool InheritDisabledAction { get; set; }
    public int LatestStartingFloor => BossMode
        ? Math.Min(Boss?.Floor ?? 1, BossRoute?.LatestStartingFloor ?? int.MaxValue)
        : Route.LatestStartingFloor;
    public string BattleMode { get; set; }

    public CupSetup(CupSettings settings)
    {
        Route = Route.All.FirstOrDefault(route => route.Id == settings.RouteId) ?? Route.All[0];
        Endings = Route.All.Where(route => route.Partner == Route.Partner).ToArray();
        BossMode = !string.IsNullOrEmpty(settings.BossId);
        FastStory = settings.FastStory;
        SelectBoss(BossMode ? settings.BossId : BossGoal.All[0].Id);
        if (BossMode) SelectBossRoute(settings.RouteId);
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
        FixedWeaponCombination = settings.FixedWeaponCombination;
        ExtraActionAfterSteal = settings.ExtraActionAfterSteal;
        StolenWeaponDamageMultiplierText = settings.StolenWeaponDamageMultiplier.ToString(CultureInfo.InvariantCulture);
        EnemyHealthMultiplierText = settings.EnemyHealthMultiplier.ToString(CultureInfo.InvariantCulture);
        EnemyAttackMultiplierText = settings.EnemyAttackMultiplier.ToString(CultureInfo.InvariantCulture);
        DisabledCharacter = settings.DisabledCharacter ?? "";
        InheritDisabledAction = settings.InheritDisabledAction;
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

    public void SelectBoss(string id)
    {
        Boss = BossGoal.All.FirstOrDefault(goal => goal.Id == id);
        BossRoutes = Boss is { } goal ? CupBossRoutes.For(goal) : Array.Empty<Route>();
        if (BossRoute is { } old && !BossRoutes.Contains(old)) BossRoute = null;
    }

    public void SelectBossRoute(string id) => BossRoute = BossRoutes.FirstOrDefault(route => route.Id == id);

    public bool TryGetSettings(out CupSettings? settings, out string error, CupLanguage language = default)
    {
        settings = null;
        error = "";
        string eventId = EventId.Trim();
        if (eventId.Length is < 1 or > 64 || eventId.Any(char.IsControl))
            error = language.Text("请输入 1 至 64 个字符的杯赛编号。", "Enter a Cup ID of 1 to 64 printable characters.");
        else if (!int.TryParse(SeedText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int seed))
            error = language.Text("种子须为 -2147483648 至 2147483647 的整数。", "Enter a seed from -2147483648 to 2147483647.");
        else if (!int.TryParse(string.IsNullOrWhiteSpace(LimitText) ? "0" : LimitText, NumberStyles.Integer,
                     CultureInfo.InvariantCulture, out int limit) || limit is < 0 or > 1440)
            error = language.Text("限时须为 0 至 1440 分钟，0 表示不限时。", "Enter 0 to 1440 minutes for the time limit. 0 means unlimited.");
        else if (BossMode && Boss == null)
            error = language.Text("请选择目标 Boss。", "Select a target boss.");
        else if (BossMode && BossRoute == null)
            error = language.Text("请选择能到达目标 Boss 的剧情分支。", "Select a story branch that reaches the target boss.");
        else if (BattleMode is not ("timing" or "probability"))
            error = language.Text("请选择战斗方式。", "Select a battle mode.");
        else if (!TryReadNonnegative(RoomRetriesText, out int roomRetries) || !TryReadNonnegative(DeathsText, out int deaths))
            error = language.Text("SL 次数和死亡上限须为非负整数。", "Retry and death limits must be nonnegative integers.");
        else if (!TryReadNonnegative(RoomRetryPenaltyText, out int retryPenalty) || !TryReadNonnegative(DeathPenaltyText, out int deathPenalty))
            error = language.Text("SL 和死亡的罚秒须为非负整数。", "Retry and death penalties must be nonnegative whole seconds.");
        else if (!TryReadNonnegative(StartingCurseText, out int startingCurse) ||
                 !TryReadNonnegative(StartingAllocationText, out int allocationPoints))
            error = language.Text("开局诅咒和属性点须为非负整数。", "Starting Curse and stat points must be nonnegative integers.");
        else if (!BossMode && Route.Id == "ritual" && (startingCurse > 0 || allocationPoints > 0 || OpeningDeath))
            error = language.Text("献祭仪式在进入迷宫前结束。请将开局诅咒和属性点设为 0，并关闭开局死亡。", "Sacrifice Ritual ends before the dungeon. Set starting Curse and stat points to 0, and turn off opening death.");
        else if (!TryReadNonnegative(StartingFloorText, out int startingFloor) || startingFloor < 1 || startingFloor > LatestStartingFloor)
            error = LatestStartingFloor == 1 ? language.Text("当前目标须从第 1 层开始。", "This goal must start on Floor 1.") : language.Text($"当前目标可从第 1 至 {LatestStartingFloor} 层开始。", $"This goal can start on Floors 1–{LatestStartingFloor}.");
        else if (!TryReadNonnegative(string.IsNullOrWhiteSpace(MaxWeaponsText) ? "0" : MaxWeaponsText, out int maxWeapons))
            error = language.Text("武器上限填非负整数，留空表示不限。", "Enter a nonnegative weapon limit, or leave it blank for unlimited.");
        else if (!TryReadMultiplier(StolenWeaponDamageMultiplierText, out double stealDamage) ||
                 !TryReadMultiplier(EnemyHealthMultiplierText, out double health) ||
                 !TryReadMultiplier(EnemyAttackMultiplierText, out double attack))
            error = language.Text("娱乐规则中的倍率须为大于 0 的数字。小数请使用点号。", "Enter positive rule multipliers. Use a decimal point for fractions.");
        else if (DisabledCharacter is not ("" or "player" or "hero" or "mage" or "warrior" or "priest"))
            error = language.Text("请选择有效的禁用角色。", "Select a valid character to ban.");
        else
            settings = new CupSettings
            {
                EventId = eventId, RouteId = BossMode ? BossRoute!.Id : Route.Id, BossId = BossMode ? Boss!.Id : "",
                FastStory = FastStory, Seed = seed, LimitMinutes = limit, BattleMode = BattleMode,
                MaxRoomRetries = roomRetries, RoomRetryPenaltySeconds = retryPenalty,
                MaxDeaths = deaths, DeathPenaltySeconds = deathPenalty,
                StartingCurse = startingCurse, StartingAllocationPoints = allocationPoints,
                OpeningDeath = OpeningDeath || allocationPoints > 0, RunKnife = RunKnife,
                StartingFloor = startingFloor, MaxWeapons = string.IsNullOrWhiteSpace(MaxWeaponsText) ? null : maxWeapons,
                KickOnly = KickOnly, HarshTraining = HarshTraining,
                FixedWeaponCombination = FixedWeaponCombination, ExtraActionAfterSteal = ExtraActionAfterSteal,
                StolenWeaponDamageMultiplier = stealDamage, EnemyHealthMultiplier = health, EnemyAttackMultiplier = attack,
                DisabledCharacter = DisabledCharacter, InheritDisabledAction = InheritDisabledAction
            };
        return settings != null;
    }

    private static bool TryReadNonnegative(string value, out int number) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out number) && number >= 0;

    private static bool TryReadMultiplier(string value, out double multiplier) =>
        double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out multiplier) &&
        double.IsFinite(multiplier) && multiplier > 0;
}
