using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Media.Imaging;
using RemoteDesktopViewer.Utils.Compress;

namespace RemoteDesktopViewer.Utils.Image
{
    public static class ImageProcess
    {
        public static byte[] ToCompress233(Bitmap image, ref byte[] beforeCompressed, PixelFormat format)
        {
            using var changedPixelsStream = new MemoryStream();
            using var info = new MemoryStream();
            var bitmapData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly,
                format);

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

            if (changedPixelsStream.Length == 0) return null;
            
            using var ms = new MemoryStream();
            var changedPixels = changedPixelsStream.ToArray();
            var length = changedPixels.Length;

            var compressed = ByteHelper.Compress(changedPixels);
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
            
            Write(ms, ByteHelper.Compress(info.ToArray()));
            
            return ms.ToArray();
        }
        public static byte[] Compress565(Bitmap image, ref byte[] beforeCompressed, PixelFormat format)
        {
            using var changedPixelsStream = new MemoryStream();
            using var info = new MemoryStream();
            var bitmapData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly,
                format);

            var height = bitmapData.Height;
            var width = bitmapData.Width;
            var pixelsPer = System.Drawing.Image.GetPixelFormatSize(bitmapData.PixelFormat) >> 3;
            var pixels = new byte[height * width << 1];
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
                        
                        // var b = *point++;
                        // var g = *point++;
                        // var r = *point++;

                        // pixels[pos++] = (byte) (b >> 3 << 3 | g >> 5);
                        // pixels[pos++] = (byte) ((g & 0x7) << 5 | r >> 3);
                        pixels[pos++] = *point++;
                        pixels[pos++] = *point++;
                        
                        var check = pos - 2;
                        changed = pixels[check] != beforeCompressed[check] || pixels[check + 1] != beforeCompressed[check + 1];

