using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Annihilated.TextureEditor
{
    public sealed class Asset
    {
        private static readonly Dictionary<string, string> Names = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "goblin", "哥布林" }, { "goblinLancer", "哥布林枪兵" }, { "goblinLord", "哥布林领主" },
            { "guardianGoblin", "守卫哥布林" }, { "kobold", "狗头人" }, { "blackKnight", "黑骑士" },
            { "priest", "祭司" }, { "slime", "史莱姆" }, { "slimeMountain", "史莱姆山" },
            { "slimePiece", "史莱姆碎块" }, { "evilGod", "邪神" }, { "warriorFinal", "战士（最终战）" },
            { "dragon", "龙" }, { "seaDragon", "海龙" }, { "dragonEye", "龙之眼" },
            { "ironSword", "铁剑" }, { "heroSword", "勇者之剑" }, { "blackKnightSword", "黑骑士剑" },
            { "club", "木棒" }, { "katana", "武士刀" }, { "rapier", "刺剑" }, { "scythe", "镰刀" },
            { "hunterBow", "猎弓" }, { "heavyBowgun", "重弩" }, { "fireRod", "火焰法杖" },
            { "iceRod", "冰霜法杖" }, { "thunderRod", "雷霆法杖" }
        };

        private readonly string family;
        public string Group { get; private set; }
        public string Id { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int Frames { get; private set; }
        public string Section { get; private set; }
        public string Variant { get; private set; }
        public string ExportPath { get; private set; }
        public string EnemyKey
        {
            get
            {
                if (family == "史莱姆")
                {
                    if (Id.StartsWith("slimeMountain", StringComparison.OrdinalIgnoreCase)) return "slimeMountain";
                    if (Id.StartsWith("slimePiece", StringComparison.OrdinalIgnoreCase)) return "slimePiece";
                    return "slime";
                }
                if (family == "warriorFinal2" || family == "warriorFinal3") return "warriorFinal";
                return family.Length == 0 ? Id : family;
            }
        }

        public string EnemyTitle
        {
            get
            {
                string mapped;
                return Names.TryGetValue(EnemyKey, out mapped) ? mapped : EnemyKey;
            }
        }

        private Asset(string[] fields)
        {
            Group = fields[0];
            Id = fields[1];
            Width = int.Parse(fields[2], CultureInfo.InvariantCulture);
            Height = int.Parse(fields[3], CultureInfo.InvariantCulture);
            Frames = int.Parse(fields[4], CultureInfo.InvariantCulture);
            Section = fields[5];
            family = fields[6];
            Variant = fields[7];
            ExportPath = fields[8];
        }

        public string Title
        {
            get
            {
                string mapped;
                if (Names.TryGetValue(Id, out mapped) || Names.TryGetValue(family, out mapped))
                    return mapped;
                if (family.Length != 0 && !Regex.IsMatch(family, "^[a-zA-Z0-9_ -]+$"))
                    return family;
                return Id;
            }
        }
        public string Details
        {
            get
            {
                var values = new[] { Title == Id ? "" : Id, Section, Variant };
                return string.Join("  ·  ", values.Where(x => x.Length > 0));
            }
        }
        public bool IsSheet { get { return Frames > 1 || Regex.IsMatch(Id, @"_x\d+$", RegexOptions.IgnoreCase); } }
        public string StateLabel { get { return (Variant.Length == 0 ? "" : Variant + "  ·  ") + Id + (IsSheet ? "  ·  " + Frames + " 帧" : ""); } }
        public string SizeLabel { get { return Width + " × " + Height; } }
        public string SearchText { get { return Title + " " + Details + " " + family + " " + EnemyTitle + " " + Id; } }

        public static List<Asset> Load()
        {
            var result = new List<Asset>();
            using (var reader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("catalog.tsv")))
                for (string line = reader.ReadLine(); line != null; line = reader.ReadLine())
                {
                    var fields = line.Split('\t');
                    if (fields.Length != 9) throw new InvalidDataException("资源目录格式有误。");
                    result.Add(new Asset(fields));
                }
            return result.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase).ToList();
        }
    }

    internal sealed class GameFolder
    {
        public string Root { get; private set; }
        public bool HasMod { get { return File.Exists(Path.Combine(Root, "Mods", "AnnihilatedTextureOverride.dll")); } }
        public GameFolder(string root) { Root = Path.GetFullPath(root); }

        public static bool IsGame(string path)
        {
            return !string.IsNullOrEmpty(path) && File.Exists(Path.Combine(path, "Annihilated.exe")) &&
                Directory.Exists(Path.Combine(path, "Annihilated_Data"));
        }

        public static GameFolder Detect()
        {
            for (var folder = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory); folder != null; folder = folder.Parent)
                if (IsGame(folder.FullName)) return new GameFolder(folder.FullName);
            return null;
        }

        public string OverridePath(Asset asset)
        {
            return Path.Combine(Root, "UserData", "TextureOverrides", asset.Group, asset.Id + ".png");
        }

        public string ExportPath(Asset asset)
        {
            return Path.Combine(Root, "mod开发相关", "Annihilated_美术资源", asset.ExportPath.Replace('/', Path.DirectorySeparatorChar));
        }

        public void Apply(Asset asset, PreparedImage image)
        {
            if (image.Width != asset.Width || image.Height != asset.Height)
                throw new InvalidDataException("替换图的尺寸和当前资源不一致。");
            string destination = OverridePath(asset);
            string directory = Path.GetDirectoryName(destination);
            Directory.CreateDirectory(directory);
            string pending = Path.Combine(directory, "." + Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                File.WriteAllBytes(pending, image.Bytes);
                if (File.Exists(destination))
                    File.Replace(pending, destination, NewBackupPath(asset));
                else
                    File.Move(pending, destination);
            }
            finally
            {
                if (File.Exists(pending)) File.Delete(pending);
            }
        }

        public string Restore(Asset asset)
        {
            string destination = OverridePath(asset);
            if (!File.Exists(destination)) return null;
            string backup = NewBackupPath(asset);
            File.Move(destination, backup);
            return backup;
        }

        private string NewBackupPath(Asset asset)
        {
            string directory = Path.Combine(Root, "UserData", "TextureOverrides", ".editor-backups", asset.Group);
            Directory.CreateDirectory(directory);
            return Path.Combine(directory, asset.Id + "." + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture)
                + "." + Guid.NewGuid().ToString("N") + ".png");
        }
    }

    internal sealed partial class PreparedImage
    {
        private static readonly byte[] Signature = { 137, 80, 78, 71, 13, 10, 26, 10 };
        public byte[] Bytes { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public bool Fitted { get; private set; }

        private PreparedImage(byte[] bytes, Asset asset, bool fitted)
        {
            Bytes = bytes;
            Width = asset.Width;
            Height = asset.Height;
            Fitted = fitted;
        }

        public static PreparedImage Load(string path, Asset asset, bool fit)
        {
            if (new FileInfo(path).Length > 160L * 1024 * 1024)
                throw new InvalidDataException("PNG 文件大于 160 MB。");
            byte[] bytes = File.ReadAllBytes(path);
            if (bytes.Length < 24 || !bytes.Take(8).SequenceEqual(Signature))
                throw new InvalidDataException("请选择有效的 PNG 文件。");
            using (var stream = new MemoryStream(bytes, false))
            using (var image = Image.FromStream(stream, true, true))
            {
                if (image.Width == asset.Width && image.Height == asset.Height)
                    return new PreparedImage(bytes, asset, false);
                if (asset.IsSheet || !fit)
                    throw new InvalidDataException(string.Format("此图为 {0} × {1}，需要 {2} 的{3}。",
                        image.Width, image.Height, asset.SizeLabel, asset.IsSheet ? "原画布图集" : "PNG"));
                using (var canvas = new Bitmap(asset.Width, asset.Height, PixelFormat.Format32bppArgb))
                using (var drawing = Graphics.FromImage(canvas))
                using (var output = new MemoryStream())
                {
                    double ratio = Math.Min((double)asset.Width / image.Width, (double)asset.Height / image.Height);
                    int width = Math.Max(1, (int)Math.Round(image.Width * ratio));
                    int height = Math.Max(1, (int)Math.Round(image.Height * ratio));
                    drawing.CompositingMode = CompositingMode.SourceCopy;
                    drawing.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    drawing.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    drawing.DrawImage(image, new Rectangle((asset.Width - width) / 2, (asset.Height - height) / 2, width, height));
                    canvas.Save(output, ImageFormat.Png);
                    return new PreparedImage(output.ToArray(), asset, true);
                }
            }
        }
    }
}
