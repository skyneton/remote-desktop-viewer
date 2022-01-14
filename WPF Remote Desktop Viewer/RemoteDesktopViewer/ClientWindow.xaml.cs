using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using RemoteDesktopViewer.Network;
using RemoteDesktopViewer.Network.Packet.Data;
using RemoteDesktopViewer.Utils;

namespace RemoteDesktopViewer
{
    public partial class ClientWindow : Window
    {
        private WriteableBitmap _bitmap;
        private readonly NetworkManager _networkManager;
        private Vector _beforePoint = new (0, 0);
        private ConcurrentDictionary<Utils.Tuple<int, int>, PixelData> _pixelMap = new();

        private ThreadFactory _factory = new ();

        private bool _isAlive = true;
        
        private const long RenderTerm = 20;
        private static long _beforeRenderTime = TimeManager.CurrentTimeMillis;

        public ClientWindow(NetworkManager networkManager)
        {
            _networkManager = networkManager;
            InitializeComponent();

            _factory.LaunchThread(new Thread(ScreenRender));
        }

        private void ScreenRender()
        {
            while (_isAlive)
            {
                if (_bitmap == null || _pixelMap.IsEmpty) continue;
                
                // var now = TimeManager.CurrentTimeMillis;
                // if(now - _beforeRenderTime < RenderTerm) continue;
                // _beforeRenderTime = now;

                Dispatcher.Invoke(ScreenRendering);
            }
        }

        private void ScreenRendering()
        {
            _bitmap.Lock();

            foreach (var tuple in _pixelMap.Keys)
            {
                if (!_pixelMap.TryRemove(tuple, out var data)) continue;

                var stride = data.Pixels.Length / data.Height;
                var rect = new Int32Rect(tuple.X, tuple.Y, data.Width, data.Height);
                
                // var pixelPer = stride / data.Width;
                // for (var y = 0; y < data.Height; y++)
                // {
                //     for (var x = 0; x < data.Width; x++)
                //     {
                //         var ptr = y * stride + x * pixelPer;
                //         var color = data.Pixels[ptr] << 16;
                //         color |= data.Pixels[ptr + 1] << 8;
                //         color |= data.Pixels[ptr + 2];
                //         // *((int*) backBuffer + (tuple.Y + y) * backBufferStride + (tuple.X + x) * 4) = color;
                //         var color_data = 255 << 16;
                //         color_data |= 128 << 8;
                //         color_data |= 255 << 0;
                //         unsafe
                //         {
                //             *((int*) backBuffer) = color_data;
                //         }
                //         // *((byte*) pBackBuffer + 1) = 0;
                //         // *((byte*) pBackBuffer + 1) = 0;
                //         backBuffer += pixelsPerBlock;
                //     }
                //
                //     backBuffer += backBufferStride;
                // }


                _bitmap.WritePixels(rect, data.Pixels, stride, 0);
                _bitmap.AddDirtyRect(new Int32Rect(0, 0, data.Width, data.Height));
            }

            _bitmap.Unlock();
        }

        internal void DrawScreenChunk(int x, int y, byte[] data)
        {
            try
            {
                var image = data.ToBitmapImage();
                var stride = image.PixelWidth * (image.Format.BitsPerPixel >> 3);
                // var rect = new Int32Rect(x, y, image.PixelWidth, image.PixelHeight);
                
                var pixels = new byte[image.PixelHeight * stride];
                image.CopyPixels(pixels, stride, 0);
                
                var tuple = Utils.Tuple.Create(x, y);
                var val = new PixelData()
                {
                    Width = image.PixelWidth,
                    Height = image.PixelHeight,
                    Pixels = pixels
                };
                
                _pixelMap.AddOrUpdate(tuple, val, (key, oldValue) => val);
                
                // DrawWritePixels(image, rect, stride);
            }
            catch (Exception err)
            {
                Debug.WriteLine(err);
            }
        }
        

        internal void DrawScreenChunk(int x, int y, int width, int height, int size, NibbleArray array)
        {
            var pixels = array.UnLoss(size, width);

            var tuple = Utils.Tuple.Create(x, y);
            var data = new PixelData()
            {
                Width = width,
                Height = height,
                Pixels = pixels
            };
            
            _pixelMap.AddOrUpdate(tuple, data, (key, oldValue) => data);
            // var stride = pixels.Length / height;
            // var rect = new Int32Rect(x, y, width, height);
            // Dispatcher.Invoke(() =>
            // {
            //     _bitmap.Lock();
            //     _bitmap.WritePixels(rect, pixels, stride, 0);
            //     _bitmap.AddDirtyRect(rect);
            //     _bitmap.Unlock();
            // });
        }

        internal void DrawFullScreen(byte[] pixels)
        {
            var source = pixels.ToBitmapImage();
            
            _pixelMap.Clear();
            
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

        internal void DrawFullScreen(int width, int height, double dpiX, double dpiY, PixelFormat format, int size, NibbleArray array)
        {
            var pixels = array.UnLoss(size, height);
            _pixelMap.Clear();
            format = PixelFormats.Bgr24;
            
            Dispatcher.Invoke(() =>
            {
                _bitmap = new WriteableBitmap(width, height, dpiX, dpiY, format, null);
                _bitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, pixels.Length / height, 0);
                Image.BeginInit();
                Image.Source = _bitmap;
                Image.EndInit();
            });
        
            //
            // _width = _image.Width;
            // _height = _image.Height;
            // _graphics.DrawImage(_image, 0, 0, ClientSize.Width, ClientSize.Height);
        }

        private void DrawWritePixels(BitmapSource image, Int32Rect rect, int stride)
        {
            var pixels = new byte[image.PixelHeight * stride];
            image.CopyPixels(pixels, stride, 0);

            Dispatcher.Invoke(() =>
            {
                _bitmap.Lock();
                _bitmap?.WritePixels(rect, pixels, stride, 0);
                _bitmap.AddDirtyRect(rect);
                _bitmap.Unlock();
            });
        }

        private void ClientWindow_OnClosed(object sender, EventArgs e)
        {
            _isAlive = false;
            _factory.KillAll();
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

        private void ClientWindow_OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!IsActive || !_networkManager.ServerControl) return;
            e.Handled = true;
            if(e.Key == Key.ImeProcessed)
                _networkManager.SendPacket(new PacketKeyEvent((uint) KeyInterop.VirtualKeyFromKey(e.ImeProcessedKey),
                    PacketKeyEvent.KeyDown));
            else
                _networkManager.SendPacket(new PacketKeyEvent((uint) KeyInterop.VirtualKeyFromKey(e.Key),
                    PacketKeyEvent.KeyDown));
        }

        private void ClientWindow_OnPreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (!IsActive || !_networkManager.ServerControl) return;
            e.Handled = true;
            if(e.Key == Key.ImeProcessed)
                _networkManager.SendPacket(new PacketKeyEvent((uint) KeyInterop.VirtualKeyFromKey(e.ImeProcessedKey),
                    PacketKeyEvent.KeyUp));
            else
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

    internal class PixelData
    {
        public int Width, Height;
        public byte[] Pixels;
    }
}