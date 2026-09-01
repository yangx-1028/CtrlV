using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CtrlV.Services
{
    public class AppSettings
    {
        [JsonPropertyName("maxHistoryCount")]
        public int MaxHistoryCount { get; set; } = 200;

        [JsonPropertyName("autoStart")]
        public bool AutoStart { get; set; } = false;

        [JsonPropertyName("hotkeyModifiers")]
        public int HotkeyModifiers { get; set; } = 0x0002 | 0x0001; // Ctrl+Alt

        [JsonPropertyName("hotkeyKey")]
        public int HotkeyKey { get; set; } = 0x56; // V

        [JsonPropertyName("memoryAlertEnabled")]
        public bool MemoryAlertEnabled { get; set; } = false;

        [JsonPropertyName("memoryAlertThreshold")]
        public int MemoryAlertThreshold { get; set; } = 90;
    }

    public static class SettingsManager
    {
        private static readonly string AppDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CtrlV");
        private static readonly string SettingsFilePath = Path.Combine(AppDataDir, "settings.json");

        public static AppSettings Load()
        {
            try
            {
                if (!File.Exists(SettingsFilePath))
                    return new AppSettings();

                var json = File.ReadAllText(SettingsFilePath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
            catch
            {
                return new AppSettings();
            }
        }

        public static void Save(AppSettings settings)
        {
            try
            {
                if (!Directory.Exists(AppDataDir))
                    Directory.CreateDirectory(AppDataDir);

                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(settings, options);
                File.WriteAllText(SettingsFilePath, json);
            }
            catch
            {
                // 保存失败时静默处理
            }
        }
    }
}