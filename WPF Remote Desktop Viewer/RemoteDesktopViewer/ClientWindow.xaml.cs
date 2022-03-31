using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using RemoteDesktopViewer.Hook;
using RemoteDesktopViewer.Network;
using RemoteDesktopViewer.Network.Packet.Data;
using RemoteDesktopViewer.Utils;

namespace RemoteDesktopViewer
{
    public partial class ClientWindow
    {
        private WriteableBitmap _bitmap;
        private readonly NetworkManager _networkManager;
        private Vector _beforePoint = new (0, 0);

        private const int ShowRadis = 140;
        
        private readonly Queue<Key> _pressedKey = new();

        private bool ControlPressed => _pressedKey.Contains(Key.LeftCtrl) || _pressedKey.Contains(Key.RightCtrl);
        private bool AltPressed => _pressedKey.Contains(Key.LeftAlt) || _pressedKey.Contains(Key.RightAlt);
        private bool ShiftPressed => _pressedKey.Contains(Key.LeftShift) || _pressedKey.Contains(Key.RightShift);
        private bool WinPressed => _pressedKey.Contains(Key.LWin) || _pressedKey.Contains(Key.RWin);

        public ClientWindow(NetworkManager networkManager)
        {
            _networkManager = networkManager;
            InitializeComponent();
        }

        internal void DrawScreenChunk(ByteBuf buf)
        {
            var data = new Queue<ImageChunk>();
            while (buf.Length > 0)
            {
                var posX = buf.ReadVarInt();
                var posY = buf.ReadVarInt();
                var image = buf.Read(buf.ReadVarInt()).ToBitmapImage();
                var stride = image.PixelWidth * (image.Format.BitsPerPixel >> 3);
                var pixels = new byte[image.PixelHeight * stride];
                image.CopyPixels(pixels, stride, 0);
                data.Enqueue(new ImageChunk(new Int32Rect(posX, posY, image.PixelWidth, image.PixelHeight), stride, pixels));
            }
            
            Dispatcher.Invoke(() =>
            {
                RenderChunkMainThread(data);
            });
        }

        internal void DrawScreenChunkCompress(ByteBuf buf)
        {
            var data = new Queue<ImageChunk>();
            while (buf.Length > 0)
            {
                var posX = buf.ReadVarInt();
                var posY = buf.ReadVarInt();
                var width = buf.ReadVarInt();
                var height = buf.ReadVarInt();
                var pixelLength = buf.ReadVarInt();
                var pixels = new NibbleArray(buf.Read(buf.ReadVarInt())).ImageDecompress(pixelLength);
                var stride = pixelLength / height;
                data.Enqueue(new ImageChunk(new Int32Rect(posX, posY, width, height), stride, pixels));
            }

            Dispatcher.Invoke(() => { RenderChunkMainThread(data); });
        }
        
        private void RenderChunkMainThread(Queue<ImageChunk> queue)
        {
            _bitmap.Lock();
            while (queue.Count > 0)
            {
                var chunk = queue.Dequeue();
                _bitmap.WritePixels(chunk.Rect, chunk.Pixels, chunk.Stride, 0);
                _bitmap.AddDirtyRect(chunk.Rect);
            }

            _bitmap.Unlock();
        }

        internal void DrawFullScreen(byte[] pixels)
        {
            var source = pixels.ToBitmapImage();

            Dispatcher.Invoke(() =>
            {
                _bitmap = new WriteableBitmap(source);
                Image.BeginInit();
                Image.Source = _bitmap;
                Image.EndInit();
            });
        }

        internal void DrawFullScreen(int width, int height, double dpiX, double dpiY, PixelFormat format, int size, NibbleArray array)
        {
            Debug.WriteLine(format);
            //var pixels = array.UnLoss(size, height);
            var pixels = array.ImageDecompress(size);
            
            Dispatcher.Invoke(() =>
            {
                _bitmap = new WriteableBitmap(width, height, dpiX, dpiY, format, null);
                _bitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, pixels.Length / height, 0);
                Image.BeginInit();
                Image.Source = _bitmap;
                Image.EndInit();
            });
        }
        
