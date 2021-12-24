using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using RemoteDesktopViewer.Utils;

namespace RemoteDesktopViewer.Network.Packet.Data
{
    public class ServerControl
    {
        [DllImport("user32.dll")]
        internal static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, int dwExtraInfo);
        [DllImport("user32.dll")]
        internal static extern void keybd_event(uint vk, uint scan, uint flags, uint extraInfo);
    }
    
    public class PacketServerControl : Packet
    {
        private bool _control;
        internal PacketServerControl() {}
        public PacketServerControl(bool control)
        {
            _control = control;
        }
        internal override void Write(ByteBuf buf)
        {
            buf.WriteVarInt((int) PacketType.ServerControl);
            buf.WriteBool(_control);
        }

        internal override void Read(NetworkManager networkManager, ByteBuf buf)
        {
            networkManager.UpdateServerControl(buf.ReadBool());
        }
    }
    
    public class PacketMouseMove : Packet
    {
        private float _percentX, _percentY;
        internal PacketMouseMove() {}
        public PacketMouseMove(float percentX, float percentY)
        {
            _percentX = percentX;
            _percentY = percentY;
        }
        public PacketMouseMove(PointF pos) : this(pos.X, pos.Y) {}
        internal override void Write(ByteBuf buf)
        {
            buf.WriteVarInt((int) PacketType.MouseMove);
            buf.WriteFloat(_percentX);
            buf.WriteFloat(_percentY);
        }

        internal override void Read(NetworkManager networkManager, ByteBuf buf)
        {
            var posX = (int) (Screen.PrimaryScreen.Bounds.Width * buf.ReadFloat());
            var posY = (int) (Screen.PrimaryScreen.Bounds.Height * buf.ReadFloat());
            
            if(networkManager.IsAuthenticate && (RemoteServer.Instance?.ServerControl ?? false))
                Cursor.Position = new Point(posX, posY);
        }
    }

    public class PacketMouseEvent : Packet
    {
        public const uint LeftButtonDown = 0x02;
        public const uint LeftButtonUp = 0x04;
        
        public const uint RightButtonDown = 0x08;
        public const uint RightButtonUp = 0x10;
        
        public const uint MiddleDown = 0x0020;
        public const uint MiddleUp = 0x01;

        public const uint XButtonDown = 0x80;
        public const uint XButtonUp = 0x100;
        
        public const uint Wheel = 0x0800;

        private uint _id, _data;

        internal PacketMouseEvent()
        {
        }

        public PacketMouseEvent(uint id, uint data)
        {
            _id = id;
            _data = data;
        }

        internal override void Write(ByteBuf buf)
        {
            buf.WriteVarInt((int) PacketType.MouseEvent);
            buf.WriteUInt(_id);
            buf.WriteUInt(_data);
        }

        internal override void Read(NetworkManager networkManager, ByteBuf buf)
        {
            if (networkManager.IsAuthenticate && (RemoteServer.Instance?.ServerControl ?? false))
                ServerControl.mouse_event(buf.ReadUInt(), 0, 0, buf.ReadUInt(), 0);
        }
    }

    public class PacketKeyEvent : Packet
    {
        public const uint KeyDown = 0;
        public const uint KeyUp = 0x02;
        
        private uint _id;
        private uint _flag;

        internal PacketKeyEvent() {}
        public PacketKeyEvent(uint id, uint flag)
        {
            _id = id;
            _flag = flag;
        }

        internal override void Write(ByteBuf buf)
        {
            buf.WriteVarInt((int) PacketType.KeyEvent);
            buf.WriteUInt(_id);
            buf.WriteUInt(_flag);
        }

        internal override void Read(NetworkManager networkManager, ByteBuf buf)
        {
            if(networkManager.IsAuthenticate && (RemoteServer.Instance?.ServerControl ?? false))
                ServerControl.keybd_event(buf.ReadUInt(), 0, buf.ReadUInt(), 0);
        }
    }
}
