using System.Diagnostics;
using Il2CppNaninovel;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AnnihilatedGallery;

internal static class AppContextPath
{
    public static string GameRoot => Path.GetDirectoryName(Application.dataPath);
}

internal sealed class ReplayLibrary : IDisposable
{
    private const int PageSize = 7;
    private readonly GalleryView controls;
    private readonly ReplayEntry[] entries;
    private readonly Button entryButton;
    private readonly GameObject overlay;
    private readonly RectTransform board;
    private readonly Text title;
    private readonly Text status;
    private readonly Text pageText;
    private readonly Button previous;
    private readonly Button next;
    private readonly Button close;
    private readonly Button scenes;
    private readonly Button endings;
    private readonly Button longHair;
    private readonly Button shortHair;
    private readonly Button[] rows = new Button[PageSize];
    private readonly (string Route, Button Button)[] routes;
    private ReplayEntry[] filtered = Array.Empty<ReplayEntry>();
    private string route = "全部";
    private bool ending;
    private int appearance = 2;
    private int page;
    private bool canOpen;
    private Process process;

    public bool Opened => overlay.activeSelf;

    public ReplayLibrary(GalleryView controls, RectTransform root, ReplayEntry[] entries)
    {
        this.controls = controls;
        this.entries = entries;
        entryButton = controls.Button(root, "OpenStoryReplay", "剧情回放", 0, 0, 136, 42, Open);
        var entryRect = entryButton.transform.Cast<RectTransform>();
        entryRect.anchorMin = entryRect.anchorMax = new Vector2(1, 0);
        entryRect.pivot = new Vector2(1, 0);
        entryRect.anchoredPosition = new Vector2(-174, 24);
        GalleryView.ColorButton(entryButton, GalleryView.Chosen);
        entryButton.gameObject.SetActive(false);

        var scrim = GalleryView.Surface(root, "ReplayOverlay", new Color(0.025f, 0.027f, 0.033f, 0.98f), true);
        overlay = scrim.gameObject;
        GalleryView.Stretch(scrim.rectTransform);
        board = GalleryView.Board(overlay.transform, "ReplayLibrary");
        controls.Label(board, "ReplayHeading", "剧情回放", 28, 22, 185, 42, 29, GalleryView.Gold);
        controls.Label(board, "ReplaySpoiler", "包含全路线剧透", 28, 65, 185, 25, 15, GalleryView.Muted);
        close = controls.Button(board, "CloseReplayLibrary", "返回标题", 1040, 24, 126, 38, Close);
        GalleryView.Box(board, "ReplaySidebar", GalleryView.Card, 20, 105, 186, 389);

        routes = new[] { "全部", "序章", "勇者线", "法师线", "祭司线", "战士线" }.Select((name, i) =>
        {
            var button = controls.Button(board, "ReplayRoute-" + name, name, 30, 115 + 58 * i, 166, 46, () =>
            {
                route = name;
                page = 0;
                Refresh();
            });
            return (name, button);
        }).ToArray();
        scenes = controls.Button(board, "ReplayScenes", "剧情场景", 229, 25, 134, 39, () => SelectKind(false));
        endings = controls.Button(board, "ReplayEndings", "结局回放", 371, 25, 134, 39, () => SelectKind(true));
        longHair = controls.Button(board, "ReplayLongHair", "长发主角", 632, 25, 120, 39, () => SelectAppearance(1));
        shortHair = controls.Button(board, "ReplayShortHair", "短发主角", 760, 25, 120, 39, () => SelectAppearance(2));
        title = controls.Label(board, "ReplayGroup", "", 230, 69, 918, 31, 19, GalleryView.Muted);

        for (int i = 0; i < rows.Length; i++)
        {
            int slot = i;
            rows[i] = controls.Button(board, "ReplayClip-" + i, "", 229, 114 + 60 * i, 937, 50, () => Start(slot));
            var label = rows[i].GetComponentInChildren<Text>();
            label.alignment = TextAnchor.MiddleLeft;
            label.rectTransform.offsetMin = new Vector2(18, 0);
        }

        previous = controls.Button(board, "PreviousReplayPage", "上一页", 229, 550, 100, 40, () => ChangePage(-1));
        pageText = controls.Label(board, "ReplayPage", "", 331, 549, 96, 40, 17, GalleryView.Ink, TextAnchor.MiddleCenter);
        next = controls.Button(board, "NextReplayPage", "下一页", 430, 550, 100, 40, () => ChangePage(1));
        status = controls.Label(board, "ReplayStatus", "", 229, 601, 937, 36, 16, GalleryView.Muted);
        overlay.SetActive(false);
    }

    public void Update(bool onTitle)
    {
        canOpen = onTitle;
        if (process != null && process.HasExited)
        {
            int exit = process.ExitCode;
            process.Dispose();
            process = null;
            status.text = exit == 0 ? "" : $"回放进程结束（代码 {exit}）。详情见 UserData/AnnihilatedGallery/Replay.log。";
            Refresh();
        }
        if (!onTitle && Opened && process == null)
            Close();
        bool visible = onTitle && !Opened;
        if (entryButton.gameObject.activeSelf != visible)
            entryButton.gameObject.SetActive(visible);
    }