        private void ClientWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            KeyboardManager.SetupHook();
            KeyboardManager.AddCallback(KeyHookCallback);
        }

        private bool KeyHookCallback(int code, int wParam, int vkCode)
        {
            if (!IsActive || !_networkManager.ServerControl) return false;
            var key = KeyInterop.KeyFromVirtualKey(vkCode);

            switch (wParam)
            {
                case KeyboardManager.KeyDown:
                    _networkManager.SendPacket(new PacketKeyEvent((byte) vkCode, PacketKeyEvent.KeyDown));
                    _pressedKey.Enqueue(key);
                    break;
                case KeyboardManager.KeyUp:
                    if (_pressedKey.Contains(key))
                    {
                        Debug.WriteLine(key);
                        _networkManager.SendPacket(new PacketKeyEvent((byte) vkCode, PacketKeyEvent.KeyUp));
                        _pressedKey.Remove(key);
                    }

                    break;
            }
            return true;
        }
        
        private void ClientWindow_OnClosed(object sender, EventArgs e)
        {
            KeyboardManager.RemoveCallback(KeyHookCallback);
            _networkManager?.Disconnect();
        }

        private void ClientWindow_OnMouseMove(object sender, MouseEventArgs e)
        { 
            ButtonShow(e.GetPosition(NormalMaxBtn));
            if (!IsActive || !_networkManager.ServerControl || !CursorWidthInScreen(e)) return;
            

            var point = e.GetPosition(Image);
            _beforePoint = new Vector(point.X / Image.RenderSize.Width, point.Y / Image.RenderSize.Height);
            _networkManager.SendPacket(new PacketMouseMove(_beforePoint));
        }

        private void ButtonShow(Point point)
        {
            if (Math.Abs(point.X) < ShowRadis && Math.Abs(point.Y) < ShowRadis)
            {
                if (NormalMaxBtn.Visibility == Visibility.Hidden)
                {
                    NormalMaxBtn.Visibility = Visibility.Visible;
                }

                return;
            }

            if (NormalMaxBtn.Visibility == Visibility.Visible) NormalMaxBtn.Visibility = Visibility.Hidden;
        }

        private void ClientWindow_OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!IsActive || !_networkManager.ServerControl || !CursorWidthInScreen(e)) return;
            
            var point = e.GetPosition(Image);
            var pos = new Vector(point.X / Image.RenderSize.Width, point.Y / Image.RenderSize.Height);
            if (Math.Abs(pos.X - _beforePoint.X) > 0.05 || Math.Abs(pos.Y - _beforePoint.Y) > 0.05)
            {
                _beforePoint = pos;
                _networkManager.SendPacket(new PacketMouseMove(pos));
            }
            
            _networkManager.SendPacket(new PacketMouseEvent(PacketMouseEvent.Wheel, (uint) e.Delta));
        }

        private void ClientWindow_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!IsActive || !_networkManager.ServerControl || !CursorWidthInScreen(e)) return;
            
            var point = e.GetPosition(Image);
            var pos = new Vector(point.X / Image.RenderSize.Width, point.Y / Image.RenderSize.Height);
            if (Math.Abs(pos.X - _beforePoint.X) > 0.05 || Math.Abs(pos.Y - _beforePoint.Y) > 0.05)
            {
                _beforePoint = pos;
                _networkManager.SendPacket(new PacketMouseMove(pos));
            }

            switch (e.ChangedButton)
            {
                case MouseButton.Left:
                    _networkManager.SendPacket(new PacketMouseEvent(PacketMouseEvent.LeftButtonDown, 0));
                    break;
                case MouseButton.Middle:
                    _networkManager.SendPacket(new PacketMouseEvent(PacketMouseEvent.MiddleDown, 0));
                    break;
                case MouseButton.Right:
                    _networkManager.SendPacket(new PacketMouseEvent(PacketMouseEvent.RightButtonDown, 0));
                    break;
                case MouseButton.XButton1:
                    _networkManager.SendPacket(new PacketMouseEvent(PacketMouseEvent.XButtonDown, 1));
                    break;
                case MouseButton.XButton2:
                    _networkManager.SendPacket(new PacketMouseEvent(PacketMouseEvent.XButtonDown, 2));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void ClientWindow_OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!IsActive || !_networkManager.ServerControl || !CursorWidthInScreen(e)) return;
            
            var point = e.GetPosition(Image);
            var pos = new Vector(point.X / Image.RenderSize.Width, point.Y / Image.RenderSize.Height);
            if (Math.Abs(pos.X - _beforePoint.X) > 0.05 || Math.Abs(pos.Y - _beforePoint.Y) > 0.05)
            {
                _beforePoint = pos;
                _networkManager.SendPacket(new PacketMouseMove(pos));
            }

            switch (e.ChangedButton)
            {
                case MouseButton.Left:
                    _networkManager.SendPacket(new PacketMouseEvent(PacketMouseEvent.LeftButtonUp, 0));
                    break;
                case MouseButton.Middle:
                    _networkManager.SendPacket(new PacketMouseEvent(PacketMouseEvent.MiddleUp, 0));
                    break;
                case MouseButton.Right:
                    _networkManager.SendPacket(new PacketMouseEvent(PacketMouseEvent.RightButtonUp, 0));
                    break;
                case MouseButton.XButton1:
                    _networkManager.SendPacket(new PacketMouseEvent(PacketMouseEvent.XButtonUp, 1));
                    break;
                case MouseButton.XButton2:
                    _networkManager.SendPacket(new PacketMouseEvent(PacketMouseEvent.XButtonUp, 2));
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

        private void ClientWindow_OnDeactivated(object sender, EventArgs e)
        {
            while (_pressedKey.Count > 0)
            {
                var key = _pressedKey.Dequeue();
                _networkManager.SendPacket(new PacketKeyEvent((byte) KeyInterop.VirtualKeyFromKey(key), PacketKeyEvent.KeyUp));
            }
        }
    }

    internal class ImageChunk
    {
        public readonly Int32Rect Rect;
        public readonly int Stride;
        public readonly byte[] Pixels;

        public ImageChunk(Int32Rect rect, int stride, byte[] pixels)
        {
            Rect = rect;
            Stride = stride;
            Pixels = pixels;
        }
    }
}