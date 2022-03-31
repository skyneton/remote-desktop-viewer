using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using RemoteDesktopViewer.Network.Packet.Data;
using RemoteDesktopViewer.Utils;

namespace RemoteDesktopViewer.Network
{
    public static class ScreenThreadManager
    {
        // private const int MaxWidth = 1920;
        // private const int MaxHeight = 1080;
        private const int ImageSplitSize = 150;
        private const int ThreadEmptyDelay = 500;
        private const int ThreadDelay = 20;
        private static readonly Dictionary<DoubleKey<int, int>, string> BeforeMd5 = new ();
        private static Bitmap _bitmap;
        private static readonly ConcurrentQueue<NetworkManager> FullScreenNetworks = new();
        private static DoubleKey<int, int> _beforeSize = GetScreenSize();
        
        internal static void Worker()
        {
            while (RemoteServer.Instance?.IsAvailable ?? false)
            {
                try
                {
                    _bitmap?.Dispose();
                    if (RemoteServer.Instance.ClientLength == 0)
                    {
                        Thread.Sleep(ThreadEmptyDelay);
                        continue;
                    }

                    TakeDesktop();
                    SendFullScreen();
                    SendResizeFullScreen();
                    // ScreenChunkCompress();
                    ScreenChunk();
                    // SendScreenChunk();

                    Thread.Sleep(ThreadDelay);
                }
                catch (Exception)
                {
                    //ignored
                }
            }
        }

        private static void SendFullScreen()
        {
            if(FullScreenNetworks.Count == 0) return;
                
            var packet = new PacketScreen(_bitmap);
            var size = FullScreenNetworks.Count;
            while(size-- > 0) {
                if(!FullScreenNetworks.TryDequeue(out var networkManager)) continue;
                networkManager.SendPacket(packet);
            }
        }

        private static void SendResizeFullScreen()
        {
            var size = new DoubleKey<int, int>(_bitmap.Width, _bitmap.Height);
            if (_beforeSize == size) return;
            
            RemoteServer.Instance?.Broadcast(new PacketScreen(_bitmap));
            _beforeSize = size;
        }

        internal static void SendFullScreen(NetworkManager networkManager)
        {
            FullScreenNetworks.Enqueue(networkManager);
        }

        private static void ScreenChunk()
        {
            using var ms = new MemoryStream();
            GetSplitAmount(out var sizeX, out var sizeY);
            for (var x = 0; x < sizeX; x++)
            {
                for (var y = 0; y < sizeY; y++)
                {
                    var posX = x * ImageSplitSize;
                    var posY = y * ImageSplitSize;
                    GetSize(posX, posY, out var width, out var height);
                    using var image = GetSplitImage(posX, posY, width, height);
                    var byteArray = image.ToByteArray();
                    if (!IsChanged(new DoubleKey<int, int>(x, y), ToMd5(byteArray))) continue;
                    Write(ms, ByteBuf.GetVarInt(posX));
                    Write(ms, ByteBuf.GetVarInt(posY));
                    var length = byteArray.Length;
                    Write(ms, ByteBuf.GetVarInt(length));
                    ms.Write(byteArray, 0, length);
                }
            }

            if (ms.Length <= 0) return;
            var data = ms.ToArray();
            Task.Run(() => { RemoteServer.Instance?.Broadcast(new PacketScreenChunk(data)); });
        }

        private static void ScreenChunkCompress()
        {
            using var ms = new MemoryStream();
            GetSplitAmount(out var sizeX, out var sizeY);
            for (var x = 0; x < sizeX; x++)
            {
                for (var y = 0; y < sizeY; y++)
                {
                    var posX = x * ImageSplitSize;
                    var posY = y * ImageSplitSize;
                    GetSize(posX, posY, out var width, out var height);
                    using var image = GetSplitImage(posX, posY, width, height);
                    var pixels = image.ToPixelArray();
                    var byteArray = pixels.ImageCompress().Data;
                    if (!IsChanged(new DoubleKey<int, int>(x, y), ToMd5(byteArray))) continue;
                    Write(ms, ByteBuf.GetVarInt(posX));
                    Write(ms, ByteBuf.GetVarInt(posY));
                    Write(ms, ByteBuf.GetVarInt(width));
                    Write(ms, ByteBuf.GetVarInt(height));
                    Write(ms, ByteBuf.GetVarInt(pixels.Length));
                    var length = byteArray.Length;
                    Write(ms, ByteBuf.GetVarInt(length));
                    ms.Write(byteArray, 0, length);
                }
            }

            if (ms.Length <= 0) return;
            var data = ms.ToArray();
            Task.Run(() => { RemoteServer.Instance?.Broadcast(new PacketScreenChunk(data)); });
        }

        private static void Write(Stream ms, byte[] arr)
        {
            ms.Write(arr, 0, arr.Length);
        }

        private static Bitmap GetSplitImage(int x, int y, int width, int height)
        {
            return _bitmap.Clone(new Rectangle(x, y, width, height), _bitmap.PixelFormat);
        }

        private static void GetSplitAmount(out int amountX, out int amountY)
        {
            amountX = _bitmap.Width / ImageSplitSize;
            amountY = _bitmap.Height / ImageSplitSize;
            if (_bitmap.Width % ImageSplitSize != 0) amountX++;
            if (_bitmap.Height % ImageSplitSize != 0) amountY++;
        }

        private static void GetSize(int posX, int posY, out int width, out int height)
        {
            width = posX + ImageSplitSize;
            height = posY + ImageSplitSize;
            if (width > _bitmap.Width) width = _bitmap.Width;
            if (height > _bitmap.Height) height = _bitmap.Height;
            width -= posX;
            height -= posY;
        }

        private static bool IsChanged(DoubleKey<int, int> pos, string md5)
        {
            var result = true;
            if (BeforeMd5.TryGetValue(pos, out var beforeMd5))
            {
                result = !beforeMd5.Equals(md5);
                BeforeMd5[pos] = md5;
            }
            else
                BeforeMd5.Add(pos, md5);

            return result;
        }

        private static void TakeDesktop()
        {
            _bitmap?.Dispose();
            var size = GetScreenSize();
            _bitmap = new Bitmap(size.X, size.Y, System.Drawing.Imaging.PixelFormat.Format16bppRgb555);
            // var bitmap = new Bitmap(size.X, size.Y, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            // _bitmap = new Bitmap(size.X, size.Y, System.Drawing.Imaging.PixelFormat.Format16bppRgb565);
  
            using var graphics = Graphics.FromImage(_bitmap);
            graphics.CopyFromScreen(0, 0, 0, 0, new System.Drawing.Size(size.X, size.Y));
            // _bitmap = bitmap.Width > MaxWidth || bitmap.Height > MaxHeight
            //     ? new Bitmap(bitmap, Math.Min(bitmap.Width, MaxWidth), Math.Min(bitmap.Height, MaxHeight))
            //     : bitmap;
        }

        private static string ToMd5(byte[] input)
        {
            using var md5 = MD5.Create();
            return Convert.ToBase64String(md5.ComputeHash(input));
        }

        public static DoubleKey<int, int> GetScreenSize()
        {
            // var scale = SystemParameters.MenuWidth - 18;
            return new DoubleKey<int, int>(
                (int) Math.Round(SystemParameters.PrimaryScreenWidth),
                (int) Math.Round(SystemParameters.PrimaryScreenHeight)
            );
        }
    }
}