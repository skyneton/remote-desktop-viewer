using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows;
using RemoteDesktopViewer.Network.Packet.Data;
using RemoteDesktopViewer.Utils;

namespace RemoteDesktopViewer.Network
{
    public static class ScreenThreadManager
    {
        private const int ImageSplitSize = 150;
        private const long Term = 8;
        private static readonly Dictionary<Utils.Tuple<int, int>, string> BeforeMd5 = new ();
        private static Bitmap _bitmap;
        private static readonly ConcurrentQueue<NetworkManager> FullScreenNetworks = new ();
        private static long _beforeUpdateTime = TimeManager.CurrentTimeMillis;
        private static Utils.Tuple<int, int> _beforeSize = GetScreenSize();
        
        internal static void Worker()
        {
            while (RemoteServer.Instance?.IsAvailable ?? false)
            {
                _bitmap?.Dispose();
                if (RemoteServer.Instance.ClientLength == 0) continue;
                
                var now = TimeManager.CurrentTimeMillis;
                if(now - _beforeUpdateTime < Term) continue;
                _beforeUpdateTime = now;
                
                _bitmap = TakeDesktop();
                SendResizeFullScreen();
                SendFullScreen();
                SendScreenChunk();
            }
        }

        private static void SendFullScreen()
        {
            if(FullScreenNetworks.Count == 0) return;
                
            var packet = new PacketScreen(_bitmap);
            var size = FullScreenNetworks.Count;
            while(size-- > 0) {
                if(!FullScreenNetworks.TryDequeue(out var networkManager))
                    continue;
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
                    var height = posY + ImageSplitSize;
                    if (width > _bitmap.Width)
                        width = _bitmap.Width;
                    if (height > _bitmap.Height)
                        height = _bitmap.Height;
                    width -= posX;
                    height -= posY;

                    // var bitmapData = Bitmap.LockBits(new Rectangle(posX, posY, width, height), ImageLockMode.ReadWrite, PixelFormat.Format8bppIndexed);
                    // var bytes = new byte[bitmapData.Stride * bitmapData.Height];
                    // Marshal.Copy(bitmapData.Scan0, bytes, 0, bytes.Length);
                    // Bitmap.UnlockBits(bitmapData);

                    using var split = _bitmap.Clone(new Rectangle(posX, posY, width, height), _bitmap.PixelFormat);
                    var pixels = split.ToPixelArray();
                    var bytes = new NibbleArray((IReadOnlyList<byte>) pixels).Data;
                    var currentMd5 = ToMd5(bytes);
                    BeforeMd5.TryGetValue(Utils.Tuple.Create(posX, posY), out var beforeMd5);

                    if (currentMd5 == beforeMd5) continue;
                    
                    Task.Run(() =>
                    {
                        RemoteServer.Instance?.Broadcast(new PacketScreenChunk(posX, posY, width, height, pixels.Length, bytes));
                    });
                    
                    if (string.IsNullOrEmpty(beforeMd5))
                        BeforeMd5.Add(Utils.Tuple.Create(posX, posY), currentMd5);
                    else
                        BeforeMd5[Utils.Tuple.Create(posX, posY)] = currentMd5;
                }
            }
        }

        private static Bitmap TakeDesktop()
        {
            var size = GetScreenSize();
            var bitmap = new Bitmap(size.X, size.Y, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            using var graphics = Graphics.FromImage(bitmap);
            graphics.CopyFromScreen(0, 0, 0, 0, new System.Drawing.Size(size.X, size.Y));
            return bitmap;
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