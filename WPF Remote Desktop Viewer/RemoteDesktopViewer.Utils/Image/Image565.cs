using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using RemoteDesktopViewer.Utils.Byte;

namespace RemoteDesktopViewer.Utils.Image
{
    public static class Image565
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
            
            var compressed = ByteHelper.Compress(changedPixels);
            // var compressed = ToTifImage(length, 1, PixelFormat.Format8bppIndexed, changedPixels);
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

        public static void DecompressChunk(WriteableBitmap bitmap, ByteBuf chunk)
        {
            var pixels = chunk.ReadBool()
                ? ByteHelper.Decompress(chunk.Read(chunk.ReadVarInt()))
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

        private static void Write(Stream ms, byte[] arr)
        {
            ms.Write(arr, 0, arr.Length);
        }
    }
}