using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Media.Imaging;

namespace RemoteDesktopViewer.Utils
{
    public static class RemoteExtensions
    {
        private const int LossBlock = 3;
        
        public static void Remove<T>(this ConcurrentBag<T> data, T target)
        {
            var removeQueue = new Queue<T>();
            while(!data.IsEmpty)
            {
                if (data.TryTake(out var item) && item.Equals(target))
                    break;
                
                removeQueue.Enqueue(item);
            }
            
            foreach (var item in removeQueue)
            {
                data.Add(item);
            }
        }
        
        public static void Remove<T>(this Queue<T> data, T target)
        {
            var removeQueue = new Queue<T>();
            while(data.Count > 0)
            {
                var item = data.Dequeue();
                if (item.Equals(target))
                    break;
                
                removeQueue.Enqueue(item);
            }
            
            foreach (var item in removeQueue)
            {
                data.Enqueue(item);
            }
        }
        
        public static string ToSha256(this string str)
        {
            using (var sha256 = new SHA256Managed())
            {
                var encrypt = sha256.ComputeHash(Encoding.UTF8.GetBytes(str));

                return Convert.ToBase64String(encrypt);
            }
        }

        public static NibbleArray ImageCompress(this byte[] pixels)
        {
            var size = pixels.Length;
            var data = new NibbleArray(size / 3);
            size /= 3;
            var pos = 0;
            for (var i = 0; i < size; i+=3)
            {
                var target = i * 3;
                data[pos++] = (byte) ((pixels[target] + pixels[target + 3] + pixels[target + 6]) / 3);
                data[pos++] = (byte) ((pixels[target + 1] + pixels[target + 4] + pixels[target + 7]) / 3);
                data[pos++] = (byte) ((pixels[target + 2] + pixels[target + 5] + pixels[target + 8]) / 3);
                // data[pos++] = pixels[target];
                // data[pos++] = pixels[target + 1];
                // data[pos++] = pixels[target + 2];
            }

            return data;
        }

        public static byte[] ImageDecompress(this NibbleArray array, int size)
        {
            var data = new byte[size];
            size = array.Length;
            size /= 3;
            // size /= 2;
            var pos = 0;
            for (var i = 0; i < size; i++)
            {
                var target = i * 3;
                // var target = i * 2;
                data[pos++] = array[target];
                data[pos++] = array[target + 1];
                data[pos++] = array[target + 2];

                data[pos++] = array[target];
                data[pos++] = array[target + 1];
                data[pos++] = array[target + 2];

                data[pos++] = array[target];
                data[pos++] = array[target + 1];
                data[pos++] = array[target + 2];
            }

            return data;
        }

        // internal static void CreateCompactArray(ByteBuf buf, int bitsPerBlock, byte[] array)
        // {
        //     ulong buffer = 0;
        //     var bitIndex = 0;
        //     buf.WriteVarInt(ChunkSection.Size * bitsPerBlock / 64);
        //
        //     for (var i = 0; i < ChunkSection.Size; i++)
        //     {
        //         var value = func.Invoke(i);
        //         buffer |= (ulong) (uint) value << bitIndex;
        //         var remaining = bitsPerBlock - (64 - bitIndex);
        //         if (remaining >= 0)
        //         {
        //             buf.WriteULong(buffer);
        //             buffer = (ulong) (value >> (bitsPerBlock - remaining));
        //             bitIndex = remaining;
        //         }
        //         else
        //             bitIndex += bitsPerBlock;
        //     }
        //     
        //     if(bitIndex > 0)
        //         buf.WriteULong(buffer);
        // }

        public static byte[] ToByteArray(this Bitmap image)
        {
            // var bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly,
            //     PixelFormat.Format8bppIndexed);
            //
            // var bytes = new byte[bitmapData.Stride * bitmapData.Height];
            // Marshal.Copy(bitmapData.Scan0, bytes, 0, bytes.Length);
            // bitmap.UnlockBits(bitmapData);
            //
            // return bytes;

            using var stream = new MemoryStream();
            image.Save(stream, ImageFormat.Jpeg);
            return stream.ToArray();
        }

