using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using RemoteDesktopViewer.Networks.Threading;
using RemoteDesktopViewer.Utils;
using RemoteDesktopViewer.Utils.Byte;
using RemoteDesktopViewer.Utils.Clipboard;

namespace RemoteClientViewer
{
    public class ClipboardHelper
    {
        private ClipboardManager _clipboardManager;
        private IDataObject _beforeClipboardData;

        private bool _updateClipboard;
        private Visual _visual;
        
        private readonly Dictionary<string, ByteBuf> _clipReceived = new();

        public void Create(Visual window)
        {
            _visual = window;
            _clipboardManager?.Close();
            _clipboardManager = new ClipboardManager(window);
            _clipboardManager.AddCallback(ClipboardCallback);
        }

        public void Close()
        {
            _clipboardManager?.Close();
        }
        

        private void ClipboardCallback()
        {
            var obj = Clipboard.GetDataObject();
            Task.Run(() =>
            {
                if (!MainWindow.Instance.ServerControl) return;
                try
                {
                    // var current = GetDataFromIData(obj);
                    if (obj == null || obj.Equals(_beforeClipboardData)) return;

                    _beforeClipboardData = obj;

                    if (_updateClipboard)
                    {
                        _updateClipboard = false;
                        return;
                    }

                    ClipboardThreadManager.Worker(MainWindow.Instance.Client.Network, _beforeClipboardData);
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                }
            });
        }

        public void ClipboardChunkReceived(string id, byte type, IEnumerable<byte> data)
        {
            switch (type)
            {
                case 0:
                    if (!_clipReceived.TryGetValue(id, out var buf))
                    {
                        buf = new ByteBuf();
                        _clipReceived.Add(id, buf);
                    }
                    
                    buf.Write(data);
                    break;
                case 1:
                    if (!_clipReceived.TryGetValue(id, out buf))
                        return;
                    _clipReceived.Remove(id);

                    ClipboardChunkFinished(buf);
                    break;
            }
        }

        private void ClipboardChunkFinished(ByteBuf buf)
        {
            buf = new ByteBuf(ByteHelper.Decompress(buf.GetBytes()));
            var isFile = buf.ReadBool();
            if (isFile)
                ClipboardRecvFile(buf);
            else
                ClipboardRecv(buf);
        }

        private void ClipboardRecvFile(ByteBuf buf)
        {
            var paths = new List<string>();
            while (buf.Length > 0)
            {
                var isDir = buf.ReadBool();
                var name = buf.ReadString();
                var data = buf.Read(buf.ReadVarInt());

                try
                {
                    var path = FileHelper.GetTempFilePath(name);
                    if (isDir)
                    {
                        ByteHelper.DirectoryDecompress(path, data);
                    }
                    else
                    {
                        data = ByteHelper.Decompress(data);
                        File.WriteAllBytes(path, data);
                    }

                    paths.Add(path);
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                }
            }

            var result = new DataObject();
            result.SetData("FileNameW", paths.ToArray());
            SetClipboard(result);
        }

        private void ClipboardRecv(ByteBuf buf)
        {
            var dataObject = new DataObject();
            while (buf.Length > 0)
            {
                var format = buf.ReadString();
                var data = ClipboardThreadManager.GetData(format, buf.Read(buf.ReadVarInt()));
                dataObject.SetData(format, data);
            }

            SetClipboard(dataObject);
        }

        [STAThread]
        public void SetClipboard(DataObject obj)
        {
            _updateClipboard = true;
            _visual.Dispatcher.Invoke(() => Clipboard.SetDataObject(obj));
        }
    }
}