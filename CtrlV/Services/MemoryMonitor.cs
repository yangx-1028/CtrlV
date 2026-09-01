using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Threading;

namespace CtrlV.Services
{
    /// <summary>
    /// 轻量内存监控：系统内存占用超过阈值时托盘图标红色闪烁提醒。
    /// 设计原则（轻量化）：
    /// - 用 Win32 GlobalMemoryStatusEx 取内存占用（单次调用约1微秒，无性能计数器开销）
    /// - 图标启动时预生成3个并缓存，之后切换只是指针赋值，零内存分配
    /// - 功能未启用时定时器不启动，完全零开销
    /// </summary>
    public class MemoryMonitor : IDisposable
    {
        private const int CheckIntervalSeconds = 10; // 检测间隔
        private const int BlinkIntervalMs = 500;     // 闪烁切换间隔（亮红/暗红交替）
        private const int Hysteresis = 2;            // 滞回：恢复阈值 = 触发阈值 - 2，防边界抖动

        private readonly Icon _blueIcon;    // 正常态
        private readonly Icon _redIconA;    // 报警态 第1帧（亮红）
        private readonly Icon _redIconB;    // 报警态 第2帧（暗红）
        private readonly Action<Icon> _setIcon;
        private readonly Action<string, string, int> _showBalloon;

        private DispatcherTimer? _timer;     // 检测定时器（10秒）
        private DispatcherTimer? _blinkTimer; // 闪烁定时器（500ms，仅报警期间运行）
        private bool _alerting;
        private bool _alertFrame;   // 报警闪烁帧切换
        private bool _disposed;

        public bool IsEnabled { get; private set; }
        public int Threshold { get; private set; }

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

        [StructLayout(LayoutKind.Sequential)]
        private struct MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;       // 物理内存占用百分比（0~100）
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
        }

        /// <summary>
        /// 获取当前物理内存占用百分比，失败返回 -1
        /// </summary>
        public static int GetMemoryLoad()
        {
            try
            {
                var status = new MEMORYSTATUSEX();
                status.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
                if (GlobalMemoryStatusEx(ref status))
                    return (int)status.dwMemoryLoad;
            }
            catch
            {
            }
            return -1;
        }

        public MemoryMonitor(Action<Icon> setIcon, Action<string, string, int> showBalloon)
        {
            _setIcon = setIcon;
            _showBalloon = showBalloon;
            _blueIcon = CreateCircleIcon(74, 144, 217, 255);   // 与原蓝图标一致
            _redIconA = CreateCircleIcon(231, 76, 60, 255);    // 亮红
            _redIconB = CreateCircleIcon(231, 76, 60, 110);    // 暗红（半透明），闪烁用
        }

        /// <summary>启动监控（若已在运行则先停止再以新阈值启动）</summary>
        public void Start(int threshold)
        {
            Stop();
            Threshold = Math.Clamp(threshold, 10, 99);
            IsEnabled = true;
            _alerting = false;

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(CheckIntervalSeconds) };
            _timer.Tick += (s, e) => Check();
            _timer.Start();
            Check(); // 启动后立即检测一次，不等第一个周期
        }

        /// <summary>停止监控并恢复蓝图标</summary>
        public void Stop()
        {
            _timer?.Stop();
            _timer = null;
            IsEnabled = false;
            StopBlink();
            if (_alerting)
            {
                _alerting = false;
                SafeSetIcon(_blueIcon);
            }
        }

        private void Check()
        {
            int load = GetMemoryLoad();
            if (load < 0) return; // API 失败，跳过本次

            if (!_alerting)
            {
                if (load < Threshold) return;

                // 首次越过阈值：报警 + 弹一次气泡 + 启动闪烁
                _alerting = true;
                _alertFrame = false;
                SafeSetIcon(_redIconA);
                StartBlink();
                SafeBalloon("内存占用过高",
                    $"系统内存已占用 {load}%（阈值 {Threshold}%），请关闭部分程序。图标将红色闪烁提醒。");
            }
            else
            {
                // 滞回：降到阈值-2 以下才解除
                if (load < Threshold - Hysteresis)
                {
                    _alerting = false;
                    StopBlink();
                    SafeSetIcon(_blueIcon);
                }
            }
        }

        /// <summary>
        /// 闪烁用独立快定时器：每 500ms 亮红/暗红切换一次（1Hz 闪频，肉眼清晰可见）。
        /// 与 10 秒的检测定时器分离——检测只负责判断越阈/恢复，闪烁只负责闪。
        /// </summary>
        private void StartBlink()
        {
            if (_blinkTimer == null)
            {
                _blinkTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(BlinkIntervalMs) };
                _blinkTimer.Tick += (s, e) =>
                {
                    _alertFrame = !_alertFrame;
                    SafeSetIcon(_alertFrame ? _redIconA : _redIconB);
                };
            }
            if (!_blinkTimer.IsEnabled) _blinkTimer.Start();
        }

        private void StopBlink()
        {
            _blinkTimer?.Stop();
        }

        private void SafeSetIcon(Icon icon)
        {
            try { _setIcon(icon); } catch { }
        }

        private void SafeBalloon(string title, string text)
        {
            try { _showBalloon(title, text, 3000); } catch { }
        }

        /// <summary>
        /// 画一个带白色对勾的圆形图标（与 App 原蓝图标同款画法）
        /// </summary>
        public static Icon CreateCircleIcon(byte r, byte g, byte b, byte alpha)
        {
            using var bmp = new Bitmap(32, 32);
            using var gfx = Graphics.FromImage(bmp);
            gfx.SmoothingMode = SmoothingMode.AntiAlias;
            gfx.Clear(Color.Transparent);

            using var brush = new SolidBrush(Color.FromArgb(alpha, r, g, b));
            gfx.FillEllipse(brush, 1, 1, 30, 30);

            using var pen = new Pen(Color.White, 3f);
            pen.StartCap = LineCap.Round;
            pen.EndCap = LineCap.Round;
            gfx.DrawLines(pen, new[]
            {
                new PointF(8f, 10f),
                new PointF(16f, 22f),
                new PointF(24f, 10f)
            });

            IntPtr hIcon = bmp.GetHicon();
            var icon = Icon.FromHandle(hIcon);
            var result = (Icon)icon.Clone();
            icon.Dispose();
            return result;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Stop();
            _blueIcon.Dispose();
            _redIconA.Dispose();
            _redIconB.Dispose();
        }
    }
}
