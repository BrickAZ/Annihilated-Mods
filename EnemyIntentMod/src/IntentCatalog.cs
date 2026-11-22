namespace EnemyIntent;

internal readonly record struct IntentMove(string Id, string Label);

internal static class IntentCatalog
{
    public static (string Title, string Description) Format(
        IEnumerable<IntentMove> candidates, IntentMove current, string? locale)
    {
        var (title, marker) = locale switch
        {
            { } code when code.StartsWith("zh", StringComparison.OrdinalIgnoreCase) => ("全部意图", "当前"),
            { } code when code.StartsWith("ja", StringComparison.OrdinalIgnoreCase) => ("すべての行動意図", "現在"),
            _ => ("All intents", "Current"),
        };

        // Display the live plan first, even when a scripted or copied move bypassed the base table.
        var moves = candidates.Prepend(current).DistinctBy(move => move.Id, StringComparer.Ordinal).ToArray();
        var sharedLabels = moves.GroupBy(move => move.Label, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToHashSet(StringComparer.Ordinal);

        return (title, string.Join("\n", moves.Select((move, index) =>
        {
            string label = sharedLabels.Contains(move.Label) ? $"{move.Label} ({move.Id})" : move.Label;
            return index == 0 ? $"<color=#F1CF87>▶ {marker}　{label}</color>" : $"　{label}";
        })));
    }
}
