using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
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

        public static byte[] GZipCompress(this byte[] input)
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

        public static byte[] GZipDecompress(this byte[] input)
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

        public static BitmapImage ToBitmapImage(this byte[] input)
        {
            using (var stream = new MemoryStream(input))
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = stream;
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
        }

        public static Image ToImage(this byte[] input)
        {
            using (var stream = new MemoryStream(input))
            {
                return Image.FromStream(stream);
            }
        }
    }
}