                        if (changed)
                        {
                            changedPixelsStream.WriteByte(pixels[check]);
                            changedPixelsStream.WriteByte(pixels[check + 1]);
                        }
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
                    var count = pos - startChanged - 2;
                    Write(info, ByteBuf.GetVarInt(startChanged));
                    Write(info, ByteBuf.GetVarInt(count));
                    changedPixelsStream.Write(pixels, startChanged, count);
                }
            }
            
            
            beforeCompressed = pixels;
            image.UnlockBits(bitmapData);

            if (changedPixelsStream.Length == 0) return null;
            
            using var ms = new MemoryStream();
            var changedPixels = changedPixelsStream.ToArray();
            var length = changedPixels.Length;
            
            // var compressed = ByteHelper.Compress(changedPixels);
            var compressed = ToTifImage(length, 1, PixelFormat.Format8bppIndexed, changedPixels);
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
            
            Write(ms, ByteHelper.Compress(info.ToArray()));
            return ms.ToArray();
        }
        public static byte[] Compress565(Bitmap image, PixelFormat format)
        {
            var bitmapData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly,
                format);

            var height = bitmapData.Height;
            var width = bitmapData.Width;
            var pixelsPer = System.Drawing.Image.GetPixelFormatSize(bitmapData.PixelFormat) >> 3;
            var pixels = new byte[height * width << 1];
            var offset = bitmapData.Stride - width * pixelsPer;
            unsafe
            {
                var pos = 0;
                var point = (byte*)bitmapData.Scan0;
                for (var y = 0; y < height; y++)
                {
                    for (var x = 0; x < width; x++)
                    {
                        // var b = *point++;
                        // var g = *point++;
                        // var r = *point++;
                        //
                        // pixels[pos++] = (byte) (b >> 3 << 3 | g >> 5);
                        // pixels[pos++] = (byte) ((g & 0x7) << 5 | r >> 3);
                        pixels[pos++] = *point++;
                        pixels[pos++] = *point++;
                    }

                    point += offset;
                }
            }

            image.UnlockBits(bitmapData);
            return pixels;
        }

        
        public static byte[] ToCompress233(Bitmap image, PixelFormat format)
        {
            var bitmapData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly,
                format);
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
                        // pixels[pos++] = (byte) ((byte) Math.Round(b / 36.42857142857143) >> 1 << 6 | (byte) Math.Round(g / 36.42857142857143) << 3 | (byte) Math.Round(r / 36.42857142857143));
                    }

                    // Marshal.Copy(point, pixels, y * width, width);
                    point += offset;
                }
            }

            image.UnlockBits(bitmapData);
            return pixels;
        }

        public static byte[] ToCompress(Bitmap image, PixelFormat format) => GetPixels(image, format);


        public static byte[] CompressPalette(Bitmap image, ref byte[] beforeCompressed, PixelFormat format)
        {
            using var info = new MemoryStream();
            using var changedPixelsStream = new MemoryStream();
            using var paletteStream = new MemoryStream();
            var palette = new Palette();

            var bitmapData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly,
                format);

            var height = bitmapData.Height;
            var width = bitmapData.Width;
            var pixelsPer = System.Drawing.Image.GetPixelFormatSize(bitmapData.PixelFormat) >> 3;
            var pixels = new byte[height * width * pixelsPer];
            var offset = bitmapData.Stride - width * pixelsPer;
            unsafe
            {
                var pos = 0;
                var point = (byte*)bitmapData.Scan0;
                var changed = false;
                var startChanged = 0;
                for (var y = 0; y < height; y++)
                {
                    for (var x = 0; x < width; x++)
                    {
                        var before = changed;

                        pixels[pos++] = *point++;
                        pixels[pos++] = *point++;

                        var check = pos - 2;

                        changed = pixels[check] != beforeCompressed[check] ||
                                  pixels[check + 1] != beforeCompressed[check + 1];

                        if (changed)
                        {
                            var index = (short)palette.GetOrCreatePaletteIndex((short)((pixels[check] << 8) | pixels[check + 1]));
                            paletteStream.WriteByte((byte)(index >> 8));
                            paletteStream.WriteByte((byte)index);
                            changedPixelsStream.WriteByte(pixels[check]);
                            changedPixelsStream.WriteByte(pixels[check + 1]);
                        }
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
                    var count = pos - startChanged - 2;
                    Write(info, ByteBuf.GetVarInt(startChanged));
                    Write(info, ByteBuf.GetVarInt(count));
                }
            }

            beforeCompressed = pixels;
            image.UnlockBits(bitmapData);

            if (palette.Length == 0) return null;

            var pixelsLength = (int)paletteStream.Length;

            using var result = new MemoryStream();

            var bitsPerBlock = ByteHelper.GetBitsPerBlock(palette.Length);
            byte[] paletteCompactChunk;
            if ((bitsPerBlock * paletteStream.Length >> 1 >> 3) + palette.Length << 1 > changedPixelsStream.Length)
            {
                result.WriteByte(0);
                paletteCompactChunk = changedPixelsStream.ToArray();
                // var compressed = ByteHelper.Compress(paletteCompactChunk);
                var compressed = ToTifImage(paletteCompactChunk.Length, 1, PixelFormat.Format8bppIndexed, paletteCompactChunk);
                if (changedPixelsStream.Length > compressed.Length)
                {
                    result.WriteByte(1);
                    paletteCompactChunk = compressed;
                }
                else
                    result.WriteByte(0);
            }
            else
            {
                result.WriteByte(1);

                var paletteLength = palette.Length;
                Write(result, ByteBuf.GetVarInt(paletteLength));
                for (var i = 0; i < paletteLength; i++)
                {
                    result.WriteByte((byte)(palette[i] >> 8));
                    result.WriteByte((byte)palette[i]);
                }

                using var ms = new MemoryStream();
                ByteHelper.CreateCompactArray(ms, ByteHelper.GetBitsPerBlock(palette.Length), new ByteBuf(paletteStream.ToArray()));
                paletteCompactChunk = ByteHelper.Compress(ms.ToArray());
                
                Write(result, ByteBuf.GetVarInt(pixelsLength));
            }
            var paletteCompactLength = paletteCompactChunk.Length;
            Write(result, ByteBuf.GetVarInt(paletteCompactLength));
            result.Write(paletteCompactChunk, 0, paletteCompactLength);
            Write(result, ByteHelper.Compress(info.ToArray()));
            
            return result.ToArray();
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
                pixels = ByteHelper.Decompress(pixels);
                // using var palette = ToBitmap(pixels);
                // pixels = GetPixels(palette, PixelFormat.Format8bppIndexed);
                // Debug.WriteLine($"Decom: {pixels.Length}");
                // Debug.WriteLine($"Decompress: {ToMd5(pixels)}");
            }

            chunk = new ByteBuf(ByteHelper.Decompress(chunk.Read(chunk.Length)));
            
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

        private static byte[] DecompressTif(byte[] tif)
        {
            using var bitmap = ToBitmap(tif);
            return GetPixels(bitmap, PixelFormat.Format8bppIndexed);
        }

        public static void DecompressChunk565(WriteableBitmap bitmap, ByteBuf chunk)
        {
            var pixels = chunk.ReadBool()
                ? DecompressTif(chunk.Read(chunk.ReadVarInt()))
                : chunk.Read(chunk.ReadVarInt());

            chunk = new ByteBuf(ByteHelper.Decompress(chunk.Read(chunk.Length)));
            
            bitmap.Lock();
            unsafe
            {
                var pixelPos = 0;
                var backBuffer = (byte*) bitmap.BackBuffer;
                while (chunk.Length > 0)
                {
                    var pos = chunk.ReadVarInt();
                    var length = chunk.ReadVarInt();
                    // Debug.WriteLine($"Changed: {pixels.Length} pixelPos: {pixelPos} Length: {length} pos: {pos}");
                    for (var i = 0; i < length; i++)
                    {
                        *(backBuffer + pos++) = pixels[pixelPos++];
                    }
                }
            }
            bitmap.AddDirtyRect(new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight));
            bitmap.Unlock();
        }


        public static void DecompressChunkPalette(WriteableBitmap bitmap, ByteBuf buf)
        {
            var isPalette = buf.ReadBool();
            byte[] pixels;
            if (!isPalette)
            {
                pixels = buf.ReadBool() ? DecompressTif(buf.Read(buf.ReadVarInt())) : buf.Read(buf.ReadVarInt());
            }
            else
            {
                var paletteLength = buf.ReadVarInt();
                var palette = new Palette(paletteLength);
                for (var i = 0; i < paletteLength; i++)
                {
                    palette.GetOrCreatePaletteIndex(buf.ReadShort());
                }

                var pixelLength = buf.ReadVarInt();
                var compactChunk = ByteHelper.Decompress(buf.Read(buf.ReadVarInt()));
                var compactLength = compactChunk.Length;
                pixels = new byte[pixelLength];
                ByteHelper.IterateCompactArray(ByteHelper.GetBitsPerBlock(paletteLength), pixelLength >> 1, compactChunk,
                    (index, paletteIndex) =>
                    {
                        var bgr = palette[paletteIndex];
                        index <<= 1;
                        pixels[index] = (byte)(bgr >> 8);
                        pixels[index + 1] = (byte)bgr;
                    });
            }

            var info = new ByteBuf(ByteHelper.Decompress(buf.Read(buf.Length)));

            bitmap.Dispatcher.Invoke(() => DecompressChunkPalette(pixels, info, bitmap));
        }

        private static void DecompressChunkPalette(byte[] pixels, ByteBuf info, WriteableBitmap bitmap)
        {
            bitmap.Lock();
            unsafe
            {
                var pixelPos = 0;
                var backBuffer = (byte*)bitmap.BackBuffer;
                while (info.Length > 0)
                {
                    var pos = info.ReadVarInt();
                    var length = info.ReadVarInt();
                    for (var i = 0; i < length; i++)
                    {
                        *(backBuffer + pos++) = pixels[pixelPos++];
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