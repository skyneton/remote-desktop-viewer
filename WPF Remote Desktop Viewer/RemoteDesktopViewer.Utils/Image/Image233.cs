using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using RemoteDesktopViewer.Utils.Byte;

namespace RemoteDesktopViewer.Utils.Image
{
    public static class Image233
    {
        public static byte[] Compress(Bitmap image, ref byte[] beforeCompressed, PixelFormat format)
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

        
        public static byte[] Compress(Bitmap image, PixelFormat format)
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

        public static void DecompressChunk(WriteableBitmap bitmap, ByteBuf chunk)
        {
            var compressed = chunk.ReadBool();
            var pixels = chunk.Read(chunk.ReadVarInt());
            if (compressed)
            {
                pixels = ByteHelper.Decompress(pixels);
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

        private static void Write(Stream ms, byte[] arr)
        {
            ms.Write(arr, 0, arr.Length);
        }
    }
}