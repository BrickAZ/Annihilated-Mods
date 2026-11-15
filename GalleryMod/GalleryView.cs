using Il2CppInterop.Runtime;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace AnnihilatedGallery;

internal sealed class GalleryView : IDisposable
{
    private const int PageSize = 8;
    internal static readonly Color Ink = new(0.96f, 0.93f, 0.86f);
    internal static readonly Color Muted = new(0.71f, 0.69f, 0.63f);
    internal static readonly Color Gold = new(0.89f, 0.76f, 0.52f);
    private static readonly Color Panel = new(0.079f, 0.086f, 0.10f);
    internal static readonly Color Card = new(0.125f, 0.131f, 0.15f);
    internal static readonly Color Chosen = new(0.30f, 0.24f, 0.17f);
    private static readonly Color Matte = new(0.035f, 0.038f, 0.046f);

    private readonly GalleryEntry[] entries;
    private readonly Func<GalleryEntry[]> storyEntries;
    private readonly ReplayLibrary replayLibrary;
    private readonly Action<string> warn;
    private readonly Font font;
    private readonly RectTransform root;
    private readonly Button entryButton;
    private readonly GameObject overlay;
    private readonly RectTransform browser;
    private readonly RectTransform viewer;
    private readonly RectTransform chapters;
    private readonly (string Name, Button Button)[] routeButtons;
    private readonly Button[] appearanceButtons;
    private readonly Tile[] tiles = new Tile[PageSize];
    private readonly Text groupName;
    private readonly Text empty;
    private readonly Text pageText;
    private readonly Button previousPage;
    private readonly Button nextPage;
    private readonly Image fullImage;
    private readonly Text fullStatus;
    private readonly Text fullName;
    private readonly Text fullCount;
    private readonly Button previous;
    private readonly Button next;
    private readonly Text playLabel;

    private GalleryEntry[] filtered = Array.Empty<GalleryEntry>();
    private string route = "序章";
    private string chapter = "";
    private int page;
    private int index;
    private int appearance;
    private GalleryImage fullAsset;
    private bool canOpen;
    private bool playing;
    private float nextFrameAt;

