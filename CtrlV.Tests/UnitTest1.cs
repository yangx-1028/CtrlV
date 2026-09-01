using System.Text.Json;

namespace CtrlV.Tests;

/// <summary>
/// ClipItem 数据模型测试
/// </summary>
public class ClipItemTests
{
    [Fact]
    public void ClipItem_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var item = new CtrlV.Models.ClipItem();

        // Assert
        Assert.False(string.IsNullOrEmpty(item.Id));
        Assert.Equal(string.Empty, item.Content);
        Assert.False(item.IsPinned);
        Assert.Equal(string.Empty, item.Note);
        Assert.False(item.HasNote);
        Assert.False(item.IsMouseOver);
        Assert.False(item.IsFavoriteView);
    }

    [Fact]
    public void ClipItem_MultiLineContent_IsStoredCorrectly()
    {
        // Arrange
        var multiLineContent = "第一行\n第二行\n第三行";

        // Act
        var item = new CtrlV.Models.ClipItem
        {
            Content = multiLineContent,
            IsPinned = true
        };

        // Assert - 多行内容应完整保存
        Assert.Equal(multiLineContent, item.Content);
        Assert.Contains("\n", item.Content);
        Assert.Equal(3, item.Content.Split('\n').Length);
    }

    [Fact]
    public void ClipItem_ContentWithSpaces_IsStoredCorrectly()
    {
        // Arrange
        var contentWithSpaces = "  hello   world   with   spaces  ";

        // Act
        var item = new CtrlV.Models.ClipItem
        {
            Content = contentWithSpaces,
            IsPinned = true
        };

        // Assert - 含空格内容应完整保存（模型层不做Trim）
        Assert.Equal(contentWithSpaces, item.Content);
    }

    [Fact]
    public void ClipItem_MultiLineWithSpaces_IsStoredCorrectly()
    {
        // Arrange - 多行 + 带空格的复杂内容
        var complexContent = "  第一行前面有空格\n  第二行前面有空格  \n第三行没有\n  ";

        // Act
        var item = new CtrlV.Models.ClipItem
        {
            Content = complexContent,
            IsPinned = true
        };

        // Assert
        Assert.Equal(complexContent, item.Content);
        Assert.Contains("\n", item.Content);
    }

    [Fact]
    public void ClipItem_HasNote_ReturnsCorrectValue()
    {
        // Arrange
        var itemWithNote = new CtrlV.Models.ClipItem { Note = "test note" };
        var itemWithEmptyNote = new CtrlV.Models.ClipItem { Note = "" };
        var itemWithWhitespaceNote = new CtrlV.Models.ClipItem { Note = "   " };

        // Assert
        Assert.True(itemWithNote.HasNote);
        Assert.False(itemWithEmptyNote.HasNote);
        Assert.False(itemWithWhitespaceNote.HasNote);
    }

    [Fact]
    public void ClipItem_NoteDisplay_FormatsCorrectly()
    {
        // Arrange
        var itemWithNote = new CtrlV.Models.ClipItem { Note = "用于工作" };
        var itemWithoutNote = new CtrlV.Models.ClipItem { Note = "" };

        // Assert
        Assert.Equal("备注: 用于工作", itemWithNote.NoteDisplay);
        Assert.Equal("", itemWithoutNote.NoteDisplay);
    }

    [Fact]
    public void ClipItem_Serialization_MultiLineContent_PreservesNewlines()
    {
        // Arrange
        var multiLineContent = "line1\nline2\nline3";
        var item = new CtrlV.Models.ClipItem
        {
            Id = "test-id-1",
            Content = multiLineContent,
            IsPinned = true,
            Note = "multiline test"
        };

        // Act - JSON 序列化后反序列化
        var json = JsonSerializer.Serialize(item);
        var deserialized = JsonSerializer.Deserialize<CtrlV.Models.ClipItem>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(multiLineContent, deserialized.Content);
        Assert.Contains("\n", deserialized.Content);
        Assert.Equal(item.Id, deserialized.Id);
        Assert.True(deserialized.IsPinned);
        Assert.Equal(item.Note, deserialized.Note);
    }

    [Fact]
    public void ClipItem_Serialization_ContentWithSpaces_PreservesSpaces()
    {
        // Arrange
        var contentWithSpaces = "  hello   world  ";
        var item = new CtrlV.Models.ClipItem
        {
            Id = "test-id-2",
            Content = contentWithSpaces,
            IsPinned = true
        };

        // Act
        var json = JsonSerializer.Serialize(item);
        var deserialized = JsonSerializer.Deserialize<CtrlV.Models.ClipItem>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(contentWithSpaces, deserialized.Content);
    }

    [Fact]
    public void ClipItem_PropertyChanged_FiresCorrectly()
    {
        // Arrange
        var item = new CtrlV.Models.ClipItem();
        var contentChanged = false;
        var isPinnedChanged = false;
        var noteChanged = false;

        item.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(CtrlV.Models.ClipItem.Content)) contentChanged = true;
            if (e.PropertyName == nameof(CtrlV.Models.ClipItem.IsPinned)) isPinnedChanged = true;
            if (e.PropertyName == nameof(CtrlV.Models.ClipItem.Note)) noteChanged = true;
        };

        // Act
        item.Content = "new content";
        item.IsPinned = true;
        item.Note = "new note";

        // Assert
        Assert.True(contentChanged);
        Assert.True(isPinnedChanged);
        Assert.True(noteChanged);
    }

    [Theory]
    [InlineData("single line text", "single line text")]
    [InlineData("text with\nnewline", "text with\nnewline")]
    [InlineData("text with\r\nCRLF", "text with\r\nCRLF")]
    [InlineData("  leading spaces", "  leading spaces")]
    [InlineData("trailing spaces  ", "trailing spaces  ")]
    [InlineData("  both sides  ", "  both sides  ")]
    [InlineData("multi\n  line\n    with\n      spaces", "multi\n  line\n    with\n      spaces")]
    [InlineData("", "")]
    public void ClipItem_Content_VariousInputs_PreservesExactValue(string input, string expected)
    {
        // Arrange & Act
        var item = new CtrlV.Models.ClipItem { Content = input };

        // Assert
        Assert.Equal(expected, item.Content);
    }
}
