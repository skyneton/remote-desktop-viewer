using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Security.Cryptography;
using System.Threading;
using System.Windows;
using RemoteDesktopViewer.Network.Packet.Data;
using RemoteDesktopViewer.Utils;

namespace RemoteDesktopViewer.Network
{
    public static class ScreenThreadManager
    {
        private const int ImageSplitSize = 150;
        private const int ThreadEmptyDelay = 500;
        private const int ThreadDelay = 8;
        private static readonly Dictionary<Utils.Tuple<int, int>, string> BeforeMd5 = new ();
        private static Bitmap _bitmap;
        private static readonly ConcurrentQueue<NetworkManager> FullScreenNetworks = new();
        private static Utils.Tuple<int, int> _beforeSize = GetScreenSize();
        
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
                    SendScreenChunk();

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
            var size = Utils.Tuple.Create(_bitmap.Width, _bitmap.Height);
            if (_beforeSize.X == size.X && _beforeSize.Y == size.Y) return;
            
            RemoteServer.Instance?.Broadcast(new PacketScreen(_bitmap));
            _beforeSize = size;
            BeforeMd5.Clear();
        }

        internal static void SendFullScreen(NetworkManager networkManager)
        {
            FullScreenNetworks.Enqueue(networkManager);
        }

        private static void SendScreenChunk()
        {
            var sizeX = _bitmap.Width / ImageSplitSize;
            var sizeY = _bitmap.Height / ImageSplitSize;
            if (_bitmap.Width % ImageSplitSize != 0) sizeX++;
            if (_bitmap.Height % ImageSplitSize != 0) sizeY++;
            
            for (var y = 0; y < sizeY; y++)
            {
                for (var x = 0; x < sizeX; x++)
                {
                    var posX = x * ImageSplitSize;
                    var posY = y * ImageSplitSize;
                    
                    var width = posX + ImageSplitSize;
                    if (width > _bitmap.Width) width = _bitmap.Width;
                    width -= posX;
                    
                    var height = posY + ImageSplitSize;
                    if (height > _bitmap.Height) height = _bitmap.Height;
                    height -= posY;

                    // var bitmapData = Bitmap.LockBits(new Rectangle(posX, posY, width, height), ImageLockMode.ReadWrite, PixelFormat.Format8bppIndexed);
                    // var bytes = new byte[bitmapData.Stride * bitmapData.Height];
                    // Marshal.Copy(bitmapData.Scan0, bytes, 0, bytes.Length);
                    // Bitmap.UnlockBits(bitmapData);

                    using var split = _bitmap.Clone(new Rectangle(posX, posY, width, height), _bitmap.PixelFormat);
                    // var pixels = split.ToPixelArray();
                    // var bytes = pixels.ImageCompress().Data;
                    var bytes = split.ToByteArray();
                    var currentMd5 = ToMd5(bytes);

                    var pos = Utils.Tuple.Create(posX, posY);
                    BeforeMd5.TryGetValue(pos, out var beforeMd5);

                    if (currentMd5 == beforeMd5) continue;
                    // RemoteServer.Instance?.Broadcast(new PacketScreenChunk(posX, posY, width, height, pixels.Length, bytes));
                    
                    // Task.Run(() =>
                    // {
                    RemoteServer.Instance?.Broadcast(new PacketScreenChunk(posX, posY, bytes));
                    // });
                    
                    if (string.IsNullOrEmpty(beforeMd5))
                        BeforeMd5.Add(pos, currentMd5);
                    else
                        BeforeMd5[pos] = currentMd5;
                }
            }
        }

        private static void TakeDesktop()
        {
            var size = GetScreenSize();
            var bitmap = new Bitmap(size.X, size.Y, System.Drawing.Imaging.PixelFormat.Format16bppRgb555);
            // var bitmap = new Bitmap(size.X, size.Y, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            // var bitmap = new Bitmap(size.X, size.Y, System.Drawing.Imaging.PixelFormat.Format16bppRgb565);

            using var graphics = Graphics.FromImage(bitmap);
            graphics.CopyFromScreen(0, 0, 0, 0, new System.Drawing.Size(size.X, size.Y));
            _bitmap = bitmap;
        }

        private static string ToMd5(byte[] input)
        {
            using (var md5 = MD5.Create())
            {
                return Convert.ToBase64String(md5.ComputeHash(input));
            }
        }

        public static Utils.Tuple<int, int> GetScreenSize()
        {
            // var scale = SystemParameters.MenuWidth - 18;
            return Utils.Tuple.Create(
                (int) Math.Round(SystemParameters.PrimaryScreenWidth),
                (int) Math.Round(SystemParameters.PrimaryScreenHeight)
            );
        }
    }
}