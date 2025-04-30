using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace RemoteDeskopControlPannel.ImageProcessing
{
    static class ImageCompress
    {
        public static byte[] PixelToImage(int width, int height, PixelFormat format, int pixelPer, byte[] data, ImageFormat imageFormat)
        {
            using var image = new Bitmap(width, height, format);
            var bitmapData = image.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, format);
            var stride = bitmapData.Stride;
            var ptr = bitmapData.Scan0;
            for (int y = 0; y < height; y++)
            {
                Marshal.Copy(data, y * stride, ptr, width * pixelPer);
                ptr += stride;
            }
            image.UnlockBits(bitmapData);
            var ms = new MemoryStream();
            image.Save(ms, imageFormat);

            return ms.ToArray();
        }

        public static byte[] PixelToImage(Bitmap image, ImageFormat imageFormat)
        {
            using var ms = new MemoryStream();
            image.Save(ms, imageFormat);

            return ms.ToArray();
        }

        public static Bitmap ArrayToBitmap(byte[] data)
        {
            using var ms = new MemoryStream(data);
            return new Bitmap(ms);
        }

        public static byte[] ToPixelArrray(Bitmap bitmap)
        {
            var pixelPer = Image.GetPixelFormatSize(bitmap.PixelFormat) >> 3;
            var array = new byte[bitmap.Width * bitmap.Height * pixelPer];
            var bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);
            var ptr = bitmapData.Scan0;
            var pixelWidth = bitmap.Width * pixelPer;
            for (int y = 0; y < bitmap.Height; y++)
            {
                Marshal.Copy(ptr, array, y * pixelWidth, y * pixelWidth + pixelWidth);
                ptr += bitmapData.Stride;
            }
            bitmap.UnlockBits(bitmapData);
            return array;
        }

        public static byte[] ArrayToPixelArray(byte[] data)
        {
            using var bitmap = ArrayToBitmap(data);
            return ToPixelArrray(bitmap);
        }

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
    }
}
