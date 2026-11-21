using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Forms = System.Windows.Forms;

[assembly: AssemblyTitle("Annihilated Texture Editor")]
[assembly: AssemblyProduct("Annihilated Texture Editor")]
[assembly: AssemblyVersion("1.1.1.0")]

namespace Annihilated.TextureEditor
{
    internal sealed partial class Editor
    {
        private readonly Window window;
        private readonly List<Asset> catalog;
        private readonly TextBox search;
        private readonly CheckBox onlyReplaced;
        private readonly ListBox list;
        private readonly CheckBox fit;
        private readonly Button choosePng;
        private readonly Button chooseAll;
        private readonly Button apply;
        private readonly Button restore;
        private readonly Button exportOriginal;
        private GameFolder game;
        private Asset selected;
        private PreparedImage candidate;
        private BitmapSource[] candidateViews;
        private BitmapSource[] originalViews;
        private BitmapSource[] installedViews;
        private System.Drawing.Rectangle[] frameRects = new System.Drawing.Rectangle[0];
        private string candidatePath;
        private readonly ListBox frameList;
        private readonly ComboBox variants;
        private int frameIndex;
        private bool selectingFrames;
        private bool selectingVariant;
        private string group = "Enemy";
        private bool refreshing;

        [STAThread]
        public static int Main(string[] args)
        {
            if (args.Length == 2 && args[0] == "--self-test") return RunChecks(args[1]);
            try
            {
                var app = new Application();
                var editor = new Editor(GameFolder.Detect());
                return app.Run(editor.window);
            }
            catch (Exception e)
            {
                MessageBox.Show("无法打开贴图编辑器。\n" + e.Message, "贴图编辑器", MessageBoxButton.OK, MessageBoxImage.Error);
                return 1;
            }
        }

        private Editor(GameFolder game)
        {
            using (var layout = Assembly.GetExecutingAssembly().GetManifestResourceStream("EditorWindow.xaml"))
                window = (Window)XamlReader.Load(layout);
            this.game = game;
            catalog = Asset.Load();
            search = Find<TextBox>("Search");
            onlyReplaced = Find<CheckBox>("OnlyReplaced");
            list = Find<ListBox>("Assets");
            fit = Find<CheckBox>("Fit");
            choosePng = Find<Button>("ChoosePng");
            chooseAll = Find<Button>("ChooseAll");
            apply = Find<Button>("Apply");
            restore = Find<Button>("Restore");
            exportOriginal = Find<Button>("ExportOriginal");
            frameList = Find<ListBox>("Frames");
            variants = Find<ComboBox>("Variants");

            Find<Button>("Enemies").Click += (s, e) => SwitchGroup("Enemy");
            Find<Button>("Items").Click += (s, e) => SwitchGroup("Item");
            Find<Button>("ChooseRoot").Click += (s, e) => BrowseGame();
            choosePng.Click += (s, e) => BrowseImage();
            chooseAll.Click += (s, e) => BrowseAllFrames();
            apply.Click += (s, e) => ApplySelected();
            restore.Click += (s, e) => RestoreSelected();
            exportOriginal.Click += (s, e) => SaveOriginal();
            search.TextChanged += (s, e) => RefreshList(followSearch: true);
            onlyReplaced.Checked += (s, e) => RefreshList();
            onlyReplaced.Unchecked += (s, e) => RefreshList();
            list.SelectionChanged += (s, e) => { if (!refreshing) SetSelected((AssetRow)list.SelectedItem); };
            variants.SelectionChanged += (s, e) =>
            {
                var choice = (AssetChoice)variants.SelectedItem;
                if (!selectingVariant) SetAsset(choice == null ? null : choice.Asset);
            };
            frameList.SelectionChanged += (s, e) =>
            {
                if (selectingFrames || frameList.SelectedIndex < 0) return;
                frameIndex = frameList.SelectedIndex;
                ShowFrame();
            };
            fit.Checked += (s, e) => ReloadCandidate();
            fit.Unchecked += (s, e) => ReloadCandidate();
            var drop = Find<Border>("DropTarget");
            drop.DragOver += OnDragOver;
            drop.Drop += OnDrop;
            window.PreviewKeyDown += OnShortcut;
            window.Activated += (s, e) => RefreshList(refreshImages: true);

            RefreshGame();
            RefreshList("goblin");
        }

        private T Find<T>(string name) where T : class { return (T)window.FindName(name); }
        private void SetText(string name, string text) { Find<TextBlock>(name).Text = text; }

