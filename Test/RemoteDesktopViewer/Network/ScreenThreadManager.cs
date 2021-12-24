using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Security.Cryptography;
using System.Windows;
using RemoteDesktopViewer.Network.Packet.Data;
using RemoteDesktopViewer.Utils;

namespace RemoteDesktopViewer.Network
{
    public static class ScreenThreadManager
    {
        private const int ImageSplitSize = 150;
        private const long Term = 10;
        private static readonly Dictionary<Tuple<int, int>, string> BeforeMd5 = new Dictionary<Tuple<int, int>, string>();
        private static Bitmap _bitmap;
        private static readonly ConcurrentQueue<NetworkManager> FullScreenNetworks = new ConcurrentQueue<NetworkManager>();
        private static long _beforeUpdateTime = TimeManager.CurrentTimeMillis;
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

                    using (var split = _bitmap.Clone(new Rectangle(posX, posY, width, height), _bitmap.PixelFormat))
                    {
                        var bytes = split.ToByteArray();
                        var currentMd5 = ToMd5(bytes);
                        BeforeMd5.TryGetValue(Tuple.Create<int, int>(posX, posY), out var beforeMd5);

                        if (currentMd5 == beforeMd5) continue;

                        RemoteServer.Instance?.Broadcast(new PacketScreenChunk(posX, posY, width, height, bytes));
                        if (string.IsNullOrEmpty(beforeMd5))
                            BeforeMd5.Add(Tuple.Create(posX, posY), currentMd5);
                        else
                            BeforeMd5[Tuple.Create(posX, posY)] = currentMd5;
                    }
                }
            }
        }

        private static Bitmap TakeDesktop()
        {
            var width = SystemParameters.PrimaryScreenWidth;
            var height = SystemParameters.PrimaryScreenHeight;
            var bitmap = new Bitmap((int) width, (int) height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.CopyFromScreen(0, 0, 0, 0, new System.Drawing.Size((int) Math.Ceiling(width), (int) Math.Ceiling(height)));
                return bitmap;
            }
        }

        private static string ToMd5(byte[] input)
        {
            using (var md5 = MD5.Create())
            {
                return Convert.ToBase64String(md5.ComputeHash(input));
            }
        }
    }
    public readonly struct Tuple<T1, T2> {
        public readonly T1 Item1;
        public readonly T2 Item2;
        public Tuple(T1 item1, T2 item2) { Item1 = item1; Item2 = item2;} 
    }

    public static class Tuple { // for type-inference goodness.
        public static Tuple<T1,T2> Create<T1,T2>(T1 item1, T2 item2) { 
            return new Tuple<T1,T2>(item1, item2); 
        }
    }
}