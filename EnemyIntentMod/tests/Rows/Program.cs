using EnemyIntent;

var front = new AttackKey(1, 0);
var back = new AttackKey(1, 1);
var ranged = new AttackKey(12, 0);

Expect("identical consecutive hits", "a 6<BR>a 6<BR>block 4<BR>extra effect",
    new AttackKey?[] { front, front, null }, Array.Empty<int>(),
    "a 6 ×2<BR>block 4<BR>extra effect", 2);
Expect("retargeted hits", "a 6<BR>a 6",
    new AttackKey?[] { front, back }, Array.Empty<int>(),
    "a 6 ×1<BR>a 6 ×1", 2);
Expect("different attack kind", "a 6<BR>a 6",
    new AttackKey?[] { front, ranged }, Array.Empty<int>(),
    "a 6 ×1<BR>a 6 ×1", 2);
Expect("different damage", "a 6<BR>a 7",
    new AttackKey?[] { front, front }, Array.Empty<int>(),
    "a 6 ×1<BR>a 7 ×1", 2);
Expect("effects retain order", "a 6<BR>poison 3<BR>a 6",
    new AttackKey?[] { front, null, front }, Array.Empty<int>(),
    "a 6 ×1<BR>poison 3<BR>a 6 ×1", 3);
Expect("warning index moves with its action", "a 6<BR>a 6<BR>kill 12<BR>kill 12",
    new AttackKey?[] { front, front, front, front }, new[] { 2, 3 },
    "a 6 ×2<BR>kill 12 ×2", 2, 1);
Expect("different warning", "a 6<BR>a 6",
    new AttackKey?[] { front, front }, new[] { 1 },
    "a 6 ×1<BR>a 6 ×1", 2, 1);
Expect("empty intent stays hidden", "", new AttackKey?[] { front }, Array.Empty<int>(), "", 1);
Expect("unmatched native format", "a 6", new AttackKey?[] { front, front }, Array.Empty<int>(), "a 6", 2);
Expect("pure effects retain original text", "heal 5<BR>summon",
    new AttackKey?[] { null, null }, Array.Empty<int>(), "heal 5<BR>summon", 2);
Console.WriteLine("All 10 intent row checks passed.");

static void Expect(string name, string text, AttackKey?[] attacks, int[] warnings,
    string expectedText, int expectedCount, params int[] expectedWarnings)
{
    var rows = text.Split("<BR>");
    var captured = attacks.Select((attack, i) =>
        new NativeIntentRow(i < rows.Length ? rows[i] : "", attack)).ToArray();
    var result = IntentRows.Reformat(text, captured, warnings);
    if (result.Text != expectedText || result.ActionCount != expectedCount ||
        !result.Warnings.SequenceEqual(expectedWarnings))
        throw new InvalidOperationException($"{name}: {result.Text}, rows={result.ActionCount}, warnings={string.Join(",", result.Warnings)}");
}
