using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Windows;
using RemoteDesktopViewer.Image;
using RemoteDesktopViewer.Network;
using RemoteDesktopViewer.Network.Packet.Data;
using RemoteDesktopViewer.Utils;

namespace RemoteDesktopViewer.Threading
{
    public static class ScreenThreadManager
    {
        private const int ThreadEmptyDelay = 500;
        private const int ThreadDelay = 10;
        private static readonly ConcurrentQueue<NetworkManager> FullScreenNetworks = new();
        private static DoubleKey<int, int> _beforeSize;
        private static DoubleKey<int, int> _currentSize = GetScreenSize();

        private static bool SizeUpdated => _beforeSize != _currentSize;
        
        private static byte[] _beforeImageData;
        private static byte[] _changedData;

        // private static readonly PixelFormat ImageFormat = PixelFormat.Format24bppRgb;
        public static readonly PixelFormat Format = PixelFormat.Format16bppRgb565;
        private static int Width, Height;
        private static DoubleKey<float, float> Dpi;

        internal static void Worker()
        {
            while (RemoteServer.Instance?.IsAvailable ?? false)
            {
                try
                {
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
            var packet = new PacketScreen(Format, Width, Height, Dpi, _beforeImageData);
            var size = FullScreenNetworks.Count;
            while(size-- > 0) {
                if(!FullScreenNetworks.TryDequeue(out var networkManager)) continue;
                networkManager.SendPacket(packet);
            }
        }

        private static void SendResizeFullScreen()
        {
            if (!SizeUpdated) return;
            RemoteServer.Instance?.Broadcast(new PacketScreen(Format, Width, Height, Dpi, _beforeImageData));
        }

        internal static void SendFullScreen(NetworkManager networkManager)
        {
            FullScreenNetworks.Enqueue(networkManager);
        }

        private static void ScreenChunk()
        {
            if (_changedData.Length == 0) return;
            if (_changedData.Length >= _beforeImageData.Length >> 5)
            {
                RemoteServer.Instance?.Broadcast(new PacketScreen(Format, Width, Height, Dpi, _beforeImageData));
                Debug.WriteLine($"Changed: {_changedData.Length}");
            }
            else
                RemoteServer.Instance?.Broadcast(new PacketScreenChunk(_changedData));
        }

        private static void Write(Stream ms, byte[] arr)
        {
            ms.Write(arr, 0, arr.Length);
        }

        private static void TakeDesktop()
        {
            _beforeSize = _currentSize;
            _currentSize = GetScreenSize();
            // _bitmap = new Bitmap(size.X, size.Y, System.Drawing.Imaging.PixelFormat.Format16bppRgb555);
            using var bitmap = new Bitmap(_currentSize.X, _currentSize.Y, Format);
            Width = bitmap.Width;
            Height = bitmap.Height;
            Dpi = new DoubleKey<float, float>(bitmap.HorizontalResolution, bitmap.VerticalResolution);
            // _bitmap = new Bitmap(size.X, size.Y, System.Drawing.Imaging.PixelFormat.Format16bppRgb565);
  
            using var graphics = Graphics.FromImage(bitmap);
            graphics.CopyFromScreen(0, 0, 0, 0, new System.Drawing.Size(_currentSize.X, _currentSize.Y));
            if (_beforeImageData == null || SizeUpdated)
            {
                _beforeImageData = ImageProcess.ToCompress(bitmap);
            }
            else
            {
                _changedData = ImageProcess.ToCompress(bitmap, ref _beforeImageData);
            }
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