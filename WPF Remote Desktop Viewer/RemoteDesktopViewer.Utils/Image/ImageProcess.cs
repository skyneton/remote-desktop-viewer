using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;

namespace RemoteDesktopViewer.Utils.Image
{
    public static class ImageProcess
    {
        /*
         * 0: 565       - PixelFormat = 16bpp
         * 1: 233       - PixelFormat = 24bpp
         * 2: Palette   - PixelFormat = 16bpp
         */
        public const byte ProcessType = 1;

        public static byte[] Compress(Bitmap bitmap, PixelFormat format) => ProcessType switch
        {
            0 => Image565.Compress(bitmap, format),
            1 => Image233.Compress(bitmap, format),
            2 => ImagePalette.Compress(bitmap, format)
        };
        
        public static byte[] Compress(Bitmap bitmap, ref byte[] before, PixelFormat format) => ProcessType switch
        {
            0 => Image565.Compress(bitmap, ref before, format),
            1 => Image233.Compress(bitmap, ref before, format),
            2 => ImagePalette.Compress(bitmap, ref before, format)
        };

        public static void DecompressChunk(WriteableBitmap bitmap, ByteBuf buf)
        {
            switch(ProcessType)
            {
                case 0:
                    bitmap.Dispatcher.Invoke(() => Image565.DecompressChunk(bitmap, buf));
                    break;
                case 1:
                    bitmap.Dispatcher.Invoke(() => Image233.DecompressChunk(bitmap, buf));
                    break;
                case 2:
                    ImagePalette.DecompressChunk(bitmap, buf);
                    break;
            };
        }


        public static byte[] GetPixels(Bitmap image, PixelFormat format)
        {
            var width = image.Width;
            var height = image.Height;
            var bitmapData = image.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, format);
            var pixelsPer = System.Drawing.Image.GetPixelFormatSize(bitmapData.PixelFormat) >> 3;

            width *= pixelsPer;
            var pixels = new byte[height * width];
            var point = bitmapData.Scan0;
            for (var y = 0; y < height; y++)
            {
                Marshal.Copy(point, pixels, y * width, width);
                point += bitmapData.Stride;
            }

            image.UnlockBits(bitmapData);
            
            return pixels;
        }

        private static byte[] DecompressTif(byte[] tif)
        {
            using var bitmap = ToBitmap(tif);
            return GetPixels(bitmap, PixelFormat.Format8bppIndexed);
        }

        public static byte[] ToTifImage(int width, int height, PixelFormat format, byte[] data)
        {
            // var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            // using var bitmap = new Bitmap(width, height, width, format, Marshal.UnsafeAddrOfPinnedArrayElement(data, 0));
            using var image = new Bitmap(width, height, format);
            
            var bitmapData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, format);
            // var pixelsPer = System.Drawing.Image.GetPixelFormatSize(bitmapData.PixelFormat) >> 3;

            // height = bitmapData.Height;
            // width = bitmapData.Width * pixelsPer;
            var point = bitmapData.Scan0;
            for (var y = 0; y < height; y++)
            {
                Marshal.Copy(data, y * width, point, width);
                point += bitmapData.Stride;
            }

            image.UnlockBits(bitmapData);
            
            using var ms = new MemoryStream();
            // image.Save(ms, ImageFormat.Gif);
            image.Save(ms, ImageFormat.Tiff);
            // bitmap.Save(ms, ImageFormat.Jpeg);
            image.Dispose();
            // handle.Free();
            return ms.ToArray();
        }

        public static byte[] ToGifImage(int width, int height, PixelFormat format, byte[] data)
        {
            // var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            // using var bitmap = new Bitmap(width, height, width, format, Marshal.UnsafeAddrOfPinnedArrayElement(data, 0));
            using var image = new Bitmap(width, height, format);
            
            var bitmapData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, format);
            // var pixelsPer = System.Drawing.Image.GetPixelFormatSize(bitmapData.PixelFormat) >> 3;

            // height = bitmapData.Height;
            // width = bitmapData.Width * pixelsPer;
            var point = bitmapData.Scan0;
            for (var y = 0; y < height; y++)
            {
                Marshal.Copy(data, y * width, point, width);
                point += bitmapData.Stride;
            }

            image.UnlockBits(bitmapData);
            
            using var ms = new MemoryStream();
            image.Save(ms, ImageFormat.Gif);
            image.Dispose();
            // handle.Free();
            return ms.ToArray();
        }

        public static byte[] ToJpegImage(int width, int height, PixelFormat format, byte[] data, long quality = 30)
        {
            var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            using var bitmap = new Bitmap(width, height, data.Length / height, format, handle.AddrOfPinnedObject());
            using var ms = new MemoryStream();
            var param = new EncoderParameters(1);
            param.Param[0] = new EncoderParameter(Encoder.Quality, quality);
            bitmap.Save(ms, GetEncoder(ImageFormat.Jpeg), param);
            handle.Free();
            return ms.ToArray();
        }

        public static byte[] ToJpegImage(Bitmap bitmap, long quality = 30)
        {
            using var ms = new MemoryStream();
            var param = new EncoderParameters(1);
            param.Param[0] = new EncoderParameter(Encoder.Quality, quality);
            bitmap.Save(ms, GetEncoder(ImageFormat.Jpeg), param);
            return ms.ToArray();
        }

        private static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            return ImageCodecInfo.GetImageEncoders().FirstOrDefault(codec => codec.FormatID == format.Guid);
        }

        public static BitmapImage ToBitmapImage(byte[] data)
        {
            using var ms = new MemoryStream(data);
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = ms;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }

        public static Bitmap ToBitmap(byte[] data)
        {
            using var ms = new MemoryStream(data);
            var bitmap = new Bitmap(ms);
            return bitmap;
        }

        private static void Write(Stream ms, byte[] arr)
        {
            ms.Write(arr, 0, arr.Length);
        }
    }
}