using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using RemoteDesktopViewer.Utils.Byte;

namespace RemoteDesktopViewer.Utils.Image
{
    public static class Image444
    {
        public static byte[] Compress(Bitmap image, ref NibbleArray beforeCompressed, PixelFormat format)
        {
            using var info = new MemoryStream();
            var bitmapData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly,
                format);

            var pixelsPer = System.Drawing.Image.GetPixelFormatSize(bitmapData.PixelFormat) >> 3;
            
            var height = bitmapData.Height;
            var width = bitmapData.Width * pixelsPer;
            
            var pixels = new NibbleArray(width * height);
            var changedPixelData = new NibbleArray(width * height);
            var offset = bitmapData.Stride - width;

            var changedPos = 0;
            
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
                        
                        var color = pixels[pos++] = *point++;
                        
                        var check = pos - 1;
                        changed = pixels[check] != beforeCompressed[check];
                        
                        if(changed) changedPixelData[changedPos++] = color;
                        if (before == changed) continue;
                        if (!before)
                            startChanged = check;
                        else
                        {
                            var count = check - startChanged - 1;
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

            if (changedPos == 0) return null;
            
            using var ms = new MemoryStream();
            var changedPixels = changedPixelData.GetData(changedPos);
            var length = changedPixels.Length;

            // var compressed = ByteHelper.Compress(changedPixels);
            // if (length > compressed.Length)
            // {
            //     // Debug.WriteLine($"{changedPixels.Length} -> {compressed.Length}");
            //     length = compressed.Length;
            //     changedPixels = compressed;
            //     ms.WriteByte(1);
            // }
            // else
                ms.WriteByte(0);
            
            Write(ms, ByteBuf.GetVarInt(length));
            ms.Write(changedPixels, 0, length);
            
            Write(ms, info.ToArray());
            
            return ms.ToArray();
        }

        
        public static NibbleArray Compress(Bitmap image, PixelFormat format)
        {
            var bitmapData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly,
                format);
            // var bitmapData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly,
            //     image.PixelFormat);
            var pixelsPer = System.Drawing.Image.GetPixelFormatSize(bitmapData.PixelFormat) >> 3;

            var height = bitmapData.Height;
            var width = bitmapData.Width * pixelsPer;
            var offset = bitmapData.Stride - width;
            var pixels = new NibbleArray(width * height);
            unsafe
            {
                var point = (byte*) bitmapData.Scan0;
                // width *= pixelsPer;
                var pos = 0;
                for (var y = 0; y < height; y++)
                {
                    for (var x = 0; x < width; x++)
                    {
                        var color = *point++;
                        pixels[pos++] = color;
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
            var pixels = new NibbleArray(chunk.ReadBool()
                    ? chunk.Read(chunk.ReadVarInt())
                    : ByteHelper.Decompress(chunk.Read(chunk.ReadVarInt())));

            chunk = new ByteBuf(chunk.Read(chunk.Length));
            
            bitmap.Lock();
            unsafe
            {
                var pixelPos = 0;
                var backBuffer = (byte*) bitmap.BackBuffer;
                while (chunk.Length > 0)
                {
                    var pos = chunk.ReadVarInt();
                    var length = chunk.ReadVarInt();
                    for (var i = 0; i < length; i++)
                    {
                        *(backBuffer + pos++) = pixels[pixelPos++];
                        // *(backBuffer + pos++) = pixels[pixelPos++];
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