using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using RemoteDesktopViewer.Networks.Packet;
using RemoteDesktopViewer.Networks.Packet.Data;
using RemoteDesktopViewer.Utils.Byte;
using RemoteDesktopViewer.Utils.Image;

namespace RemoteDesktopViewer.Networks.Threading
{
    public class ClipboardThreadManager
    {
        public static void Worker(NetworkManager manager, object data)
        {
            var result = GetData(data);
            if (!result.HasValue) return;
            var v = result.Value;
            manager.SendPacket(new PacketClipboard(v.Key, v.Value));
        }
        public static void Worker(Action<IPacket, bool> action, object data)
        {
            var result = GetData(data);
            if (!result.HasValue) return;
            var v = result.Value;
            action.Invoke(new PacketClipboard(v.Key, v.Value), true);
        }

        private static KeyValuePair<byte, byte[]>? GetData(object data)
        {
            if (data.GetType().IsSerializable)
                return new KeyValuePair<byte, byte[]>(0, ByteHelper.Object2ByteArray(data));
            
            if (data is not InteropBitmap bitmap) return null;
            
            var encoder = new JpegBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));

            using var ms = new MemoryStream();
            encoder.Save(ms);
            
            return new KeyValuePair<byte, byte[]>(1, ms.ToArray());
        }

        public static object GetData(byte type, byte[] data)
        {
            return type switch
            {
                0 => ByteHelper.ByteArray2Object(data),
                1 => ImageProcess.ToBitmapImage(data),
                _ => null
            };
        }
    }
}