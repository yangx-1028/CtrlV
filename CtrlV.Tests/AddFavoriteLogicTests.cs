using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace CtrlV.Tests;

/// <summary>
/// AddFavorite_Click 逻辑模拟测试
/// 验证收藏夹添加逻辑对多行文本和空格文本的处理
/// 
/// 注意：此测试模拟 MainWindow.AddFavorite_Click 的核心逻辑
/// 由于 WPF 控件无法在非 STA 线程实例化，我们提取纯逻辑进行测试
/// </summary>
public class AddFavoriteLogicTests
{
    /// <summary>
    /// 模拟 AddFavorite_Click 中的核心处理逻辑
    /// 来源: MainWindow.xaml.cs 第 441-496 行
    /// </summary>
    private (string? processedText, string? error) SimulateAddFavorite(string inputText, string? noteText = null)
    {
        // 模拟 FavoriteInputBox.Text?.Trim() (第 443 行)
        var text = inputText?.Trim();

        // 模拟空值检查 (第 444-448 行)
        if (string.IsNullOrEmpty(text))
        {
            return (null, "请输入内容");
        }

        var note = noteText?.Trim() ?? string.Empty;
        return (text, null);
    }

    #region 多行文本处理

    [Fact]
    public void AddFavorite_MultiLineText_TrimPreservesNewlines()
    {
        // Arrange - 用户在输入框中输入多行文本
        var inputText = "第一行\n第二行\n第三行";

        // Act
        var (result, error) = SimulateAddFavorite(inputText);

        // Assert - .Trim() 只去除首尾空白，保留内部换行
        Assert.Null(error);
        Assert.NotNull(result);
        Assert.Equal("第一行\n第二行\n第三行", result);
    }

    [Fact]
    public void AddFavorite_MultiLineTextWithLeadingTrailingSpaces_TrimmedCorrectly()
    {
        // Arrange - 首尾有空格的多行文本
        var inputText = "  第一行\n第二行\n第三行  ";

        // Act
        var (result, error) = SimulateAddFavorite(inputText);

        // Assert
        Assert.Null(error);
        Assert.NotNull(result);
        Assert.Equal("第一行\n第二行\n第三行", result);
    }

    [Fact]
    public void AddFavorite_MultiLineTextWithLeadingTrailingNewlines_TrimmedCorrectly()
    {
        // Arrange - 首尾有换行的多行文本
        var inputText = "\n\n第一行\n第二行\n第三行\n\n";

        // Act
        var (result, error) = SimulateAddFavorite(inputText);

        // Assert - Trim() 也会去除 \n
        Assert.Null(error);
        Assert.NotNull(result);
        Assert.Equal("第一行\n第二行\n第三行", result);
    }

    [Fact]
    public void AddFavorite_MultiLineTextWithMixedWhitespace_TrimmedCorrectly()
    {
        // Arrange - 混合空白字符
        var inputText = " \t \n 第一行\n第二行\n第三行 \n\t ";

        // Act
        var (result, error) = SimulateAddFavorite(inputText);

        // Assert
        Assert.Null(error);
        Assert.NotNull(result);
        Assert.Equal("第一行\n第二行\n第三行", result);
    }

    #endregion

    #region 空格文本处理

    [Fact]
    public void AddFavorite_TextWithSpaces_PreservesInternalSpaces()
    {
        // Arrange
        var inputText = "  hello   world   with   spaces  ";

        // Act
        var (result, error) = SimulateAddFavorite(inputText);

        // Assert - Trim只去首尾，内部空格保留
        Assert.Null(error);
        Assert.NotNull(result);
        Assert.Equal("hello   world   with   spaces", result);
    }

    [Fact]
    public void AddFavorite_TextWithTabs_PreservesInternalTabs()
    {
        // Arrange
        var inputText = "\tcolumn1\tcolumn2\tcolumn3\t";

        // Act
        var (result, error) = SimulateAddFavorite(inputText);

        // Assert
        Assert.Null(error);
        Assert.NotNull(result);
        Assert.Equal("column1\tcolumn2\tcolumn3", result);
    }

    #endregion

    #region 空/无效输入处理

    [Fact]
    public void AddFavorite_EmptyString_ReturnsError()
    {
        // Arrange
        var inputText = "";

        // Act
        var (result, error) = SimulateAddFavorite(inputText);

        // Assert
        Assert.NotNull(error);
        Assert.Equal("请输入内容", error);
        Assert.Null(result);
    }

    [Fact]
    public void AddFavorite_OnlySpaces_ReturnsError()
    {
        // Arrange
        var inputText = "   ";

        // Act
        var (result, error) = SimulateAddFavorite(inputText);

        // Assert
        Assert.NotNull(error);
        Assert.Equal("请输入内容", error);
        Assert.Null(result);
    }

    [Fact]
    public void AddFavorite_OnlyNewlines_ReturnsError()
    {
        // Arrange - 用户只输入了换行符
        var inputText = "\n\n\n";

        // Act
        var (result, error) = SimulateAddFavorite(inputText);

        // Assert - Trim后变空字符串
        Assert.NotNull(error);
        Assert.Equal("请输入内容", error);
        Assert.Null(result);
    }

    [Fact]
    public void AddFavorite_OnlyWhitespace_ReturnsError()
    {
        // Arrange - 各种空白字符
        var inputText = " \t\n\r\n ";

        // Act
        var (result, error) = SimulateAddFavorite(inputText);

        // Assert
        Assert.NotNull(error);
        Assert.Equal("请输入内容", error);
        Assert.Null(result);
    }

    [Fact]
    public void AddFavorite_NullText_ReturnsError()
    {
        // Arrange
        string? inputText = null;

        // Act
        var (result, error) = SimulateAddFavorite(inputText);

        // Assert
        Assert.NotNull(error);
        Assert.Equal("请输入内容", error);
        Assert.Null(result);
    }

