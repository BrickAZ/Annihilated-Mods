using System.Text.Json;

namespace AnnihilatedGallery;

internal sealed class GalleryEntry
{
    public string Route { get; set; }
    public string Chapter { get; set; }
    public string Title { get; set; }
    public int Appearance { get; set; }
    public string Address { get; set; }
    public string CapturedPath { get; set; }
}

internal static class GalleryCatalog
{
    public static GalleryEntry[] Read()
    {
        using var data = typeof(GalleryCatalog).Assembly.GetManifestResourceStream("AnnihilatedGallery.Catalog")
            ?? throw new InvalidDataException("缺少 CG 索引。");
        return JsonSerializer.Deserialize<GalleryEntry[]>(data) ?? Array.Empty<GalleryEntry>();
    }
}
