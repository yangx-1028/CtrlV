using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using CtrlV.Models;

namespace CtrlV.Services
{
    /// <summary>
    /// 数据持久化服务。
    /// 使用原子写入 + 备份机制确保收藏夹数据不会丢失。
    ///
    /// 根因分析：
    /// 原实现使用 File.WriteAllText 直接覆盖写入，该操作非原子性——
    /// 先将文件截断为 0 字节，再写入新内容。如果在截断后、写入完成前
    /// 发生进程崩溃、系统断电或 Windows 更新重启，文件将变成空文件或损坏。
    /// 下次启动时 Load() 解析失败返回空列表，ClearHistoryOnStartup() 再将
    /// 空列表写回文件，导致收藏夹数据永久丢失。
    ///
    /// 修复策略：
    /// 1. 原子写入：先写入临时文件，再用 File.Move(overwrite:true) 替换目标文件
    /// 2. 备份机制：每次保存前将现有文件复制为 .bak 备份
    /// 3. 容错加载：主文件损坏时自动尝试加载备份文件
    /// 4. 空数据保护：如果主文件和备份都无法加载，返回空列表但不覆盖现有文件
    /// </summary>
    public static class DataStorage
    {
        private static readonly string AppDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CtrlV");
        private static readonly string DataFilePath = Path.Combine(AppDataDir, "history.json");
        private static readonly string BackupFilePath = Path.Combine(AppDataDir, "history.json.bak");
        private static readonly string TempFilePath = Path.Combine(AppDataDir, "history.json.tmp");

        /// <summary>
        /// 加载数据。优先读取主文件，主文件损坏时自动回退到备份文件。
        /// </summary>
        public static List<ClipItem> Load()
        {
            // 尝试加载主文件
            var result = TryLoadFile(DataFilePath);
            if (result != null)
            {
                return result;
            }

            // 主文件加载失败，尝试备份文件
            Debug.WriteLine("[DataStorage] 主文件加载失败，尝试备份文件...");
            result = TryLoadFile(BackupFilePath);
            if (result != null)
            {
                Debug.WriteLine("[DataStorage] 备份文件加载成功，恢复主文件...");
                // 备份文件有效，恢复为主文件
                try
                {
                    File.Copy(BackupFilePath, DataFilePath, overwrite: true);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[DataStorage] 恢复主文件失败: {ex.Message}");
                }
                return result;
            }

            // 主文件和备份都失败，返回空列表
            Debug.WriteLine("[DataStorage] 主文件和备份文件均无法加载，返回空列表");
            return new List<ClipItem>();
        }

        /// <summary>
        /// 尝试从指定文件加载数据。
        /// </summary>
        /// <returns>加载成功返回数据列表，失败返回 null。</returns>
        private static List<ClipItem>? TryLoadFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return null;

                // 检查文件是否为空（可能是上次写入被截断）
                var fileInfo = new FileInfo(filePath);
                if (fileInfo.Length == 0)
                {
                    Debug.WriteLine($"[DataStorage] 文件为空: {filePath}");
                    return null;
                }

                var json = File.ReadAllText(filePath);

                // JSON 内容为空或仅空白
                if (string.IsNullOrWhiteSpace(json))
                {
                    Debug.WriteLine($"[DataStorage] 文件内容为空: {filePath}");
                    return null;
                }

                var items = JsonSerializer.Deserialize<List<ClipItem>>(json);
                if (items == null)
                {
                    Debug.WriteLine($"[DataStorage] 反序列化返回 null: {filePath}");
                    return null;
                }

                return items;
            }
            catch (JsonException ex)
            {
                // JSON 格式错误（文件损坏）
                Debug.WriteLine($"[DataStorage] JSON 解析失败 ({filePath}): {ex.Message}");
                return null;
            }
            catch (IOException ex)
            {
                // 文件被锁定或读取失败
                Debug.WriteLine($"[DataStorage] 文件读取失败 ({filePath}): {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DataStorage] 加载异常 ({filePath}): {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 保存数据。使用原子写入 + 备份机制。
        /// 
        /// 流程：
        /// 1. 将数据序列化为 JSON
        /// 2. 写入临时文件 (.tmp)
        /// 3. 将现有主文件复制为备份 (.bak)
        /// 4. 将临时文件原子移动为主文件（覆盖）
        /// 5. 清理临时文件
        /// </summary>
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

                // 步骤 1: 写入临时文件
                File.WriteAllText(TempFilePath, json);

                // 步骤 2: 备份现有主文件（如果存在）
                try
                {
                    if (File.Exists(DataFilePath))
                    {
                        File.Copy(DataFilePath, BackupFilePath, overwrite: true);
                    }
                }
                catch (Exception backupEx)
                {
                    // 备份失败不影响主保存流程
                    Debug.WriteLine($"[DataStorage] 备份文件创建失败: {backupEx.Message}");
                }

                // 步骤 3: 原子替换 —— 将临时文件移动为主文件
                // File.Move 在 .NET 8 中支持 overwrite 参数
                File.Move(TempFilePath, DataFilePath, overwrite: true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DataStorage] 保存失败: {ex.Message}");

                // 清理可能残留的临时文件
                try
                {
                    if (File.Exists(TempFilePath))
                        File.Delete(TempFilePath);
                }
                catch { }
            }
        }
    }
}
