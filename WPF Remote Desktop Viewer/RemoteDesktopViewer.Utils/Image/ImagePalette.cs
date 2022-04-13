using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using RemoteDesktopViewer.Utils.Byte;

namespace RemoteDesktopViewer.Utils.Image
{
    public static class ImagePalette
    {
        public static byte[] Compress(Bitmap image, PixelFormat format) => ImageProcess.GetPixels(image, format);


        public static byte[] Compress(Bitmap image, ref byte[] beforeCompressed, PixelFormat format)
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
                var compressed = ByteHelper.Compress(paletteCompactChunk);
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


        public static void DecompressChunk(WriteableBitmap bitmap, ByteBuf buf)
        {
            var isPalette = buf.ReadBool();
            byte[] pixels;
            if (!isPalette)
            {
                pixels = buf.ReadBool() ? ByteHelper.Decompress(buf.Read(buf.ReadVarInt())) : buf.Read(buf.ReadVarInt());
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

            bitmap.Dispatcher.Invoke(() => DecompressChunk(pixels, info, bitmap));
        }

        private static void DecompressChunk(byte[] pixels, ByteBuf info, WriteableBitmap bitmap)
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

        private static void Write(Stream ms, byte[] arr)
        {
            ms.Write(arr, 0, arr.Length);
        }
    }
}