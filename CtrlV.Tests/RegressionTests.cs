using System.Text.Json;
using CtrlV.Models;

namespace CtrlV.Tests;

/// <summary>
/// 回归测试
/// 验证修复没有引入新问题，现有功能仍然正常
/// </summary>
public class RegressionTests
{
    #region ClipItem 基本功能回归

    [Fact]
    public void Regression_ClipItem_SingleLineContent_WorksCorrectly()
    {
        // Arrange - 单行文本（修复前就应正常工作）
        var item = new ClipItem
        {
            Content = "简单的单行文本",
            IsPinned = true
        };

        // Assert
        Assert.Equal("简单的单行文本", item.Content);
        Assert.True(item.IsPinned);
    }

    [Fact]
    public void Regression_ClipItem_ChineseContent_WorksCorrectly()
    {
        // Arrange
        var item = new ClipItem
        {
            Content = "中文内容测试：你好世界！",
            IsPinned = true
        };

        // Assert
        Assert.Equal("中文内容测试：你好世界！", item.Content);
    }

    [Fact]
    public void Regression_ClipItem_EnglishContent_WorksCorrectly()
    {
        // Arrange
        var item = new ClipItem
        {
            Content = "Hello World! This is a test.",
            IsPinned = true
        };

        // Assert
        Assert.Equal("Hello World! This is a test.", item.Content);
    }

    [Fact]
    public void Regression_ClipItem_MixedLanguageContent_WorksCorrectly()
    {
        // Arrange
        var item = new ClipItem
        {
            Content = "Hello 你好 World 世界",
            IsPinned = true
        };

        // Assert
        Assert.Equal("Hello 你好 World 世界", item.Content);
    }

    #endregion

    #region 数据持久化回归

    [Fact]
    public void Regression_JsonSerialization_SingleItem_WorksCorrectly()
    {
        // Arrange
        var item = new ClipItem
        {
            Id = "test-1",
            Content = "test content",
            IsPinned = false,
            Note = "",
            Timestamp = new DateTime(2024, 1, 1, 12, 0, 0)
        };

        // Act
        var json = JsonSerializer.Serialize(item);
        var deserialized = JsonSerializer.Deserialize<ClipItem>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(item.Id, deserialized.Id);
        Assert.Equal(item.Content, deserialized.Content);
        Assert.Equal(item.IsPinned, deserialized.IsPinned);
        Assert.Equal(item.Note, deserialized.Note);
    }

    [Fact]
    public void Regression_JsonSerialization_MultipleItems_WorksCorrectly()
    {
        // Arrange
        var items = new List<ClipItem>
        {
            new ClipItem { Id = "1", Content = "first", IsPinned = false },
            new ClipItem { Id = "2", Content = "second", IsPinned = true },
            new ClipItem { Id = "3", Content = "third", IsPinned = false }
        };

        // Act
        var json = JsonSerializer.Serialize(items);
        var deserialized = JsonSerializer.Deserialize<List<ClipItem>>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(3, deserialized.Count);
        Assert.Equal("first", deserialized[0].Content);
        Assert.Equal("second", deserialized[1].Content);
        Assert.Equal("third", deserialized[2].Content);
    }

    [Fact]
    public void Regression_JsonSerialization_Note_Preserved()
    {
        // Arrange
        var item = new ClipItem
        {
            Id = "note-test",
            Content = "content",
            IsPinned = true,
            Note = "这是一个备注"
        };

        // Act
        var json = JsonSerializer.Serialize(item);
        var deserialized = JsonSerializer.Deserialize<ClipItem>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal("这是一个备注", deserialized.Note);
    }

    #endregion

    #region 收藏夹功能回归

    [Fact]
    public void Regression_PinUnpin_WorksCorrectly()
    {
        // Arrange
        var item = new ClipItem { Content = "test", IsPinned = false };

        // Act - 收藏
        item.IsPinned = true;
        Assert.True(item.IsPinned);

        // Act - 取消收藏
        item.IsPinned = false;
        Assert.False(item.IsPinned);
    }

    [Fact]
    public void Regression_EditNote_WorksCorrectly()
    {
        // Arrange
        var item = new ClipItem { Content = "test", Note = "" };

        // Act - 添加备注
        item.Note = "新备注";
        Assert.Equal("新备注", item.Note);
        Assert.True(item.HasNote);
        Assert.Equal("备注: 新备注", item.NoteDisplay);

        // Act - 修改备注
        item.Note = "修改后的备注";
        Assert.Equal("修改后的备注", item.Note);

        // Act - 清空备注
        item.Note = "";
        Assert.Equal("", item.Note);
        Assert.False(item.HasNote);
    }

    [Fact]
    public void Regression_DeleteItem_WorksCorrectly()
    {
        // Arrange
        var items = new List<ClipItem>
        {
            new ClipItem { Id = "1", Content = "keep" },
            new ClipItem { Id = "2", Content = "delete" },
            new ClipItem { Id = "3", Content = "keep" }
        };

        // Act - 删除中间项
        var toDelete = items.First(x => x.Id == "2");
        items.Remove(toDelete);

        // Assert
        Assert.Equal(2, items.Count);
        Assert.DoesNotContain(items, x => x.Id == "2");
    }

    #endregion

    #region 时间显示回归

    [Fact]
    public void Regression_TimeDisplay_JustNow()
    {
        // Arrange
        var item = new ClipItem { Timestamp = DateTime.Now };

        // Assert
        Assert.Equal("刚刚", item.TimeDisplay);
    }

    [Fact]
    public void Regression_TimeDisplay_MinutesAgo()
    {
        // Arrange
        var item = new ClipItem { Timestamp = DateTime.Now.AddMinutes(-5) };

        // Assert
        Assert.Contains("分钟前", item.TimeDisplay);
    }

    [Fact]
    public void Regression_TimeDisplay_HoursAgo()
    {
        // Arrange
        var item = new ClipItem { Timestamp = DateTime.Now.AddHours(-2) };

        // Assert
        Assert.Contains("小时前", item.TimeDisplay);
    }

    #endregion

    #region PinDisplay 回归

    [Fact]
    public void Regression_PinDisplay_Pinned()
    {
        // Arrange
        var item = new ClipItem { IsPinned = true };

        // Assert
        Assert.Equal("★", item.PinDisplay);
    }

    [Fact]
    public void Regression_PinDisplay_Unpinned()
    {
        // Arrange
        var item = new ClipItem { IsPinned = false };

        // Assert
        Assert.Equal("☆", item.PinDisplay);
    }

    #endregion

    #region IsFavoriteView 回归

    [Fact]
    public void Regression_IsFavoriteView_ControlsVisibility()
    {
        // Arrange
        var item = new ClipItem { IsFavoriteView = false };
        Assert.False(item.IsFavoriteView);

        // Act
        item.IsFavoriteView = true;
        Assert.True(item.IsFavoriteView);
    }

    #endregion
}
