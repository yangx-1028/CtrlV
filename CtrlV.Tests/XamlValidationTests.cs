using System.IO;
using System.Xml.Linq;

namespace CtrlV.Tests;

/// <summary>
/// XAML 静态验证测试
/// 验证 MainWindow.xaml 中收藏夹输入框的属性设置正确
/// </summary>
public class XamlValidationTests
{
    private readonly XDocument _xamlDoc;
    private readonly XNamespace _ns = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

    public XamlValidationTests()
    {
        var xamlPath = Path.Combine(
            Directory.GetCurrentDirectory(), "..", "..", "..", "..", "CtrlV", "MainWindow.xaml");

        // 如果相对路径找不到，尝试绝对路径
        if (!File.Exists(xamlPath))
        {
            xamlPath = @"D:\BaiduNetdiskWorkspace\AI\workbuddy\projects\wpf_ctrlv\CtrlV\MainWindow.xaml";
        }

        _xamlDoc = XDocument.Load(xamlPath);
    }

    private XElement? FindElement(string xName)
    {
        // 在整个文档中搜索具有 x:Name 属性的元素
        return _xamlDoc.Descendants()
            .FirstOrDefault(e =>
            {
                var nameAttr = e.Attribute(_ns + "Name") ?? e.Attribute(XNamespace.Get("http://schemas.microsoft.com/winfx/2006/xaml") + "Name");
                return nameAttr?.Value == xName;
            });
    }

    #region FavoriteInputBox 属性验证

    [Fact]
    public void FavoriteInputBox_HasAcceptsReturn_True()
    {
        // Arrange & Act
        var inputBox = FindElement("FavoriteInputBox");

        // Assert
        Assert.NotNull(inputBox);
        var acceptsReturn = inputBox.Attribute("AcceptsReturn");
        Assert.NotNull(acceptsReturn);
        Assert.Equal("True", acceptsReturn.Value);
    }

    [Fact]
    public void FavoriteInputBox_HasTextWrapping_Wrap()
    {
        // Arrange & Act
        var inputBox = FindElement("FavoriteInputBox");

        // Assert
        Assert.NotNull(inputBox);
        var textWrapping = inputBox.Attribute("TextWrapping");
        Assert.NotNull(textWrapping);
        Assert.Equal("Wrap", textWrapping.Value);
    }

    [Fact]
    public void FavoriteInputBox_HasMaxHeight_120()
    {
        // Arrange & Act
        var inputBox = FindElement("FavoriteInputBox");

        // Assert
        Assert.NotNull(inputBox);
        var maxHeight = inputBox.Attribute("MaxHeight");
        Assert.NotNull(maxHeight);
        Assert.Equal("120", maxHeight.Value);
    }

    [Fact]
    public void FavoriteInputBox_HasVerticalScrollBarVisibility_Auto()
    {
        // Arrange & Act
        var inputBox = FindElement("FavoriteInputBox");

        // Assert
        Assert.NotNull(inputBox);
        var scrollBar = inputBox.Attribute("VerticalScrollBarVisibility");
        Assert.NotNull(scrollBar);
        Assert.Equal("Auto", scrollBar.Value);
    }

    [Fact]
    public void FavoriteInputBox_HasVerticalContentAlignment_Top()
    {
        // Arrange & Act
        var inputBox = FindElement("FavoriteInputBox");

        // Assert
        Assert.NotNull(inputBox);
        var verticalAlignment = inputBox.Attribute("VerticalContentAlignment");
        Assert.NotNull(verticalAlignment);
        Assert.Equal("Top", verticalAlignment.Value);
    }

    [Fact]
    public void FavoriteInputBox_HasTransparentBackground()
    {
        // Arrange & Act
        var inputBox = FindElement("FavoriteInputBox");

        // Assert
        Assert.NotNull(inputBox);
        var background = inputBox.Attribute("Background");
        Assert.NotNull(background);
        Assert.Equal("Transparent", background.Value);
    }

