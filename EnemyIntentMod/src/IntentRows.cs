namespace EnemyIntent;

internal readonly record struct AttackKey(int Action, int Target);
internal readonly record struct NativeIntentRow(string? Text, AttackKey? Attack);
internal readonly record struct IntentDisplay(string Text, int ActionCount, IReadOnlyList<int> Warnings);

internal static class IntentRows
{
    private const string Break = "<BR>";

    public static IntentDisplay Reformat(string text, IReadOnlyList<NativeIntentRow> captured, int[] warnings)
    {
        var original = new IntentDisplay(text, captured.Count, warnings);
        if (string.IsNullOrWhiteSpace(text) || captured.Count == 0 ||
            !captured.Any(row => row.Attack.HasValue))
            return original;

        var rows = text.Split(Break, StringSplitOptions.None);
        if (rows.Length < captured.Count)
            return original;

        // Transform only rows we can match to the actual call that drew them. Later rows
        // are native summaries for additional effects and stay outside the action count.
        for (int i = 0; i < captured.Count; i++)
        {
            if (captured[i].Text is not { } rendered ||
                !string.Equals(rows[i].TrimEnd(), rendered.TrimEnd(), StringComparison.Ordinal))
                return original;
        }

        var output = new List<string>(rows.Length);
        var remappedWarnings = new List<int>(warnings.Length);
        for (int i = 0; i < captured.Count;)
        {
            var attack = captured[i].Attack;
            bool warning = Array.IndexOf(warnings, i) >= 0;
            int end = i + 1;
            string row = rows[i];
            if (attack.HasValue && !string.IsNullOrWhiteSpace(row))
            {
                while (end < captured.Count && captured[end].Attack == attack &&
                    string.Equals(row.TrimEnd(), rows[end].TrimEnd(), StringComparison.Ordinal) &&
                    (Array.IndexOf(warnings, end) >= 0) == warning)
                    end++;

                row = FormattableString.Invariant($"{row.TrimEnd()} ×{end - i}");
            }

            if (warning)
                remappedWarnings.Add(output.Count);
            output.Add(row);
            i = end;
        }

        int actionCount = output.Count;
        output.AddRange(rows.Skip(captured.Count));
        return new IntentDisplay(string.Join(Break, output), actionCount, remappedWarnings);
    }
}