        private void Status(string text, bool error = false)
        {
            var field = Find<TextBlock>("Status");
            field.Text = text;
            field.ToolTip = text;
            field.Foreground = error ? new SolidColorBrush(Color.FromRgb(255, 170, 143)) : (Brush)window.FindResource("Muted");
        }

        private void RefreshGame()
        {
            SetText("RootPath", game == null ? "请选择游戏目录" : game.Root);
            Find<TextBlock>("RootPath").ToolTip = game == null ? null : game.Root;
            SetText("ModState", game == null ? "" : game.HasMod ? "换皮 Mod 已安装" : "未找到换皮 Mod");
            Status(game == null ? "请选择包含 Annihilated.exe 的游戏目录。" : "应用或恢复后，重启游戏生效。");
        }

        private void SwitchGroup(string next)
        {
            if (group == next) return;
            group = next;
            RefreshList();
        }

        private void RefreshList(string preferred = null, bool refreshImages = false, bool followSearch = false)
        {
            if (preferred == null && selected != null && selected.Group == group) preferred = selected.Id;
            string query = search.Text.Trim();
            string[] terms = query.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            if (followSearch)
            {
                var exact = catalog.FirstOrDefault(asset => asset.Group == group &&
                    asset.Id.Equals(query, StringComparison.OrdinalIgnoreCase));
                if (exact != null) preferred = exact.Id;
            }
            var rows = new List<AssetRow>();
            foreach (var family in catalog.Where(asset => asset.Group == group)
                         .GroupBy(asset => group == "Enemy" ? asset.EnemyKey : asset.Id, StringComparer.OrdinalIgnoreCase))
            {
                if (!family.Any(asset => terms.All(term => asset.SearchText.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0))) continue;
                var row = new AssetRow(family.Key, family.ToArray(), family.Any(HasOverride));
                if (onlyReplaced.IsChecked != true || row.Replaced) rows.Add(row);
            }
            refreshing = true;
            list.ItemsSource = rows;
            var cursor = rows.FirstOrDefault(row => row.Variants.Any(a => a.Id == preferred)) ?? rows.FirstOrDefault();
            list.SelectedItem = cursor;
            if (cursor != null) list.ScrollIntoView(cursor);
            refreshing = false;
            SetSelected(cursor, preferred, refreshImages);
            SetText("Count", rows.Count + (group == "Enemy" ? " 种敌人" : " 项"));
            Find<TextBlock>("EmptyList").Visibility = rows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            Find<TextBlock>("SearchPlaceholder").Visibility = search.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
            StyleGroup("Enemies", group == "Enemy");
            StyleGroup("Items", group == "Item");
        }

        private void StyleGroup(string name, bool active)
        {
            var button = Find<Button>(name);
            button.Background = new SolidColorBrush(active ? Color.FromRgb(53, 72, 45) : Color.FromRgb(37, 48, 41));
            button.BorderBrush = new SolidColorBrush(active ? Color.FromRgb(105, 138, 83) : Color.FromRgb(55, 67, 59));
        }

        private bool HasOverride(Asset asset) { return asset != null && game != null && File.Exists(game.OverridePath(asset)); }

        private void SetSelected(AssetRow row, string preferred = null, bool refreshImages = false)
        {
            selectingVariant = true;
            var choices = row == null ? new AssetChoice[0] :
                row.Variants.Select(asset => new AssetChoice(asset, HasOverride(asset))).ToArray();
            variants.ItemsSource = choices;
            var next = choices.FirstOrDefault(x => x.Asset.Id == preferred) ?? choices.FirstOrDefault();
            if (next != null && onlyReplaced.IsChecked == true && !next.HasPng &&
                (selected != next.Asset || candidate == null))
                next = choices.FirstOrDefault(x => x.HasPng) ?? next;
            variants.SelectedItem = next;
            selectingVariant = false;
            SetAsset(next == null ? null : next.Asset, refreshImages);
        }