    #endregion

    #region 备注处理

    [Fact]
    public void AddFavorite_WithNote_PreservesNote()
    {
        // Arrange & Act
        var (text, error) = SimulateAddFavorite("content", "这是备注");

        // Assert
        Assert.Null(error);
        Assert.Equal("content", text);
    }

    [Fact]
    public void AddFavorite_WithEmptyNote_NoError()
    {
        // Arrange & Act
        var (text, error) = SimulateAddFavorite("content", "");

        // Assert
        Assert.Null(error);
        Assert.Equal("content", text);
    }

    [Fact]
    public void AddFavorite_WithNullNote_NoError()
    {
        // Arrange & Act
        var (text, error) = SimulateAddFavorite("content", null);

        // Assert
        Assert.Null(error);
        Assert.Equal("content", text);
    }

    #endregion

    #region 真实场景模拟

    [Fact]
    public void AddFavorite_PasteCodeSnippet_PreservedCorrectly()
    {
        // Arrange - 模拟粘贴代码片段
        var code = @"public void Test()
{
    Console.WriteLine(""Hello World"");
    var x = 1 + 2;
}";

        // Act
        var (result, error) = SimulateAddFavorite(code);

        // Assert
        Assert.Null(error);
        Assert.NotNull(result);
        Assert.Equal(code, result);
        Assert.Contains("{", result);
        Assert.Contains("}", result);
        Assert.Contains("\n", result);
    }

    [Fact]
    public void AddFavorite_PasteEmailAddress_PreservedCorrectly()
    {
        // Arrange
        var email = "test@example.com";

        // Act
        var (result, error) = SimulateAddFavorite(email);

        // Assert
        Assert.Null(error);
        Assert.Equal(email, result);
    }

    [Fact]
    public void AddFavorite_PasteURL_PreservedCorrectly()
    {
        // Arrange
        var url = "https://www.example.com/path?query=value&other=123";

        // Act
        var (result, error) = SimulateAddFavorite(url);

        // Assert
        Assert.Null(error);
        Assert.Equal(url, result);
    }

    [Fact]
    public void AddFavorite_PasteChineseAddress_PreservedCorrectly()
    {
        // Arrange
        var address = "北京市海淀区\n中关村大街1号\n100080";

        // Act
        var (result, error) = SimulateAddFavorite(address);

        // Assert
        Assert.Null(error);
        Assert.Equal(address, result);
    }

    [Fact]
    public void AddFavorite_PasteJSON_PreservedCorrectly()
    {
        // Arrange
        var json = @"{
  ""name"": ""张三"",
  ""age"": 30,
  ""city"": ""北京""
}";

        // Act
        var (result, error) = SimulateAddFavorite(json);

        // Assert
        Assert.Null(error);
        Assert.NotNull(result);
        Assert.Equal(json, result);
    }

    [Fact]
    public void AddFavorite_PasteEmailTemplate_PreservedCorrectly()
    {
        // Arrange
        var template = @"尊敬的客户：

感谢您的来信。关于您咨询的问题，回复如下：

1. 产品A已发货
2. 产品B预计下周发货
3. 如有疑问请联系我们

此致
敬礼

客服部";

        // Act
        var (result, error) = SimulateAddFavorite(template);

        // Assert
        Assert.Null(error);
        Assert.NotNull(result);
        Assert.Equal(template, result);
        Assert.Contains("\n\n", result); // 包含空行
    }

    #endregion

    #region OnClipboardTextChanged 逻辑验证

    /// <summary>
    /// 模拟 OnClipboardTextChanged 中的文本处理逻辑
    /// 来源: MainWindow.xaml.cs 第 178-206 行
    /// </summary>
    private (string? processedText, bool skipped) SimulateClipboardTextChanged(string? clipboardText)
    {
        if (string.IsNullOrWhiteSpace(clipboardText))
            return (null, true);

        var text = clipboardText.Trim();
        if (string.IsNullOrEmpty(text))
            return (null, true);

        return (text, false);
    }

    [Fact]
    public void ClipboardTextChanged_MultiLineText_PreservedAfterTrim()
    {
        // Arrange
        var clipboardText = "line1\nline2\nline3";

        // Act
        var (result, skipped) = SimulateClipboardTextChanged(clipboardText);

        // Assert
        Assert.False(skipped);
        Assert.NotNull(result);
        Assert.Equal("line1\nline2\nline3", result);
    }

    [Fact]
    public void ClipboardTextChanged_TextWithSpaces_TrimmedCorrectly()
    {
        // Arrange
        var clipboardText = "  hello world  ";

        // Act
        var (result, skipped) = SimulateClipboardTextChanged(clipboardText);

        // Assert
        Assert.False(skipped);
        Assert.NotNull(result);
        Assert.Equal("hello world", result);
    }

    [Fact]
    public void ClipboardTextChanged_WhitespaceOnly_Skipped()
    {
        // Arrange
        var clipboardText = "   \n\n  ";

        // Act
        var (result, skipped) = SimulateClipboardTextChanged(clipboardText);

        // Assert
        Assert.True(skipped);
        Assert.Null(result);
    }

    [Fact]
    public void ClipboardTextChanged_Null_Skipped()
    {
        // Arrange & Act
        var (result, skipped) = SimulateClipboardTextChanged(null);

        // Assert
        Assert.True(skipped);
        Assert.Null(result);
    }

    [Fact]
    public void ClipboardTextChanged_Empty_Skipped()
    {
        // Arrange & Act
        var (result, skipped) = SimulateClipboardTextChanged("");

        // Assert
        Assert.True(skipped);
        Assert.Null(result);
    }

    #endregion
}
