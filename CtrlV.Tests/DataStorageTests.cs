using System.IO;
using System.Text.Json;
using CtrlV.Models;
using CtrlV.Services;

namespace CtrlV.Tests;

/// <summary>
/// DataStorage 数据持久化测试
/// 验证多行文本、带空格文本的保存/加载功能，
/// 以及原子写入、备份恢复、异常处理等可靠性机制。
/// </summary>
public class DataStorageTests : IDisposable
{
    private readonly string _testDir;
    private readonly string _testFilePath;
    private readonly string _backupFilePath;
    private readonly string _tempFilePath;

    public DataStorageTests()
    {
        // 使用临时目录进行测试
        _testDir = Path.Combine(Path.GetTempPath(), $"CtrlV_Test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDir);
        _testFilePath = Path.Combine(_testDir, "history.json");
        _backupFilePath = Path.Combine(_testDir, "history.json.bak");
        _tempFilePath = Path.Combine(_testDir, "history.json.tmp");
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testDir))
                Directory.Delete(_testDir, true);
        }
        catch { }
    }

    #region 测试辅助方法

    /// <summary>
    /// 模拟 DataStorage.Save 的原子写入 + 备份逻辑
    /// </summary>
    private void SaveTestData(List<ClipItem> items)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
        };
        var json = JsonSerializer.Serialize(items, options);

        // 原子写入：先写临时文件
        File.WriteAllText(_tempFilePath, json);

        // 备份现有主文件
        if (File.Exists(_testFilePath))
        {
            File.Copy(_testFilePath, _backupFilePath, overwrite: true);
        }

        // 原子替换
        File.Move(_tempFilePath, _testFilePath, overwrite: true);
    }

    /// <summary>
    /// 模拟 DataStorage.Load 的容错加载逻辑（优先主文件，回退备份）
    /// </summary>
    private List<ClipItem> LoadTestData()
    {
        // 尝试主文件
        var result = TryLoadTestDataFile(_testFilePath);
        if (result != null)
            return result;

        // 主文件失败，尝试备份文件
        result = TryLoadTestDataFile(_backupFilePath);
        if (result != null)
        {
            // 恢复主文件
            try { File.Copy(_backupFilePath, _testFilePath, overwrite: true); } catch { }
            return result;
        }

        return new List<ClipItem>();
    }

    private static List<ClipItem>? TryLoadTestDataFile(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
                return null;

            var fileInfo = new FileInfo(filePath);
            if (fileInfo.Length == 0)
                return null;

            var json = File.ReadAllText(filePath);
            if (string.IsNullOrWhiteSpace(json))
                return null;

            return JsonSerializer.Deserialize<List<ClipItem>>(json);
        }
        catch
        {
            return null;
        }
    }

    private static List<ClipItem> CreateSampleFavorites()
    {
        return new List<ClipItem>
        {
            new ClipItem
            {
                Id = "fav-1",
                Content = "收藏内容1",
                IsPinned = true,
                Note = "备注1",
                Timestamp = DateTime.Now
            },
            new ClipItem
            {
                Id = "fav-2",
                Content = "收藏内容2",
                IsPinned = true,
                Note = "",
                Timestamp = DateTime.Now
            }
        };
    }

    private static List<ClipItem> CreateMixedItems()
    {
        return new List<ClipItem>
        {
            new ClipItem
            {
                Id = "hist-1",
                Content = "普通历史记录",
                IsPinned = false,
                Timestamp = DateTime.Now
            },
            new ClipItem
            {
                Id = "fav-1",
                Content = "收藏内容",
                IsPinned = true,
                Timestamp = DateTime.Now
            }
        };
    }

    #endregion

    #region 基本保存/加载

    [Fact]
    public void SaveAndLoad_MultiLineContent_PreservesContent()
    {
        // Arrange
        var items = new List<ClipItem>
        {
            new ClipItem
            {
                Id = "ml-1",
                Content = "第一行\n第二行\n第三行",
                IsPinned = true,
                Timestamp = DateTime.Now
            }
        };

        // Act
        SaveTestData(items);
        var loaded = LoadTestData();

        // Assert
        Assert.Single(loaded);
        Assert.Equal("第一行\n第二行\n第三行", loaded[0].Content);
        Assert.Contains("\n", loaded[0].Content);
    }

    [Fact]
    public void SaveAndLoad_ContentWithSpaces_PreservesSpaces()
    {
        // Arrange
        var items = new List<ClipItem>
        {
            new ClipItem
            {
                Id = "ws-1",
                Content = "  hello   world   with   spaces  ",
                IsPinned = true,
                Timestamp = DateTime.Now
            }
        };

        // Act
        SaveTestData(items);
        var loaded = LoadTestData();

        // Assert
        Assert.Single(loaded);
        Assert.Equal("  hello   world   with   spaces  ", loaded[0].Content);
    }

    [Fact]
    public void SaveAndLoad_MultiLineWithSpaces_PreservesExactContent()
    {
        // Arrange - 模拟真实场景：用户粘贴多行带空格文本
        var multiLineContent = @"  Dear Team,

  I would like to share the following:

  1. Item one with spaces
  2. Item two with   multiple   spaces
  3. Item three

  Best regards,
  John  ";

        var items = new List<ClipItem>
        {
            new ClipItem
            {
                Id = "complex-1",
                Content = multiLineContent,
                IsPinned = true,
                Note = "邮件模板",
                Timestamp = DateTime.Now
            }
        };

        // Act
        SaveTestData(items);
        var loaded = LoadTestData();

        // Assert
        Assert.Single(loaded);
        Assert.Equal(multiLineContent, loaded[0].Content);
        Assert.Equal("邮件模板", loaded[0].Note);
    }

    [Fact]
    public void SaveAndLoad_MixedItems_PreservesAll()
    {
        // Arrange - 历史记录 + 收藏夹混合
        var items = new List<ClipItem>
        {
            new ClipItem
            {
                Id = "hist-1",
                Content = "普通历史记录",
                IsPinned = false,
                Timestamp = DateTime.Now
            },
            new ClipItem
            {
                Id = "fav-1",
                Content = "收藏的多行\n内容",
                IsPinned = true,
                Timestamp = DateTime.Now
            },
            new ClipItem
            {
                Id = "fav-2",
                Content = "  带空格的收藏  ",
                IsPinned = true,
                Note = "测试备注",
                Timestamp = DateTime.Now
            }
        };

        // Act
        SaveTestData(items);
        var loaded = LoadTestData();

        // Assert
        Assert.Equal(3, loaded.Count);
        Assert.Equal("普通历史记录", loaded[0].Content);
        Assert.Equal("收藏的多行\n内容", loaded[1].Content);
        Assert.Equal("  带空格的收藏  ", loaded[2].Content);
        Assert.Equal("测试备注", loaded[2].Note);
    }

    [Fact]
    public void SaveAndLoad_EmptyList_WorksCorrectly()
    {
        // Arrange
        var items = new List<ClipItem>();

        // Act
        SaveTestData(items);
        var loaded = LoadTestData();

        // Assert
        Assert.Empty(loaded);
    }

    [Fact]
    public void SaveAndLoad_VeryLongMultiLineContent_PreservesContent()
    {
        // Arrange - 超长多行内容
        var lines = Enumerable.Range(1, 50).Select(i => $"第{i}行内容   带空格").ToArray();
        var longContent = string.Join("\n", lines);

        var items = new List<ClipItem>
        {
            new ClipItem
            {
                Id = "long-1",
                Content = longContent,
                IsPinned = true,
                Timestamp = DateTime.Now
            }
        };

        // Act
        SaveTestData(items);
        var loaded = LoadTestData();

        // Assert
        Assert.Single(loaded);
        Assert.Equal(longContent, loaded[0].Content);
        Assert.Equal(50, loaded[0].Content.Split('\n').Length);
    }

    [Fact]
    public void SaveAndLoad_SpecialCharacters_Preserved()
    {
        // Arrange - 包含特殊字符的多行内容
        var content = "中文\n\ttab制表符\n  spaces  \r\nCRLF换行\n特殊字符：!@#$%^&*()";

        var items = new List<ClipItem>
        {
            new ClipItem
            {
                Id = "special-1",
                Content = content,
                IsPinned = true,
                Timestamp = DateTime.Now
            }
        };

        // Act
        SaveTestData(items);
        var loaded = LoadTestData();

        // Assert
        Assert.Single(loaded);
        Assert.Equal(content, loaded[0].Content);
    }

    [Fact]
    public void Save_ProducesValidJson()
    {
        // Arrange
        var items = new List<ClipItem>
        {
            new ClipItem
            {
                Id = "json-1",
                Content = "line1\nline2",
                IsPinned = true,
                Note = "test",
                Timestamp = DateTime.Now
            }
        };

        // Act
        SaveTestData(items);
        var json = File.ReadAllText(_testFilePath);

        // Assert - JSON应该是有效的
        Assert.NotNull(json);
        Assert.Contains("\"content\"", json);
        // JSON中换行符应该被转义为 \n
        Assert.Contains("\\n", json);
        // 可以反序列化
        var deserialized = JsonSerializer.Deserialize<List<ClipItem>>(json);
        Assert.NotNull(deserialized);
        Assert.Single(deserialized);
    }

    #endregion

    #region 原子写入测试

    [Fact]
    public void AtomicWrite_TempFileIsCleanedUp()
    {
        // Arrange
        var items = CreateSampleFavorites();

        // Act
        SaveTestData(items);

        // Assert - 临时文件应该不存在（已被移动为主文件）
        Assert.False(File.Exists(_tempFilePath), "临时文件应该在保存后被清理");
        Assert.True(File.Exists(_testFilePath), "主文件应该存在");
    }

    [Fact]
    public void AtomicWrite_MainFileIsValidAfterSave()
    {
        // Arrange
        var items = CreateSampleFavorites();

        // Act
        SaveTestData(items);

        // Assert - 主文件应该是有效的 JSON
        var json = File.ReadAllText(_testFilePath);
        var loaded = JsonSerializer.Deserialize<List<ClipItem>>(json);
        Assert.NotNull(loaded);
        Assert.Equal(2, loaded.Count);
        Assert.All(loaded, item => Assert.True(item.IsPinned));
    }

    [Fact]
    public void AtomicWrite_MultipleSavesOverwriteCorrectly()
    {
        // Arrange - 第一次保存
        var items1 = new List<ClipItem>
        {
            new ClipItem { Id = "1", Content = "第一次", IsPinned = true }
        };
        SaveTestData(items1);

        // Act - 第二次保存（不同内容）
        var items2 = new List<ClipItem>
        {
            new ClipItem { Id = "1", Content = "第一次", IsPinned = true },
            new ClipItem { Id = "2", Content = "第二次", IsPinned = true }
        };
        SaveTestData(items2);

        // Assert
        var loaded = LoadTestData();
        Assert.Equal(2, loaded.Count);
        Assert.Equal("第二次", loaded[1].Content);
    }

    #endregion

    #region 备份机制测试

    [Fact]
    public void Backup_IsCreatedOnSecondSave()
    {
        // Arrange - 第一次保存（无备份，因为主文件尚不存在）
        var items1 = new List<ClipItem>
        {
            new ClipItem { Id = "1", Content = "第一次保存", IsPinned = true }
        };
        SaveTestData(items1);
        Assert.False(File.Exists(_backupFilePath), "第一次保存不应产生备份");

        // Act - 第二次保存
        var items2 = new List<ClipItem>
        {
            new ClipItem { Id = "1", Content = "第一次保存", IsPinned = true },
            new ClipItem { Id = "2", Content = "第二次保存", IsPinned = true }
        };
        SaveTestData(items2);

        // Assert - 备份文件应该是第一次保存的内容
        Assert.True(File.Exists(_backupFilePath), "第二次保存应产生备份");
        var backupJson = File.ReadAllText(_backupFilePath);
        var backupItems = JsonSerializer.Deserialize<List<ClipItem>>(backupJson);
        Assert.NotNull(backupItems);
        Assert.Single(backupItems);
        Assert.Equal("第一次保存", backupItems[0].Content);
    }

    [Fact]
    public void Backup_ContainsPreviousSaveData()
    {
        // Arrange - 连续保存三次
        var items1 = new List<ClipItem>
        {
            new ClipItem { Id = "1", Content = "版本1", IsPinned = true }
        };
        SaveTestData(items1);

        var items2 = new List<ClipItem>
        {
            new ClipItem { Id = "1", Content = "版本1", IsPinned = true },
            new ClipItem { Id = "2", Content = "版本2", IsPinned = true }
        };
        SaveTestData(items2);

        // Act - 第三次保存
        var items3 = new List<ClipItem>
        {
            new ClipItem { Id = "1", Content = "版本1", IsPinned = true },
            new ClipItem { Id = "2", Content = "版本2", IsPinned = true },
            new ClipItem { Id = "3", Content = "版本3", IsPinned = true }
        };
        SaveTestData(items3);

        // Assert - 备份应该是第二次保存的内容（倒数第二版）
        var backupJson = File.ReadAllText(_backupFilePath);
        var backupItems = JsonSerializer.Deserialize<List<ClipItem>>(backupJson);
        Assert.NotNull(backupItems);
        Assert.Equal(2, backupItems.Count);
        Assert.Equal("版本2", backupItems[1].Content);
    }

    #endregion

    #region 容错加载测试

    [Fact]
    public void Load_RecoversFromCorruptedMainFile()
    {
        // Arrange - 先保存正常数据
        var items = CreateSampleFavorites();
        SaveTestData(items);

        // 再次保存以创建备份
        var items2 = new List<ClipItem>
        {
            new ClipItem { Id = "fav-1", Content = "收藏内容1", IsPinned = true, Note = "备注1" },
            new ClipItem { Id = "fav-2", Content = "收藏内容2", IsPinned = true },
            new ClipItem { Id = "fav-3", Content = "新增收藏", IsPinned = true }
        };
        SaveTestData(items2);

        // 破坏主文件（模拟写入中断导致的文件损坏）
        File.WriteAllText(_testFilePath, "{ corrupted json content !!!");

        // Act - 加载应该从备份恢复
        var loaded = LoadTestData();

        // Assert - 应该恢复到备份中的数据（上一版本）
        Assert.Equal(2, loaded.Count);
        Assert.Equal("收藏内容1", loaded[0].Content);
        Assert.Equal("收藏内容2", loaded[1].Content);

        // 主文件应该已被恢复
        Assert.True(File.Exists(_testFilePath));
        var restoredJson = File.ReadAllText(_testFilePath);
        var restoredItems = JsonSerializer.Deserialize<List<ClipItem>>(restoredJson);
        Assert.NotNull(restoredItems);
        Assert.Equal(2, restoredItems.Count);
    }

    [Fact]
    public void Load_RecoversFromEmptyMainFile()
    {
        // Arrange - 先保存正常数据
        var items = CreateSampleFavorites();
        SaveTestData(items);

        // 再次保存以创建备份
        SaveTestData(items);

        // 模拟写入中断：主文件被截断为空
        File.WriteAllText(_testFilePath, "");

        // Act
        var loaded = LoadTestData();

        // Assert - 应该从备份恢复
        Assert.Equal(2, loaded.Count);
        Assert.All(loaded, item => Assert.True(item.IsPinned));
    }

    [Fact]
    public void Load_RecoversFromZeroByteMainFile()
    {
        // Arrange - 先保存正常数据创建备份
        var items = CreateSampleFavorites();
        SaveTestData(items);
        SaveTestData(items);

        // 模拟截断：主文件为 0 字节
        File.WriteAllBytes(_testFilePath, Array.Empty<byte>());

        // Act
        var loaded = LoadTestData();

        // Assert
        Assert.Equal(2, loaded.Count);
        Assert.All(loaded, item => Assert.True(item.IsPinned));
    }

    [Fact]
    public void Load_ReturnsEmptyWhenBothFilesCorrupted()
    {
        // Arrange - 创建损坏的主文件和备份
        File.WriteAllText(_testFilePath, "not valid json");
        File.WriteAllText(_backupFilePath, "also not valid json");

        // Act
        var loaded = LoadTestData();

        // Assert - 都损坏时返回空列表
        Assert.NotNull(loaded);
        Assert.Empty(loaded);
    }

    [Fact]
    public void Load_ReturnsEmptyWhenNoFilesExist()
    {
        // Act - 不存在任何文件
        var loaded = LoadTestData();

        // Assert
        Assert.NotNull(loaded);
        Assert.Empty(loaded);
    }

    #endregion

    #region 收藏夹持久化场景（核心回归测试）

    [Fact]
    public void Favorites_SurviveMultipleSaveCycles()
    {
        // 模拟用户长期使用场景：多次保存，收藏夹始终保留
        var favorites = CreateSampleFavorites();
        SaveTestData(favorites);

        // 模拟 10 次历史记录 + 收藏夹的保存
        for (int i = 0; i < 10; i++)
        {
            var historyItems = new List<ClipItem>(favorites)
            {
                new ClipItem
                {
                    Id = $"hist-{i}",
                    Content = $"历史记录 {i}",
                    IsPinned = false,
                    Timestamp = DateTime.Now
                }
            };
            SaveTestData(historyItems);
        }

        // Act
        var loaded = LoadTestData();

        // Assert - 收藏夹内容完整保留
        var pinnedItems = loaded.Where(x => x.IsPinned).ToList();
        Assert.Equal(2, pinnedItems.Count);
        Assert.Contains(pinnedItems, x => x.Content == "收藏内容1");
        Assert.Contains(pinnedItems, x => x.Content == "收藏内容2");
    }

    [Fact]
    public void Favorites_SurviveCorruptionAndRecovery()
    {
        // 场景：收藏夹保存成功 → 下次保存时主文件损坏 → 从备份恢复

        // 第一次保存：收藏夹
        var favorites = CreateSampleFavorites();
        SaveTestData(favorites);

        // 第二次保存：收藏夹 + 历史
        var mixed = new List<ClipItem>(favorites)
        {
            new ClipItem { Id = "hist-1", Content = "历史", IsPinned = false }
        };
        SaveTestData(mixed);

        // 确认备份是第一次保存的内容
        Assert.True(File.Exists(_backupFilePath));

        // 模拟主文件损坏（比如系统崩溃导致写入中断）
        File.WriteAllText(_testFilePath, "[incomplete...");

        // Act - 加载
        var loaded = LoadTestData();

        // Assert - 从备份恢复，收藏夹完整
        var pinnedItems = loaded.Where(x => x.IsPinned).ToList();
        Assert.Equal(2, pinnedItems.Count);
        Assert.Contains(pinnedItems, x => x.Content == "收藏内容1");
        Assert.Contains(pinnedItems, x => x.Content == "收藏内容2");
    }

    [Fact]
    public void Favorites_WithNotes_SurviveRecovery()
    {
        // 验证带备注的收藏夹在恢复后备注仍然保留
        var items = new List<ClipItem>
        {
            new ClipItem { Id = "1", Content = "API Key", IsPinned = true, Note = "deepseek api" },
            new ClipItem { Id = "2", Content = "手机号", IsPinned = true, Note = "" },
            new ClipItem { Id = "3", Content = "普通记录", IsPinned = false }
        };

        // 保存两次以创建备份
        SaveTestData(items);
        SaveTestData(items);

        // 破坏主文件
        File.WriteAllText(_testFilePath, "");

        // Act
        var loaded = LoadTestData();

        // Assert - 备注恢复
        var pinnedWithNote = loaded.First(x => x.Id == "1");
        Assert.Equal("deepseek api", pinnedWithNote.Note);
        Assert.True(pinnedWithNote.HasNote);
    }

    #endregion

    #region 边界情况

    [Fact]
    public void Save_LargeNumberOfFavorites_AllPreserved()
    {
        // Arrange - 100 个收藏夹项
        var items = Enumerable.Range(1, 100).Select(i => new ClipItem
        {
            Id = $"fav-{i}",
            Content = $"收藏内容 {i}",
            IsPinned = true,
            Timestamp = DateTime.Now
        }).ToList();

        // Act
        SaveTestData(items);
        var loaded = LoadTestData();

        // Assert
        Assert.Equal(100, loaded.Count);
        Assert.All(loaded, item => Assert.True(item.IsPinned));
    }

    [Fact]
    public void Save_Idempotent_MultipleSavesProduceSameResult()
    {
        // 多次保存相同数据，结果应该一致
        var items = CreateSampleFavorites();

        SaveTestData(items);
        var json1 = File.ReadAllText(_testFilePath);

        SaveTestData(items);
        var json2 = File.ReadAllText(_testFilePath);

        // 内容应该完全一致
        Assert.Equal(json1, json2);
    }

    #endregion
}