    [Fact]
    public void FavoriteInputBox_HasZeroBorderThickness()
    {
        // Arrange & Act
        var inputBox = FindElement("FavoriteInputBox");

        // Assert
        Assert.NotNull(inputBox);
        var borderThickness = inputBox.Attribute("BorderThickness");
        Assert.NotNull(borderThickness);
        Assert.Equal("0", borderThickness.Value);
    }

    [Fact]
    public void FavoriteInputBox_HasTextChangedHandler()
    {
        // Arrange & Act
        var inputBox = FindElement("FavoriteInputBox");

        // Assert
        Assert.NotNull(inputBox);
        var textChanged = inputBox.Attribute("TextChanged");
        Assert.NotNull(textChanged);
        Assert.Equal("FavoriteInputBox_TextChanged", textChanged.Value);
    }

    #endregion

    #region PlaceholderText 属性验证

    [Fact]
    public void PlaceholderText_HasPadding_12_9()
    {
        // Arrange & Act
        var placeholder = FindElement("PlaceholderText");

        // Assert
        Assert.NotNull(placeholder);
        var padding = placeholder.Attribute("Padding");
        Assert.NotNull(padding);
        Assert.Equal("12,9", padding.Value);
    }

    [Fact]
    public void PlaceholderText_HasVerticalAlignment_Top()
    {
        // Arrange & Act
        var placeholder = FindElement("PlaceholderText");

        // Assert
        Assert.NotNull(placeholder);
        var verticalAlignment = placeholder.Attribute("VerticalAlignment");
        Assert.NotNull(verticalAlignment);
        Assert.Equal("Top", verticalAlignment.Value);
    }

    [Fact]
    public void PlaceholderText_IsHitTestVisible_False()
    {
        // Arrange & Act
        var placeholder = FindElement("PlaceholderText");

        // Assert
        Assert.NotNull(placeholder);
        var isHitTestVisible = placeholder.Attribute("IsHitTestVisible");
        Assert.NotNull(isHitTestVisible);
        Assert.Equal("False", isHitTestVisible.Value);
    }

    [Fact]
    public void PlaceholderText_HasCorrectText()
    {
        // Arrange & Act
        var placeholder = FindElement("PlaceholderText");

        // Assert
        Assert.NotNull(placeholder);
        // Text 属性值存储在 Text attribute 中，不是元素的 inner text
        var textAttr = placeholder.Attribute("Text");
        Assert.NotNull(textAttr);
        Assert.Equal("输入常用语，点击添加到收藏夹", textAttr.Value);
    }

    #endregion

    #region 收藏夹输入区域验证

    [Fact]
    public void FavoriteInputArea_InitiallyCollapsed()
    {
        // Arrange & Act
        var inputArea = FindElement("FavoriteInputArea");

        // Assert
        Assert.NotNull(inputArea);
        var visibility = inputArea.Attribute("Visibility");
        Assert.NotNull(visibility);
        Assert.Equal("Collapsed", visibility.Value);
    }

    [Fact]
    public void AddButton_HasClickHandler()
    {
        // Arrange - 查找添加按钮
        var buttons = _xamlDoc.Descendants(_ns + "Button");
        var addButton = buttons.FirstOrDefault(b => b.Attribute("Content")?.Value == "添加");

        // Assert
        Assert.NotNull(addButton);
        var click = addButton.Attribute("Click");
        Assert.NotNull(click);
        Assert.Equal("AddFavorite_Click", click.Value);
    }

    #endregion

    #region 完整属性集验证

    [Fact]
    public void FavoriteInputBox_HasAllMultilineProperties()
    {
        // 综合验证：所有多行支持属性都正确设置
        var inputBox = FindElement("FavoriteInputBox");
        Assert.NotNull(inputBox);

        // 核心多行支持属性
        Assert.Equal("True", inputBox.Attribute("AcceptsReturn")?.Value);
        Assert.Equal("Wrap", inputBox.Attribute("TextWrapping")?.Value);
        Assert.Equal("Auto", inputBox.Attribute("VerticalScrollBarVisibility")?.Value);

        // UI适配属性
        Assert.Equal("Top", inputBox.Attribute("VerticalContentAlignment")?.Value);
        Assert.Equal("120", inputBox.Attribute("MaxHeight")?.Value);
    }

    #endregion
}
