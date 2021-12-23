using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using RemoteDesktopViewer.Network;
using RemoteDesktopViewer.Network.Packet.Data;
using RemoteDesktopViewer.Utils;

namespace RemoteDesktopViewer
{
    public partial class ClientForm : Form
    {
        private Graphics _graphics;
        private Graphics _imageGraphics;
        private int _width, _height;
        private Image _image;
        internal NetworkManager NetworkManager { get; private set; }
        
        public ClientForm(NetworkManager networkManager)
        {
            NetworkManager = networkManager;
            InitializeComponent();
            _graphics = CreateGraphics();
            _graphics.SmoothingMode = SmoothingMode.HighSpeed;
        }

        internal void DrawScreenChunk(int x, int y, byte[] pixels)
        {
            using (var image = pixels.ByteArray2Image())
            {
                var percentX = (float) ClientSize.Width / _width;
                var percentY = (float) ClientSize.Height / _height;
                try
                {
                    _graphics.DrawImage(image, (int) Math.Round(x * percentX), (int) Math.Round(y * percentY),
                        (int) Math.Ceiling(image.Width * percentX), (int) Math.Ceiling(image.Height * percentY));
                }
                catch (Exception)
                {
                    // ignored
                }

                _imageGraphics?.DrawImage(image, x, y);
            }
        }

        internal void DrawFullScreen(byte[] pixels)
        {
            _imageGraphics?.Dispose();
            _image?.Dispose();
            _image = pixels.ByteArray2Image();
            _imageGraphics = Graphics.FromImage(_image);
            
            _width = _image.Width;
            _height = _image.Height;
            _graphics.DrawImage(_image, 0, 0, ClientSize.Width, ClientSize.Height);
        }

        private void ClientForm_Resize(object sender, EventArgs e)
        {
            _graphics?.Dispose();
            _graphics = CreateGraphics();
            _graphics.SmoothingMode = SmoothingMode.HighSpeed;
            if(_image != null)
                _graphics.DrawImage(_image, 0, 0, ClientSize.Width, ClientSize.Height);
        }

        private void ClientForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            NetworkManager?.Disconnect(false);
        }

        private void ClientForm_MouseMove(object sender, MouseEventArgs e)
        {
            if(Focused && NetworkManager.ServerControl)
                NetworkManager.SendPacket(new PacketMouseMove((float) e.Location.X / ClientSize.Width, (float) e.Location.Y / ClientSize.Height));
        }

        private void ClientForm_MouseWheel(object sender, MouseEventArgs e)
        {
            if(Focused && NetworkManager.ServerControl)
                NetworkManager.SendPacket(new PacketMouseEvent(PacketMouseEvent.Wheel, (uint) e.Delta));
        }

        private void ClientForm_MouseDown(object sender, MouseEventArgs e)
        {
            if (!Focused || !NetworkManager.ServerControl) return;

            switch (e.Button)
            {
                case MouseButtons.Left:
                    NetworkManager.SendPacket(new PacketMouseEvent(PacketMouseEvent.LeftButtonDown, 0));
                    break;
                case MouseButtons.Middle:
                    NetworkManager.SendPacket(new PacketMouseEvent(PacketMouseEvent.MiddleDown, 0));
                    break;
                case MouseButtons.Right:
                    NetworkManager.SendPacket(new PacketMouseEvent(PacketMouseEvent.RightButtonDown, 0));
                    break;
                case MouseButtons.XButton1:
                    NetworkManager.SendPacket(new PacketMouseEvent(PacketMouseEvent.XButtonDown, 1));
                    break;
                case MouseButtons.XButton2:
                    NetworkManager.SendPacket(new PacketMouseEvent(PacketMouseEvent.XButtonDown, 2));
                    break;
                case MouseButtons.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void ClientForm_MouseUp(object sender, MouseEventArgs e)
        {
            if (!Focused || !NetworkManager.ServerControl) return;

            switch (e.Button)
            {
                case MouseButtons.Left:
                    NetworkManager.SendPacket(new PacketMouseEvent(PacketMouseEvent.LeftButtonUp, 0));
                    break;
                case MouseButtons.Middle:
                    NetworkManager.SendPacket(new PacketMouseEvent(PacketMouseEvent.MiddleUp, 0));
                    break;
                case MouseButtons.Right:
                    NetworkManager.SendPacket(new PacketMouseEvent(PacketMouseEvent.RightButtonUp, 0));
                    break;
                case MouseButtons.XButton1:
                    NetworkManager.SendPacket(new PacketMouseEvent(PacketMouseEvent.XButtonUp, 1));
                    break;
                case MouseButtons.XButton2:
                    NetworkManager.SendPacket(new PacketMouseEvent(PacketMouseEvent.XButtonUp, 2));
                    break;
                case MouseButtons.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void ClientForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (!Focused || !NetworkManager.ServerControl) return;
            e.Handled = true;
            NetworkManager.SendPacket(new PacketKeyEvent((uint) e.KeyValue, PacketKeyEvent.KeyDown));
        }

        private void ClientForm_KeyUp(object sender, KeyEventArgs e)
        {
            if (!Focused || !NetworkManager.ServerControl) return;
            e.Handled = true;
            NetworkManager.SendPacket(new PacketKeyEvent((uint) e.KeyValue, PacketKeyEvent.KeyUp));
        }
    }
}