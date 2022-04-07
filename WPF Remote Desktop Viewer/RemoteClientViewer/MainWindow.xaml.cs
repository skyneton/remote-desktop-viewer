using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using RemoteClientViewer.Threading;
using RemoteDesktopViewer.Hook;
using RemoteDesktopViewer.Networks.Packet.Data;
using RemoteDesktopViewer.Utils;
using RemoteDesktopViewer.Utils.Compress;
using RemoteDesktopViewer.Utils.Image;

namespace RemoteClientViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public const int DefaultPort = 33062;
        public static MainWindow Instance { get; private set; }
        private WriteableBitmap _bitmap;
        private RemoteClient _client;
        // private DoubleKey<float, float> _beforePoint = new (0, 0);

        private const int ShowRadis = 140;
        
        private readonly Queue<Key> _pressedKey = new();

        // private byte[] _beforeImageData;
        private long _beforeMouseMove;

        private readonly ThreadFactory _fileUploadFactory = new();
        
        public bool ServerControl { get; internal set; }

        public int CursorValue { get; internal set; } = -1;
        public MainWindow()
        {
            Instance = this;
            InitializeComponent();
            InitServer();
        }

        private async void InitServer()
        {
            GetAddress(out var ip, out var port, out var password);
            Title = $"{ip}:{port}";
            _client = new RemoteClient();
            if (await _client.Connect(ip, port) != null)
            {
                _client.Network.SendPacket(new PacketLogin(password));
                return;
            }
            MessageBox.Show("Can't connect or address wrong.");
            Close();
        }

        private static void GetAddress(out string ip, out int port, out string password)
        {
            var ipArgs = "127.0.0.1";
            var portArgs = 33062;
            var passwordArgs = string.Empty;
            
            var commendLine = Environment.GetCommandLineArgs();
            foreach (var currentArgs in commendLine)
            {
                if (currentArgs.StartsWith("-ip="))
                    ipArgs = currentArgs.Split('=')[1];
                else if (currentArgs.StartsWith("-port="))
                {
                    var portInfo = currentArgs.Split('=')[1];
                    portArgs = int.TryParse(portInfo, out var info) ? info : DefaultPort;
                }
                else if (currentArgs.StartsWith("-password="))
                    passwordArgs = currentArgs.Split('=')[1];
            }

            ip = ipArgs;
            port = portArgs;
            password = passwordArgs;
        }

        public void Invoke(Action action)
        {
            if (!Dispatcher.CheckAccess())
                Dispatcher.Invoke(action);
            else
                action.Invoke();
        }

        internal void DrawFullScreen(int width, int height, int format, byte[] data)
        {
            var source = ImageProcess.ToBitmap(data);
            var pixels = ImageProcess.GetPixels(source, PixelFormatHelper.FromId(format));
            Dispatcher.Invoke(() =>
            {
                _bitmap = new WriteableBitmap(width, height, 96, 96,
                    PixelFormatHelper.ToPixelFormat(format), null);
                _bitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, pixels.Length / height, 0);
                Image.BeginInit();
                Image.Source = _bitmap;
                Image.EndInit();
            });
        }
        
        internal void DrawScreenChunk(ByteBuf buf)
        {
            //Dispatcher.Invoke(() => ImageProcess.DecompressChunk(_bitmap, buf));
            Task.Run(() => ImageProcess.DecompressChunkPalette(_bitmap, buf));
        }
        
        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            KeyboardManager.SetupHook();
            KeyboardManager.AddCallback(KeyHookCallback);
        }

        private bool KeyHookCallback(int code, int wParam, int vkCode)
        {
            if (!IsActive || !ServerControl) return false;
            var key = KeyInterop.KeyFromVirtualKey(vkCode);

            switch (wParam)
            {
                case KeyboardManager.KeyDown: case KeyboardManager.SystemKeyDown:
                    _client.Network.SendPacket(new PacketKeyEvent((byte) vkCode, (int) LowHelper.KeyType.KeyDown));
                    _pressedKey.Enqueue(key);
                    break;
                case KeyboardManager.KeyUp: case KeyboardManager.SystemKeyUp:
                    if (_pressedKey.Contains(key))
                    {
                        _client.Network.SendPacket(new PacketKeyEvent((byte) vkCode, (int) LowHelper.KeyType.KeyUp));
                        _pressedKey.Remove(key);
                    }

                    break;
            }
            return true;
        }
        
        private void MainWindow_OnClosed(object sender, EventArgs e)
        {
            KeyboardManager.RemoveCallback(KeyHookCallback);
            KeyboardManager.ShutdownHook();
            
            _fileUploadFactory.KillAll();
            _client.Close();
        }

        private void MainWindow_OnMouseMove(object sender, MouseEventArgs e)
        {
            CursorShow();
            TopMenuShow(e.GetPosition(TopMenu));
            var now = TimeManager.CurrentTimeMillis;
            if (!IsActive || !ServerControl || !CursorWidthInScreen(e) || now - _beforeMouseMove < 17) return;
            _beforeMouseMove = now;

            var point = e.GetPosition(Image);
            _client.Network.SendPacket(new PacketMouseMove((float) (point.X / Image.RenderSize.Width), (float) (point.Y / Image.RenderSize.Height)));
        }

        private void CursorShow()
        {
            if (CursorValue == -1) return;
            LowHelper.SetCursor(CursorValue);
        }

        private void TopMenuShow(Point point)
        {
            if (Math.Abs(point.X) < ShowRadis && Math.Abs(point.Y) < ShowRadis)
            {
                if (TopMenu.Visibility == Visibility.Hidden)
                {
                    TopMenu.Visibility = Visibility.Visible;
                }

                return;
            }

            if (TopMenu.Visibility == Visibility.Visible) TopMenu.Visibility = Visibility.Hidden;
        }

        private void MainWindow_OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!IsActive || !ServerControl || !CursorWidthInScreen(e)) return;
            
            // var point = e.GetPosition(Image);
            // var pos = new DoubleKey<float, float>((float) (point.X / Image.RenderSize.Width), (float) (point.Y / Image.RenderSize.Height));
            // if (Math.Abs(pos.X - _beforePoint.X) > 0.05 || Math.Abs(pos.Y - _beforePoint.Y) > 0.05)
            // {
            //     _beforePoint = pos;
            //     _client.Network.SendPacket(new PacketMouseMove(pos));
            // }
            
            _client.Network.SendPacket(new PacketMouseEvent((int) LowHelper.MouseType.Wheel, e.Delta));
        }

        private void MainWindow_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!IsActive || !ServerControl || !CursorWidthInScreen(e)) return;
            
            // var point = e.GetPosition(Image);
            // var pos = new DoubleKey<float, float>((float) (point.X / Image.RenderSize.Width), (float) (point.Y / Image.RenderSize.Height));
            // if (Math.Abs(pos.X - _beforePoint.X) > 0.05 || Math.Abs(pos.Y - _beforePoint.Y) > 0.05)
            // {
            //     _beforePoint = pos;
            //     _client.Network.SendPacket(new PacketMouseMove(pos));
            // }

            switch (e.ChangedButton)
            {
                case MouseButton.Left:
                    _client.Network.SendPacket(new PacketMouseEvent((int) LowHelper.MouseType.LeftButtonDown, 0));
                    break;
                case MouseButton.Middle:
                    _client.Network.SendPacket(new PacketMouseEvent((int) LowHelper.MouseType.MiddleDown, 0));
                    break;
                case MouseButton.Right:
                    _client.Network.SendPacket(new PacketMouseEvent((int) LowHelper.MouseType.RightButtonDown, 0));
                    break;
                case MouseButton.XButton1:
                    _client.Network.SendPacket(new PacketMouseEvent((int) LowHelper.MouseType.XButtonDown, 1));
                    break;
                case MouseButton.XButton2:
                    _client.Network.SendPacket(new PacketMouseEvent((int) LowHelper.MouseType.XButtonDown, 2));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void MainWindow_OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!IsActive || !ServerControl || !CursorWidthInScreen(e)) return;

            switch (e.ChangedButton)
            {
                case MouseButton.Left:
                    _client.Network.SendPacket(new PacketMouseEvent((int) LowHelper.MouseType.LeftButtonUp, 0));
                    break;
                case MouseButton.Middle:
                    _client.Network.SendPacket(new PacketMouseEvent((int) LowHelper.MouseType.MiddleUp, 0));
                    break;
                case MouseButton.Right:
                    _client.Network.SendPacket(new PacketMouseEvent((int) LowHelper.MouseType.RightButtonUp, 0));
                    break;
                case MouseButton.XButton1:
                    _client.Network.SendPacket(new PacketMouseEvent((int) LowHelper.MouseType.XButtonUp, 1));
                    break;
                case MouseButton.XButton2:
                    _client.Network.SendPacket(new PacketMouseEvent((int) LowHelper.MouseType.XButtonUp, 2));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private bool CursorWidthInScreen(MouseEventArgs e)
        {
            var point = e.GetPosition(Image);
            return !(point.X < 0 || point.Y < 0 ||
                     point.X > Image.RenderSize.Width ||
                     point.Y > Image.RenderSize.Height);
        }

        private void NormalMaxBtn_OnClick(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowStyle = WindowStyle.SingleBorderWindow;
                WindowState = WindowState.Normal;
                NormalMaxBtn.Content = "Maximize";
                return;
            }

            WindowStyle = WindowStyle.None;
            WindowState = WindowState.Maximized;
            NormalMaxBtn.Content = "Normal";
        }

        private void MainWindow_OnDeactivated(object sender, EventArgs e)
        {
            while (_pressedKey.Count > 0)
            {
                var key = _pressedKey.Dequeue();
                _client.Network.SendPacket(new PacketKeyEvent((byte) KeyInterop.VirtualKeyFromKey(key), (int) LowHelper.KeyType.KeyUp));
            }
        }

        private void Window_DragEnter(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
                ? DragDropEffects.Copy
                : DragDropEffects.None;
            e.Handled = true;
        }

        private void Window_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
                ? DragDropEffects.Copy
                : DragDropEffects.None;
            e.Handled = true;
        }
        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                UploadFiles((string[]) e.Data.GetData(DataFormats.FileDrop));
        }

        private void UploadBtn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Multiselect = true,
                DefaultExt = "*"
            };
            var result = dialog.ShowDialog() ?? false;
            if (result)
                UploadFiles(dialog.FileNames);
        }

        private void UploadFiles(string[] files)
        {
            if (!ServerControl) return;
            foreach (var file in files)
            {
                _fileUploadFactory.LaunchThread(new Thread(() => FileThreadManager.Worker(_client.Network, file)));
            }

            MessageBox.Show($"Started uploading {files.Length} file(s).");
        }
    }
}