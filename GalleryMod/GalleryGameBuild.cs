using System.Text.Json;

namespace AnnihilatedGallery;

internal sealed class GalleryGameBuild
{
    private static readonly Lazy<GalleryGameBuild> Detected = new(Detect);

    public static GalleryGameBuild Current => Detected.Value;

    public int BackgroundReturnLine { get; }
    public Coordinates Lines { get; }

    private GalleryGameBuild(int backgroundReturnLine, string coordinates = null)
    {
        BackgroundReturnLine = backgroundReturnLine;
        if (coordinates != null)
        {
            using var stream = typeof(GalleryGameBuild).Assembly.GetManifestResourceStream(coordinates)
                ?? throw new InvalidDataException("缺少当前版本的剧情坐标索引。");
            Lines = JsonSerializer.Deserialize<Coordinates>(stream);
        }
    }

    private static GalleryGameBuild Detect()
    {
        string catalog = Path.Combine(AppContextPath.GameRoot, "Annihilated_Data", "StreamingAssets", "aa", "catalog.hash");
        if (!File.Exists(catalog))
            return null;
        return File.ReadAllText(catalog).Trim() switch
        {
            "b9254bd24d0daef581f2186fa1da77c5" => new GalleryGameBuild(530),
            "c867cf04b05171a82710f970f4122af7"
                => new GalleryGameBuild(549, "AnnihilatedGallery.Coordinates.26705080"),
            "004a514c06f1f4a7563e5f75c24d55e4"
                => new GalleryGameBuild(549, "AnnihilatedGallery.Coordinates.26845264"),
            _ => null
        };
    }

    public sealed class Coordinates
    {
        public Dictionary<string, int[]> Replays { get; set; }
        public Dictionary<string, Dictionary<int, int>> StoryCg { get; set; }
        public string[] RemovedGalleryAddresses { get; set; } = Array.Empty<string>();
        public GalleryEntry[] AddedGalleryEntries { get; set; } = Array.Empty<GalleryEntry>();
        public StoryCgArchive.Mark[] AddedStoryCgMarks { get; set; } = Array.Empty<StoryCgArchive.Mark>();
    }
}
