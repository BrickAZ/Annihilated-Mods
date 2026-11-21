using System;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Drawing = System.Drawing;

namespace Annihilated.TextureEditor
{
    internal sealed partial class Editor
    {
        private static int RunChecks(string output)
        {
            Directory.CreateDirectory(output);
            string report = Path.Combine(output, "result.txt");
            try
            {
                var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
                var source = GameFolder.Detect();
                var fixture = new GameFolder(Path.Combine(output, "fixture-" + Guid.NewGuid().ToString("N")));
                Directory.CreateDirectory(Path.Combine(fixture.Root, "Annihilated_Data"));
                Directory.CreateDirectory(Path.Combine(fixture.Root, "Mods"));
                File.WriteAllBytes(Path.Combine(fixture.Root, "Annihilated.exe"), new byte[0]);
                File.WriteAllBytes(Path.Combine(fixture.Root, "Mods", "AnnihilatedTextureOverride.dll"), new byte[0]);
                var assets = Asset.Load();
                var goblin = assets.Single(x => x.Id == "goblin" && x.Group == "Enemy");
                var sword = assets.Single(x => x.Id == "ironSword" && x.Group == "Item");
                var dragonEye = assets.SingleOrDefault(x => x.Id == "dragonEye" && x.Group == "Item");
                var sheet = assets.Single(x => x.Id == "priest_x10" && x.Group == "Enemy");
                Require(goblin.Width == 511 && goblin.Height == 481 && !goblin.IsSheet, "Goblin catalog entry");
                Require(sword.Width == 65 && sword.Height == 65 && !sword.IsSheet, "Iron sword catalog entry");
                Require(sheet.IsSheet && sheet.Width == 11010, "Priest sheet catalog entry");
                Require(assets.Count(x => x.IsSheet && x.Group == "Enemy") == 4 &&
                    assets.Where(x => x.IsSheet && x.Group == "Enemy").All(x => FrameLayout.Get(x).Length == x.Frames),
                    "All four native sheets have editable frame layouts");
                Require(!assets.Any(x => x.Id == "potion" && x.Group == "Item"), "Weapon list excludes potion");
                Require(assets.Select(x => x.Group + "/" + x.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count() ==
                    assets.Count, "Unique resource IDs");

                if (source != null && File.Exists(source.ExportPath(goblin)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(fixture.ExportPath(goblin)));
                    File.Copy(source.ExportPath(goblin), fixture.ExportPath(goblin));
                }
                Directory.CreateDirectory(Path.GetDirectoryName(fixture.ExportPath(sheet)));
                if (source != null && File.Exists(source.ExportPath(sheet)))
                    File.Copy(source.ExportPath(sheet), fixture.ExportPath(sheet));
                else
                    MakePng(fixture.ExportPath(sheet), sheet.Width, sheet.Height, Drawing.Color.DarkCyan);

                string matching = Path.Combine(fixture.Root, "matching.png");
                string undersized = Path.Combine(fixture.Root, "different.png");
                string broken = Path.Combine(fixture.Root, "broken.png");
                MakePng(matching, 511, 481, Drawing.Color.DeepPink);
                MakePng(undersized, 128, 32, Drawing.Color.SpringGreen);
                File.WriteAllText(broken, "invalid PNG");
                var editor = new Editor(fixture);
                editor.window.WindowStartupLocation = WindowStartupLocation.Manual;
                editor.window.Left = -12000;
                editor.window.Top = 0;
                editor.window.ShowActivated = false;
                editor.window.ShowInTaskbar = false;
                editor.window.Show();

                editor.search.Text = "哥布林";
                Require(editor.selected == goblin || editor.selected.Id == "goblin", "Chinese goblin search and selection");
                var goblinRow = ((AssetRow)editor.list.SelectedItem);
                Require(goblinRow.Variants.Length == 2 && goblinRow.Variants.Any(x => x.Id == "goblin_old"),
                    "Goblin and legacy look share one enemy row");
                Require(!editor.apply.IsEnabled, "An image must be imported first");
                var sample = source != null ? source.OverridePath(goblin) : null;
                editor.Prepare(sample != null && File.Exists(sample) ? sample : matching);
                Require(editor.apply.IsEnabled && editor.candidate != null, "Preview enables apply");
                Capture(editor.window, Path.Combine(output, "window.png"));
                editor.Prepare(matching);
                editor.apply.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Require(File.ReadAllBytes(fixture.OverridePath(goblin)).SequenceEqual(File.ReadAllBytes(matching)),
                    "Exact-size replacement file written byte-for-byte");
                Require(editor.restore.IsEnabled, "Restore available");

                editor.Prepare(broken);
                Require(!editor.apply.IsEnabled && File.Exists(fixture.OverridePath(goblin)), "Invalid file leaves current replacement intact");
                editor.Prepare(undersized);
                Require(editor.candidate != null && editor.candidate.Fitted && editor.apply.IsEnabled, "Fit with transparent margin");
                editor.apply.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                using (var result = new Drawing.Bitmap(fixture.OverridePath(goblin)))
                {
                    Require(result.Size == new Drawing.Size(511, 481), "Saved fit dimensions");
                    Require(result.GetPixel(0, 0).A == 0 && result.GetPixel(255, 240).A == 255, "Fit preserves transparency");
                }
                string backups = Path.Combine(fixture.Root, "UserData", "TextureOverrides", ".editor-backups", "Enemy");
                Require(Directory.GetFiles(backups).Any(x => File.ReadAllBytes(x).SequenceEqual(File.ReadAllBytes(matching))),
                    "Overwritten replacement preserved");
                editor.restore.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Require(!File.Exists(fixture.OverridePath(goblin)) && Directory.GetFiles(backups).Length == 2,
                    "Restore disables the override and keeps the previous file");

                editor.search.Text = "priest_x10";
                Require(editor.selected.Id == "priest_x10" && editor.list.Items.Count == 1 &&
                    ((AssetRow)editor.list.SelectedItem).Variants.Length == 31 && editor.frameList.Items.Count == 10,
                    "Priest is one enemy with 31 selectable states and a ten-frame strip");
                Require(editor.variants.Items.Cast<AssetChoice>().Any(x => x.Asset.Id == "priest_damage"), "Damage state in picker");
                editor.variants.SelectedItem = editor.variants.Items.Cast<AssetChoice>().Single(x => x.Asset.Id == "priest_damage");
                Require(editor.frameList.Items.Count == 1 && editor.selected.Id == "priest_damage" && !editor.chooseAll.IsEnabled,
                    "Switching state updates the frame strip");
                editor.variants.SelectedItem = editor.variants.Items.Cast<AssetChoice>().Single(x => x.Asset.Id == "priest_x10");
                editor.ImportAllFrames(new[] { undersized });
                Require(!editor.apply.IsEnabled, "Wrong-size atlas is rejected by replace all");
                editor.frameList.SelectedIndex = 3;
                string firstFrame = Path.Combine(fixture.Root, "first-replacement.png");
                string secondFrame = Path.Combine(fixture.Root, "second-replacement.png");
                MakePng(firstFrame, 1101, 1038, Drawing.Color.DeepPink);
                MakePng(secondFrame, 1101, 1038, Drawing.Color.SpringGreen);
                editor.ImportFrame(firstFrame);
                Require(editor.apply.IsEnabled && editor.frameIndex == 3, "Selected frame preview staged");
                Capture(editor.window, Path.Combine(output, "enemy-frames.png"));
                editor.frameList.SelectedIndex = 8;
                editor.ImportFrame(secondFrame);
                editor.apply.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                using (var original = new Drawing.Bitmap(fixture.ExportPath(sheet)))
                using (var result = new Drawing.Bitmap(fixture.OverridePath(sheet)))
                {
                    Require(result.Width == sheet.Width && result.Height == sheet.Height, "Composed sheet keeps its original dimensions");
                    for (int i = 0; i < 10; i++)
                    {
                        int x = i * 1101 + 550;
                        var expected = i == 3 ? Drawing.Color.DeepPink : i == 8 ? Drawing.Color.SpringGreen : original.GetPixel(x, 500);
                        Require(result.GetPixel(x, 500).ToArgb() == expected.ToArgb(), "Compose isolated frame " + i);
                    }
                }
                var batch = Enumerable.Range(1, 10).Reverse().Select(i =>
                {
                    string path = Path.Combine(fixture.Root, "frame" + i + ".png");
                    MakePng(path, 64, 64, Drawing.Color.FromArgb(i * 20, 90, 150));
                    return path;
                }).ToArray();
                editor.ImportAllFrames(batch);
                Require(editor.apply.IsEnabled, "All frames staged from individual PNGs");
                editor.apply.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                using (var result = new Drawing.Bitmap(fixture.OverridePath(sheet)))
                {
                    for (int i = 0; i < 10; i++)
                        Require(result.GetPixel(i * 1101 + 550, 500).R == (i + 1) * 20, "Natural filename ordering " + i);
                }
                editor.ImportAllFrames(new[] { fixture.ExportPath(sheet) });
                editor.apply.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Require(File.ReadAllBytes(fixture.OverridePath(sheet)).SequenceEqual(File.ReadAllBytes(fixture.ExportPath(sheet))),
                    "Full sheet accepted and copied exactly");
                Require(!File.Exists(fixture.OverridePath(assets.Single(x => x.Id == "priest_damage"))),
                    "Other state was never replaced");
                var damage = assets.Single(x => x.Id == "priest_damage");
                editor.variants.SelectedItem = editor.variants.Items.Cast<AssetChoice>().Single(x => x.Asset.Id == damage.Id);
                editor.Prepare(undersized);
                editor.apply.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Require(editor.selected.Id == "priest_damage" && File.Exists(fixture.OverridePath(damage)) &&
                    editor.Find<TextBlock>("PreviewState").Text == "已替换",
                    "Exact resource search cannot jump back to another state after saving");
                Require(((AssetChoice)editor.variants.SelectedItem).HasPng,
                    "The picker marks the saved state even when another state is also replaced");
                editor.Find<Button>("Items").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                editor.search.Text = "铁剑";
                Require(editor.selected.Id == "ironSword", "Chinese weapon search and category button");
                editor.Prepare(undersized);
                editor.apply.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Require(File.Exists(fixture.OverridePath(sword)) && !File.Exists(fixture.OverridePath(goblin)), "Weapon written to Item");
                editor.onlyReplaced.IsChecked = true;
                Require(editor.list.Items.Count == 1, "Only-replaced filter shows the saved sword");
                File.Copy(undersized, fixture.OverridePath(sword), true);
                editor.RefreshList(refreshImages: true);
                Require(editor.Find<Image>("Replacement").Source == null &&
                    editor.Find<TextBlock>("PreviewState").Text == "PNG 无效",
                    "A file with the wrong dimensions is visible as invalid, not active");
                MakePng(fixture.OverridePath(sword), 65, 65, Drawing.Color.MediumTurquoise);
                editor.window.RaiseEvent(new System.Windows.Input.KeyEventArgs(
                    System.Windows.Input.Keyboard.PrimaryDevice, PresentationSource.FromVisual(editor.window),
                    Environment.TickCount, System.Windows.Input.Key.F5)
                    { RoutedEvent = System.Windows.Input.Keyboard.PreviewKeyDownEvent });
                Require(editor.Find<Image>("Replacement").Source != null &&
                    editor.Find<TextBlock>("PreviewState").Text == "已替换",
                    "F5 reloads an override edited outside the editor");
                Require(dragonEye != null && dragonEye.Width == 65 && dragonEye.Height == 65,
                    "Dragon eye is an editable 65 x 65 item icon");
                string eyeOverride = fixture.OverridePath(dragonEye);
                var actualEye = source == null ? null : source.OverridePath(dragonEye);
                if (actualEye != null && File.Exists(actualEye))
                    File.Copy(actualEye, eyeOverride);
                else
                    MakePng(eyeOverride, 65, 65, Drawing.Color.CornflowerBlue);
                if (source != null && File.Exists(source.ExportPath(dragonEye)))
                {
                    string eyeOriginal = fixture.ExportPath(dragonEye);
                    Directory.CreateDirectory(Path.GetDirectoryName(eyeOriginal));
                    File.Copy(source.ExportPath(dragonEye), eyeOriginal);
                }
                editor.search.Text = "龙之眼";
                Require(editor.list.Items.Count == 1 && editor.selected != null && editor.selected.Id == "dragonEye",
                    "Chinese search finds the existing dragon eye in only-replaced mode");
                Require(((AssetRow)editor.list.SelectedItem).Replaced && editor.restore.IsEnabled &&
                    editor.Find<Image>("Replacement").Source != null,
                    "The editor marks and previews the already installed dragon eye");
                Capture(editor.window, Path.Combine(output, "dragon-eye.png"));
                editor.window.Close();
                app.Shutdown();
                File.WriteAllText(report, "PASS: " + assets.Count + " catalog entries; existing dragon eye found by Chinese name and previewed in only-replaced mode; state stays selected after saving; invalid PNG status; F5 disk refresh; enemy grouping, per-frame preservation, sorted batch import, restore and backups.");
                return 0;
            }
            catch (Exception e)
            {
                File.WriteAllText(report, e.ToString());
                return 1;
            }
        }

        private static void MakePng(string path, int width, int height, Drawing.Color color)
        {
            using (var bitmap = new Drawing.Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            using (var graphics = Drawing.Graphics.FromImage(bitmap))
            {
                graphics.Clear(color);
                bitmap.Save(path, ImageFormat.Png);
            }
        }

        private static void Capture(Window window, string path)
        {
            window.UpdateLayout();
            window.Dispatcher.Invoke(DispatcherPriority.Render, new Action(() => { }));
            var content = (FrameworkElement)window.Content;
            var image = new RenderTargetBitmap((int)Math.Ceiling(content.ActualWidth), (int)Math.Ceiling(content.ActualHeight), 96, 96,
                PixelFormats.Pbgra32);
            image.Render(content);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(image));
            using (var output = File.Create(path)) encoder.Save(output);
        }

        private static void Require(bool good, string condition)
        {
            if (!good) throw new InvalidOperationException("Check failed: " + condition);
        }
    }
}
