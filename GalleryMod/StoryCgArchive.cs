using System.Text.Json;
using System.Text.RegularExpressions;
using Il2CppNaninovel;
using UnityEngine;

namespace AnnihilatedGallery;

internal sealed class StoryCgArchive : IDisposable
{
    private sealed class Mark
    {
        public string Script { get; set; }
        public int Line { get; set; }
        public string Route { get; set; }
        public string Title { get; set; }
        public string Asset { get; set; }
        public string Actor { get; set; }
        public string Key => $"{Script}-{Line}";
        public int PlaybackLine { get; set; }
    }

    private readonly string directory = Path.Combine(AppContextPath.GameRoot, "UserData", "AnnihilatedGallery", "StoryCG");
    private readonly string contextDirectory;
    private readonly Dictionary<(string Script, int Line), Mark> marks;
    private readonly Dictionary<string, Mark> filenames;
    private readonly ReplayEntry[] scenes = ReplayCatalog.Read();
    private readonly CustomScriptPlayer player;
    private readonly Il2CppSystem.Action<Command> listener;
    private readonly Action<string> warn;
    private bool enabled;
    private Mark pending;
    private float after;
    private bool warned;

    public StoryCgArchive(Action<string> warn)
    {
        using var stream = typeof(StoryCgArchive).Assembly.GetManifestResourceStream("AnnihilatedGallery.StoryCgMarks");
        var entries = JsonSerializer.Deserialize<Mark[]>(stream);
        contextDirectory = Path.Combine(directory, "SceneContext");
        marks = new Dictionary<(string Script, int Line), Mark>();
        filenames = new Dictionary<string, Mark>(StringComparer.Ordinal);
        var game = GalleryGameBuild.Current;
        var lines = game?.Lines?.StoryCg;
        foreach (var mark in entries)
        {
            filenames.Add(mark.Key, mark);
            mark.PlaybackLine = lines != null && lines.TryGetValue(mark.Script, out var script) &&
                script.TryGetValue(mark.Line, out int line) ? line : mark.Line;
            if (game != null)
                marks.Add((mark.Script, mark.PlaybackLine), mark);
        }
        this.warn = warn;
        player = Engine.GetService<IScriptPlayer>().Cast<CustomScriptPlayer>();
        listener = (Il2CppSystem.Action<Command>)OnCommand;
        player.add_OnCommandExecutionFinish(listener);
    }

    private void OnCommand(Command command)
    {
        if (!enabled)
            return;
        var spot = command.PlaybackSpot;
        if (marks.TryGetValue((spot.ScriptName, spot.LineIndex), out var mark))
        {
            pending = mark;
            after = Time.unscaledTime + 0.65f;
        }
    }

