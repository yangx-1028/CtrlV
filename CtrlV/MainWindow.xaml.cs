using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using CtrlV.Models;
using CtrlV.Services;

namespace CtrlV
{
    public partial class MainWindow : Window
    {
        private readonly ClipboardHelper _clipboardHelper;
        private AppSettings _settings;
        private readonly ObservableCollection<ClipItem> _allItems;
        private bool _isShowingHistory = true;
        private bool _isInternalCopy;
        private DispatcherTimer _saveTimer;
        private bool _forceClose;

        // Win32 全局热键
        private const int WM_HOTKEY = 0x0312;
        private const int HOTKEY_ID = 9000;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_ALT = 0x0001;
        private const uint VK_V = 0x56;

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        public MainWindow()
        {
            InitializeComponent();

            _settings = SettingsManager.Load();
            _allItems = new ObservableCollection<ClipItem>(DataStorage.Load());
            _clipboardHelper = new ClipboardHelper();

            // 启动时清空历史记录（保留收藏夹）
            ClearHistoryOnStartup();

            // 初始化保存定时器（防抖）
            _saveTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            _saveTimer.Tick += (s, e) =>
            {
                _saveTimer.Stop();
                DataStorage.Save(_allItems.ToList());
            };

            // 初始化剪贴板监听
            _clipboardHelper.ClipboardTextChanged += OnClipboardTextChanged;
            _clipboardHelper.StartListening();

            // 初始显示
            RefreshList();
        }

        /// <summary>
        /// 启动时清空历史记录，保留收藏夹
        /// </summary>
        private void ClearHistoryOnStartup()
        {
            var pinnedItems = _allItems.Where(x => x.IsPinned).ToList();
            _allItems.Clear();
            foreach (var item in pinnedItems)
            {
                _allItems.Add(item);
            }
            DataStorage.Save(_allItems.ToList());
        }

        #region 全局热键

        private void RegisterGlobalHotkey()
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            // 使用设置中的快捷键
            uint modifiers = (uint)_settings.HotkeyModifiers;
            uint key = (uint)_settings.HotkeyKey;
            RegisterHotKey(hwnd, HOTKEY_ID, modifiers, key);
        }

