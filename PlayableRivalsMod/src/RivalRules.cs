namespace PlayableRivals;

internal enum Rival { None, Outcast, Villainess, Dropout }
internal enum Card
{
    Sword = -7101, Dagger = -7102, Thread = -7103, CodexWeapon = -7104, Punch = -7105,
    Steal = -7111, Switch = -7112, Enchant = -7113,
    Love = -7121, Dominate = -7122, Doll = -7123,
    Codex = -7131, Barrier = -7132, Armor = -7133,
    Fire = -7141, Freeze = -7142, Thunder = -7143, Poison = -7144
}

internal readonly record struct Stats(int Hp, int Str, int Dex, int Agi, int Mag);
internal readonly record struct Skill(int Floor, int First, int Cooldown, bool Free = false,
    bool Front = false, bool Magic = false);

internal static class RivalRules
{
    internal static bool IsDirectDamageType(string type) => type is "" or "attack" or "rangeAttack" or "killAttack";
    internal static int LimitDirectDamage(int damage, int spent) => Math.Clamp(damage, 0, Math.Max(0, 30 - Math.Max(0, spent)));

    internal static Rival FromPartner(string route) => route.ToLowerInvariant() switch
    {
        "hero" => Rival.Outcast, "warrior" => Rival.Villainess, "mage" => Rival.Dropout, _ => Rival.None
    };

    internal static string Id(Rival role) => role.ToString().ToLowerInvariant();

    internal static string Name(Rival role) => role switch
    {
        Rival.Outcast => "放逐者", Rival.Villainess => "反派千金", Rival.Dropout => "落选者", _ => ""
    };

    internal static Stats GetStats(Rival role, int lv)
    {
        decimal n = Math.Max(1, lv) + 1m;
        static int R(decimal x) => (int)Math.Round(x, MidpointRounding.AwayFromZero);
        return role switch
        {
            Rival.Outcast => new(R(20 + 4.50m*n), R(3 + 1.22m*n), R(1 + 0.80m*n), R(1 + n), R(1 + 0.75m*n)),
            Rival.Villainess => new(R(12 + 2.65m*n), R(1 + 0.70m*n), R(2 + 1.20m*n), R(1 + 0.75m*n), R(2 + 1.10m*n)),
            Rival.Dropout => new(R(13 + 2.60m*n), R(2 + 0.80m*n), R(1 + 0.75m*n), R(2 + 0.95m*n), R(1 + n)),
            _ => throw new ArgumentOutOfRangeException(nameof(role))
        };
    }

    internal static Card FormFor(Rival role, int round, int switched, bool noMagic) => role switch
    {
        Rival.Outcast => switched > 0 && round > switched ? Card.Dagger : Card.Sword,
        Rival.Villainess => Card.Thread,
        Rival.Dropout => noMagic ? Card.Punch : Card.CodexWeapon,
        _ => throw new ArgumentOutOfRangeException(nameof(role))
    };

    internal static int Amount(Card card, int level, Stats stat) => card switch
    {
        Card.Sword => 18 + stat.Str, Card.Dagger => (18 + stat.Str) / 2, Card.Thread => 13 + (stat.Dex + 1) / 2,
        Card.CodexWeapon => 13 + (stat.Mag + 1) / 2,
        Card.Punch => (stat.Str + (int)Math.Ceiling(0.75m * (level + 1m))) / 2,
        Card.Love => 4 + (stat.Mag + 1) / 2, Card.Doll => 8 + stat.Dex,
        Card.Barrier => 12 + stat.Mag, Card.Armor => 6 + stat.Mag, _ => 0
    };

    internal static Skill GetSkill(Card card) => card switch
    {
        Card.Steal => new(1, 2, 3, Front: true),
        Card.Switch => new(2, 1, -1),
        Card.Enchant or Card.Fire or Card.Freeze or Card.Thunder or Card.Poison => new(3, 2, 3, Free: true),
        Card.Love => new(1, 2, 3),
        Card.Dominate => new(2, 2, 4),
        Card.Doll => new(3, 3, -1),
        Card.Codex => new(1, 1, 3, Magic: true),
        Card.Barrier => new(2, 3, -1, Magic: true),
        Card.Armor => new(3, 4, -1),
        _ => default
    };

    internal static bool CanUse(Card card, int floor, int round, int last, bool front, bool noMagic)
    {
        var skill = GetSkill(card);
        return floor >= skill.Floor && round >= skill.First && (!skill.Front || front) &&
            (!skill.Magic || !noMagic) && (last == 0 || skill.Cooldown >= 0 && round > last + skill.Cooldown);
    }

    internal static Card CooldownKey(Card card) =>
        card is Card.Fire or Card.Freeze or Card.Thunder or Card.Poison ? Card.Enchant : card;

    internal static string Element(Card card) => card switch
    {
        Card.Fire => "fire", Card.Freeze => "freeze", Card.Thunder => "paralysis", Card.Poison => "poison", _ => ""
    };

    internal static int ElementStacks(string element, int mag) => element switch
    {
        "fire" => 2 + mag / 10, "freeze" or "paralysis" => 5 + mag / 10, "poison" => 1, _ => 0
    };

    internal static Card[] Skills(Rival role) => role switch
    {
        Rival.Outcast => new[] { Card.Steal, Card.Switch, Card.Enchant },
        Rival.Villainess => new[] { Card.Love, Card.Dominate, Card.Doll },
        Rival.Dropout => new[] { Card.Codex, Card.Barrier, Card.Armor },
        _ => Array.Empty<Card>()
    };
}
