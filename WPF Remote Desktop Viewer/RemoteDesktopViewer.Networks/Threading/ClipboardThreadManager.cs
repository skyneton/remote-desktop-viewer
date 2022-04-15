using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using RemoteDesktopViewer.Networks.Packet;
using RemoteDesktopViewer.Networks.Packet.Data;
using RemoteDesktopViewer.Utils;
using RemoteDesktopViewer.Utils.Byte;
using RemoteDesktopViewer.Utils.Image;

namespace RemoteDesktopViewer.Networks.Threading
{
    public static class ClipboardThreadManager
    {
        private const int FileChunk = 9000;
        private const int ThreadDelay = 8;
        
        private static string _beforeString; 
        public static void Worker(NetworkManager manager, IDataObject data)
        {
            try
            {
                var bytes = GetDataFromDataObject(data);
                if (bytes == null) return;
                bytes = ByteHelper.Compress(bytes);
                var id = Guid.NewGuid().ToString();
                
                var currentIndex = 0;
                
                while (manager.Connected)
                {
                    if (currentIndex >= bytes.Length)
                    {
                        manager.SendPacket(new PacketClipboard(id, 1, null));
                        break;
                    }
                    manager.SendPacket(new PacketClipboard(id, 0, ChunkSplit(ref currentIndex, bytes)));
                    Thread.Sleep(ThreadDelay);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }
        public static void Worker(Action<IPacket, bool> action, IDataObject data)
        {
            try
            {
                var bytes = GetDataFromDataObject(data);
                if (bytes == null) return;
                bytes = ByteHelper.Compress(bytes);
                var id = Guid.NewGuid().ToString();
                
                var currentIndex = 0;
                
                while (true)
                {
                    if (currentIndex >= bytes.Length)
                    {
                        action.Invoke(new PacketClipboard(id, 1, null), true);
                        break;
                    }
                    action.Invoke(new PacketClipboard(id, 0, ChunkSplit(ref currentIndex, bytes)), true);
                    Thread.Sleep(ThreadDelay);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }

        private static byte[] ChunkSplit(ref int currentIndex, byte[] bytes)
        {
            var end = currentIndex + FileChunk;
            if (end > bytes.Length)
                end = bytes.Length;

            var length = end - currentIndex;
            var buffer = new byte[length];
            Array.Copy(bytes, currentIndex, buffer, 0, length);
            currentIndex += length;

            return buffer;
        }

        private static byte[] GetDataFromDataObject(IDataObject dataObject)
        {
            var buf = new ByteBuf();
            if (dataObject.GetFormats().Contains("FileNameW"))
            {
                buf.WriteBool(true);
                return !GetDataFromFile(buf, (string[]) dataObject.GetData("FileNameW"))
                    ? null
                    : buf.GetBytes();
            }
            
            buf.WriteBool(false);

            var isFirst = true;
            foreach (var format in dataObject.GetFormats())
            {
                if (!dataObject.GetDataPresent(format)) continue;
                try
                {
                    var data = GetData(format, dataObject.GetData(format));
                    if (data == null) continue;
                    buf.WriteString(format);
                    buf.WriteVarInt(data.Length);
                    buf.Write(data);
                    if (isFirst)
                    {
                        isFirst = false;
                        if (CheckBeforeMd5(data)) return null;
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                }
            }

            return buf.GetBytes();
        }

        private static bool GetDataFromFile(ByteBuf buf, IReadOnlyList<string> files)
        {
            for (var i = 0; i < files.Count; i++)
            {
                var path = files[i];
                
                buf.WriteBool(Directory.Exists(path));
                buf.WriteString(Path.GetFileName(path));
                var bytes = FileHelper.GetData(path);
                buf.WriteVarInt(bytes.Length);
                buf.Write(bytes);
                if (i == 0 && CheckBeforeMd5(bytes))
                    return false;
            }

            return true;
        }

        private static byte[] GetData(string format, object data)
        {
            if (data.GetType().IsSerializable)
                return ByteHelper.Object2ByteArray(data);

            if (data is not InteropBitmap bitmap || !format.Equals("Bitmap")) return null;
            
            var encoder = new JpegBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));

            using var ms = new MemoryStream();
            encoder.Save(ms);
            
            return ms.ToArray();
        }

        private static bool CheckBeforeMd5(byte[] data)
        {
            var md5 = ToMd5(data);
            var result = md5.Equals(_beforeString);

            _beforeString = md5;
            
            return result;
        }

        public static object GetData(string format, byte[] data)
        {
            return format switch
            {
                "Bitmap" => ImageProcess.ToBitmap(data),
                _ => ByteHelper.ByteArray2Object(data)
            };
        }

        private static string ToMd5(byte[] data)
        {
            using var md5 = MD5.Create();
            return BitConverter.ToString(md5.ComputeHash(data));
        }
    }
}