        private void SetAsset(Asset next, bool refreshImages = false)
        {
            bool changed = selected != next;
            if (changed)
            {
                ResetCandidate();
                frameIndex = 0;
                frameRects = next == null ? new System.Drawing.Rectangle[0] : FrameLayout.Get(next);
                Status(game == null ? "请选择包含 Annihilated.exe 的游戏目录。" : "应用或恢复后，重启游戏生效。");
            }
            selected = next;
            bool enemy = next != null && next.Group == "Enemy";
            SetText("AssetTitle", next == null ? "请选择资源" : enemy ? next.EnemyTitle : next.Title);
            SetText("AssetId", next == null ? "" : next.Group + "/" + next.Id + ".png");
            Find<DockPanel>("VariantBar").Visibility = enemy ? Visibility.Visible : Visibility.Collapsed;
            Find<Border>("FrameBar").Visibility = enemy ? Visibility.Visible : Visibility.Collapsed;
            choosePng.Content = enemy ? "替换当前帧…" : "选择 PNG…";
            choosePng.IsEnabled = game != null && next != null;
            chooseAll.Visibility = enemy && frameRects.Length > 1 ? Visibility.Visible : Visibility.Collapsed;
            chooseAll.IsEnabled = choosePng.IsEnabled && frameRects.Length > 1;
            fit.IsEnabled = choosePng.IsEnabled;
            restore.IsEnabled = HasOverride(next);
            restore.Content = enemy && frameRects.Length > 1 ? "恢复此图集" : "恢复原图";
            if (changed || refreshImages)
            {
                originalViews = game != null && next != null ? TryReadViews(game.ExportPath(next)) : null;
                installedViews = HasOverride(next) ? TryReadViews(game.OverridePath(next)) : null;
            }
            RefreshReplacement();
        }

        private void RefreshReplacement()
        {
            var strip = candidateViews ?? installedViews ?? originalViews;
            selectingFrames = true;
            frameList.ItemsSource = frameRects.Select((rect, index) => new FrameRow(index + 1, ViewAt(strip, index))).ToArray();
            frameList.SelectedIndex = frameRects.Length == 0 ? -1 : frameIndex;
            if (frameList.SelectedItem != null) frameList.ScrollIntoView(frameList.SelectedItem);
            selectingFrames = false;
            ShowFrame();
        }

        private void ShowFrame()
        {
            bool installed = HasOverride(selected);
            var original = ViewAt(originalViews, frameIndex);
            var image = ViewAt(candidateViews ?? installedViews, frameIndex);
            Find<Image>("Original").Source = original;
            Find<Image>("Replacement").Source = image;
            exportOriginal.IsEnabled = original != null;
            exportOriginal.Content = frameRects.Length > 1 ? "导出原图集" : "导出原图";
            SetText("OriginalHint", selected == null || original != null ? "" :
                game == null ? "选择游戏目录后预览" : "当前目录没有导出的原图");
            SetText("AssetSize", selected == null ? "" : frameRects.Length > 1
                ? frameRects[frameIndex].Width + " × " + frameRects[frameIndex].Height + " / 帧"
                : selected.SizeLabel);
            SetText("FrameCount", frameRects.Length == 0 ? "" : (frameIndex + 1) + " / " + frameRects.Length);
            SetText("ImageNote", selected == null ? "" : frameRects.Length > 1
                ? "全部帧可导入整张图集，或一次选择 " + frameRects.Length + " 张 PNG（按文件名排序）。"
                : "尺寸不同时，等比缩放并在四周补透明边。");
            SetText("PreviewTitle", candidate != null ? "替换预览" : "当前替换图");
            SetText("PreviewState", candidate != null ? "待应用" : installed ? installedViews == null ? "PNG 无效" : "已替换" : "");
            SetText("ReplacementHint", image != null || selected == null ? "" : installed
                ? "当前 PNG 无法读取或尺寸不符，游戏会显示原图。"
                : game == null ? "请先选择游戏目录" : "拖入 PNG，或点下方的导入按钮");
            apply.IsEnabled = candidate != null && game != null && selected != null;
        }

        private static BitmapSource ViewAt(BitmapSource[] views, int index)
        {
            return views != null && index >= 0 && index < views.Length ? views[index] : null;
        }

        private void ResetCandidate()
        {
            candidate = null;
            candidatePath = null;
            candidateViews = null;
        }

        private void BrowseGame()
        {
            using (var dialog = new Forms.FolderBrowserDialog())
            {
                dialog.Description = "选择包含 Annihilated.exe 的游戏目录";
                dialog.ShowNewFolderButton = false;
                if (game != null) dialog.SelectedPath = game.Root;
                if (dialog.ShowDialog() != Forms.DialogResult.OK) return;
                if (!GameFolder.IsGame(dialog.SelectedPath))
                {
                    Status("这个目录没有找到 Annihilated.exe 和 Annihilated_Data。", true);
                    return;
                }
                game = new GameFolder(dialog.SelectedPath);
            }
            ResetCandidate();
            RefreshGame();
            RefreshList(refreshImages: true);
        }

