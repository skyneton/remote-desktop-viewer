using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace RemoteDeskopControlPannel.Utils
{
    static class DisplaySettings
    {
        [DllImport("user32.dll")]
        private static extern bool EnumDisplaySettings(string deviceName, int mode, ref DEVMODE dev);
        [DllImport("user32.dll")]
        private static extern int ChangeDisplaySettings(ref DEVMODE dev, int flags);
        [DllImport("user32.dll")]
        private static extern nint GetDC(nint hWnd);
        [DllImport("gdi32.dll")]
        private static extern int GetDeviceCaps(nint hdc, int idx);
        [DllImport("user32.dll")]
        private static extern int ReleaseDC(nint hWnd, nint hdcDst);

        [DllImport("user32.dll")]
        public static extern nint GetDesktopWindow();

        [DllImport("user32.dll")]
        public static extern bool PrintWindow(nint hwnd, nint hdc, int flags);

        [DllImport("gdi32.dll")]
        public static extern nint BitBlt(nint hdc, int x, int y, int width, int height, nint sdc, int sx, int sy, int rop);

        [StructLayout(LayoutKind.Sequential)]
        private struct DEVMODE
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string dmDeviceName;

            public short dmSpecVersion;
            public short dmDriverVersion;
            public short dmSize;
            public short dmDriverExtra;
            public int dmFields;
            public short dmOrientation;
            public short dmPaperSize;
            public short dmPaperLength;
            public short dmPaperWidth;
            public short dmScale;
            public short dmCopies;
            public short dmDefaultSource;
            public short dmPrintQuality;
            public short dmColor;
            public short dmDuplex;
            public short dmYResolution;
            public short dmTTOption;
            public short dmCollate;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string dmFormName;

            public short dmLogPixels;
            public int dmBitsPerPel;
            public int dmPelsWidth;
            public int dmPelsHeight;
            public int dmDisplayFlags;
            public int dmDisplayFrequency;
            public int dmICMMethod;
            public int dmICMIntent;
            public int dmMediaType;
            public int dmDitherType;
            public int dmReserved1;
            public int dmReserved2;
            public int dmPanningWidth;
            public int dmPanningHeight;
        }

        public struct ResolutionInfo
        {
            public int Width;
            public int Height;
            public float ScalingFactorX;
            public float ScalingFactorY;
        }

        private const int ENUM_CURRENT_SETTINGS = 1;
        private const int CDS_UPDATEREGISTRY = 0x01;
        private const int CDS_FULLSCREEN = 0x04;
        private const int LOGPIXELSX = 88;
        private const int LOGPIXELSY = 90;
        private const int HORZRES = 8;
        private const int VERTRES = 10;

        public static bool ChangeDisplayResolution(int width, int height)
        {
            var devMode = new DEVMODE();
            devMode.dmSize = (short)Marshal.SizeOf(devMode);

            if (DetectResolution(width, height, ref devMode))
            {
                return ChangeDisplaySettings(ref devMode, CDS_UPDATEREGISTRY) == 0;
            }
            return false;
        }

        private static bool DetectResolution(int width, int height, ref DEVMODE devMode)
        {
            var mode = new DEVMODE();
            mode.dmSize = (short)Marshal.SizeOf(mode);
            var area = width * height;
            var min = area;
            var minIdx = -1;
            for (var i = 0; EnumDisplaySettings(null, i, ref mode); i++)
            {
                if (mode.dmPelsWidth > width || mode.dmPelsHeight > height) continue;
                var a = mode.dmPelsWidth * mode.dmPelsHeight;
                var sub = area - a;
                if (min > sub)
                {
                    min = sub;
                    minIdx = i;
                }
            }
            if (minIdx < 0) return false;
            return EnumDisplaySettings(null, minIdx, ref devMode);
        }

        public static bool ChangeDisplayScaling(int scalingPercent)
        {
            var devMode = new DEVMODE();
            devMode.dmSize = (short)Marshal.SizeOf(devMode);

            if (EnumDisplaySettings(null, ENUM_CURRENT_SETTINGS, ref devMode))
            {
                devMode.dmFields |= 0x20000;
                devMode.dmDisplayFlags = scalingPercent == 100 ? 0 : 0x0001;

                return ChangeDisplaySettings(ref devMode, CDS_UPDATEREGISTRY) == 0;
            }
            return false;
        }

        public static void CopyFromScreen(Bitmap bitmap)
        {
            //var window = GetDesktopWindow();
            //var desktopDC = GetDC(window);
            var desktopDC = GetDC(nint.Zero);
            //var desktop = Worker.Desktop.GetDesktop();
            //var desktopDC = GetDC(desktop);

            using var g = Graphics.FromImage(bitmap);
            var hdc = g.GetHdc();

            BitBlt(hdc, 0, 0, bitmap.Width, bitmap.Height, desktopDC, 0, 0, (int)(CopyPixelOperation.SourceCopy | CopyPixelOperation.CaptureBlt));
            //PrintWindow(hdc, desktopDC, 0);

            //if(desktopDC != IntPtr.Zero) ReleaseDC(window, desktopDC);
            //if(desktopDC != IntPtr.Zero) ReleaseDC(desktop, desktopDC);
            if (desktopDC != nint.Zero) ReleaseDC(nint.Zero, desktopDC);
            g.ReleaseHdc(hdc);
        }

        public static Bitmap Screenshot(PixelFormat format)
        {
            var res = GetResolution();
            var bitmap = new Bitmap(res.Width, res.Height, format);
            CopyFromScreen(bitmap);
            return bitmap;
        }

        public static ResolutionInfo GetResolution()
        {
            var hdc = GetDC(nint.Zero);
            int dpiX = GetDeviceCaps(hdc, LOGPIXELSX);
            int dpiY = GetDeviceCaps(hdc, LOGPIXELSY);
            int width = GetDeviceCaps(hdc, HORZRES);
            int height = GetDeviceCaps(hdc, VERTRES);

            if (hdc != nint.Zero) ReleaseDC(nint.Zero, hdc);

            return new ResolutionInfo()
            {
                Width = width,
                Height = height,
                ScalingFactorX = dpiX / 96F,
                ScalingFactorY = dpiY / 96F,
            };
        }
    }
}