    public void Close()
    {
        if (process != null)
            return;
        overlay.SetActive(false);
        EventSystem.current?.SetSelectedGameObject(null);
    }

    private void Open()
    {
        if (!canOpen)
            return;
        overlay.SetActive(true);
        entryButton.gameObject.SetActive(false);
        EventSystem.current?.SetSelectedGameObject(null);
        Refresh();
    }

    private void SelectKind(bool showEndings)
    {
        ending = showEndings;
        page = 0;
        Refresh();
    }

    private void SelectAppearance(int value)
    {
        appearance = value;
        Refresh();
    }

    private void ChangePage(int offset)
    {
        if (page + offset < 0 || (page + offset) * PageSize >= filtered.Length)
            return;
        page += offset;
        Refresh();
    }

    private void Refresh()
    {
        filtered = entries.Where(item => item.Ending == ending && (route == "全部" || item.Route == route)).ToArray();
        bool busy = process != null;
        foreach (var control in routes)
        {
            GalleryView.ColorButton(control.Button, control.Route == route ? GalleryView.Chosen : GalleryView.Card);
            control.Button.interactable = !busy;
        }
        GalleryView.ColorButton(scenes, !ending ? GalleryView.Chosen : GalleryView.Card);
        GalleryView.ColorButton(endings, ending ? GalleryView.Chosen : GalleryView.Card);
        GalleryView.ColorButton(longHair, appearance == 1 ? GalleryView.Chosen : GalleryView.Card);
        GalleryView.ColorButton(shortHair, appearance == 2 ? GalleryView.Chosen : GalleryView.Card);
        longHair.interactable = shortHair.interactable = !busy;
        scenes.interactable = endings.interactable = close.interactable = !busy;
        title.text = filtered.Length == 0 ? $"{route}  ·  暂无条目" : $"{route}  ·  {filtered.Length} 段";
        int pageCount = Math.Max(1, (filtered.Length + PageSize - 1) / PageSize);
        previous.interactable = !busy && page > 0;
        next.interactable = !busy && page + 1 < pageCount;
        pageText.text = $"{page + 1} / {pageCount}";
        for (int i = 0; i < rows.Length; i++)
        {
            var row = rows[i];
            int index = page * PageSize + i;
            row.gameObject.SetActive(index < filtered.Length);
            row.interactable = !busy;
            if (index < filtered.Length)
                row.GetComponentInChildren<Text>().text = $"{filtered[index].Route}    {filtered[index].Title}";
        }
    }

    private void Start(int slot)
    {
        int index = page * PageSize + slot;
        if (process != null || index >= filtered.Length)
            return;
        try
        {
            var entry = filtered[index];
            string root = AppContextPath.GameRoot;
            string logs = Path.Combine(root, "UserData", "AnnihilatedGallery");
            Directory.CreateDirectory(logs);
            var launch = new ProcessStartInfo
            {
                FileName = Path.Combine(root, "Annihilated.exe"),
                WorkingDirectory = root,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Normal
            };
            foreach (string argument in new[] { $"--gallery-worker={entry.Id}", $"--gallery-session={Guid.NewGuid():N}",
                         $"--gallery-locale={Engine.GetService<ILocalizationManager>().SelectedLocale}",
                         $"--gallery-appearance={appearance}",
                         "--melonloader.hideconsole", "--melonloader.agfoffline", "-screen-fullscreen", "0",
                         "-screen-width", "1280", "-screen-height", "720", "-logFile", Path.Combine(logs, "Replay.log") })
                launch.ArgumentList.Add(argument);

            string runtime = Path.Combine(root, "MelonLoader", "Dependencies", "dotnet");
            string host = Path.Combine(runtime, "host", "fxr");
            if (Directory.Exists(host))
            {
                string hostFxr = Directory.EnumerateDirectories(host).OrderByDescending(path => Version.TryParse(Path.GetFileName(path), out var version) ? version : null)
                    .Select(path => Path.Combine(path, "hostfxr.dll")).FirstOrDefault(File.Exists);
                if (hostFxr != null)
                {
                    launch.ArgumentList.Add("--melonloader.hostfxr");
                    launch.ArgumentList.Add(hostFxr);
                    launch.Environment["DOTNET_ROOT"] = runtime;
                    launch.Environment["DOTNET_ROLL_FORWARD"] = "Major";
                }
            }
            process = Process.Start(launch) ?? throw new InvalidOperationException("无法启动回放进程。");
            status.text = "正在打开独立回放窗口。播完或按 Esc 关闭后会回到这里。";
            Refresh();
        }
        catch (Exception ex)
        {
            status.text = $"无法打开回放。{ex.Message}";
        }
    }

    public void Dispose() => process?.Dispose();
}