        private void BrowseImage()
        {
            if (!choosePng.IsEnabled) return;
            var dialog = new OpenFileDialog { Filter = "PNG 图片 (*.png)|*.png", Title = "选择替换 PNG", CheckFileExists = true };
            if (dialog.ShowDialog(window) == true) ImportFrame(dialog.FileName);
        }

        private void BrowseAllFrames()
        {
            if (!chooseAll.IsEnabled) return;
            var dialog = new OpenFileDialog
            {
                Filter = "PNG 图片 (*.png)|*.png", Title = "选择 1 张完整图集，或 " + frameRects.Length + " 张单帧 PNG",
                CheckFileExists = true, Multiselect = true
            };
            if (dialog.ShowDialog(window) == true) ImportAllFrames(dialog.FileNames);
        }

        private void ReloadCandidate()
        {
            if (candidatePath != null) Prepare(candidatePath);
        }

        private void Prepare(string path)
        {
            if (selected == null || game == null) return;
            ResetCandidate();
            candidatePath = path;
            try
            {
                var prepared = PreparedImage.Load(path, selected, fit.IsChecked == true);
                SetCandidate(prepared);
                Status(Path.GetFileName(path) + (prepared.Fitted ? " 已适配到 " + selected.SizeLabel + "。点击「应用替换」保存。" : " 已载入。点击「应用替换」保存。"));
            }
            catch (Exception e)
            {
                Status("无法导入 PNG。 " + e.Message, true);
            }
            RefreshReplacement();
        }

        private void ImportFrame(string path)
        {
            if (frameRects.Length < 2) { Prepare(path); return; }
            try
            {
                byte[] baseSheet = candidate == null ? null : candidate.Bytes;
                if (baseSheet == null)
                {
                    var basePath = HasOverride(selected) ? game.OverridePath(selected) : game.ExportPath(selected);
                    if (File.Exists(basePath)) baseSheet = File.ReadAllBytes(basePath);
                }
                SetCandidate(PreparedImage.ComposeFrames(selected, frameRects, frameIndex, new[] { path }, fit.IsChecked == true, baseSheet));
                Status("第 " + (frameIndex + 1) + " 帧已更新预览。点「应用替换」保存。");
            }
            catch (Exception e) { Status("无法替换当前帧。 " + e.Message, true); }
            RefreshReplacement();
        }

        private void ImportAllFrames(string[] files)
        {
            try
            {
                PreparedImage next;
                if (files.Length == 1) next = PreparedImage.Load(files[0], selected, false);
                else if (files.Length == frameRects.Length)
                    next = PreparedImage.ComposeFrames(selected, frameRects, 0, FrameLayout.SortSources(files), fit.IsChecked == true, null);
                else throw new InvalidDataException("请选择 1 张完整图集，或 " + frameRects.Length + " 张单帧 PNG。");
                SetCandidate(next);
                Status("当前状态的 " + frameRects.Length + " 帧已更新预览。点「应用替换」保存。");
            }
            catch (Exception e) { Status("无法替换全部帧。 " + e.Message, true); }
            RefreshReplacement();
        }

        private void SetCandidate(PreparedImage next)
        {
            BitmapSource[] views;
            using (var stream = new MemoryStream(next.Bytes, false))
                views = ReadViews(stream);
            if (views == null || views.Length != frameRects.Length) throw new InvalidDataException("无法生成替换预览。");
            candidateViews = views;
            candidate = next;
            if (frameRects.Length > 1) candidatePath = null;
        }

        private void ApplySelected()
        {
            if (game == null || selected == null || candidate == null) return;
            try
            {
                var asset = selected;
                game.Apply(asset, candidate);
                ResetCandidate();
                RefreshList(asset.Id, true);
                Status("已应用 " + asset.Id + "。重启游戏后查看。");
            }
            catch (Exception e) { Status("无法应用替换。 " + e.Message, true); }
        }

        private void RestoreSelected()
        {
            if (game == null || selected == null) return;
            try
            {
                var asset = selected;
                var backup = game.Restore(asset);
                ResetCandidate();
                RefreshList(asset.Id, true);
                Status(backup == null ? "这项资源已经使用原图。" :
                    "已恢复 " + asset.Id + "。重启游戏生效。旧 PNG 已放入 .editor-backups。");
            }
            catch (Exception e) { Status("无法恢复原图。 " + e.Message, true); }
        }

