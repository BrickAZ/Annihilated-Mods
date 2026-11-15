using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace AnnihilatedGallery;

internal sealed class GalleryImage : IDisposable
{
    private AsyncOperationHandle<Sprite> load;
    private readonly Action<string> warn;
    private readonly string address;
    private Texture2D captured;
    public Sprite Sprite { get; private set; }
    public bool Finished { get; private set; }

    public GalleryImage(GalleryEntry entry, Action<string> warn)
    {
        address = entry.Address ?? entry.Title;
        this.warn = warn;
        try
        {
            if (entry.CapturedPath != null)
            {
                Finished = true;
                captured = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!ImageConversion.LoadImage(captured, File.ReadAllBytes(entry.CapturedPath), true))
                    throw new InvalidDataException("无法解码剧情画面。");
                Sprite = Sprite.Create(captured, new Rect(0, 0, captured.width, captured.height), new Vector2(0.5f, 0.5f));
                return;
            }
            load = Addressables.LoadAssetAsync<Sprite>((Il2CppSystem.String)address);
        }
        catch (Exception ex)
        {
            Finished = true;
            warn($"无法读取 CG {address}。{ex.Message}");
        }
    }

    public bool Poll()
    {
        if (Finished || load == null || !load.IsDone)
            return false;
        Finished = true;
        if (load.Status == AsyncOperationStatus.Succeeded)
            Sprite = load.Result;
        if (Sprite == null)
            warn($"无法读取 CG {address}。");
        return true;
    }

    public void Dispose()
    {
        if (captured != null)
        {
            if (Sprite != null)
                UnityEngine.Object.Destroy(Sprite);
            UnityEngine.Object.Destroy(captured);
            captured = null;
        }
        Sprite = null;
        Finished = true;
        if (load != null && load.IsValid())
            load.Release();
        load = null;
    }
}
