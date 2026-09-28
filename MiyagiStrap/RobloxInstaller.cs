using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace MiyagiStrap
{
    public static class RobloxInstaller
    {
        private const string CdnBase = "https://setup.rbxcdn.com";
        private const string VersionApi = "https://clientsettings.roblox.com/v2/client-version/WindowsPlayer";

        private static readonly HttpClient Http = new HttpClient();

        private static readonly string BaseDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MiyagiStrap");
        private static readonly string VersionsDir = Path.Combine(BaseDir, "Versions");

        private static readonly Dictionary<string, string> DirectoryMap = new Dictionary<string, string>
        {
            { "RobloxApp.zip", "" },
            { "shaders.zip", "shaders/" },
            { "ssl.zip", "ssl/" },
            { "WebView2.zip", "" },
            { "WebView2RuntimeInstaller.zip", "WebView2RuntimeInstaller/" },
            { "content-avatar.zip", "content/avatar/" },
            { "content-configs.zip", "content/configs/" },
            { "content-fonts.zip", "content/fonts/" },
            { "content-sky.zip", "content/sky/" },
            { "content-sounds.zip", "content/sounds/" },
            { "content-textures2.zip", "content/textures/" },
            { "content-models.zip", "content/models/" },
            { "content-qt_translations.zip", "content/qt_translations/" },
            { "content-api-docs.zip", "content/api_docs/" },
            { "content-textures3.zip", "PlatformContent/pc/textures/" },
            { "content-terrain.zip", "PlatformContent/pc/terrain/" },
            { "content-platform-fonts.zip", "PlatformContent/pc/fonts/" },
            { "extracontent-luapackages.zip", "ExtraContent/LuaPackages/" },
            { "extracontent-translations.zip", "ExtraContent/translations/" },
            { "extracontent-models.zip", "ExtraContent/models/" },
            { "extracontent-textures.zip", "ExtraContent/textures/" },
            { "extracontent-places.zip", "ExtraContent/places/" },
        };

        public static void RegisterProtocol(string exe = null)
        {
            exe = exe ?? Environment.ProcessPath;
            foreach (string scheme in new[] { "roblox-player", "roblox" })
            {
                using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Classes\" + scheme))
                {
                    key.SetValue("", "URL: " + scheme + " Protocol");
                    key.SetValue("URL Protocol", "");
                    using (var cmd = key.CreateSubKey(@"shell\open\command"))
                    {
                        cmd.SetValue("", "\"" + exe + "\" \"%1\"");
                    }
                }
            }
        }

        public static async Task<string> EnsureLatestAsync(IProgress<(string Text, int Percent)> progress)
        {
            progress.Report(("Verificando a versão mais recente...", 0));

            string json = await Http.GetStringAsync(VersionApi);
            string version;
            using (var doc = JsonDocument.Parse(json))
            {
                version = doc.RootElement.GetProperty("clientVersionUpload").GetString();
            }

            string dir = Path.Combine(VersionsDir, version);
            string exe = Path.Combine(dir, "RobloxPlayerBeta.exe");
            string marker = Path.Combine(dir, "AppSettings.xml");

            if (File.Exists(exe) && File.Exists(marker))
            {
                progress.Report(("Roblox já está atualizado", 100));
                return exe;
            }

            if (Directory.Exists(dir))
                Directory.Delete(dir, true);
            Directory.CreateDirectory(dir);

            string manifest = await Http.GetStringAsync(CdnBase + "/" + version + "-rbxPkgManifest.txt");
            string[] lines = manifest.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            var packages = new List<(string Name, string Md5)>();
            for (int i = 1; i + 3 < lines.Length; i += 4)
            {
                string name = lines[i].Trim();
                if (name.EndsWith(".zip"))
                    packages.Add((name, lines[i + 1].Trim()));
            }

            int done = 0;
            foreach (var package in packages)
            {
                progress.Report(("Baixando " + package.Name + "...", done * 100 / packages.Count));

                byte[] data = await Http.GetByteArrayAsync(CdnBase + "/" + version + "-" + package.Name);

                string sub = DirectoryMap.TryGetValue(package.Name, out string mapped) ? mapped : "";
                string target = Path.Combine(dir, sub.Replace('/', Path.DirectorySeparatorChar));
                string expectedMd5 = package.Md5.ToLowerInvariant();
                string packageName = package.Name;

                await Task.Run(() =>
                {
                    string hash = Convert.ToHexString(MD5.HashData(data)).ToLowerInvariant();
                    if (hash != expectedMd5)
                        throw new Exception("Arquivo corrompido: " + packageName);

                    Directory.CreateDirectory(target);
                    ExtractZip(data, target);
                });
                done++;
            }

            File.WriteAllText(marker,
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\r\n" +
                "<Settings>\r\n" +
                "\t<ContentFolder>content</ContentFolder>\r\n" +
                "\t<BaseUrl>http://www.roblox.com</BaseUrl>\r\n" +
                "</Settings>\r\n");

            await Task.Run(() =>
            {
                foreach (string old in Directory.GetDirectories(VersionsDir))
                {
                    if (old != dir)
                    {
                        try { Directory.Delete(old, true); } catch { }
                    }
                }
            });

            progress.Report(("Instalação concluída", 100));
            return exe;
        }

        private static void ExtractZip(byte[] data, string targetDir)
        {
            string root = Path.GetFullPath(targetDir);
            if (!root.EndsWith(Path.DirectorySeparatorChar.ToString()))
                root += Path.DirectorySeparatorChar;

            using (var ms = new MemoryStream(data))
            using (var zip = new ZipArchive(ms))
            {
                foreach (var entry in zip.Entries)
                {
                    string name = entry.FullName.Replace('\\', '/').TrimStart('/');
                    if (name.Length == 0)
                        continue;

                    string dest = Path.GetFullPath(Path.Combine(root, name.Replace('/', Path.DirectorySeparatorChar)));
                    if (!dest.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (name.EndsWith("/"))
                    {
                        Directory.CreateDirectory(dest);
                        continue;
                    }

                    Directory.CreateDirectory(Path.GetDirectoryName(dest));
                    entry.ExtractToFile(dest, true);
                }
            }
        }

        public static bool TryParseCustomFlags(string json, out Dictionary<string, object> flags, out string error)
        {
            flags = new Dictionary<string, object>();
            error = null;
            try
            {
                using (var doc = JsonDocument.Parse(json))
                {
                    var root = doc.RootElement;

                    if (root.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var prop in root.EnumerateObject())
                            flags[prop.Name] = NormalizeFlagValue(prop.Value);
                        return true;
                    }

                    if (root.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in root.EnumerateArray())
                        {
                            if (item.ValueKind != JsonValueKind.Object ||
                                !item.TryGetProperty("name", out var nameEl) ||
                                nameEl.ValueKind != JsonValueKind.String)
                            {
                                error = "Cada item da lista precisa ter um campo \"name\".";
                                return false;
                            }

                            if (item.TryGetProperty("enabled", out var enabledEl) &&
                                enabledEl.ValueKind == JsonValueKind.False)
                                continue;

                            string raw = "";
                            if (item.TryGetProperty("value", out var valueEl))
                                raw = valueEl.ValueKind == JsonValueKind.String ? valueEl.GetString() : valueEl.GetRawText();

                            string type = "";
                            if (item.TryGetProperty("type", out var typeEl) && typeEl.ValueKind == JsonValueKind.String)
                                type = typeEl.GetString().ToLowerInvariant();

                            string name = nameEl.GetString();
                            bool looksBool = raw.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                                             raw.Equals("false", StringComparison.OrdinalIgnoreCase);

                            if (type == "bool" || looksBool)
                                flags[name] = raw.Equals("true", StringComparison.OrdinalIgnoreCase);
                            else if (long.TryParse(raw, out long number))
                                flags[name] = number;
                            else
                                flags[name] = raw;
                        }
                        return true;
                    }

                    error = "O JSON precisa ser um objeto { ... } ou uma lista [ ... ].";
                    return false;
                }
            }
            catch (JsonException)
            {
                error = "JSON inválido, confere as vírgulas e aspas.";
                return false;
            }
        }

        private static object NormalizeFlagValue(JsonElement value)
        {
            if (value.ValueKind != JsonValueKind.String)
                return value.Clone();

            string raw = value.GetString();
            if (bool.TryParse(raw, out bool boolean))
                return boolean;
            if (long.TryParse(raw, out long integer))
                return integer;
            return raw;
        }

        public static string ApplyFastFlags(string exe, AppSettings settings, out int appliedCount)
        {
            string dir = Path.Combine(Path.GetDirectoryName(exe), "ClientSettings");
            string file = Path.Combine(dir, "ClientAppSettings.json");

            var flags = new Dictionary<string, object>();
            if (settings.FpsLimit > 0) flags["DFIntTaskSchedulerTargetFps"] = settings.FpsLimit;
            if (settings.Msaa > 0) flags["FIntDebugForceMSAASamples"] = settings.Msaa;
            if (settings.Renderer == "d3d11") flags["FFlagDebugGraphicsPreferD3D11"] = true;
            else if (settings.Renderer == "vulkan") flags["FFlagDebugGraphicsPreferVulkan"] = true;

            if (!string.IsNullOrWhiteSpace(settings.CustomFlags) &&
                TryParseCustomFlags(settings.CustomFlags, out var custom, out _))
            {
                foreach (var kv in custom)
                    flags[kv.Key] = kv.Value;
            }

            appliedCount = flags.Count;

            if (flags.Count == 0)
            {
                if (File.Exists(file)) File.Delete(file);
                return null;
            }

            Directory.CreateDirectory(dir);
            string json = JsonSerializer.Serialize(flags, new JsonSerializerOptions { WriteIndented = true });
            string tempFile = file + ".tmp";

            // Grava em um temporário e valida o JSON antes de substituir o arquivo ativo.
            File.WriteAllText(tempFile, json);
            using (var check = JsonDocument.Parse(File.ReadAllText(tempFile)))
            {
                if (check.RootElement.ValueKind != JsonValueKind.Object)
                    throw new InvalidDataException("O arquivo de flags gerado não é um objeto JSON.");
            }

            File.Move(tempFile, file, true);
            if (!File.Exists(file))
                throw new IOException("O arquivo de flags não foi criado.");

            return file;
        }

        public static void Launch(string exe, string uri)
        {
            var psi = new ProcessStartInfo(exe)
            {
                UseShellExecute = false,
                WorkingDirectory = Path.GetDirectoryName(exe)
            };
            psi.ArgumentList.Add(uri ?? "--app");
            Process.Start(psi);
        }
    }
}