        private void UnregisterGlobalHotkey()
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            UnregisterHotKey(hwnd, HOTKEY_ID);
        }

        /// <summary>
        /// 重新注册快捷键（设置更改后调用）
        /// </summary>
        public void ReRegisterHotkey()
        {
            UnregisterGlobalHotkey();
            // 重新加载设置
            var newSettings = SettingsManager.Load();
            _settings.HotkeyModifiers = newSettings.HotkeyModifiers;
            _settings.HotkeyKey = newSettings.HotkeyKey;
            RegisterGlobalHotkey();
            // 更新快捷键提示
            UpdateHotkeyHint();
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
            {
                ToggleVisibility();
                handled = true;
            }
            return IntPtr.Zero;
        }

        #endregion

        #region 窗口显隐

        public void ToggleVisibility()
        {
            if (IsVisible)
            {
                Hide();
            }
            else
            {
                ShowAndPosition();
            }
        }

        public void ShowAndPosition()
        {
            Dispatcher.Invoke(() =>
            {
                RefreshList();
                UpdateLayout();
            }, DispatcherPriority.Render);

            Hide();
            Topmost = true;
            Show();
            WindowState = WindowState.Normal;
            PositionWindowNearTaskbar();
            Activate();
            Focus();
        }

        private void PositionWindowNearTaskbar()
        {
            var workArea = SystemParameters.WorkArea;
            var windowWidth = Width;
            var windowHeight = Height;

            double left = workArea.Right - windowWidth - 8;
            double top = workArea.Bottom - windowHeight - 8;

            if (left < workArea.Left) left = workArea.Left;
            if (top < workArea.Top) top = workArea.Top;

            Left = left;
            Top = top;
        }

        #endregion

        #region 剪贴板监听

        private void OnClipboardTextChanged(string text)
        {
            if (_isInternalCopy) return;
            if (string.IsNullOrWhiteSpace(text)) return;

            text = text.Trim();
            if (string.IsNullOrEmpty(text)) return;

            Dispatcher.BeginInvoke(() =>
            {
                // 去重：检查是否已存在相同内容
                var existing = _allItems.FirstOrDefault(x => x.Content == text);
                if (existing != null)
                {
                    existing.Timestamp = DateTime.Now;
                    _allItems.Remove(existing);
                    _allItems.Insert(0, existing);
                }
                else
                {
                    var item = new ClipItem { Content = text, Timestamp = DateTime.Now };
                    _allItems.Insert(0, item);
                }

                TrimHistory();
                ScheduleSave();
                RefreshList();
            });
        }

        private void TrimHistory()
        {
            var nonPinned = _allItems.Where(x => !x.IsPinned).ToList();
            while (nonPinned.Count > _settings.MaxHistoryCount)
            {
                var oldest = nonPinned.Last();
                _allItems.Remove(oldest);
                nonPinned.Remove(oldest);
            }
        }

        private void ScheduleSave()
        {
            _saveTimer.Stop();
            _saveTimer.Start();
        }

        private void SaveImmediate()
        {
            _saveTimer.Stop();
            DataStorage.Save(_allItems.ToList());
        }

        #endregion

        #region 列表刷新

        private void RefreshList()
        {
            IEnumerable<ClipItem> items;

            if (_isShowingHistory)
            {
                // 历史记录：只显示非收藏的内容
                items = _allItems.Where(x => !x.IsPinned);
            }
            else
            {
                // 收藏夹：只显示收藏的内容
                items = _allItems.Where(x => x.IsPinned);
            }

            var list = items.ToList();

            // 设置 IsFavoriteView 标志
            foreach (var item in _allItems)
            {
                item.IsFavoriteView = !_isShowingHistory;
            }

            ClipListBox.ItemsSource = list;

            var totalCount = _isShowingHistory ? _allItems.Count(x => !x.IsPinned) : _allItems.Count(x => x.IsPinned);
            ItemCountText.Text = $"共 {totalCount} 条记录";

            EmptyHint.Visibility = list.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            EmptyHint.Text = _isShowingHistory
                ? "暂无剪贴板历史记录\n复制内容后将自动出现在这里"
                : "暂无收藏内容\n在历史记录中点击☆收藏常用内容";
        }

        #endregion

        #region 窗口事件

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Hide();
            // 注册全局热键（需要窗口句柄）
            var hwnd = new WindowInteropHelper(this).Handle;
            var source = HwndSource.FromHwnd(hwnd);
            source?.AddHook(WndProc);
            RegisterGlobalHotkey();
            // 更新快捷键提示
            UpdateHotkeyHint();
        }

        /// <summary>
        /// 更新底部快捷键提示文字
        /// </summary>
        private void UpdateHotkeyHint()
        {
            var modifiers = new List<string>();
            uint mod = (uint)_settings.HotkeyModifiers;
            if ((mod & 0x0002) != 0) modifiers.Add("Ctrl");
            if ((mod & 0x0001) != 0) modifiers.Add("Alt");
            if ((mod & 0x0004) != 0) modifiers.Add("Shift");

            // 查找按键名称
            string keyName = "V";
            uint keyVal = (uint)_settings.HotkeyKey;
            // 常用按键映射
            var keyNames = new Dictionary<uint, string>
            {
                {0x41, "A"}, {0x42, "B"}, {0x43, "C"}, {0x44, "D"}, {0x45, "E"},
                {0x46, "F"}, {0x47, "G"}, {0x48, "H"}, {0x49, "I"}, {0x4A, "J"},
                {0x4B, "K"}, {0x4C, "L"}, {0x4D, "M"}, {0x4E, "N"}, {0x4F, "O"},
                {0x50, "P"}, {0x51, "Q"}, {0x52, "R"}, {0x53, "S"}, {0x54, "T"},
                {0x55, "U"}, {0x56, "V"}, {0x57, "W"}, {0x58, "X"}, {0x59, "Y"},
                {0x5A, "Z"},
                {0x70, "F1"}, {0x71, "F2"}, {0x72, "F3"}, {0x73, "F4"},
                {0x74, "F5"}, {0x75, "F6"}, {0x76, "F7"}, {0x77, "F8"},
                {0x78, "F9"}, {0x79, "F10"}, {0x7A, "F11"}, {0x7B, "F12"},
                {0x20, "Space"}, {0x31, "1"}, {0x32, "2"}, {0x33, "3"},
                {0x34, "4"}, {0x35, "5"}, {0x36, "6"}, {0x37, "7"},
                {0x38, "8"}, {0x39, "9"}, {0x30, "0"}
            };
            if (keyNames.ContainsKey(keyVal))
                keyName = keyNames[keyVal];

            string hint = string.Join("+", modifiers) + "+" + keyName + " 呼出";
            HotkeyHintText.Text = hint;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (!_forceClose)
            {
                e.Cancel = true;
                Hide();
            }
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            if (IsVisible)
            {
                Hide();
            }
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                var source = e.OriginalSource as DependencyObject;
                if (source != null)
                {
                    // 检查是否点击在卡片或按钮上
                    var parent = source;
                    while (parent != null)
                    {
                        if (parent is Border border && border.Name == "CardBorder")
                            return;
                        if (parent is Button)
                            return;
                        if (parent is TextBox)
                            return;
                        parent = VisualTreeHelper.GetParent(parent);
                    }
                }
                DragMove();
            }
        }

        /// <summary>
        /// 用于从 App 调用的强制关闭
        /// </summary>
        public void ForceClose()
        {
            _forceClose = true;
            Close();
        }

        #endregion

        #region Tab 切换

        private void TabHistory_Click(object sender, MouseButtonEventArgs e)
        {
            _isShowingHistory = true;
            TabHistoryBorder.Background = FindResource("PrimaryBrush") as SolidColorBrush;
            TabHistoryText.Foreground = Brushes.White;
            TabFavoriteBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F0F0F0"));
            TabFavoriteText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333333"));
            FavoriteInputArea.Visibility = Visibility.Collapsed;
            RefreshList();
            e.Handled = true;
        }

        private void TabFavorite_Click(object sender, MouseButtonEventArgs e)
        {
            _isShowingHistory = false;
            TabFavoriteBorder.Background = FindResource("PrimaryBrush") as SolidColorBrush;
            TabFavoriteText.Foreground = Brushes.White;
            TabHistoryBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F0F0F0"));
            TabHistoryText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333333"));
            FavoriteInputArea.Visibility = Visibility.Visible;
            RefreshList();
            e.Handled = true;
        }

        #endregion

        #region 关闭按钮

        private void CloseButton_Click(object sender, MouseButtonEventArgs e)
        {
            Hide();
            e.Handled = true;
        }

        #endregion

        #region 收藏夹输入

        private void FavoriteInputBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            PlaceholderText.Visibility = string.IsNullOrEmpty(FavoriteInputBox.Text)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void AddFavorite_Click(object sender, RoutedEventArgs e)
        {
            var text = FavoriteInputBox.Text?.Trim();
            if (string.IsNullOrEmpty(text))
            {
                ShowToast("请输入内容");
                return;
            }

            // 防止剪贴板监听捕获
            _isInternalCopy = true;
            try
            {
                // 检查是否已存在
                var existing = _allItems.FirstOrDefault(x => x.Content == text);
                if (existing != null)
                {
                    if (!existing.IsPinned)
                    {
                        existing.IsPinned = true;
                    }
                    ShowToast("该内容已在收藏夹中");
                }
                else
                {
                    var item = new ClipItem
                    {
                        Content = text,
                        Timestamp = DateTime.Now,
                        IsPinned = true
                    };
                    _allItems.Insert(0, item);
                }

                FavoriteInputBox.Text = string.Empty;
                SaveImmediate();
                RefreshList();
                ShowToast("添加成功");
            }
            finally
            {
                Dispatcher.BeginInvoke(() =>
                {
                    _isInternalCopy = false;
                }, System.Windows.Threading.DispatcherPriority.Background);
            }
        }

        #endregion

        #region 卡片事件

        private void Card_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Border border && border.DataContext is ClipItem item)
            {
                item.IsMouseOver = true;
            }
        }

        private void Card_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Border border && border.DataContext is ClipItem item)
            {
                item.IsMouseOver = false;
            }
        }

        /// <summary>
        /// 点击卡片 -> 复制内容到剪贴板
        /// </summary>
        private void Card_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is ClipItem item)
            {
                _isInternalCopy = true;
                try
                {
                    Clipboard.SetText(item.Content);
                }
                catch
                {
                    // 剪贴板写入失败，静默处理
                }
                // 延迟重置标志，确保异步的 WM_CLIPBOARDUPDATE 消息先被处理
                Dispatcher.BeginInvoke(() =>
                {
                    _isInternalCopy = false;
                }, System.Windows.Threading.DispatcherPriority.Background);
                ShowToast("复制成功");
                e.Handled = true;
            }
        }

        /// <summary>
        /// 历史记录模式：收藏按钮
        /// </summary>
        private void PinButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement btn)
            {
                var clipItem = FindClipItemFromSender(btn);
                if (clipItem != null)
                {
                    clipItem.IsPinned = true;
                    SaveImmediate();
                    RefreshList();
                    ShowToast("收藏成功");
                }
            }
            e.Handled = true;
        }

        /// <summary>
        /// 收藏夹模式：取消收藏
        /// </summary>
        private void UnpinButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement btn)
            {
                var clipItem = FindClipItemFromSender(btn);
                if (clipItem != null)
                {
                    clipItem.IsPinned = false;
                    SaveImmediate();
                    RefreshList();
                    ShowToast("已取消收藏");
                }
            }
            e.Handled = true;
        }

        /// <summary>
        /// 收藏夹模式：删除
        /// </summary>
        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement btn)
            {
                var clipItem = FindClipItemFromSender(btn);
                if (clipItem != null)
                {
                    _allItems.Remove(clipItem);
                    SaveImmediate();
                    RefreshList();
                    ShowToast("已删除");
                }
            }
            e.Handled = true;
        }

        private ClipItem? FindClipItemFromSender(FrameworkElement element)
        {
            // 尝试从 DataContext 获取
            if (element.DataContext is ClipItem dc)
                return dc;

            // 向上查找 Border 的 DataContext
            var parent = VisualTreeHelper.GetParent(element);
            while (parent != null)
            {
                if (parent is FrameworkElement fe && fe.DataContext is ClipItem parentItem)
                    return parentItem;
                parent = VisualTreeHelper.GetParent(parent);
            }
            return null;
        }

        #endregion

        #region Toast 提示

        private DispatcherTimer? _toastTimer;

        private void ShowToast(string message)
        {
            ToastText.Text = message;
            ToastBorder.Opacity = 0;

            _toastTimer?.Stop();
            ToastBorder.BeginAnimation(OpacityProperty, null);

            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200));
            ToastBorder.BeginAnimation(OpacityProperty, fadeIn);

            _toastTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            _toastTimer.Tick += (s, args) =>
            {
                _toastTimer.Stop();
                var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(300));
                ToastBorder.BeginAnimation(OpacityProperty, fadeOut);
            };
            _toastTimer.Start();
        }

        #endregion

        protected override void OnClosed(EventArgs e)
        {
            _clipboardHelper?.Dispose();
            UnregisterGlobalHotkey();
            DataStorage.Save(_allItems.ToList());
            base.OnClosed(e);
        }
    }
}