    public GalleryView(GalleryEntry[] entries, ReplayEntry[] replays, Func<GalleryEntry[]> storyEntries, Action<string> warn)
    {
        this.entries = entries;
        this.storyEntries = storyEntries;
        this.warn = warn;
        font = Font.CreateDynamicFontFromOSFont("Microsoft YaHei", 22);
        root = Rect("Annihilated CG Gallery", null);
        Object.DontDestroyOnLoad(root.gameObject);
        var canvas = root.gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 28000;
        var scaler = root.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        root.gameObject.AddComponent<GraphicRaycaster>();

        entryButton = Button(root, "OpenGallery", "CG 画廊", 0, 0, 136, 42, Open);
        var entryRect = entryButton.transform.Cast<RectTransform>();
        entryRect.anchorMin = entryRect.anchorMax = new Vector2(1, 0);
        entryRect.pivot = new Vector2(1, 0);
        entryRect.anchoredPosition = new Vector2(-24, 24);
        ColorButton(entryButton, Chosen);
        entryButton.gameObject.SetActive(false);

        var scrim = Surface(root, "GalleryOverlay", new Color(0.025f, 0.027f, 0.033f, 0.98f), true);
        overlay = scrim.gameObject;
        Stretch(scrim.rectTransform);
        browser = Board(overlay.transform, "Browser");
        viewer = Board(overlay.transform, "Viewer");

        Label(browser, "Heading", "CG 画廊", 28, 20, 250, 42, 29, Gold);
        Label(browser, "SpoilerNotice", "包含全路线剧透", 28, 63, 180, 26, 15, Muted);
        Button(browser, "CloseGallery", "返回标题", 1040, 24, 126, 38, Close);
        Box(browser, "Sidebar", Card, 20, 105, 186, 496);
        var available = entries.Select(item => item.Route).Distinct().ToArray();
        var routes = new[] { "剧情实景", "序章", "勇者线", "法师线", "祭司线", "战士线", "地牢事件", "共通素材", "结局与标题", "全部原图" };
        routeButtons = routes.Where(name => name is "剧情实景" or "全部原图" || available.Contains(name)).Select((name, i) =>
        {
            var button = Button(browser, "Route-" + name, name, 30, 115 + 46 * i, 166, 39, () => SelectRoute(name));
            return (name, button);
        }).ToArray();
        groupName = Label(browser, "GroupTitle", "", 227, 22, 430, 38, 26, Ink);
        appearanceButtons = new[] { "全部外观", "长发", "短发" }.Select((text, i) =>
            Button(browser, "GalleryAppearance-" + i, text, 700 + 104 * i, 25, 97, 36, () => SelectAppearance(i))).ToArray();
        ColorButton(appearanceButtons[0], Chosen);
        empty = Label(browser, "EmptyGallery", "在原版剧情或剧情回放中看到的 CG 演出会收录到这里。",
            227, 150, 920, 46, 20, Muted);
        chapters = Position(Rect("Chapters", browser), 227, 67, 935, 38);

        for (int i = 0; i < PageSize; i++)
        {
            int slot = i;
            float x = 227 + 236 * (i % 4);
            float y = 121 + 228 * (i / 4);
            var button = Button(browser, "Tile-" + i, "", x, y, 225, 211, () => OpenAt(page * PageSize + slot));
            var frame = Box(button.transform, "Matte", Matte, 7, 7, 211, 153);
            var picture = Surface(frame.transform, "Picture", Color.white);
            Stretch(picture.rectTransform);
            picture.preserveAspect = true;
            picture.enabled = false;
            var status = Label(frame.transform, "Status", "", 8, 55, 195, 42, 15, Muted, TextAnchor.MiddleCenter);
            var caption = Label(button.transform, "Caption", "", 8, 166, 209, 38, 14, Ink);
            caption.resizeTextForBestFit = true;
            caption.resizeTextMinSize = 12;
            caption.resizeTextMaxSize = 14;
            tiles[i] = new Tile(button, picture, status, caption);
        }

        previousPage = Button(browser, "PreviousPage", "上一页", 227, 602, 98, 36, () => ChangePage(-1));
        nextPage = Button(browser, "NextPage", "下一页", 438, 602, 98, 36, () => ChangePage(1));
        pageText = Label(browser, "Page", "", 325, 602, 113, 36, 17, Ink, TextAnchor.MiddleCenter);
        Label(browser, "BrowserKeys", "点击图片放大   ·   Esc 返回", 771, 602, 395, 36, 16, Muted, TextAnchor.MiddleRight);

        Button(viewer, "BackToGrid", "返回缩略图", 24, 20, 138, 42, BackToGrid);
        Button(viewer, "CloseViewer", "返回标题", 1040, 20, 126, 42, Close);
        fullName = Label(viewer, "ImageTitle", "", 178, 16, 845, 32, 21, Ink, TextAnchor.MiddleCenter);
        fullCount = Label(viewer, "ImageCount", "", 178, 46, 845, 24, 15, Muted, TextAnchor.MiddleCenter);
        var fullFrame = Box(viewer, "FullMatte", Matte, 24, 79, 1142, 491);
        fullImage = Surface(fullFrame.transform, "FullImage", Color.white);
        Stretch(fullImage.rectTransform);
        fullImage.preserveAspect = true;
        fullImage.enabled = false;
        fullStatus = Label(fullFrame.transform, "FullStatus", "", 221, 207, 700, 70, 19, Muted, TextAnchor.MiddleCenter);
        previous = Button(viewer, "PreviousImage", "上一张", 320, 586, 115, 42, () => Shift(-1));
        var play = Button(viewer, "PlaySlideshow", "自动播放", 452, 586, 121, 42, TogglePlay);
        playLabel = play.GetComponentInChildren<Text>();
        next = Button(viewer, "NextImage", "下一张", 590, 586, 115, 42, () => Shift(1));
        Label(viewer, "ViewerKeys", "← / → 切图   ·   空格 播放 / 暂停", 746, 588, 420, 39, 16, Muted, TextAnchor.MiddleRight);
        viewer.gameObject.SetActive(false);
        overlay.SetActive(false);
        replayLibrary = new ReplayLibrary(this, root, replays);
    }

