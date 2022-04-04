using System;
using System.Runtime.InteropServices;

namespace RemoteDesktopViewer.Utils
{

    public class LowHelper
    {
        [DllImport("user32.dll")]
        public static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);

        [DllImport("user32.dll")]
        public static extern void keybd_event(byte vk, uint scan, int flags, uint extraInfo);

        [DllImport("user32.dll")]
        public static extern void SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        private static extern bool GetCursorInfo(out CursorInfo pci);

        [DllImport("user32.dll")]
        public static extern int LoadCursor(IntPtr hInstance, int hCursor);

        [DllImport("user32.dll")]
        public static extern int SetCursor(int hCursor);

        [DllImport("gdi32.dll")]
        public static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

        public enum DeviceCaps
        {
            DesktopVertres = 117,
            DesktopHorzres = 118,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CursorInfo
        {
            public Int32 cbSize;
            public Int32 flags;
            public IntPtr hCursor;
            public POINT ptScreenPos;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public Int32 x;
            public Int32 y;
        }

        public enum CursorType
        {
            None = 0,
            Arrow = 65539,
            IBeam = 65541,
            Wait = 65543,
            Cross = 65545,
            ScrollNW = 65549,
            ScrollWE = 65553,
            ScrollNE = 65551,
            ScrollW = 65553,
            ScrollNS = 65555,
            ScrollAll = 65557,
            No = 65559,
            Progress = 65561,
            Pointer = 65563,

            Grabbing = 13896596,
            Alias = 31327887,
            ColResize = 32770565,
            VerticalText = 38668561,
            ZoomIn = 62917193,
            Cell = 64882867,
            Grab = 69339379,
            RowResize = 85000401,
            Copy = 132646983,
            ZoomOut = 186320971
        }

        public enum MouseType
        {
            LeftButtonDown = 0x02,
            LeftButtonUp = 0x04,

            RightButtonDown = 0x08,
            RightButtonUp = 0x10,

            MiddleDown = 0x0020,
            MiddleUp = 0x01,

            XButtonDown = 0x80,
            XButtonUp = 0x100,

            Wheel = 0x0800,
        }

        public enum KeyType
        {
            KeyDown = 0,
            KeyUp = 0x02,
        }

        public static CursorInfo GetCursorInfo(out bool result)
        {
            CursorInfo pci;
            pci.cbSize = Marshal.SizeOf(typeof(CursorInfo));
            result = GetCursorInfo(out pci);
            return pci;
        }
    }
}