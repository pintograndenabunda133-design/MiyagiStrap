using System;
using System.IO;
using System.Text.Json;

namespace MiyagiStrap
{
    public class AppSettings
    {
        public bool PetalsEnabled { get; set; } = true;
        public int PetalCount { get; set; } = 16;
        public int FpsLimit { get; set; } = 0;
        public int Msaa { get; set; } = -1;
        public string Renderer { get; set; } = "auto";
        public string CustomFlags { get; set; } = "";

        private static string FilePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MiyagiStrap", "settings.json");

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(FilePath))
                    return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath)) ?? new AppSettings();
            }
            catch { }
            return new AppSettings();
        }

        public void Save()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}
