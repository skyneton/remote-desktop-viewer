using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media;
using RemoteDesktopViewer.Networks.Threading;
using RemoteDesktopViewer.Utils.Clipboard;

namespace RemoteClientViewer
{
    public class ClipboardHelper
    {
        private ClipboardManager _clipboardManager;
        private object _beforeClipboardData;

        private bool _updateClipboard;
        private Visual _visual;

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
            if (!MainWindow.Instance.ServerControl) return;
            try
            {
                var obj = Clipboard.GetDataObject();
                var current = GetDataFromIData(obj);
                if (!current.HasValue || current.Value.Value.Equals(_beforeClipboardData)) return;

                _beforeClipboardData = current.Value.Value;

                if (_updateClipboard)
                {
                    _updateClipboard = false;
                    return;
                }
                ClipboardThreadManager.Worker(MainWindow.Instance.Client.Network, current.Value.Key, _beforeClipboardData);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }

        private static KeyValuePair<string, object>? GetDataFromIData(IDataObject dataObject)
        {
            if (dataObject == null) return null;
            
            if (dataObject.GetDataPresent("Bitmap"))
                return new ("Bitmap", dataObject.GetData("Bitmap"));
            // if (dataObject.GetDataPresent("FileNameW"))
            //     return new ClipboardTypeFile(dataObject.GetData("FileNameW") as string[]);
            
            return new (dataObject.GetFormats()[0], dataObject.GetData(dataObject.GetFormats()[0]));
        }

        [STAThread]
        public void SetClipboard(string format, object obj)
        {
            _updateClipboard = true;
            _visual.Dispatcher.Invoke(() => Clipboard.SetData(format, obj));
        }
    }
}