using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace CtrlV
{
    public partial class SettingsWindow : Window
    {
        private readonly Services.AppSettings _settings;

        // 常用按键列表
        private readonly Dictionary<string, uint> _keyMap = new Dictionary<string, uint>
        {
            {"A", 0x41}, {"B", 0x42}, {"C", 0x43}, {"D", 0x44}, {"E", 0x45},
            {"F", 0x46}, {"G", 0x47}, {"H", 0x48}, {"I", 0x49}, {"J", 0x4A},
            {"K", 0x4B}, {"L", 0x4C}, {"M", 0x4D}, {"N", 0x4E}, {"O", 0x4F},
            {"P", 0x50}, {"Q", 0x51}, {"R", 0x52}, {"S", 0x53}, {"T", 0x54},
            {"U", 0x55}, {"V", 0x56}, {"W", 0x57}, {"X", 0x58}, {"Y", 0x59},
            {"Z", 0x5A},
            {"F1", 0x70}, {"F2", 0x71}, {"F3", 0x72}, {"F4", 0x73},
            {"F5", 0x74}, {"F6", 0x75}, {"F7", 0x76}, {"F8", 0x77},
            {"F9", 0x78}, {"F10", 0x79}, {"F11", 0x7A}, {"F12", 0x7B},
            {"Space", 0x20}, {"1", 0x31}, {"2", 0x32}, {"3", 0x33},
            {"4", 0x34}, {"5", 0x35}, {"6", 0x36}, {"7", 0x37},
            {"8", 0x38}, {"9", 0x39}, {"0", 0x30}
        };

        public SettingsWindow()
        {
            InitializeComponent();
            _settings = Services.SettingsManager.Load();
            MaxCountBox.Text = _settings.MaxHistoryCount.ToString();
            AutoStartCheckBox.IsChecked = _settings.AutoStart;

            // 初始化内存提醒设置
            MemoryAlertCheckBox.IsChecked = _settings.MemoryAlertEnabled;
            MemoryThresholdBox.Text = _settings.MemoryAlertThreshold.ToString();
            UpdateMemoryThresholdEnabled();

            // 初始化快捷键设置
            InitHotkeyControls();
        }

        private void MemoryAlertCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            UpdateMemoryThresholdEnabled();
        }

        private void UpdateMemoryThresholdEnabled()
        {
            MemoryThresholdBox.IsEnabled = MemoryAlertCheckBox.IsChecked == true;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // 窗口位置：贴近任务栏右下角
            PositionNearTaskbar();
        }

        private void PositionNearTaskbar()
        {
            var workArea = SystemParameters.WorkArea;
            Left = workArea.Right - Width - 8;
            Top = workArea.Bottom - Height - 8;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // 窗口关闭时不做特殊处理
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }

        private void CloseButton_Click(object sender, MouseButtonEventArgs e)
        {
            Close();
        }

        private void InitHotkeyControls()
        {
            // 填充按键下拉框
            foreach (var key in _keyMap.Keys)
            {
                KeyComboBox.Items.Add(key);
            }

            // 解析当前快捷键设置
            uint modifiers = (uint)_settings.HotkeyModifiers;
            uint keyVal = (uint)_settings.HotkeyKey;

            // 设置修饰键复选框
            CtrlCheckBox.IsChecked = (modifiers & 0x0002) != 0; // MOD_CONTROL
            AltCheckBox.IsChecked = (modifiers & 0x0001) != 0;   // MOD_ALT
            ShiftCheckBox.IsChecked = (modifiers & 0x0004) != 0; // MOD_SHIFT

            // 设置按键下拉框
            foreach (var kvp in _keyMap)
            {
                if (kvp.Value == keyVal)
                {
                    KeyComboBox.SelectedItem = kvp.Key;
                    break;
                }
            }

            // 如果没有选中任何按键，默认选择V
            if (KeyComboBox.SelectedIndex < 0)
                KeyComboBox.SelectedItem = "V";
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(MaxCountBox.Text, out int maxCount) && maxCount > 0)
            {
                _settings.MaxHistoryCount = maxCount;
            }
            _settings.AutoStart = AutoStartCheckBox.IsChecked == true;

            // 保存内存提醒设置（阈值限制在 10~99）
            _settings.MemoryAlertEnabled = MemoryAlertCheckBox.IsChecked == true;
            if (int.TryParse(MemoryThresholdBox.Text, out int threshold))
            {
                _settings.MemoryAlertThreshold = Math.Clamp(threshold, 10, 99);
            }
            else
            {
                _settings.MemoryAlertThreshold = 90;
            }

            // 保存快捷键设置
            SaveHotkeySettings();

            // 设置开机自启（通过注册表）
            SetAutoStart(_settings.AutoStart);

            Services.SettingsManager.Save(_settings);
            MessageBox.Show("设置已保存！快捷键将在下次启动时生效。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            Close();
        }

        private void SaveHotkeySettings()
        {
            uint modifiers = 0;
            if (CtrlCheckBox.IsChecked == true) modifiers |= 0x0002;  // MOD_CONTROL
            if (AltCheckBox.IsChecked == true) modifiers |= 0x0001;   // MOD_ALT
            if (ShiftCheckBox.IsChecked == true) modifiers |= 0x0004; // MOD_SHIFT

            // 至少需要一个修饰键
            if (modifiers == 0)
            {
                modifiers = 0x0002; // 默认 Ctrl
                CtrlCheckBox.IsChecked = true;
            }

            _settings.HotkeyModifiers = (int)modifiers;

            // 获取选中的按键
            if (KeyComboBox.SelectedItem is string selectedKey && _keyMap.ContainsKey(selectedKey))
            {
                _settings.HotkeyKey = (int)_keyMap[selectedKey];
            }
            else
            {
                _settings.HotkeyKey = 0x56; // 默认 V
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private static void SetAutoStart(bool enable)
        {
            try
            {
                var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                if (key == null) return;

                if (enable)
                {
                    // 使用 Environment.ProcessPath 获取可执行文件路径
                    // .NET 6+ 推荐方式，兼容单文件发布
                    var exePath = Environment.ProcessPath ?? System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                    if (!string.IsNullOrEmpty(exePath))
                    {
                        key.SetValue("CtrlV", $"\"{exePath}\"");
                    }
                }
                else
                {
                    key.DeleteValue("CtrlV", false);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"设置开机自启失败: {ex.Message}");
            }
        }
    }
}