    public void Update(bool onTitle)
    {
        replayLibrary.Update(onTitle && !overlay.activeSelf);
        canOpen = onTitle && !replayLibrary.Opened;
        if (!onTitle && overlay.activeSelf)
            Close();
        bool entryVisible = canOpen && !overlay.activeSelf;
        if (entryButton.gameObject.activeSelf != entryVisible)
            entryButton.gameObject.SetActive(entryVisible);
        if (!overlay.activeSelf)
        {
            if (replayLibrary.Opened && Keyboard.current?.escapeKey.wasPressedThisFrame == true)
                replayLibrary.Close();
            return;
        }

        var keyboard = Keyboard.current;
        if (keyboard?.escapeKey.wasPressedThisFrame == true)
        {
            if (viewer.gameObject.activeSelf) BackToGrid();
            else Close();
            return;
        }
        if (!viewer.gameObject.activeSelf)
        {
            foreach (var tile in tiles)
                if (tile.Asset?.Poll() == true)
                    Present(tile.Asset, tile.Picture, tile.Status);
            return;
        }

        if (keyboard?.leftArrowKey.wasPressedThisFrame == true) Shift(-1);
        if (keyboard?.rightArrowKey.wasPressedThisFrame == true) Shift(1);
        if (keyboard?.spaceKey.wasPressedThisFrame == true) TogglePlay();
        if (fullAsset?.Poll() == true)
        {
            Present(fullAsset, fullImage, fullStatus);
            nextFrameAt = Time.unscaledTime + 3;
        }
        if (playing && fullAsset?.Finished == true && Time.unscaledTime >= nextFrameAt)
        {
            if (index + 1 < filtered.Length) OpenAt(index + 1);
            else SetPlaying(false);
        }
    }

    private void Open()
    {
        if (!canOpen || overlay.activeSelf)
            return;
        // Drop focus from the original title buttons before the gallery receives input.
        EventSystem.current?.SetSelectedGameObject(null);
        entryButton.gameObject.SetActive(false);
        overlay.SetActive(true);
        browser.gameObject.SetActive(true);
        SelectRoute(route);
    }

    private void Close()
    {
        if (!overlay.activeSelf)
            return;
        ClearPage();
        ClearFullImage();
        SetPlaying(false);
        viewer.gameObject.SetActive(false);
        overlay.SetActive(false);
        EventSystem.current?.SetSelectedGameObject(null);
    }

    private void SelectRoute(string name)
    {
        route = name;
        chapter = "";
        foreach (var item in routeButtons)
            ColorButton(item.Button, item.Name == route ? Chosen : Card);
        var group = RouteEntries();
        groupName.text = name;
        for (int i = chapters.childCount - 1; i >= 0; i--)
        {
            var old = chapters.GetChild(i).gameObject;
            old.SetActive(false);
            Object.Destroy(old);
        }
        if (name != "全部原图")
        {
            var values = group.Select(item => item.Chapter).Where(value => value.Length > 0).Distinct().ToArray();
            if (values.Length > 1)
            {
                var filters = new[] { "" }.Concat(values).ToArray();
                var controls = new List<(string Filter, Button Button)>();
                float width = Math.Min(122, 935f / filters.Length);
                for (int i = 0; i < filters.Length; i++)
                {
                    string filter = filters[i];
                    var button = Button(chapters, "Chapter-" + filter, filter == "" ? (route == "剧情实景" ? "全部路线" : "全部阶段") : filter,
                        i * width, 0, width - 5, 33, () =>
                        {
                            chapter = filter;
                            foreach (var control in controls)
                                ColorButton(control.Button, control.Filter == chapter ? Chosen : Card);
                            Filter();
                        });
                    controls.Add((filter, button));
                    if (i == 0) ColorButton(button, Chosen);
                }
            }
        }
        Filter();
    }

