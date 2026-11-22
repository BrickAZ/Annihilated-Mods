using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace Annihilated.TextureEditor
{
    internal static class RuntimeInstaller
    {
        internal const string ModFile = "AnnihilatedTextureOverride.dll";
        private const string ReleaseUrl = "https://github.com/BrickAZ/Annihilated-Mods/releases/download/" +
            "texture-override-v0.1.0/Annihilated-TextureOverride-v0.1.0-Editor-v1.1.1.zip";
        private const string ReleaseSha256 = "CCED2772199F36AFD7EAAD0B7DCE0B0C9B570088EC70B04D1DC0561C09318337";
        private const int ReleaseSize = 46667;

        internal static bool IsModAssembly(string path)
        {
            if (!File.Exists(path)) return false;
            try { return AssemblyName.GetAssemblyName(path).Name == "AnnihilatedTextureOverride"; }
            catch (BadImageFormatException) { return false; }
            catch (IOException) { return false; }
            catch (UnauthorizedAccessException) { return false; }
        }

        internal static void Install(GameFolder game)
        {
            if (!GameFolder.IsGame(game.Root))
                throw new InvalidOperationException("请重新选择有效的游戏目录。");
            if (game.HasRuntime) return;

            string stage = Path.Combine(Path.GetTempPath(), "Annihilated-Texture-" + Guid.NewGuid().ToString("N"));
            string mod = Path.Combine(stage, "Mods", ModFile);
            string archive = Path.Combine(stage, "release.zip");
            string script = Path.Combine(stage, "Install-Mod.ps1");
            string manifest = Path.Combine(stage, "Install-Package.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(mod));
            try
            {
                if (game.HasMod)
                    File.Copy(game.ModPath, mod);
                else
                {
                    DownloadRelease(archive);
                    ExtractVerifiedMod(archive, mod);
                }

                using (var program = typeof(RuntimeInstaller).Assembly.GetManifestResourceStream("Install-Mod.ps1"))
                using (var output = File.Create(script))
                    program.CopyTo(output);
                File.WriteAllText(manifest, "texture-runtime");
                RunInstaller(script, game.Root);
                if (!game.HasRuntime)
                    throw new InvalidOperationException("安装程序退出后，仍缺少换皮 Mod 或 MelonLoader。");
            }
            finally
            {
                foreach (var file in new[] { mod, archive, script, manifest })
                    File.Delete(file);
                Directory.Delete(Path.GetDirectoryName(mod));
                Directory.Delete(stage);
            }
        }

        private static void DownloadRelease(string destination)
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            var request = WebRequest.CreateHttp(ReleaseUrl);
            request.UserAgent = "AnnihilatedTextureEditor/1.1.2";
            request.Timeout = 30000;
            request.ReadWriteTimeout = 30000;
            try
            {
                using (var response = request.GetResponse())
                using (var input = response.GetResponseStream())
                using (var output = File.Create(destination))
                {
                    var buffer = new byte[8192];
                    int count;
                    while ((count = input.Read(buffer, 0, buffer.Length)) != 0)
                    {
                        if (output.Length + count > ReleaseSize)
                            throw new InvalidDataException("GitHub 安装资产大小与预期不符。");
                        output.Write(buffer, 0, count);
                    }
                }
            }
            catch (WebException error)
            {
                throw new IOException("从 GitHub 下载换皮 Mod 失败。 " + error.Message, error);
            }
        }

        internal static void ExtractVerifiedMod(string archive, string destination)
        {
            using (var stream = File.OpenRead(archive))
            using (var sha = SHA256.Create())
            {
                if (stream.Length != ReleaseSize ||
                    BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "") != ReleaseSha256)
                    throw new InvalidDataException("GitHub 安装资产 SHA-256 校验失败。");
                stream.Position = 0;
                using (var zip = new ZipArchive(stream, ZipArchiveMode.Read))
                {
                    var entry = zip.GetEntry("Mods/" + ModFile);
                    if (entry == null) throw new InvalidDataException("GitHub 安装资产内缺少换皮 DLL。");
                    using (var input = entry.Open())
                    using (var output = File.Create(destination))
                        input.CopyTo(output);
                }
            }
            if (!IsModAssembly(destination))
                throw new InvalidDataException("GitHub 安装资产内的换皮 DLL 无法识别。");
        }

        private static void RunInstaller(string script, string gameRoot)
        {
            var start = new ProcessStartInfo
            {
                FileName = Path.Combine(Environment.SystemDirectory, "WindowsPowerShell", "v1.0", "powershell.exe"),
                Arguments = "-NoProfile -NonInteractive -ExecutionPolicy Bypass -File " + QuotePath(script) +
                    " -GameDirectory " + QuotePath(gameRoot),
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };
            start.EnvironmentVariables["PSModulePath"] = "";
            start.EnvironmentVariables["ANNIHILATED_INSTALL_OUTPUT_UTF8"] = "1";
            using (var process = Process.Start(start))
            {
                var output = process.StandardOutput.ReadToEndAsync();
                var errors = process.StandardError.ReadToEndAsync();
                process.WaitForExit();
                if (process.ExitCode != 0)
                {
                    var lines = (output.Result + "\n" + errors.Result).Split(new[] { '\r', '\n' },
                        StringSplitOptions.RemoveEmptyEntries);
                    throw new InvalidOperationException(lines.LastOrDefault(line => line.StartsWith("安装失败")) ??
                        lines.LastOrDefault() ?? "换皮安装程序运行失败。");
                }
            }
        }

        private static string QuotePath(string path)
        {
            return "\"" + path + new string('\\', path.Length - path.TrimEnd('\\').Length) + "\"";
        }
    }
}
