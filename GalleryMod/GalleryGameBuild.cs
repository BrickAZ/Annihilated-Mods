using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;

namespace AnnihilatedGallery;

internal sealed class GalleryGameBuild
{
    private static readonly Lazy<GalleryGameBuild> Detected = new(Detect);

    public static GalleryGameBuild Current => Detected.Value;

    public nint StateManagerConstructor { get; }
    public int BackgroundReturnLine { get; }
    public Coordinates Lines { get; }

    private GalleryGameBuild(nint moduleBase, int constructorRva, int backgroundReturnLine, string coordinates = null)
    {
        StateManagerConstructor = moduleBase + constructorRva;
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
        using var process = Process.GetCurrentProcess();
        var module = process.Modules.Cast<ProcessModule>()
            .Single(m => m.ModuleName.Equals("GameAssembly.dll", StringComparison.OrdinalIgnoreCase));
        using var binary = File.OpenRead(module.FileName);
        using var sha256 = SHA256.Create();
        return Convert.ToHexString(sha256.ComputeHash(binary)) switch
        {
            "38F5ADA37FEC235248A61971C06E4ACCF4C95A74B5C74FCCBE32412AB04C26F1"
                => new GalleryGameBuild(module.BaseAddress, 0xAF96C0, 530),
            "13B13F97BBE50154B451753351656F99595AE0A542586097F27536F55C24F3C4"
                => new GalleryGameBuild(module.BaseAddress, 0xB17770, 549, "AnnihilatedGallery.Coordinates.26705080"),
            _ => null
        };
    }

    public sealed class Coordinates
    {
        public Dictionary<string, int[]> Replays { get; set; }
        public Dictionary<string, Dictionary<int, int>> StoryCg { get; set; }
    }
}