    private IEnumerable<GalleryEntry> RouteEntries()
        => route switch
        {
            "全部原图" => entries,
            "剧情实景" => storyEntries(),
            _ => entries.Where(item => item.Route == route)
        };

    private void Filter()
    {
        filtered = RouteEntries().Where(item => (chapter == "" || item.Chapter == chapter) &&
            (appearance == 0 || item.Appearance == 0 || item.Appearance == appearance)).ToArray();
        page = 0;
        ShowPage();
    }

    private void SelectAppearance(int value)
    {
        appearance = value;
        for (int i = 0; i < appearanceButtons.Length; i++)
            ColorButton(appearanceButtons[i], i == value ? Chosen : Card);
        Filter();
    }

    private void ChangePage(int direction)
    {
        int target = page + direction;
        if (target < 0 || target * PageSize >= filtered.Length)
            return;
        page = target;
        ShowPage();
    }

    private void ShowPage()
    {
        ClearPage();
        for (int i = 0; i < tiles.Length; i++)
        {
            var tile = tiles[i];
            int entryIndex = page * PageSize + i;
            bool visible = entryIndex < filtered.Length;
            tile.Button.gameObject.SetActive(visible);
            if (!visible)
                continue;
            var entry = filtered[entryIndex];
            tile.Caption.text = entry.Title;
            tile.Status.text = "读取中…";
            tile.Asset = new GalleryImage(entry, warn);
            if (tile.Asset.Finished) Present(tile.Asset, tile.Picture, tile.Status);
        }
        int pageCount = Math.Max(1, (filtered.Length + PageSize - 1) / PageSize);
        pageText.text = $"{page + 1} / {pageCount}";
        previousPage.interactable = page > 0;
        nextPage.interactable = page + 1 < pageCount;
        groupName.text = $"{route}  ·  {filtered.Length} 张";
        empty.text = route == "剧情实景" && appearance == 0
            ? "在原版剧情或剧情回放中看到的 CG 演出会收录到这里。"
            : "当前筛选没有图片。";
        empty.gameObject.SetActive(filtered.Length == 0);
    }

    private void OpenAt(int itemIndex)
    {
        if (!overlay.activeSelf || itemIndex < 0 || itemIndex >= filtered.Length)
            return;
        if (browser.gameObject.activeSelf)
        {
            browser.gameObject.SetActive(false);
            ClearPage();
        }
        viewer.gameObject.SetActive(true);
        ClearFullImage();
        index = itemIndex;
        var entry = filtered[index];
        fullName.text = entry.Title;
        fullCount.text = entry.CapturedPath == null
            ? $"{entry.Route}  ·  {entry.Chapter}  ·  {index + 1} / {filtered.Length}"
            : $"{entry.Route}  ·  剧情实景  ·  {index + 1} / {filtered.Length}";
        fullStatus.text = "读取中…";
        previous.interactable = index > 0;
        next.interactable = index + 1 < filtered.Length;
        fullAsset = new GalleryImage(entry, warn);
        if (fullAsset.Finished) Present(fullAsset, fullImage, fullStatus);
        nextFrameAt = Time.unscaledTime + 3;
    }

    private void BackToGrid()
    {
        SetPlaying(false);
        ClearFullImage();
        viewer.gameObject.SetActive(false);
        browser.gameObject.SetActive(true);
        page = index / PageSize;
        ShowPage();
    }

    private void Shift(int direction) => OpenAt(index + direction);

    private void TogglePlay()
    {
        if (playing)
        {
            SetPlaying(false);
            return;
        }
        if (filtered.Length < 2)
            return;
        SetPlaying(true);
        if (index + 1 == filtered.Length)
            OpenAt(0);
    }

    private void SetPlaying(bool enable)
    {
        playing = enable;
        playLabel.text = playing ? "暂停播放" : "自动播放";
        nextFrameAt = Time.unscaledTime + 3;
    }

