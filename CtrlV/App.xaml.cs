using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using Application = System.Windows.Application;

namespace CtrlV
{
    public partial class App : Application
    {
        private MainWindow? _mainWindow;
        private NotifyIcon? _notifyIcon;
        private ContextMenuStrip? _contextMenu;
        private Services.MemoryMonitor? _memoryMonitor;

        [DllImport("shell32.dll", SetLastError = true)]
        private static extern bool Shell_NotifyIcon(uint dwMessage, ref NOTIFYICONDATA lpData);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct NOTIFYICONDATA
        {
            public int cbSize;
            public IntPtr hWnd;
            public uint uID;
            public uint uFlags;
            public uint uCallbackMessage;
            public IntPtr hIcon;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szTip;
            public uint dwState;
            public uint dwStateMask;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string szInfo;
            public uint uTimeoutOrVersion;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string szInfoTitle;
            public uint dwInfoFlags;
        }

        private const uint NIM_ADD = 0x00000000;
        private const uint NIM_MODIFY = 0x00000001;
        private const uint NIM_SETVERSION = 0x00000004;
        private const uint NOTIFYICON_VERSION_4 = 4;
        private const uint NIF_TIP = 0x00000004;
        private const uint NIF_ICON = 0x00000002;
        private const uint NIF_SHOWTIP = 0x00000080;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            CreateTrayIcon();
            InitMemoryMonitor();
            _mainWindow = new MainWindow();
            // 强制创建窗口句柄但不显示，确保热键在启动时就注册
            var helper = new System.Windows.Interop.WindowInteropHelper(_mainWindow);
            helper.EnsureHandle();
            _mainWindow.InitializeHotkey();
            // 确保启动时窗口隐藏
            _mainWindow.Visibility = System.Windows.Visibility.Collapsed;
            ShowFirstRunTip();
        }

        private void CreateTrayIcon()
        {
            _notifyIcon = new NotifyIcon();
            _notifyIcon.Icon = CreateBlueCircleIcon();
            _notifyIcon.Text = "YX剪贴板 - 剪贴板管理器";
            _notifyIcon.Visible = true;

            _notifyIcon.MouseClick += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    _contextMenu?.Close();
                    _mainWindow?.ToggleVisibility();
                }
            };

            _contextMenu = new ContextMenuStrip();
            _contextMenu.Items.Add("设置", null, (s, e) => ShowSettings());
            _contextMenu.Items.Add(new ToolStripSeparator());
            _contextMenu.Items.Add("退出", null, (s, e) => ExitApp());
            _notifyIcon.ContextMenuStrip = _contextMenu;

            SetNotifyIconVersion4();
        }

        private void SetNotifyIconVersion4()
        {
            try
            {
                var nid = new NOTIFYICONDATA();
                nid.cbSize = Marshal.SizeOf(typeof(NOTIFYICONDATA));
                nid.uID = 1;
                nid.uTimeoutOrVersion = NOTIFYICON_VERSION_4;
                Shell_NotifyIcon(NIM_SETVERSION, ref nid);
            }
            catch
            {
            }
        }

        private void ShowFirstRunTip()
        {
            var settingsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "CtrlV", "firstrun_shown.txt");

            if (!File.Exists(settingsPath))
            {
                try
                {
                    var dir = Path.GetDirectoryName(settingsPath)!;
                    if (!Directory.Exists(dir))
                        Directory.CreateDirectory(dir);
                    File.WriteAllText(settingsPath, DateTime.Now.ToString());

                    var timer = new System.Windows.Threading.DispatcherTimer
                    {
                        Interval = TimeSpan.FromSeconds(1)
                    };
                    timer.Tick += (s, args) =>
                    {
                        timer.Stop();
                        string hotkeyHint = GetHotkeyHint();
                        _notifyIcon?.ShowBalloonTip(3000, "YX剪贴板",
                            $"{hotkeyHint} 呼出剪贴板，拖动图标到任务栏可固定常驻", ToolTipIcon.Info);
                    };
                    timer.Start();
                }
                catch
                {
                }
            }
        }

        private string GetHotkeyHint()
        {
            var settings = Services.SettingsManager.Load();
            var modifiers = new System.Collections.Generic.List<string>();
            uint mod = (uint)settings.HotkeyModifiers;
            if ((mod & 0x0002) != 0) modifiers.Add("Ctrl");
            if ((mod & 0x0001) != 0) modifiers.Add("Alt");
            if ((mod & 0x0004) != 0) modifiers.Add("Shift");

            string keyName = "V";
            uint keyVal = (uint)settings.HotkeyKey;
            var keyNames = new System.Collections.Generic.Dictionary<uint, string>
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

            return string.Join("+", modifiers) + "+" + keyName;
        }

        private Icon CreateBlueCircleIcon()
        {
            // 复用 MemoryMonitor 的图标绘制（同款白对勾圆形，减少重复代码）
            return Services.MemoryMonitor.CreateCircleIcon(74, 144, 217, 255);
        }

        private void InitMemoryMonitor()
        {
            _memoryMonitor = new Services.MemoryMonitor(
                setIcon: icon => { if (_notifyIcon != null) _notifyIcon.Icon = icon; },
                showBalloon: (title, text, duration) =>
                    _notifyIcon?.ShowBalloonTip(duration, title, text, ToolTipIcon.Warning));

            // 只有功能启用时才启动定时器，未启用则完全零开销
            var settings = Services.SettingsManager.Load();
            if (settings.MemoryAlertEnabled)
            {
                _memoryMonitor.Start(settings.MemoryAlertThreshold);
            }
        }

        /// <summary>设置窗口关闭后调用：按最新设置启停内存监控</summary>
        private void ApplyMemoryAlertSettings()
        {
            if (_memoryMonitor == null) return;
            var settings = Services.SettingsManager.Load();
            if (settings.MemoryAlertEnabled)
                _memoryMonitor.Start(settings.MemoryAlertThreshold);
            else
                _memoryMonitor.Stop();
        }

        private void ShowSettings()
        {
            var settingsWin = new SettingsWindow();
            // 不设置 Owner，因为主窗口从未 Show 过
            settingsWin.ShowDialog();
            // 设置窗口关闭后，重新注册快捷键
            _mainWindow?.ReRegisterHotkey();
            // 按最新设置启停内存监控
            ApplyMemoryAlertSettings();
        }

        private void ExitApp()
        {
            try
            {
                // 释放顺序很重要：
                // 1. 先停监控定时器（闪烁定时器若在跑也一并停掉）
                _memoryMonitor?.Stop();
                // 2. 再释放 NotifyIcon（它 Dispose 内部还会访问当前 Icon，必须趁图标还活着先放它走）
                if (_notifyIcon != null)
                {
                    _notifyIcon.Visible = false;
                    _notifyIcon.Dispose();
                }
                // 3. 最后才销毁 MemoryMonitor 持有的图标资源
                _memoryMonitor?.Dispose();

                _mainWindow?.ForceClose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"退出清理异常: {ex.Message}");
            }
            finally
            {
                Shutdown();
            }
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            // 兜底：ExitApp 已正常清理时这里都是空操作；异常路径下也不能再抛
            try { _notifyIcon?.Dispose(); } catch { }
            try { _memoryMonitor?.Dispose(); } catch { }
        }
    }
}