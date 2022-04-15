using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using RemoteDesktopViewer.Networks.Packet;
using RemoteDesktopViewer.Networks.Packet.Data;
using RemoteDesktopViewer.Utils.Byte;
using RemoteDesktopViewer.Utils.Clipboard;
using RemoteDesktopViewer.Utils.Image;

namespace RemoteDesktopViewer.Networks.Threading
{
    public class ClipboardThreadManager
    {
        private static string _beforeString; 
        public static void Worker(NetworkManager manager, string format, object data)
        {
            var result = GetData(data);
            if (!result.HasValue) return;
            var v = result.Value;
            var md5 = ToMd5(v.Value);
            if(!md5.Equals(_beforeString))
                manager.SendPacket(new PacketClipboard(format, v.Key, v.Value));
            _beforeString = md5;
        }
        public static void Worker(Action<IPacket, bool> action, string format, object data)
        {
            var result = GetData(data);
            if (!result.HasValue) return;
            var v = result.Value;
            var md5 = ToMd5(v.Value);
            if(!md5.Equals(_beforeString))
                action.Invoke(new PacketClipboard(format, v.Key, v.Value), true);
            _beforeString = md5;
        }

        public static KeyValuePair<byte, byte[]>? GetData(object data)
        {
            if (data.GetType().IsSerializable)
                return new KeyValuePair<byte, byte[]>(0, ByteHelper.Object2ByteArray(data));

            if (data is ClipboardTypeFile typeFile) return new KeyValuePair<byte, byte[]>(0, GetDataFromFile(typeFile));
            if (data is not InteropBitmap bitmap) return null;
            
            var encoder = new JpegBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));

            using var ms = new MemoryStream();
            encoder.Save(ms);
            
            return new KeyValuePair<byte, byte[]>(1, ms.ToArray());
        }

        private static byte[] GetDataFromFile(ClipboardTypeFile file)
        {
            var length = file.Name.Length;
            var result = new FileStream[length];
            for (var i = 0; i < length; i++)
            {
                using var fs = new FileStream(file.Name[i], FileMode.Open, FileAccess.Read);
                result[i] = fs;
            }

            return ByteHelper.Object2ByteArray(result);
        }

        public static object GetData(byte type, byte[] data)
        {
            return type switch
            {
                0 => ByteHelper.ByteArray2Object(data),
                1 => ImageProcess.ToBitmap(data),
                _ => null
            };
        }

        private static string ToMd5(byte[] data)
        {
            using var md5 = MD5.Create();
            return BitConverter.ToString(md5.ComputeHash(data));
        }
    }
}