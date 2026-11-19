using System.Text.Json;

namespace CupCompetition;

internal readonly record struct CupLanguage(bool English)
{
    public static CupLanguage ForLocale(string? locale)
    {
        locale = locale?.Trim() ?? "";
        return new(!(locale.Equals("zh", StringComparison.OrdinalIgnoreCase) ||
            locale.StartsWith("zh-", StringComparison.OrdinalIgnoreCase) ||
            locale.StartsWith("zh_", StringComparison.OrdinalIgnoreCase)));
    }

    public string Text(string chinese, string english) => English ? english : chinese;

    public const string TrainingTitle = "The training, the punishing missions nearly killed me.";

    // Naninovel stores each settings service as a JSON string in parallel key/value arrays.
    public static byte[]? CopyLocaleSetting(ReadOnlyMemory<byte> settings)
    {
        using var document = JsonDocument.Parse(settings);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("objectJsonMap", out var map) ||
            map.ValueKind != JsonValueKind.Object ||
            !map.TryGetProperty("keys", out var keys) || keys.ValueKind != JsonValueKind.Array ||
            !map.TryGetProperty("values", out var values) || values.ValueKind != JsonValueKind.Array ||
            keys.GetArrayLength() != values.GetArrayLength()) return null;

        for (int i = 0; i < keys.GetArrayLength(); i++)
        {
            if (keys[i].ValueKind != JsonValueKind.String ||
                keys[i].GetString()?.StartsWith("Naninovel.LocalizationManager+Settings,", StringComparison.Ordinal) != true ||
                values[i].ValueKind != JsonValueKind.String) continue;
            using var entry = JsonDocument.Parse(values[i].GetString()!);
            if (entry.RootElement.ValueKind != JsonValueKind.Object ||
                !entry.RootElement.TryGetProperty("SelectedLocale", out var selected) ||
                selected.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(selected.GetString())) return null;
            return JsonSerializer.SerializeToUtf8Bytes(new
            {
                objectJsonMap = new
                {
                    keys = new[] { keys[i].GetString() },
                    values = new[] { JsonSerializer.Serialize(new { SelectedLocale = selected.GetString() }) }
                }
            });
        }
        return null;
    }
}
