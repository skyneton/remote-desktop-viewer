using System.Windows;
using System.Windows.Media;
using RemoteDesktopViewer.Networks.Threading;
using RemoteDesktopViewer.Utils.Clipboard;

namespace RemoteDesktopViewer
{
    public class ClipboardHelper
    {
        private ClipboardManager _clipboardManager;
        private object _beforeClipboardData;

        internal bool UpdateClipboard;

        public void Create(Visual window)
        {
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
            if (!RemoteServer.Instance.ServerControl) return;
            
            var obj = Clipboard.GetDataObject();
            var current = obj?.GetData(obj.GetFormats()[0]);
            if (current == null || current.Equals(_beforeClipboardData)) return;
            
            _beforeClipboardData = current;
            
            if (UpdateClipboard)
            {
                UpdateClipboard = false;
                return;
            }

            ClipboardThreadManager.Worker(RemoteServer.Instance.Broadcast, current);
        }
    }
}