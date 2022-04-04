using System.Drawing.Imaging;

namespace RemoteDesktopViewer.Utils
{
    public static class PixelFormatHelper
    {
        public static System.Windows.Media.PixelFormat ToWpfPixelFormat(PixelFormat format)
        {
            return format switch
            {
                PixelFormat.Format1bppIndexed => System.Windows.Media.PixelFormats.Indexed1,
                PixelFormat.Format4bppIndexed => System.Windows.Media.PixelFormats.Indexed2,
                PixelFormat.Format8bppIndexed => System.Windows.Media.PixelFormats.Indexed4,
                PixelFormat.Format16bppGrayScale => System.Windows.Media.PixelFormats.Gray16,
                PixelFormat.Format16bppRgb555 => System.Windows.Media.PixelFormats.Bgr555,
                PixelFormat.Format16bppRgb565 => System.Windows.Media.PixelFormats.Bgr565,
                PixelFormat.Format24bppRgb => System.Windows.Media.PixelFormats.Bgr24,
                PixelFormat.Format32bppRgb => System.Windows.Media.PixelFormats.Bgr32,
                PixelFormat.Format32bppArgb => System.Windows.Media.PixelFormats.Bgra32,
                PixelFormat.Format32bppPArgb => System.Windows.Media.PixelFormats.Pbgra32,
                PixelFormat.Format48bppRgb => System.Windows.Media.PixelFormats.Rgb48,
                PixelFormat.Format64bppArgb => System.Windows.Media.PixelFormats.Rgba64,
                PixelFormat.Format64bppPArgb => System.Windows.Media.PixelFormats.Prgba64,
                _ => System.Windows.Media.PixelFormats.Default
            };
        }

        public static int ToId(System.Windows.Media.PixelFormat format)
        {
            return format.ToString() switch
            {
                "Indexed1" => 0,
                "Indexed2" => 1,
                "Indexed4" => 2,
                "Gray16" => 3,
                "Bgr555" => 4,
                "Bgr565" => 5,
                "Bgr24" => 6,
                "Bgr32" => 7,
                "Bgra32" => 8,
                "Pbgra32" => 9,
                "Rgb48" => 10,
                "Rgba64" => 11,
                "Prgba64" => 12,
                _ => 13
            };
        }

        public static System.Windows.Media.PixelFormat ToPixelFormat(int id)
        {
            return id switch
            {
                0 => System.Windows.Media.PixelFormats.Indexed1,
                1 => System.Windows.Media.PixelFormats.Indexed2,
                2 => System.Windows.Media.PixelFormats.Indexed4,
                3 => System.Windows.Media.PixelFormats.Gray16,
                4 => System.Windows.Media.PixelFormats.Bgr555,
                5 => System.Windows.Media.PixelFormats.Bgr565,
                6 => System.Windows.Media.PixelFormats.Bgr24,
                7 => System.Windows.Media.PixelFormats.Bgr32,
                8 => System.Windows.Media.PixelFormats.Bgra32,
                9 => System.Windows.Media.PixelFormats.Pbgra32,
                10 => System.Windows.Media.PixelFormats.Rgb48,
                11 => System.Windows.Media.PixelFormats.Rgba64,
                12 => System.Windows.Media.PixelFormats.Prgba64,
                _ => System.Windows.Media.PixelFormats.Default
            };
        }
    }
}