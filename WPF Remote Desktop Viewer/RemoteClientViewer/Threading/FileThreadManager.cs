using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using RemoteDesktopViewer.Networks;
using RemoteDesktopViewer.Networks.Packet.Data;
using RemoteDesktopViewer.Utils.Compress;

namespace RemoteClientViewer.Threading
{
    public static class FileThreadManager
    {
        private const int FileChunk = 10000;
        private const int ThreadDelay = 10;
        private static int _fileUploadId;
        public static void Worker(NetworkManager manager, string path)
        {
            var id = _fileUploadId++;
            var isDirectory = Directory.Exists(path);
            var bytes = isDirectory ? ByteHelper.DirectoryCompress(path) : ByteHelper.Compress(File.ReadAllBytes(path));
            var name = Path.GetFileName(path);
            
            manager.SendPacket(new PacketFileName(id, name, isDirectory));

            var currentIndex = 0;
            
            while (manager.Connected)
            {
                try
                {
                    if (currentIndex >= bytes.Length)
                    {
                        manager.SendPacket(new PacketFileFinished(id));
                        MainWindow.Instance.Invoke(() => MessageBox.Show($"{name} Uploaded."));
                        break;
                    }
                    manager.SendPacket(new PacketFileChunk(id, FileChunkSplit(ref currentIndex, bytes)));
                    Thread.Sleep(ThreadDelay);
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                }
            }
        }

        private static byte[] FileChunkSplit(ref int currentIndex, byte[] bytes)
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
    }
}