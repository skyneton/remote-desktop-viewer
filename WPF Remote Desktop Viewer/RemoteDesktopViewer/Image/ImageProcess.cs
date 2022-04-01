using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;
using RemoteDesktopViewer.Threading;
using RemoteDesktopViewer.Utils;

namespace RemoteDesktopViewer.Image
{
    public static class ImageProcess
    {
        // private const byte MinChangePixel = 2;
        public static byte[] ToCompress(Bitmap image, ref byte[] beforeCompressed)
        {
            using var changedPixelsStream = new MemoryStream();
            using var info = new MemoryStream();
            var bitmapData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly,
                image.PixelFormat);

            var height = bitmapData.Height;
            var width = bitmapData.Width;
            var pixelsPer = System.Drawing.Image.GetPixelFormatSize(bitmapData.PixelFormat) >> 3;
            width *= pixelsPer;
            var pixels = new byte[height * width];
            var offset = bitmapData.Stride - width;
            unsafe
            {
                var pos = 0;
                var point = (byte*) bitmapData.Scan0;
                var changed = false;
                var startChanged = 0;
                for (var y = 0; y < height; y++)
                {
                    for (var x = 0; x < width; x++)
                    {
                        var before = changed;
                        
                        pixels[pos++] = *point++;
                        var check = pos - 1;
                        changed = pixels[check] != beforeCompressed[check];
                        
                        if(changed) changedPixelsStream.WriteByte(pixels[check]);
                        if (before == changed) continue;
                        if (!before)
                            startChanged = check;
                        else
                        {
                            var count = check - startChanged;
                            // if (count < MinChangePixel) continue;
                            Write(info, ByteBuf.GetVarInt(startChanged));
                            Write(info, ByteBuf.GetVarInt(count));
                            
                        }
                    }

                    point += offset;
                }

                if (changed)
                {
                    var count = pos - startChanged - 1;
                    // if (count >= MinChangePixel)
                    // {
                    Write(info, ByteBuf.GetVarInt(startChanged));
                    Write(info, ByteBuf.GetVarInt(count));
                    changedPixelsStream.Write(pixels, startChanged, count);
                    // }
                }
            }

            beforeCompressed = pixels;
            image.UnlockBits(bitmapData);
            
            using var ms = new MemoryStream();
            var changedPixels = changedPixelsStream.ToArray();
            var length = changedPixels.Length;

            // var compressed = ToJpegImage(length / pixelsPer, 1, image.PixelFormat, changedPixels);
            // if (length > compressed.Length)
            // {
            //     length = compressed.Length;
            //     changedPixels = compressed;
            //     ms.WriteByte(1);
            // }
            // else
            //     ms.WriteByte(0);
            //
            Write(ms, ByteBuf.GetVarInt(length));
            ms.Write(changedPixels, 0, length);
            
            Write(ms, info.ToArray());
            
            return ms.ToArray();
        }
        public static byte[] ToCompress(Bitmap image)
        {
            var bitmapData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly,
                ScreenThreadManager.Format);
            // var bitmapData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly,
            //     image.PixelFormat);

            var height = bitmapData.Height;
            var width = bitmapData.Width;
            
            var pixelsPer = System.Drawing.Image.GetPixelFormatSize(bitmapData.PixelFormat) >> 3;
            var point = bitmapData.Scan0;
            width *= pixelsPer;
            var pixels = new byte[height * width];
            for (var y = 0; y < bitmapData.Height; y++)
            {
                Marshal.Copy(point, pixels, y * width, width);
                point += bitmapData.Stride;
            }
            
            image.UnlockBits(bitmapData);
            
            return pixels;
        }

        public static byte[] Decompress(byte[] full, int width, int height)
        {
            var size = width * height;
            var pixels = new byte[size * 3];
            var pos = 0;
            for (var i = 0; i < size; i++)
            {
                var target = i * 3;
                pixels[pos++] = full[target];
                pixels[pos++] = full[target + 1];
                pixels[pos++] = full[target + 2];
            }

            return pixels;
        }

        public static void DecompressChunk(WriteableBitmap bitmap, ByteBuf chunk)
        {
            var pixels = chunk.Read(chunk.ReadVarInt());
            
            bitmap.Lock();
            // var compressed = chunk.ReadBool();
            // if (compressed)
            // {
            //     using var bitmap = ToBitmap(pixels);
            //     pixels = ToCompress(bitmap);
            // }

            unsafe
            {
                var pixelPos = 0;
                var backBuffer = (byte*) bitmap.BackBuffer;
                while (chunk.Length > 0)
                {
                    var pos = chunk.ReadVarInt();
                    var length = chunk.ReadVarInt();
                    // Debug.WriteLine($"Full: {full.Length} Changed: {pixels.Length} pixelPos: {pixelPos} Length: {length} pos: {pos}");
                    for (var i = 0; i < length; i++)
                    {
                        *(backBuffer + pos++) = pixels[pixelPos++];
                    }
                }
            }
            // Debug.WriteLine($"Full: {full.Length} Changed: {pixels.Length} pixelPos: {pixelPos}");
            bitmap.Unlock();
        }

        public static byte[] ToJpegImage(int width, int height, PixelFormat format, byte[] data)
        {
            var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            using var bitmap = new Bitmap(width, height, data.Length / height, format, handle.AddrOfPinnedObject());
            using var ms = new MemoryStream();
            var param = new EncoderParameters(1);
            param.Param[0] = new EncoderParameter(Encoder.Quality, 30L);
            bitmap.Save(ms, GetEncoder(ImageFormat.Jpeg), param);
            handle.Free();
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