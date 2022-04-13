using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using RemoteDesktopViewer.Utils;
using RemoteDesktopViewer.Utils.Byte;

namespace RemoteDesktopViewer
{
    public class FileReceiveHelper
    {
        private Dictionary<int, ByteBuf> _fileReceived = new();
        

        internal void FileChunkCreate(int id, string name, bool isDirectory)
        {
            var stream = new ByteBuf();
            stream.WriteString(name);
            stream.WriteBool(isDirectory);
            _fileReceived.Add(id, stream);
        }

        internal void FileChunkReceived(int id, byte[] chunk)
        {
            if (!_fileReceived.TryGetValue(id, out var stream))
                return;
            stream.Write(chunk);
        }

        internal void FileChunkFinished(int id)
        {
            if (!_fileReceived.TryGetValue(id, out var stream))
                return;
            
            _fileReceived.Remove(id);
            var buf = new ByteBuf(stream.GetBytes());
            var name = buf.ReadString();
            var isDirectory = buf.ReadBool();
            Debug.WriteLine("Compress");
            Debug.WriteLine(isDirectory);
            var path = FileHelper.GetDesktopFilePath(name);
            Debug.WriteLine(path);
            try
            {
                if (isDirectory)
                {
                    ByteHelper.DirectoryDecompress(path, buf.Read(buf.Length));
                    return;
                }
                var file = ByteHelper.Decompress(buf.Read(buf.Length));
                
                File.WriteAllBytes(path, file);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }
    }
}