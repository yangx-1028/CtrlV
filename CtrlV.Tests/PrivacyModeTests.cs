using System;
using System.Text.Json;
using CtrlV.Models;

namespace CtrlV.Tests
{
    /// <summary>
    /// 收藏夹隐私模式功能测试
    /// 验证隐私模式的显示效果、切换、持久化和复制功能
    /// </summary>
    public class PrivacyModeTests
    {
        #region 隐私模式显示效果测试

        [Theory]
        [InlineData("Hello", "Hello", false)] // 内容长度 <= 6，隐私模式关闭
        [InlineData("Hello", "Hello", true)]  // 内容长度 <= 6，隐私模式开启
        [InlineData("Hello World", "He***ld", true)] // 内容长度 > 6，隐私模式开启
        [InlineData("Hello World", "Hello World", false)] // 内容长度 > 6，隐私模式关闭
        [InlineData("", "", true)] // 空内容，隐私模式开启
        [InlineData("", "", false)] // 空内容，隐私模式关闭
        [InlineData("123456", "123456", true)] // 长度等于6，隐私模式开启
        [InlineData("1234567", "12***67", true)] // 长度等于7，隐私模式开启
        [InlineData("AB", "AB", true)] // 长度等于2，隐私模式开启
        [InlineData("A", "A", true)] // 长度等于1，隐私模式开启
        public void DisplayContent_PrivacyMode_HandlesVariousLengths(string content, string expected, bool isPrivacyMode)
        {
            // Arrange
            var clipItem = new ClipItem
            {
                Content = content,
                IsPrivacyMode = isPrivacyMode
            };

            // Act
            var result = clipItem.DisplayContent;

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void DisplayContent_NullContent_ReturnsEmptyString()
        {
            // Arrange
            var clipItem = new ClipItem
            {
                Content = null,
                IsPrivacyMode = true
            };

            // Act
            var result = clipItem.DisplayContent;

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void DisplayContent_ContentExactly6Chars_ReturnsOriginal()
        {
            // Arrange
            var clipItem = new ClipItem
            {
                Content = "123456",
                IsPrivacyMode = true
            };

            // Act
            var result = clipItem.DisplayContent;

            // Assert
            Assert.Equal("123456", result);
        }

        [Fact]
        public void DisplayContent_Content7Chars_ReturnsMasked()
        {
            // Arrange
            var clipItem = new ClipItem
            {
                Content = "1234567",
                IsPrivacyMode = true
            };

            // Act
            var result = clipItem.DisplayContent;

            // Assert
            Assert.Equal("12***67", result);
        }

        [Fact]
        public void DisplayContent_ContentWithSpaces_MasksCorrectly()
        {
            // Arrange
            var clipItem = new ClipItem
            {
                Content = "Hello World Test",
                IsPrivacyMode = true
            };

            // Act
            var result = clipItem.DisplayContent;

            // Assert
            Assert.Equal("He***st", result);
        }

        #endregion

        #region 隐私模式切换测试

        [Fact]
        public void IsPrivacyMode_ToggleProperty_NotifiesPropertyChanged()
        {
            // Arrange
            var clipItem = new ClipItem();
            var propertyChangedCount = 0;
            var changedProperties = new List<string>();
            clipItem.PropertyChanged += (sender, e) =>
            {
                propertyChangedCount++;
                changedProperties.Add(e.PropertyName);
            };

            // Act
            clipItem.IsPrivacyMode = true;

            // Assert
            Assert.True(clipItem.IsPrivacyMode);
            Assert.True(propertyChangedCount >= 1);
            Assert.Contains("IsPrivacyMode", changedProperties);
        }

        [Fact]
        public void IsPrivacyMode_Toggle_ChangesDisplayContent()
        {
            // Arrange
            var clipItem = new ClipItem
            {
                Content = "Hello World",
                IsPrivacyMode = false
            };
            Assert.Equal("Hello World", clipItem.DisplayContent);

            // Act
            clipItem.IsPrivacyMode = true;

            // Assert
            Assert.Equal("He***ld", clipItem.DisplayContent);
        }

        [Fact]
        public void PrivacyToggleIcon_WhenPrivacyModeEnabled_ReturnsEyeIcon()
        {
            // Arrange
            var clipItem = new ClipItem { IsPrivacyMode = true };

            // Act
            var icon = clipItem.PrivacyToggleIcon;

            // Assert
            Assert.Equal("👁", icon);
        }

        [Fact]
        public void PrivacyToggleIcon_WhenPrivacyModeDisabled_ReturnsEyeWithSlashIcon()
        {
            // Arrange
            var clipItem = new ClipItem { IsPrivacyMode = false };

            // Act
            var icon = clipItem.PrivacyToggleIcon;

            // Assert
            Assert.Equal("👁‍🗨", icon);
        }

        [Fact]
        public void IsPrivacyMode_SetValue_UpdatesPrivacyToggleIcon()
        {
            // Arrange
            var clipItem = new ClipItem { IsPrivacyMode = false };
            Assert.Equal("👁‍🗨", clipItem.PrivacyToggleIcon);

            // Act
            clipItem.IsPrivacyMode = true;

            // Assert
            Assert.Equal("👁", clipItem.PrivacyToggleIcon);
        }

        #endregion

        #region 隐私模式持久化测试

        [Fact]
        public void IsPrivacyMode_SerializeToJson_PreservesValue()
        {
            // Arrange
            var clipItem = new ClipItem
            {
                Id = "test-id",
                Content = "Test content",
                IsPrivacyMode = true,
                Timestamp = DateTime.Now
            };

            // Act
            var json = JsonSerializer.Serialize(clipItem);
            var deserialized = JsonSerializer.Deserialize<ClipItem>(json);

            // Assert
            Assert.NotNull(deserialized);
            Assert.True(deserialized.IsPrivacyMode);
            Assert.Equal(clipItem.Id, deserialized.Id);
            Assert.Equal(clipItem.Content, deserialized.Content);
        }

        [Fact]
        public void IsPrivacyMode_SerializeFalse_PreservesFalse()
        {
            // Arrange
            var clipItem = new ClipItem
            {
                Id = "test-id",
                Content = "Test content",
                IsPrivacyMode = false,
                Timestamp = DateTime.Now
            };

            // Act
            var json = JsonSerializer.Serialize(clipItem);
            var deserialized = JsonSerializer.Deserialize<ClipItem>(json);

            // Assert
            Assert.NotNull(deserialized);
            Assert.False(deserialized.IsPrivacyMode);
        }

        [Fact]
        public void IsPrivacyMode_MultipleItems_SerializeCorrectly()
        {
            // Arrange
            var items = new List<ClipItem>
            {
                new ClipItem { Id = "1", Content = "Content 1", IsPrivacyMode = true },
                new ClipItem { Id = "2", Content = "Content 2", IsPrivacyMode = false },
                new ClipItem { Id = "3", Content = "Content 3", IsPrivacyMode = true }
            };

            // Act
            var json = JsonSerializer.Serialize(items);
            var deserialized = JsonSerializer.Deserialize<List<ClipItem>>(json);

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal(3, deserialized.Count);
            Assert.True(deserialized[0].IsPrivacyMode);
            Assert.False(deserialized[1].IsPrivacyMode);
            Assert.True(deserialized[2].IsPrivacyMode);
        }

        #endregion

        #region 隐私模式不影响复制功能

        [Fact]
        public void Content_AlwaysReturnsFullContent_RegardlessOfPrivacyMode()
        {
            // Arrange
            var fullContent = "This is a secret password that should be copied";
            var clipItem = new ClipItem
            {
                Content = fullContent,
                IsPrivacyMode = true
            };

            // Act
            var contentToCopy = clipItem.Content;
            var displayContent = clipItem.DisplayContent;

            // Assert
            Assert.Equal(fullContent, contentToCopy);
            Assert.NotEqual(displayContent, contentToCopy);
            Assert.Equal("Th***ed", displayContent);
        }

        [Fact]
        public void Content_WhenPrivacyModeOn_ReturnsOriginalContent()
        {
            // Arrange
            var clipItem = new ClipItem
            {
                Content = "Hello World Test",
                IsPrivacyMode = true
            };

            // Act
            var result = clipItem.Content;

            // Assert
            Assert.Equal("Hello World Test", result);
        }

        #endregion

        #region UI交互测试

        [Fact]
        public void IsPrivacyMode_SetToTrue_NotifiesDisplayContentChanged()
        {
            // Arrange
            var clipItem = new ClipItem { Content = "Test Content" };
            var displayContentChanged = false;
            clipItem.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == "DisplayContent")
                    displayContentChanged = true;
            };

            // Act
            clipItem.IsPrivacyMode = true;

            // Assert
            Assert.True(displayContentChanged);
        }

