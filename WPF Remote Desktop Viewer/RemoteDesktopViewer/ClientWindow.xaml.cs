using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using RemoteDesktopViewer.Network;
using RemoteDesktopViewer.Network.Packet.Data;
using RemoteDesktopViewer.Utils;

namespace RemoteDesktopViewer
{
    public partial class ClientWindow : Window
    {
        private WriteableBitmap _bitmap;
        private NetworkManager _networkManager;
        private Vector _beforePoint = new Vector(0, 0);

        public ClientWindow(NetworkManager networkManager)
        {
            _networkManager = networkManager;
            InitializeComponent();
        }

        internal void DrawScreenChunk(int x, int y, byte[] data)
        {
            try
            {
                var image = data.ToBitmapImage();
                var stride = image.PixelWidth * (image.Format.BitsPerPixel >> 3);
                var rect = new Int32Rect(x, y, image.PixelWidth, image.PixelHeight);
                
                DrawWritePixels(image, rect, stride);
            }
            catch (Exception err)
            {
                Debug.WriteLine(err);
            }
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

            //
            // _width = _image.Width;
            // _height = _image.Height;
            // _graphics.DrawImage(_image, 0, 0, ClientSize.Width, ClientSize.Height);
        }

        private void DrawWritePixels(BitmapImage image, Int32Rect rect, int stride)
        {
            var pixels = new byte[image.PixelHeight * stride];
            image.CopyPixels(pixels, stride, 0);

            Dispatcher.Invoke(() =>
            {
                _bitmap.WritePixels(rect, pixels, stride, 0);
            });
        }

        private void ClientWindow_OnClosed(object sender, EventArgs e)
        {
            _networkManager?.Disconnect(false);
        }

        private void ClientWindow_OnMouseMove(object sender, MouseEventArgs e)
        {
            if (!IsActive || !_networkManager.ServerControl || !CursorWidthInScreen(e)) return;
            

            var point = e.GetPosition(Image);
            _beforePoint = new Vector(point.X / Image.RenderSize.Width, point.Y / Image.RenderSize.Height);
            _networkManager.SendPacket(new PacketMouseMove(_beforePoint));
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

        private void ClientWindow_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (!IsActive || !_networkManager.ServerControl) return;
            e.Handled = true;
            _networkManager.SendPacket(new PacketKeyEvent((uint) KeyInterop.VirtualKeyFromKey(e.SystemKey),
                PacketKeyEvent.KeyDown));
        }

        private void ClientWindow_OnKeyUp(object sender, KeyEventArgs e)
        {
            if (!IsActive || !_networkManager.ServerControl) return;
            e.Handled = true;
            _networkManager.SendPacket(new PacketKeyEvent((uint) KeyInterop.VirtualKeyFromKey(e.Key), PacketKeyEvent.KeyUp));
        }

        private bool CursorWidthInScreen(MouseEventArgs e)
        {
            var point = e.GetPosition(Image);
            return !(point.X < 0 || point.Y < 0 ||
                     point.X > Image.RenderSize.Width ||
                     point.Y > Image.RenderSize.Height);
        }
    }
}