    private void ClearPage()
    {
        foreach (var tile in tiles)
        {
            ClearImage(tile.Picture, ref tile.Asset);
        }
    }

    private void ClearFullImage() => ClearImage(fullImage, ref fullAsset);

    private static void ClearImage(Image picture, ref GalleryImage asset)
    {
        picture.enabled = false;
        picture.sprite = null;
        asset?.Dispose();
        asset = null;
    }

    private static void Present(GalleryImage asset, Image picture, Text status)
    {
        picture.sprite = asset.Sprite;
        picture.enabled = asset.Sprite != null;
        status.text = picture.enabled ? "" : "图片读取失败";
    }

    public void Dispose()
    {
        if (root == null)
            return;
        Close();
        replayLibrary.Dispose();
        Object.Destroy(root.gameObject);
        Object.Destroy(font);
    }

    internal static RectTransform Board(Transform parent, string name)
    {
        var board = Surface(parent, name, Panel).rectTransform;
        board.anchorMin = board.anchorMax = board.pivot = new Vector2(0.5f, 0.5f);
        board.anchoredPosition = Vector2.zero;
        board.sizeDelta = new Vector2(1190, 660);
        Box(board, "TopBorder", Gold, 0, 0, 1190, 2);
        return board;
    }

    internal Button Button(Transform parent, string name, string caption, float x, float y, float width, float height, Action click)
    {
        var surface = Box(parent, name, Card, x, y, width, height, true);
        var button = surface.gameObject.AddComponent<Button>();
        button.targetGraphic = surface;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        button.onClick.AddListener((UnityAction)click);
        ColorButton(button, Card);
        var text = Label(button.transform, "Text", caption, 0, 0, width, height, 17, Ink, TextAnchor.MiddleCenter);
        Stretch(text.rectTransform);
        return button;
    }

    internal static void ColorButton(Button button, Color baseColor)
    {
        button.targetGraphic.color = Color.white;
        button.colors = new ColorBlock
        {
            normalColor = baseColor,
            highlightedColor = Color.Lerp(baseColor, Gold, 0.25f),
            pressedColor = Color.Lerp(baseColor, Gold, 0.38f),
            selectedColor = baseColor,
            disabledColor = Color.Lerp(baseColor, Matte, 0.65f),
            colorMultiplier = 1,
            fadeDuration = 0.08f
        };
    }

    internal Text Label(Transform parent, string name, string caption, float x, float y, float width, float height,
        int size, Color color, TextAnchor align = TextAnchor.MiddleLeft)
    {
        var rect = Position(Rect(name, parent), x, y, width, height);
        var label = rect.gameObject.AddComponent<Text>();
        label.font = font;
        label.text = caption;
        label.fontSize = size;
        label.color = color;
        label.alignment = align;
        label.supportRichText = false;
        label.raycastTarget = false;
        return label;
    }

    internal static Image Box(Transform parent, string name, Color color, float x, float y, float width, float height, bool raycast = false)
    {
        var image = Surface(parent, name, color, raycast);
        Position(image.rectTransform, x, y, width, height);
        return image;
    }

    internal static Image Surface(Transform parent, string name, Color color, bool raycast = false)
    {
        var image = Rect(name, parent).gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = raycast;
        return image;
    }

    private static RectTransform Rect(string name, Transform parent)
    {
        var node = new GameObject(name, Il2CppType.Of<RectTransform>()).transform.Cast<RectTransform>();
        if (parent != null)
            node.SetParent(parent, false);
        return node;
    }

    private static RectTransform Position(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(x, -y);
        rect.sizeDelta = new Vector2(width, height);
        return rect;
    }

    internal static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private sealed class Tile
    {
        public readonly Button Button;
        public readonly Image Picture;
        public readonly Text Status;
        public readonly Text Caption;
        public GalleryImage Asset;

        public Tile(Button button, Image picture, Text status, Text caption)
        {
            Button = button;
            Picture = picture;
            Status = status;
            Caption = caption;
        }
    }
}
