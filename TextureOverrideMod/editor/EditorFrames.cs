using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Media.Imaging;

namespace Annihilated.TextureEditor
{
    internal static class FrameLayout
    {
        // The shipped sprites use full-height, equally spaced m_Rect slices for these four sheets.
        private static readonly Dictionary<string, Size> CellSizes = new Dictionary<string, Size>(StringComparer.OrdinalIgnoreCase)
        {
            { "priest_x10", new Size(1101, 1038) },
            { "evilGodLimb_x6", new Size(1101, 1038) },
            { "evilGodLimbSmall_x6", new Size(883, 832) },
            { "slimeMountain_summon_x5", new Size(3057, 1495) }
        };

        public static Rectangle[] Get(Asset asset)
        {
            Size cell;
            if (!CellSizes.TryGetValue(asset.Id, out cell) || asset.Height != cell.Height ||
                asset.Width != cell.Width * asset.Frames)
                return new[] { new Rectangle(0, 0, asset.Width, asset.Height) };
            return Enumerable.Range(0, asset.Frames).Select(i => new Rectangle(i * cell.Width, 0, cell.Width, cell.Height)).ToArray();
        }

        public static string[] SortSources(IEnumerable<string> paths)
        {
            // Pad each numeric run so frame2 sorts before frame10.
            return paths.OrderBy(path => Regex.Replace(Path.GetFileName(path), @"\d+", m => m.Value.PadLeft(20, '0')),
                                 StringComparer.OrdinalIgnoreCase).ToArray();
        }
    }

    internal sealed partial class PreparedImage
    {
        public static PreparedImage ComposeFrames(Asset asset, Rectangle[] frames, int start, string[] inputs,
                                                   bool fit, byte[] basePng)
        {
            if (start < 0 || inputs.Length == 0 || start + inputs.Length > frames.Length)
                throw new InvalidDataException("替换图数量与选中的帧不一致。");
            if (basePng == null && inputs.Length != frames.Length)
                throw new InvalidDataException("没有可用于保留其他帧的图集。可先导入完整图集。");

            bool resized = false;
            using (var canvas = new Bitmap(asset.Width, asset.Height, PixelFormat.Format32bppArgb))
            using (var drawing = Graphics.FromImage(canvas))
            using (var output = new MemoryStream())
            {
                drawing.CompositingMode = CompositingMode.SourceCopy;
                drawing.InterpolationMode = InterpolationMode.HighQualityBicubic;
                drawing.PixelOffsetMode = PixelOffsetMode.HighQuality;
                if (basePng != null)
                {
                    using (var stream = new MemoryStream(basePng, false))
                    using (var picture = Image.FromStream(stream, true, true))
                    {
                        if (picture.Width != asset.Width || picture.Height != asset.Height)
                            throw new InvalidDataException("当前图集尺寸有误，请先恢复原图或导入完整图集。");
                        drawing.DrawImageUnscaled(picture, 0, 0);
                    }
                }

                for (int i = 0; i < inputs.Length; i++)
                {
                    string path = inputs[i];
                    var rect = frames[start + i];
                    using (var stream = File.OpenRead(path))
                    {
                        byte[] signature = new byte[Signature.Length];
                        if (stream.Read(signature, 0, signature.Length) != signature.Length || !signature.SequenceEqual(Signature))
                            throw new InvalidDataException(Path.GetFileName(path) + " 不是有效的 PNG。");
                        stream.Position = 0;
                        using (var picture = Image.FromStream(stream, true, true))
                        {
                            bool mismatch = picture.Width != rect.Width || picture.Height != rect.Height;
                            if (mismatch && (!fit || (picture.Width == asset.Width && picture.Height == asset.Height)))
                                throw new InvalidDataException(Path.GetFileName(path) + " 需要单帧尺寸 " + rect.Width + " × " + rect.Height + "。");
                            drawing.SetClip(rect);
                            drawing.FillRectangle(Brushes.Transparent, rect);
                            if (mismatch)
                            {
                                resized = true;
                                double ratio = Math.Min((double)rect.Width / picture.Width, (double)rect.Height / picture.Height);
                                int width = Math.Max(1, (int)Math.Round(picture.Width * ratio));
                                int height = Math.Max(1, (int)Math.Round(picture.Height * ratio));
                                var target = new Rectangle(rect.X + (rect.Width - width) / 2, rect.Y + (rect.Height - height) / 2, width, height);
                                drawing.DrawImage(picture, target, 0, 0, picture.Width, picture.Height, GraphicsUnit.Pixel);
                            }
                            else drawing.DrawImageUnscaled(picture, rect.X, rect.Y);
                            drawing.ResetClip();
                        }
                    }
                }
                canvas.Save(output, ImageFormat.Png);
                return new PreparedImage(output.ToArray(), asset, resized);
            }
        }
    }

    internal static class FramePreviews
    {
        public static BitmapSource[] Read(Stream stream, Asset asset, Rectangle[] frames)
        {
            using (var sheet = Image.FromStream(stream, true, true))
            {
                if (sheet.Width != asset.Width || sheet.Height != asset.Height) return null;
                return frames.Select(frame => Slice(sheet, frame)).ToArray();
            }
        }

        private static BitmapSource Slice(Image sheet, Rectangle frame)
        {
            double scale = Math.Min(1, 510.0 / Math.Max(frame.Width, frame.Height));
            int width = Math.Max(1, (int)Math.Round(frame.Width * scale));
            int height = Math.Max(1, (int)Math.Round(frame.Height * scale));
            using (var slice = new Bitmap(width, height, PixelFormat.Format32bppArgb))
            using (var drawing = Graphics.FromImage(slice))
            using (var stream = new MemoryStream())
            {
                drawing.InterpolationMode = InterpolationMode.HighQualityBicubic;
                drawing.PixelOffsetMode = PixelOffsetMode.HighQuality;
                drawing.CompositingMode = CompositingMode.SourceCopy;
                drawing.DrawImage(sheet, new Rectangle(0, 0, width, height), frame, GraphicsUnit.Pixel);
                slice.Save(stream, ImageFormat.Png);
                stream.Position = 0;
                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = stream;
                image.EndInit();
                image.Freeze();
                return image;
            }
        }
    }

    public sealed class FrameRow
    {
        public int Number { get; private set; }
        public BitmapSource Image { get; private set; }
        public FrameRow(int number, BitmapSource image) { Number = number; Image = image; }
    }
}
