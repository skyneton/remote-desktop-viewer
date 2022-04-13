using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace RemoteDesktopViewer.Utils.Clipboard
{
    public class ClipboardManager
    {
        public const int WmClipboardUpdate = 0x031D;
        public const int WmDrawClipboard = 0x308;
        public const int WmChangeCbChain = 0x030D;
        
        private static readonly IntPtr WndProcSuccess = IntPtr.Zero;
        private readonly HwndSource _source;
        // private readonly IntPtr _nextClipboardViewer;

        private event Action _clipboardChanged;
        
        public ClipboardManager(Visual window)
        {
            _source = PresentationSource.FromVisual(window) as HwndSource;
            _source!.AddHook(WndProc);
            // handle = new WindowInteropHelper(window).Handle;

            LowHelper.AddClipboardFormatListener(_source.Handle);
            // _nextClipboardViewer = LowHelper.SetClipboardViewer(_source.Handle);
        }

        public void Close()
        {
            LowHelper.RemoveClipboardFormatListener(_source.Handle);
            // LowHelper.ChangeClipboardChain(_source.Handle, _nextClipboardViewer);
            _source.RemoveHook(WndProc);
        }

        public void AddCallback(Action action)
        {
            _clipboardChanged += action;
        } 

        private IntPtr WndProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WmClipboardUpdate)
            {
                _clipboardChanged?.Invoke();
                handled = true;
            }
            return WndProcSuccess;
        }
    }
}