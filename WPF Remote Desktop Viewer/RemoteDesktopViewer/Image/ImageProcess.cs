using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Media.Imaging;
using RemoteDesktopViewer.Compress;
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
                ScreenThreadManager.Format);

            var height = bitmapData.Height;
            var width = bitmapData.Width;
            var pixelsPer = System.Drawing.Image.GetPixelFormatSize(bitmapData.PixelFormat) >> 3;
            var pixels = new byte[height * width];
            var offset = bitmapData.Stride - width * pixelsPer;
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
                        
                        var b = *point++;
                        var g = *point++;
                        var r = *point++;
                        pixels[pos++] = (byte) ((b >> 6 << 6) | (g >> 5 << 3) | (r >> 5));
                        
                        var check = pos - 1;
                        changed = pixels[check] != beforeCompressed[check];
                        
                        if(changed) changedPixelsStream.WriteByte(pixels[check]);
                        if (before == changed) continue;
                        if (!before)
                            startChanged = check;
                        else
                        {
                            var count = check - startChanged;
                            Write(info, ByteBuf.GetVarInt(startChanged));
                            Write(info, ByteBuf.GetVarInt(count));
                            
                        }
                    }

                    point += offset;
                }

                if (changed)
                {
                    var count = pos - startChanged - 1;
                    Write(info, ByteBuf.GetVarInt(startChanged));
                    Write(info, ByteBuf.GetVarInt(count));
                    changedPixelsStream.Write(pixels, startChanged, count);
                }
            }

            beforeCompressed = pixels;
            image.UnlockBits(bitmapData);
            
            using var ms = new MemoryStream();
            var changedPixels = changedPixelsStream.ToArray();
            var length = changedPixels.Length;

            if (length == 0) return Array.Empty<byte>();

            // var sqrt = (int) Math.Sqrt(length) + 1;
            var compressed = ToTifImage(length, 1, PixelFormat.Format8bppIndexed, changedPixels);
            // var compressed = ByteProcess.Compress(changedPixels);
            if (length > compressed.Length)
            {
                // Debug.WriteLine($"{changedPixels.Length} -> {compressed.Length}");
                length = compressed.Length;
                changedPixels = compressed;
                ms.WriteByte(1);
            }
            else
                ms.WriteByte(0);
            
            Write(ms, ByteBuf.GetVarInt(length));
            ms.Write(changedPixels, 0, length);
            
            Write(ms, ByteProcess.Compress(info.ToArray()));
            
            return ms.ToArray();
        }
        public static byte[] ToCompress(Bitmap image)
        {
            var bitmapData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly,
                ScreenThreadManager.Format);
            // var bitmapData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly,
            //     image.PixelFormat);
            var pixelsPer = System.Drawing.Image.GetPixelFormatSize(bitmapData.PixelFormat) >> 3;

            var height = bitmapData.Height;
            var width = bitmapData.Width;
            var offset = bitmapData.Stride - width * pixelsPer;
            var pixels = new byte[height * width];
            unsafe
            {
                var point = (byte*) bitmapData.Scan0;
                // width *= pixelsPer;
                var pos = 0;
                for (var y = 0; y < height; y++)
                {
                    for (var x = 0; x < width; x++)
                    {
                        var b = *point++;
                        var g = *point++;
                        var r = *point++;
                        pixels[pos++] = (byte) ((b >> 6 << 6) | (g >> 5 << 3) | (r >> 5));
                        // pixels[pos++] = (byte) ((byte) Math.Round(b / 85.0) << 6 | (byte) Math.Round(g / 36.42857142857143) << 3 | (byte) Math.Round(r / 36.42857142857143));
                    }

                    // Marshal.Copy(point, pixels, y * width, width);
                    point += offset;
                }
            }

            image.UnlockBits(bitmapData);
            return pixels;
        }
        public static byte[] GetPixels(Bitmap image, PixelFormat format)
        {
            var bitmapData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, format);
            var pixelsPer = System.Drawing.Image.GetPixelFormatSize(bitmapData.PixelFormat) >> 3;

            var height = bitmapData.Height;
            var width = bitmapData.Width * pixelsPer;
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

        public static byte[] Decompress(byte[] full, int width, int height)
        {
            var size = full.Length;
            var pixels = new byte[width * height * 3];
            var pos = 0;
            for (var i = 0; i < size; i++)
            {
                var bgr = full[i];
                pixels[pos++] = (byte) (bgr >> 6 << 6);
                pixels[pos++] = (byte) ((bgr >> 3 & 0x7) << 5);
                pixels[pos++] = (byte) ((bgr & 0x7) << 5);
            }

            return pixels;
        }

        public static byte[] Decompress(Bitmap image, int width, int height)
        {
            var bitmapData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly,
                PixelFormat.Format8bppIndexed);
            // var bitmapData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly,
            //     image.PixelFormat);
            
            var pixelsPer = System.Drawing.Image.GetPixelFormatSize(bitmapData.PixelFormat) >> 3;
            
            var pixels = new byte[height * width * 3];
            height = bitmapData.Height;
            width = bitmapData.Width * pixelsPer;
            
            var offset = bitmapData.Stride - width;
            width -= 3;
            unsafe
            {
                var point = (byte*) bitmapData.Scan0;
                var pos = 0;
                for (var y = 0; y < height; y++)
                {
                    for (var x = 0; x < width; x++)
                    {
                        var bgr = *point++;
                        pixels[pos++] = (byte) Math.Round((bgr >> 6 << 1 | (bgr & 1)) * 36.42857142857143);
                        pixels[pos++] = (byte) Math.Round((bgr >> 3 & 0x7) * 36.42857142857143);
                        pixels[pos++] = (byte) Math.Round((bgr & 0x7) * 36.42857142857143);
                        // pixels[pos++] = (byte) (bgr >> 6 << 6);
                        // pixels[pos++] = (byte) ((bgr >> 3 & 0x7) << 5);
                        // pixels[pos++] = (byte) ((bgr & 0x7) << 5);
                        
                        // pixels[pos] = pixels[pos + 3] = pixels[pos + 6] = *point++;
                        // pixels[pos + 1] = pixels[pos + 4] = pixels[pos + 7] = *point++;
                        // pixels[pos + 2] = pixels[pos + 5] = pixels[pos + 8] = *point++;
                        // pos += 9;
                    }
            
                    point += offset;
                }
            }
            image.UnlockBits(bitmapData);

            return pixels;
        }

        public static void DecompressChunk(WriteableBitmap bitmap, ByteBuf chunk)
        {
            var compressed = chunk.ReadBool();
            var pixels = chunk.Read(chunk.ReadVarInt());
            if (compressed)
            {
                // pixels = ByteProcess.Decompress(pixels);
                using var palette = ToBitmap(pixels);
                pixels = GetPixels(palette, PixelFormat.Format8bppIndexed);
                // Debug.WriteLine($"Decom: {pixels.Length}");
                // Debug.WriteLine($"Decompress: {ToMd5(pixels)}");
            }

            chunk = new ByteBuf(ByteProcess.Decompress(chunk.Read(chunk.Length)));
            
            bitmap.Lock();
            unsafe
            {
                var pixelPos = 0;
                var backBuffer = (byte*) bitmap.BackBuffer;
                while (chunk.Length > 0)
                {
                    var pos = chunk.ReadVarInt() * 3;
                    var length = chunk.ReadVarInt();
                    // Debug.WriteLine($"Changed: {pixels.Length} pixelPos: {pixelPos} Length: {length} pos: {pos}");
                    for (var i = 0; i < length; i++)
                    {
                        var bgr = pixels[pixelPos++];
                        *(backBuffer + pos++) = (byte) Math.Round((bgr >> 6 << 1 | (bgr & 1)) * 36.42857142857143);
                        // *(backBuffer + pos++) = (byte) Math.Round((bgr >> 6) * 85.0);
                        *(backBuffer + pos++) = (byte) Math.Round((bgr >> 3 & 0x7) * 36.42857142857143);
                        *(backBuffer + pos++) = (byte) Math.Round((bgr & 0x7) * 36.42857142857143);
                        // *(backBuffer + pos++) = pixels[pixelPos++];
                    }
                }
            }
            bitmap.AddDirtyRect(new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight));
            bitmap.Unlock();
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

        private static string ToMd5(byte[] input)
        {
            using var md5 = MD5.Create();
            return Convert.ToBase64String(md5.ComputeHash(input));
        }
    }
}