        public static byte[] ToPixelArray(this Bitmap image)
        {
            var bitmapData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly,
                image.PixelFormat);

            var pixels = new byte[bitmapData.Width * bitmapData.Height * 3];
            for (var y = 0; y < bitmapData.Height; y++)
            {
                var mem = bitmapData.Scan0 + y * bitmapData.Stride;
                Marshal.Copy(mem, pixels, y * bitmapData.Width * 3, bitmapData.Width * 3);
            }

            // var pixels = new byte[bitmapData.Stride * bitmapData.Height];
            // Marshal.Copy(bitmapData.Scan0, pixels, 0, pixels.Length);
            
            image.UnlockBits(bitmapData);

            return pixels;
        }

        public static byte[] ToPixelArray(this NibbleArray array, int size)
        {
            var pixels = new byte[size];
            for (var i = 0; i < size; i++)
            {
                pixels[i] = array[i];
            }
            // Buffer.BlockCopy((byte[]) array, 0, pixels, 0, size);
            return pixels;
        }

        public static byte[] UnLoss(this NibbleArray data, int size, int width)
        {
            var pixelBlock = size / 3;
            var result = new byte[size];
            
            (result[0], result[1], result[2]) = (data[0], data[1], data[2]);
            
            var target = 1;
            for (var i = 1; i < pixelBlock; i++)
            {
                var widthBlock = i % width;
                
                var index = i * 3;
                var dataIndex = target * 3;
                if (i % LossBlock == 0)
                {
                    var before = (i - 1) * 3;
                    // (result[index], result[index + 1], result[index + 2]) = (result[before], result[before + 1], result[before + 2]);
                    if (widthBlock == 0)
                    {
                        (result[index], result[index + 1], result[index + 2]) = (data[dataIndex], data[dataIndex + 1], data[dataIndex + 2]);
                        // (result[index], result[index + 1], result[index + 2]) = (result[before], result[before + 1], result[before + 2]);
                        continue;
                    }
                    // if (widthBlock == 1)
                    // {
                    //     (result[index], result[index + 1], result[index + 2]) = (data[dataIndex], data[dataIndex + 1], data[dataIndex + 2]);
                    //     continue;
                    // }
                    result[index] = (byte) ((result[before] + data[dataIndex]) / 2);
                    result[index + 1] = (byte) ((result[before + 1] + data[dataIndex + 1]) / 2);
                    result[index + 2] = (byte) ((result[before + 2] + data[dataIndex + 2]) / 2);
                    continue;
                }
                (result[index], result[index + 1], result[index + 2]) = (data[dataIndex], data[dataIndex + 1], data[dataIndex + 2]);
                
                target++;
            }

            return result;
        }

        public static byte[] Loss(this byte[] data)
        {
            var pixelBlock = data.Length / 3;
            var result = new byte[pixelBlock / LossBlock * (LossBlock - 1) * 3 + pixelBlock % LossBlock * 3 + 3];

            (result[0], result[1], result[2]) = (data[0], data[1], data[2]);
            var target = 1;
            for (var i = 1; i < pixelBlock; i++)
            {
                if (i % LossBlock == 0) continue;
                
                var index = target * 3;
                var dataIndex = i * 3;
                
                (result[index], result[index + 1], result[index + 2]) = (data[dataIndex], data[dataIndex + 1], data[dataIndex + 2]);
                
                target++;
            }

            return result;
        }

        public static BitmapImage ToBitmapImage(this byte[] input)
        {
            using var stream = new MemoryStream(input);
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = stream;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }

        public static Bitmap ToBitmap(this byte[] input)
        {
            using var stream = new MemoryStream(input);
            var bitmap = new Bitmap(stream);
            return bitmap;
        }

        public static System.Windows.Media.PixelFormat ToWpfPixelFormat(this PixelFormat format)
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

        public static int ToId(this System.Windows.Media.PixelFormat format)
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

        public static System.Windows.Media.PixelFormat ToPixelFormat(this int id)
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