using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
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
        
        public static string ToSha256(this string str)
        {
            using (var sha256 = new SHA256Managed())
            {
                var encrypt = sha256.ComputeHash(Encoding.UTF8.GetBytes(str));

                return Convert.ToBase64String(encrypt);
            }
        }

        public static byte[] Compress(this byte[] input)
        {
            using (var stream = new MemoryStream())
            {
                using (var zip = new GZipStream(stream, CompressionMode.Compress))
                {
                    zip.Write(input, 0, input.Length);
                    zip.Flush();
                }

                return stream.ToArray();
            }
        }

        public static byte[] Decompress(this byte[] input)
        {
            using (var stream = new MemoryStream(input))
            {
                using (var zip = new GZipStream(stream, CompressionMode.Decompress))
                {
                    using (var result = new MemoryStream())
                    {
                        zip.CopyTo(result);

                        return result.ToArray();
                    }
                }
            }
        }

        public static byte[] ToByteArray(this Image image)
        {
            // var bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly,
            //     PixelFormat.Format8bppIndexed);
            //
            // var bytes = new byte[bitmapData.Stride * bitmapData.Height];
            // Marshal.Copy(bitmapData.Scan0, bytes, 0, bytes.Length);
            // bitmap.UnlockBits(bitmapData);
            //
            // return bytes;

            using (var stream = new MemoryStream())
            {
                image.Save(stream, ImageFormat.Jpeg);
                return stream.ToArray();
            }
        }

        public static byte[] ToPixelArray(this Bitmap image)
        {
            var bitmapData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly,
                image.PixelFormat);
            var length = bitmapData.Stride * bitmapData.Height;

            var pixels = new byte[length];
            Marshal.Copy(bitmapData.Scan0, pixels, 0, length);
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
            return pixels;
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