using System.Buffers.Binary;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using MelonLoader;
using UnityEngine;

namespace AnnihilatedTextureOverride;

internal sealed class PngOverride(string path, string name, MelonLogger.Instance log)
{
    private readonly Dictionary<(string, Rect, Vector2, float, Vector4), Sprite> sprites = new();
    private Texture2D? texture;

    public bool Failed { get; private set; }

    public void Reject(string detail)
    {
        if (Failed)
            return;
        Failed = true;
        log.Warning($"Using the original art for {Path.GetFileName(Path.GetDirectoryName(path))}/{name}.png. {detail}");
    }

    public Texture2D GetTexture(Texture2D original)
    {
        if (texture != null)
        {
            CheckDimensions(texture.width, texture.height, original);
            return texture;
        }

        byte[] png = File.ReadAllBytes(path);
        ReadOnlySpan<byte> signature = stackalloc byte[] { 137, 80, 78, 71, 13, 10, 26, 10 };
        if (png.Length < 24 || !png.AsSpan(0, 8).SequenceEqual(signature) ||
            !png.AsSpan(12, 4).SequenceEqual("IHDR"u8))
            throw new InvalidDataException("Expected a PNG image.");

        int width = BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(16, 4));
        int height = BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(20, 4));
        CheckDimensions(width, height, original);
        var loaded = new Texture2D(2, 2, TextureFormat.RGBA32, original.mipmapCount > 1);
        try
        {
            if (!ImageConversion.LoadImage(loaded, png, true))
                throw new InvalidDataException("Unity could not decode the image.");
            loaded.name = $"PNG/{name}";
            loaded.filterMode = original.filterMode;
            loaded.wrapModeU = original.wrapModeU;
            loaded.wrapModeV = original.wrapModeV;
            loaded.anisoLevel = original.anisoLevel;
            loaded.hideFlags = HideFlags.DontUnloadUnusedAsset;
            texture = loaded;
            log.Msg($"Loaded {Path.GetFileName(Path.GetDirectoryName(path))}/{name}.png ({width} x {height}).");
            return loaded;
        }
        catch
        {
            UnityEngine.Object.Destroy(loaded);
            throw;
        }
    }

    public Sprite GetSprite(Sprite original)
    {
        var rect = original.rect;
        var pivot = original.pivot;
        var border = original.border;
        float pixelsPerUnit = original.pixelsPerUnit;
        var key = (original.name, rect, pivot, pixelsPerUnit, border);
        if (sprites.TryGetValue(key, out var saved) && saved != null)
            return saved;

        var picture = GetTexture(original.texture);
        var replacement = Sprite.Create(picture, rect, new Vector2(pivot.x / rect.width, pivot.y / rect.height),
            pixelsPerUnit, 0, SpriteMeshType.FullRect, border, false);
        if (replacement == null)
            throw new InvalidDataException("Unity could not reproduce the original sprite rectangle.");
        try
        {
            CopyPhysicsShapes(original, replacement);
            replacement.name = original.name;
            replacement.hideFlags = HideFlags.DontUnloadUnusedAsset;
            sprites[key] = replacement;
            return replacement;
        }
        catch
        {
            UnityEngine.Object.Destroy(replacement);
            throw;
        }
    }

    private static void CheckDimensions(int width, int height, Texture2D original)
    {
        if (width != original.width || height != original.height)
            throw new InvalidDataException($"PNG is {width} x {height}, expected {original.width} x {original.height}.");
    }

    private static void CopyPhysicsShapes(Sprite original, Sprite replacement)
    {
        int count = original.GetPhysicsShapeCount();
        if (count == 0)
            return;
        var paths = new Il2CppSystem.Collections.Generic.List<Il2CppStructArray<Vector2>>();
        var points = new Il2CppSystem.Collections.Generic.List<Vector2>();
        for (int i = 0; i < count; i++)
        {
            points.Clear();
            original.GetPhysicsShape(i, points);
            var path = new Il2CppStructArray<Vector2>(points.Count);
            for (int point = 0; point < points.Count; point++)
                path[point] = points[point];
            paths.Add(path);
        }
        replacement.OverridePhysicsShape(paths.Cast<Il2CppSystem.Collections.Generic.IList<Il2CppStructArray<Vector2>>>());
    }
}