        private void SaveOriginal()
        {
            if (!exportOriginal.IsEnabled || game == null || selected == null) return;
            var dialog = new SaveFileDialog { Filter = "PNG 图片 (*.png)|*.png", FileName = selected.Id + ".png" };
            if (dialog.ShowDialog(window) != true) return;
            try
            {
                if (string.Equals(Path.GetFullPath(dialog.FileName), game.OverridePath(selected), StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("请选择其他位置。要恢复原版画面，请使用「恢复原图」。");
                File.Copy(game.ExportPath(selected), dialog.FileName, true);
                Status("已导出原图到 " + dialog.FileName);
            }
            catch (Exception e) { Status("无法导出原图。 " + e.Message, true); }
        }

        private void OnDragOver(object sender, DragEventArgs e)
        {
            var files = e.Data.GetDataPresent(DataFormats.FileDrop) ? (string[])e.Data.GetData(DataFormats.FileDrop) : null;
            e.Effects = choosePng.IsEnabled && files != null && (files.Length == 1 || (chooseAll.IsEnabled && files.Length == frameRects.Length)) &&
                files.All(path => path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        }

        private void OnDrop(object sender, DragEventArgs e)
        {
            OnDragOver(sender, e);
            if (e.Effects == DragDropEffects.Copy)
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length == 1) ImportFrame(files[0]);
                else ImportAllFrames(files);
            }
        }

        private void OnShortcut(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F5)
            {
                RefreshList(refreshImages: true);
                e.Handled = true;
                return;
            }
            if (Keyboard.Modifiers != ModifierKeys.Control) return;
            if (e.Key == Key.O) { BrowseImage(); e.Handled = true; }
            else if (e.Key == Key.F) { search.Focus(); search.SelectAll(); e.Handled = true; }
            else if (e.Key == Key.Enter) { ApplySelected(); e.Handled = true; }
        }

        private BitmapSource[] TryReadViews(string path)
        {
            if (path == null || !File.Exists(path)) return null;
            try
            {
                using (var stream = File.OpenRead(path)) return ReadViews(stream);
            }
            catch (Exception) { return null; }
        }

        private BitmapSource[] ReadViews(Stream stream)
        {
            var header = new byte[24];
            if (stream.Read(header, 0, header.Length) != header.Length ||
                !header.Take(8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }))
                throw new InvalidDataException("PNG 文件无效。");
            int width = ReadDimension(header, 16);
            int height = ReadDimension(header, 20);
            if (width != selected.Width || height != selected.Height)
                throw new InvalidDataException("PNG 尺寸与当前资源不一致。");
            stream.Position = 0;
            return frameRects.Length > 1 ? FramePreviews.Read(stream, selected, frameRects) :
                new BitmapSource[] { ReadPreview(stream, width, height) };
        }

        private static BitmapImage ReadPreview(Stream stream, int width, int height)
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            if (width >= height && width > 1280) bitmap.DecodePixelWidth = 1280;
            if (height > width && height > 1280) bitmap.DecodePixelHeight = 1280;
            bitmap.StreamSource = stream;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }

        private static int ReadDimension(byte[] data, int offset)
        {
            return (data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3];
        }
    }

    public sealed class AssetChoice
    {
        public Asset Asset { get; private set; }
        public bool HasPng { get; private set; }
        public string Label { get { return Asset.StateLabel + (HasPng ? "  ·  有替换图" : ""); } }
        public AssetChoice(Asset asset, bool hasPng) { Asset = asset; HasPng = hasPng; }
    }

    public sealed class AssetRow
    {
        public Asset[] Variants { get; private set; }
        public string Title { get; private set; }
        public string Details { get; private set; }
        public bool Replaced { get; private set; }
        public string Mark { get { return Replaced ? "有替换图" : ""; } }
        public AssetRow(string key, Asset[] assets, bool replaced)
        {
            Variants = assets.OrderBy(x => x.Id.Equals(key, StringComparison.OrdinalIgnoreCase) ||
                System.Text.RegularExpressions.Regex.Replace(x.Id, @"_x\d+$", "").Equals(key, StringComparison.OrdinalIgnoreCase) ? 0 :
                x.Variant == "基础图或形态" ? 1 : 2).ThenBy(x => x.Id, StringComparer.OrdinalIgnoreCase).ToArray();
            var first = Variants[0];
            Title = first.Group == "Enemy" ? first.EnemyTitle : first.Title;
            Details = first.Group == "Enemy" ? first.Section + "  ·  " + assets.Length + " 个状态 / 形态" : first.Details;
            Replaced = replaced;
        }
    }
}
