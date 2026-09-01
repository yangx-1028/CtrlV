using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace CtrlV.Models
{
    public class ClipItem : INotifyPropertyChanged
    {
        private string _id = Guid.NewGuid().ToString();
        private string _content = string.Empty;
        private DateTime _timestamp = DateTime.Now;
        private bool _isPinned;
        private bool _isMouseOver;
        private bool _isFavoriteView;
        private string _note = string.Empty;
        private bool _isPrivacyMode;

        [JsonPropertyName("id")]
        public string Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }

        [JsonPropertyName("content")]
        public string Content
        {
            get => _content;
            set
            {
                _content = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayContent));
            }
        }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp
        {
            get => _timestamp;
            set { _timestamp = value; OnPropertyChanged(); OnPropertyChanged(nameof(TimeDisplay)); }
        }

        [JsonPropertyName("isPinned")]
        public bool IsPinned
        {
            get => _isPinned;
            set { _isPinned = value; OnPropertyChanged(); OnPropertyChanged(nameof(PinDisplay)); }
        }

        [JsonPropertyName("note")]
        public string Note
        {
            get => _note;
            set { _note = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasNote)); OnPropertyChanged(nameof(NoteDisplay)); }
        }

        /// <summary>
        /// 是否开启隐私模式（持久化到JSON）
        /// </summary>
        [JsonPropertyName("isPrivacyMode")]
        public bool IsPrivacyMode
        {
            get => _isPrivacyMode;
            set
            {
                _isPrivacyMode = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayContent));
                OnPropertyChanged(nameof(PrivacyToggleIcon));
            }
        }

        /// <summary>
        /// 隐私模式下的显示文本。
        /// 内容长度 ≤ 6 时直接显示；> 6 时保留前2位 + *** + 后2位。
        /// </summary>
        [JsonIgnore]
        public string DisplayContent
        {
            get
            {
                if (!IsPrivacyMode || string.IsNullOrEmpty(Content))
                    return Content ?? string.Empty;

                if (Content.Length <= 6)
                    return Content;

                return $"{Content[..2]}***{Content[^2..]}";
            }
        }

        /// <summary>
        /// 隐私模式切换按钮图标。
        /// 开启状态显示"👁‍🗨"，关闭状态显示"👁"。
        /// </summary>
        [JsonIgnore]
        public string PrivacyToggleIcon => IsPrivacyMode ? "👁" : "👁‍🗨";

        [JsonIgnore]
        public bool HasNote => !string.IsNullOrWhiteSpace(Note);

        [JsonIgnore]
        public string NoteDisplay => HasNote ? $"备注: {Note}" : "";

        [JsonIgnore]
        public bool IsMouseOver
        {
            get => _isMouseOver;
            set { _isMouseOver = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 是否在收藏夹视图中显示（用于控制按钮显示）
        /// </summary>
        [JsonIgnore]
        public bool IsFavoriteView
        {
            get => _isFavoriteView;
            set { _isFavoriteView = value; OnPropertyChanged(); }
        }

        [JsonIgnore]
        public string TimeDisplay
        {
            get
            {
                var span = DateTime.Now - Timestamp;
                if (span.TotalSeconds < 60) return "刚刚";
                if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes} 分钟前";
                if (span.TotalHours < 24) return $"{(int)span.TotalHours} 小时前";
                if (span.TotalDays < 7) return $"{(int)span.TotalDays} 天前";
                return Timestamp.ToString("MM-dd HH:mm");
            }
        }

        [JsonIgnore]
        public string PinDisplay => IsPinned ? "★" : "☆";

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}