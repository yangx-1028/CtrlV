using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace CtrlV.Services
{
    public class ClipboardHelper : IDisposable
    {
        private const int WM_CLIPBOARDUPDATE = 0x031D;
        private IntPtr _hwnd;
        private HwndSource? _hwndSource;
        private bool _disposed;

        public event Action<string>? ClipboardTextChanged;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool AddClipboardFormatListener(IntPtr hwnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

        public void StartListening()
        {
            // 创建一个不可见的消息窗口来接收剪贴板消息
            var parameters = new HwndSourceParameters("CtrlV_ClipboardListener")
            {
                Width = 0,
                Height = 0,
                WindowStyle = 0, // 不可见
            };
            _hwndSource = new HwndSource(parameters);
            _hwndSource.AddHook(WndProc);
            _hwnd = _hwndSource.Handle;
            AddClipboardFormatListener(_hwnd);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_CLIPBOARDUPDATE)
            {
                OnClipboardChanged();
                handled = true;
            }
            return IntPtr.Zero;
        }

        private void OnClipboardChanged()
        {
            try
            {
                if (Clipboard.ContainsText())
                {
                    var text = Clipboard.GetText();
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        ClipboardTextChanged?.Invoke(text);
                    }
                }
            }
            catch
            {
                // 剪贴板可能被其他进程锁定，忽略
            }
        }

        public static void CopyToClipboard(string text)
        {
            Clipboard.SetText(text);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                if (_hwnd != IntPtr.Zero)
                {
                    RemoveClipboardFormatListener(_hwnd);
                }
                _hwndSource?.Dispose();
                _disposed = true;
            }
        }
    }
}