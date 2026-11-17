using System.Text.Json;

namespace AnnihilatedGallery;

internal sealed class ReplayEntry
{
    public string Id { get; set; }
    public string Route { get; set; }
    public string Title { get; set; }
    public string Script { get; set; }
    public int Start { get; set; }
    public int End { get; set; }
    public string Partner { get; set; }
    public int DungeonId { get; set; }
    public string InheritedBackdrop { get; set; }
    public string InheritedMusic { get; set; }
    public bool Ending { get; set; }
    public Dictionary<string, JsonElement> Variables { get; set; } = new();
}

internal static class ReplayCatalog
{
    public static ReplayEntry[] Read()
    {
        using var stream = typeof(ReplayCatalog).Assembly.GetManifestResourceStream("AnnihilatedGallery.Replays")
            ?? throw new InvalidDataException("缺少剧情回放索引。");
        var entries = JsonSerializer.Deserialize<ReplayEntry[]>(stream);
        var lines = GalleryGameBuild.Current?.Lines?.Replays;
        if (lines != null)
        {
            foreach (var entry in entries)
            {
                if (lines.TryGetValue(entry.Id, out var bounds))
                    (entry.Start, entry.End) = (bounds[0], bounds[1]);
            }
        }
        return entries;
    }
}