        [Fact]
        public void IsPrivacyMode_SetToTrue_NotifiesPrivacyToggleIconChanged()
        {
            // Arrange
            var clipItem = new ClipItem();
            var privacyToggleIconChanged = false;
            clipItem.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == "PrivacyToggleIcon")
                    privacyToggleIconChanged = true;
            };

            // Act
            clipItem.IsPrivacyMode = true;

            // Assert
            Assert.True(privacyToggleIconChanged);
        }

        [Fact]
        public void IsPrivacyMode_ChangesMultipleProperties()
        {
            // Arrange
            var clipItem = new ClipItem();
            var changedProperties = new List<string>();
            clipItem.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName != null)
                    changedProperties.Add(e.PropertyName);
            };

            // Act
            clipItem.IsPrivacyMode = true;

            // Assert
            Assert.Contains("IsPrivacyMode", changedProperties);
            Assert.Contains("DisplayContent", changedProperties);
            Assert.Contains("PrivacyToggleIcon", changedProperties);
        }

        #endregion

        #region 边界条件测试

        [Theory]
        [InlineData("a", "a")] // 单字符
        [InlineData("ab", "ab")] // 两字符
        [InlineData("abc", "abc")] // 三字符
        [InlineData("abcd", "abcd")] // 四字符
        [InlineData("abcde", "abcde")] // 五字符
        [InlineData("abcdef", "abcdef")] // 六字符
        [InlineData("abcdefg", "ab***fg")] // 七字符
        public void DisplayContent_VariousLengths_MasksCorrectly(string content, string expected)
        {
            // Arrange
            var clipItem = new ClipItem
            {
                Content = content,
                IsPrivacyMode = true
            };

            // Act
            var result = clipItem.DisplayContent;

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void DisplayContent_WithSpecialCharacters_MasksCorrectly()
        {
            // Arrange
            var clipItem = new ClipItem
            {
                Content = "特殊字符!@#$%^&*()",
                IsPrivacyMode = true
            };

            // Act
            var result = clipItem.DisplayContent;

            // Assert
            Assert.Equal("特殊***()", result);
        }

        [Fact]
        public void DisplayContent_WithUnicodeCharacters_Length6_ReturnsOriginal()
        {
            // Arrange - "中文内容测试" 正好6个字符，应直接返回原文
            var clipItem = new ClipItem
            {
                Content = "中文内容测试",
                IsPrivacyMode = true
            };

            // Act
            var result = clipItem.DisplayContent;

            // Assert
            Assert.Equal("中文内容测试", result);
        }

        [Fact]
        public void DisplayContent_WithUnicodeCharacters_Length7_MasksCorrectly()
        {
            // Arrange - 7个中文字符应被遮盖: 前2 + *** + 后2
            var clipItem = new ClipItem
            {
                Content = "中文内容测试的",
                IsPrivacyMode = true
            };

            // Act
            var result = clipItem.DisplayContent;

            // Assert
            Assert.Equal("中文***试的", result);
        }

        #endregion

        #region 属性通知测试

        [Fact]
        public void DisplayContent_PropertyChanged_ShouldNotifyWhenContentChanges()
        {
            // BUG: Content setter only notifies "Content", but not "DisplayContent".
            // When Content changes and IsPrivacyMode is true, the UI won't update DisplayContent.
            // This test documents the expected behavior (should notify DisplayContent).
            // Source: ClipItem.cs Content setter needs OnPropertyChanged(nameof(DisplayContent))

            // Arrange
            var clipItem = new ClipItem { IsPrivacyMode = true };
            var displayContentChanged = false;
            clipItem.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == "DisplayContent")
                    displayContentChanged = true;
            };

            // Act
            clipItem.Content = "New content";

            // Assert - Currently FAILS because Content setter doesn't notify DisplayContent
            Assert.True(displayContentChanged);
        }

        [Fact]
        public void PrivacyToggleIcon_PropertyChanged_NotifiesWhenIsPrivacyModeChanges()
        {
            // Arrange
            var clipItem = new ClipItem();
            var privacyToggleIconChanged = false;
            clipItem.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == "PrivacyToggleIcon")
                    privacyToggleIconChanged = true;
            };

            // Act
            clipItem.IsPrivacyMode = true;

            // Assert
            Assert.True(privacyToggleIconChanged);
        }

        #endregion

        #region 集成测试

        [Fact]
        public void PrivacyMode_SerializeDeserialize_RoundTrip()
        {
            // Arrange
            var original = new ClipItem
            {
                Id = "test-123",
                Content = "Sensitive data here",
                IsPrivacyMode = true,
                IsPinned = true,
                Note = "Test note",
                Timestamp = new DateTime(2024, 1, 15, 10, 30, 0)
            };

            // Act
            var json = JsonSerializer.Serialize(original);
            var deserialized = JsonSerializer.Deserialize<ClipItem>(json);

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal(original.Id, deserialized.Id);
            Assert.Equal(original.Content, deserialized.Content);
            Assert.Equal(original.IsPrivacyMode, deserialized.IsPrivacyMode);
            Assert.Equal(original.IsPinned, deserialized.IsPinned);
            Assert.Equal(original.Note, deserialized.Note);
            Assert.Equal(original.Timestamp, deserialized.Timestamp);
            
            // 验证隐私模式显示效果在反序列化后仍然正常
            Assert.Equal("Se***re", deserialized.DisplayContent);
            Assert.Equal("👁", deserialized.PrivacyToggleIcon);
        }

        #endregion
    }
}