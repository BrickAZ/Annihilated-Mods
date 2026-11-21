using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using MelonLoader;
using MelonLoader.Utils;
using UnityEngine;
using EngineObject = UnityEngine.Object;
using LoadOperation = UnityEngine.ResourceManagement.ResourceProviders.BundledAssetProvider.InternalOp;

[assembly: MelonInfo(typeof(AnnihilatedTextureOverride.TextureOverrideMod), "Annihilated Texture Override", "0.1.0", "Brick")]
[assembly: MelonGame("DeathKumiai", "Annihilated")]

namespace AnnihilatedTextureOverride;

public sealed class TextureOverrideMod : MelonMod
{
    private static readonly Dictionary<string, PngOverride> Overrides = new(StringComparer.OrdinalIgnoreCase);
    private static MelonLogger.Instance? log;

    public override void OnInitializeMelon()
    {
        log = LoggerInstance;
        try
        {
            string root = Path.Combine(MelonEnvironment.UserDataDirectory, "TextureOverrides");
            using var stream = typeof(TextureOverrideMod).Assembly.GetManifestResourceStream("ItemIcons.txt")!;
            using var reader = new StreamReader(stream);
            var itemIcons = reader.ReadToEnd().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            Register(root, "Enemy", null);
            Register(root, "Item", itemIcons);
            if (Overrides.Count != 0)
            {
                var method = AccessTools.DeclaredMethod(typeof(LoadOperation), "ActionComplete", new[] { typeof(AsyncOperation) })
                    ?? throw new MissingMethodException(typeof(LoadOperation).FullName, "ActionComplete");
                HarmonyInstance.Patch(method, prefix: new HarmonyMethod(typeof(TextureOverrideMod), nameof(CompleteWithPng)));
            }
            LoggerInstance.Msg($"Registered {Overrides.Count} PNG override(s) in {root}. Restart after changing images.");
        }
        catch (Exception ex)
        {
            Overrides.Clear();
            LoggerInstance.Error($"PNG overrides unavailable: {ex.Message}");
        }
    }

    private static void Register(string root, string group, HashSet<string>? allowed)
    {
        string directory = Path.Combine(root, group);
        Directory.CreateDirectory(directory);
        foreach (string path in Directory.EnumerateFiles(directory, "*.png"))
        {
            string name = Path.GetFileNameWithoutExtension(path);
            if (allowed != null && !allowed.Contains(name))
            {
                log?.Warning($"Ignored Item/{name}.png: this name is absent from the supported item icon list.");
                continue;
            }
            string key = $"{group}/{name}";
            Overrides.Add(key, new PngOverride(path, name, log!));
        }
    }

    private static bool CompleteWithPng(LoadOperation __instance)
    {
        if (Overrides.Count == 0)
            return true;

        PngOverride? entry = null;
        try
        {
            var handle = __instance.m_ProvideHandle;
            var location = handle.Location;
            if (location == null || !TryFind(location.InternalId, location.PrimaryKey, out entry) || entry.Failed)
                return true;
            var request = __instance.m_RequestOperation;
            if (request == null || !request.isDone)
                return true;

            var type = handle.Type;
            if (type.FullName is "UnityEngine.Sprite" or "UnityEngine.Texture2D")
            {
                var original = string.IsNullOrEmpty(__instance.subObjectName)
                    ? request.asset
                    : FindSubObject(request, __instance.subObjectName, type);
                if (original == null || !type.IsInstanceOfType(original))
                    return true;
                var replacement = Replace(original, entry);
                if (replacement == null)
                    return true;
                __instance.m_Result = replacement;
            }
            else if (type.IsArray && type.GetElementType().FullName == "UnityEngine.Sprite")
            {
                var assets = ReplaceSprites(request, entry);
                if (assets == null)
                    return true;
                __instance.GetArrayResult(assets);
            }
            else if (type.IsGenericType && type.GetGenericTypeDefinition().FullName == "System.Collections.Generic.IList`1" &&
                     type.GetGenericArguments()[0].FullName == "UnityEngine.Sprite")
            {
                var assets = ReplaceSprites(request, entry);
                if (assets == null)
                    return true;
                __instance.GetListResult(assets);
            }
            else
                return true;
        }
        catch (Exception ex)
        {
            entry?.Reject(ex.Message);
            return true;
        }

        // Complete once, through the original Unity operation, with the same requested result type.
        __instance.CompleteOperation();
        return false;
    }

    private static bool TryFind(string internalId, string primaryKey, out PngOverride entry)
    {
        const string textures = "Assets/Textures/";
        // The shipped catalog contains the original asset path and can expose aliases as the primary key.
        if (internalId.StartsWith(textures, StringComparison.OrdinalIgnoreCase) &&
            internalId.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            return Overrides.TryGetValue(internalId[textures.Length..^4], out entry!);

        int subObject = primaryKey.IndexOf('[');
        string key = subObject >= 0 ? primaryKey[..subObject] : primaryKey;
        return Overrides.TryGetValue(key, out entry!);
    }

    private static EngineObject? FindSubObject(AssetBundleRequest request, string name, Il2CppSystem.Type type)
    {
        foreach (var asset in request.allAssets)
            if (asset != null && asset.name == name && type.IsInstanceOfType(asset))
                return asset;
        return null;
    }

    private static EngineObject? Replace(EngineObject? original, PngOverride entry)
    {
        if (original == null)
            return null;
        var sprite = original.TryCast<Sprite>();
        if (sprite != null)
            return entry.GetSprite(sprite);
        var texture = original.TryCast<Texture2D>();
        return texture != null ? entry.GetTexture(texture) : null;
    }

    private static Il2CppReferenceArray<EngineObject>? ReplaceSprites(AssetBundleRequest request, PngOverride entry)
    {
        var assets = request.allAssets;
        var result = new Il2CppReferenceArray<EngineObject>(assets.Length);
        bool changed = false;
        for (int i = 0; i < assets.Length; i++)
        {
            var original = assets[i];
            var sprite = original?.TryCast<Sprite>();
            result[i] = sprite != null ? entry.GetSprite(sprite) : original!;
            changed |= sprite != null;
        }
        return changed ? result : null;
    }
}
