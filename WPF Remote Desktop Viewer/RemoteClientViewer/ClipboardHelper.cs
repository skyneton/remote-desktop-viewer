﻿using System;
using System.Diagnostics;
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
                Debug.WriteLine(obj?.GetFormats()[0]);
                var current = obj?.GetData(obj.GetFormats()[0]);
                if (current == null || current.Equals(_beforeClipboardData)) return;

                _beforeClipboardData = current;

                if (_updateClipboard)
                {
                    _updateClipboard = false;
                    return;
                }

                ClipboardThreadManager.Worker(MainWindow.Instance.Client.Network, current);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }

        [STAThread]
        public void SetClipboard(object obj)
        {
            _updateClipboard = true;
            _visual.Dispatcher.Invoke(() => Clipboard.SetDataObject(obj));
        }
    }
}