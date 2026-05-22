using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using CtrlV.Models;

namespace CtrlV.Services
{
    public static class DataStorage
    {
        private static readonly string AppDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CtrlV");
        private static readonly string DataFilePath = Path.Combine(AppDataDir, "history.json");

        public static List<ClipItem> Load()
        {
            try
            {
                if (!File.Exists(DataFilePath))
                    return new List<ClipItem>();

                var json = File.ReadAllText(DataFilePath);
                return JsonSerializer.Deserialize<List<ClipItem>>(json) ?? new List<ClipItem>();
            }
            catch
            {
                return new List<ClipItem>();
            }
        }

        public static void Save(List<ClipItem> items)
        {
            try
            {
                if (!Directory.Exists(AppDataDir))
                    Directory.CreateDirectory(AppDataDir);

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
                };
                var json = JsonSerializer.Serialize(items, options);
                File.WriteAllText(DataFilePath, json);
            }
            catch
            {
                // 保存失败时静默处理
            }
        }
    }
}