using PlayableRivals;

static void Check(bool ok, string description)
{
    if (!ok) throw new Exception(description);
}

Check(RivalRules.GetStats(Rival.Outcast, 1) == new Stats(29, 5, 3, 3, 3), "Lv1 Outcast");
Check(RivalRules.GetStats(Rival.Villainess, 21) == new Stats(70, 16, 28, 18, 26), "Lv21 Villainess");
Check(RivalRules.GetStats(Rival.Dropout, 21) == new Stats(70, 20, 18, 23, 23), "Lv21 Dropout");
Check(RivalRules.GetStats(Rival.Dropout, 1).Dex == 3, "Midpoints round away from zero");
Check(RivalRules.FormFor(Rival.Outcast, 2, 1, false) == Card.Dagger, "Dagger switches next round");
Check(RivalRules.FormFor(Rival.Outcast, 1, 1, false) == Card.Sword, "Switch consumes the current turn");
Check(RivalRules.FormFor(Rival.Dropout, 5, 0, true) == Card.Punch, "Armor switches weapon");
var expectedCurve = new[]
{
    (Lv:1, Sword:23, Dagger:11, Thread:15, Block:15, Punch:3),
    (Lv:5, Sword:28, Dagger:14, Thread:18, Block:17, Punch:6),
    (Lv:10, Sword:34, Dagger:17, Thread:21, Block:19, Punch:10),
    (Lv:15, Sword:41, Dagger:20, Thread:24, Block:22, Punch:13),
    (Lv:21, Sword:48, Dagger:24, Thread:27, Block:25, Punch:18)
};
foreach (var row in expectedCurve)
{
    Check(RivalRules.Amount(Card.Sword, row.Lv, RivalRules.GetStats(Rival.Outcast, row.Lv)) == row.Sword, $"Lv{row.Lv} sword");
    Check(RivalRules.Amount(Card.Dagger, row.Lv, RivalRules.GetStats(Rival.Outcast, row.Lv)) == row.Dagger, $"Lv{row.Lv} dagger");
    Check(RivalRules.Amount(Card.Thread, row.Lv, RivalRules.GetStats(Rival.Villainess, row.Lv)) == row.Thread, $"Lv{row.Lv} thread");
    Check(RivalRules.Amount(Card.CodexWeapon, row.Lv, RivalRules.GetStats(Rival.Dropout, row.Lv)) == row.Block, $"Lv{row.Lv} block");
    Check(RivalRules.Amount(Card.Punch, row.Lv, RivalRules.GetStats(Rival.Dropout, row.Lv)) == row.Punch, $"Lv{row.Lv} physical");
}
Check(!RivalRules.CanUse(Card.Steal, 1, 1, 0, true, false), "Steal waits for round 2");
Check(!RivalRules.CanUse(Card.Steal, 1, 2, 0, false, false), "Steal requires front");
Check(RivalRules.CanUse(Card.Steal, 1, 2, 0, true, false), "Steal ready");
Check(!RivalRules.CanUse(Card.Steal, 1, 5, 2, true, false), "Full cooldown rounds");
Check(RivalRules.CanUse(Card.Steal, 1, 6, 2, true, false), "Ready again on round 6");
Check(!RivalRules.CanUse(Card.Switch, 1, 1, 0, true, false), "Switch needs floor 2");
Check(!RivalRules.CanUse(Card.Switch, 2, 3, 1, true, false), "One switch per battle");
Check(RivalRules.CanUse(Card.Dominate, 2, 2, 0, true, false), "Domination ready round 2");
Check(!RivalRules.CanUse(Card.Barrier, 3, 4, 0, true, true), "No magic after Armor");
Check(RivalRules.GetSkill(Card.Enchant).Free, "Enchant is free");
Check(RivalRules.ElementStacks("freeze", 18) == 6 && RivalRules.ElementStacks("poison", 18) == 1, "Element stack amounts");
Console.WriteLine("Rival rules checks passed");
Check(RivalRules.LimitDirectDamage(48, 0) == 30, "Single-hit cap");
Check(RivalRules.LimitDirectDamage(20, 20) == 10, "Multi-hit shared budget");
Check(RivalRules.LimitDirectDamage(23, 30) == 0, "Budget exhausted");
Check(RivalRules.LimitDirectDamage(23, 0) == 23, "Below-cap unchanged");
foreach (var dot in new[] { "fire", "poison", "pain", "spike", "spiritBomb", "bomb", "hpCost" })
    Check(!RivalRules.IsDirectDamageType(dot), "Indirect excluded: " + dot);
foreach (var direct in new[] { "", "attack", "rangeAttack", "killAttack" })
    Check(RivalRules.IsDirectDamageType(direct), "Direct included: " + direct);
Console.WriteLine("Outcast cap checks passed");
