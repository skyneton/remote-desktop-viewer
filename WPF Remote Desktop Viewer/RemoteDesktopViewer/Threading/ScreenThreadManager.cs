﻿using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using RemoteDesktopViewer.Networks;
using RemoteDesktopViewer.Networks.Packet.Data;
using RemoteDesktopViewer.Utils;
using RemoteDesktopViewer.Utils.Image;

namespace RemoteDesktopViewer.Threading
{
    public static class ScreenThreadManager
    {
        private const int ThreadEmptyDelay = 500;
        private const int ThreadDelay = 18;
        private static readonly ConcurrentQueue<NetworkManager> FullScreenNetworks = new();
        private static DoubleKey<int, int> _beforeSize;
        public static DoubleKey<int, int> CurrentSize { get; private set; } = GetScreenSize();

        private static bool SizeUpdated => _beforeSize != CurrentSize;

        private static Bitmap _beforeFrame;
        private static byte[] _beforeImageData;
        private static byte[] _changedData;

        private const PixelFormat Format = PixelFormat.Format16bppRgb565;
        //private const PixelFormat Format = PixelFormat.Format16bppRgb565;

        private static long _beforeMs = TimeManager.CurrentTimeMillis;

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

                    /*var jpeg = ImageProcess.ToJpegImage(_beforeFrame);
                    var def = ByteHelper.Compress(_beforeImageData);
                    var gzip = ByteHelper.GZipCompress(_beforeImageData);
                    
                    Debug.WriteLine($"JPEG: {((double)_beforeImageData.Length / jpeg.Length).ToString("0.0000%")}, DEF: {((double)_beforeImageData.Length / def.Length).ToString("0.0000%")}, GZIP: {((double)_beforeImageData.Length / gzip.Length).ToString("0.0000%")}");*/

                    var a = TimeManager.CurrentTimeMillis;
                    var jpeg = ImageProcess.ToJpegImage(_beforeFrame);
                    SendFullScreen(jpeg);
                    if(SendResizeFullScreen(jpeg)) continue;
                    ScreenChunk(jpeg);

                    Thread.Sleep(ThreadDelay);

                    Debug.WriteLine(TimeManager.CurrentTimeMillis - _beforeMs + "ms");
                    _beforeMs = TimeManager.CurrentTimeMillis;
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                }
            }
        }

        private static void SendFullScreen(byte[] compressedImage)
        {
            if(FullScreenNetworks.Count == 0) return;     
            var packet = new PacketScreen(CurrentSize.X, CurrentSize.Y, (byte) PixelFormatHelper.ToId(Format), compressedImage);
            var size = FullScreenNetworks.Count;
            while(size-- > 0) {
                if(!FullScreenNetworks.TryDequeue(out var networkManager)) continue;
                networkManager.SendPacket(packet);
            }
        }

        private static bool SendResizeFullScreen(byte[] compressedImage)
        {
            if (!SizeUpdated) return false;
            RemoteServer.Instance?.Broadcast(new PacketScreen(CurrentSize.X, CurrentSize.Y, (byte)PixelFormatHelper.ToId(Format), compressedImage));
            return true;
        }

        internal static void SendFullScreen(NetworkManager networkManager)
        {
            FullScreenNetworks.Enqueue(networkManager);
        }

        private static void ScreenChunk(byte[] compressedImage)
        {
            if (_changedData == null || _changedData.Length == 0) return;
            // if (_changedData.Length > (_beforeImageData.Length >> 4) * 1.2)
            if (_changedData.Length > compressedImage.Length)
            {
                // Debug.WriteLine($"{_changedData.Length} -> {jpeg.Length}");
                RemoteServer.Instance?.Broadcast(new PacketScreen(CurrentSize.X, CurrentSize.Y, (byte)PixelFormatHelper.ToId(Format), compressedImage));
                // Debug.WriteLine($"Changed: {_changedData.Length}");
            }
            else
                RemoteServer.Instance?.Broadcast(new PacketScreenChunk(_changedData));
        }

        private static void TakeDesktop()
        {
            _beforeFrame?.Dispose();
            _beforeSize = CurrentSize;
            CurrentSize = GetScreenSize();

            _beforeFrame = new Bitmap(CurrentSize.X, CurrentSize.Y, Format);

            using var graphics = Graphics.FromImage(_beforeFrame);
            graphics.CopyFromScreen(0, 0, 0, 0, new System.Drawing.Size(CurrentSize.X, CurrentSize.Y));

            if (_beforeImageData == null || SizeUpdated)
            {
                _beforeImageData = ImageProcess.ToCompress(_beforeFrame, Format);
            }
            else
            {
                _changedData = ImageProcess.CompressPalette(_beforeFrame, ref _beforeImageData, Format);
            }
        }

        public static DoubleKey<int, int> GetScreenSize()
        {
            using var g = Graphics.FromHwnd(IntPtr.Zero);
            var hdc = g.GetHdc();
            return new DoubleKey<int, int>(
                LowHelper.GetDeviceCaps(hdc, (int) LowHelper.DeviceCaps.DesktopHorzres),
                LowHelper.GetDeviceCaps(hdc, (int) LowHelper.DeviceCaps.DesktopVertres)
                );
            // return new DoubleKey<int, int>(
            //     (int) Math.Round(SystemParameters.PrimaryScreenWidth * scale),
            //     (int) Math.Round(SystemParameters.PrimaryScreenHeight * scale)
            // );
        }

        // private static bool GetDpi(out int dpi)
        // {
        //     using var g = Graphics.FromHwnd(IntPtr.Zero);
        //     Debug.WriteLine($"{GetDeviceCaps(g.GetHdc(), (int) DeviceCaps.DesktopHorzres)}");
        //     try
        //     {
        //         var dpiInfo =
        //             typeof(SystemParameters).GetProperty("Dpi", BindingFlags.NonPublic | BindingFlags.Static);
        //
        //         dpi = (int) (dpiInfo?.GetValue(null, null) ?? 96);
        //         return true;
        //     }
        //     catch (Exception)
        //     {
        //         dpi = 96;
        //         return false;
        //     }
        // }
    }
}