    public void Update(bool record)
    {
        enabled = record;
        if (!record || pending == null || Time.unscaledTime < after)
            return;
        if (player.PlaybackSpot.ScriptName != pending.Script)
        {
            pending = null;
            return;
        }
        if (!player.WaitingForInput && player.Playing && Time.unscaledTime < after + 2)
            return;

        var mark = pending;
        pending = null;
        var vars = Engine.GetService<ICustomVariableManager>();
        string variant = vars.GetVariableValue("playerLooksType") == "1" ? "1" : "2";
        string filename = $"{mark.Key}-{variant}.png";
        // Older captures don't record whether they came from normal play or a replay with missing scenery.
        string targetDirectory = scenes.Any(scene => scene.InheritedBackdrop != null && scene.Script == mark.Script &&
            mark.PlaybackLine >= scene.Start && mark.PlaybackLine < scene.End) ? contextDirectory : directory;
        string path = Path.Combine(targetDirectory, filename);
        if (File.Exists(path))
            return;
        Texture2D frame = null;
        try
        {
            var backgrounds = Engine.GetService<IBackgroundManager>().TryCast<IActorManager>();
            if (backgrounds == null || !backgrounds.ActorExists(mark.Actor))
                return;
            var actor = backgrounds.GetActor(mark.Actor);
            string expected = Regex.Replace(mark.Asset, @"\{(\w+)\}", match => vars.GetVariableValue(match.Groups[1].Value) ?? "");
            string appearance = actor?.Appearance ?? "";
            if (actor?.Visible != true || !string.Equals(appearance[(appearance.LastIndexOf('/') + 1)..], expected,
                    StringComparison.OrdinalIgnoreCase))
                return;
            frame = CaptureFrame();
            if (frame == null || !HasImage(frame))
                return;
            Directory.CreateDirectory(targetDirectory);
            string pendingPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllBytes(pendingPath, ImageConversion.EncodeToPNG(frame));
                File.Move(pendingPath, path, true);
            }
            finally
            {
                if (File.Exists(pendingPath)) File.Delete(pendingPath);
            }
        }
        catch (Exception ex)
        {
            if (!warned)
            {
                warn($"剧情实景截图失败。{ex.Message}");
                warned = true;
            }
        }
        finally
        {
            if (frame != null) UnityEngine.Object.Destroy(frame);
        }
    }

    private static Texture2D CaptureFrame()
    {
        var service = Engine.GetService<ICameraManager>();
        var camera = service.TryCast<CameraManager>();
        var original = camera?.thumbnailRenderTexture;
        if (original == null)
            return service.CaptureThumbnail();

        var configuration = camera.Configuration;
        var resolution = configuration.ThumbnailResolution;
        var descriptor = original.descriptor;
        descriptor.width = 1280;
        descriptor.height = 720;
        var target = new RenderTexture(descriptor);
        var activeTarget = RenderTexture.active;
        try
        {
            configuration.ThumbnailResolution = new Vector2Int(descriptor.width, descriptor.height);
            camera.thumbnailRenderTexture = target;
            return service.CaptureThumbnail();
        }
        finally
        {
            configuration.ThumbnailResolution = resolution;
            camera.thumbnailRenderTexture = original;
            RenderTexture.active = activeTarget;
            UnityEngine.Object.Destroy(target);
        }
    }

    private static bool HasImage(Texture2D frame)
    {
        Color first = frame.GetPixel(frame.width / 5, frame.height / 5);
        for (int y = 1; y <= 4; y++)
        for (int x = 1; x <= 4; x++)
        {
            var color = frame.GetPixel(frame.width * x / 5, frame.height * y / 5);
            if (Math.Abs(color.r - first.r) + Math.Abs(color.g - first.g) + Math.Abs(color.b - first.b) > 0.1f)
                return true;
        }
        return false;
    }

    public GalleryEntry[] Read()
    {
        if (!Directory.Exists(directory))
            return Array.Empty<GalleryEntry>();
        var current = Directory.Exists(contextDirectory) ? Directory.EnumerateFiles(contextDirectory, "*.png") : Array.Empty<string>();
        return Directory.EnumerateFiles(directory, "*.png").Concat(current)
            .Select(path => (Path: path, Key: Path.GetFileNameWithoutExtension(path)))
            .Where(item => item.Key.EndsWith("-1", StringComparison.Ordinal) || item.Key.EndsWith("-2", StringComparison.Ordinal))
            .GroupBy(item => item.Key)
            .Select(group => group.Last())
            .Select(item => (item.Path, Mark: filenames.GetValueOrDefault(item.Key[..^2]), Appearance: item.Key[^1] - '0'))
            .Where(item => item.Mark != null)
            .OrderBy(item => item.Mark.Route, StringComparer.Ordinal)
            .ThenBy(item => item.Mark.Script, StringComparer.Ordinal)
            .ThenBy(item => item.Mark.Line)
            .Select(item => new GalleryEntry
            {
                Route = item.Mark.Route,
                Chapter = item.Mark.Route,
                Title = item.Mark.Title + (item.Appearance == 1 ? "（长发）" : "（短发）"),
                Appearance = item.Appearance,
                CapturedPath = item.Path
            }).ToArray();
    }

    public void Dispose() => player.remove_OnCommandExecutionFinish(listener);
}
