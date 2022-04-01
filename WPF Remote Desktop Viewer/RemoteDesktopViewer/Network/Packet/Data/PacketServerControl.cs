using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using RemoteDesktopViewer.Threading;
using RemoteDesktopViewer.Utils;

namespace RemoteDesktopViewer.Network.Packet.Data
{
    public class ServerControl
    {
        [DllImport("user32.dll")]
        internal static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, int dwExtraInfo);
        [DllImport("user32.dll")]
        internal static extern void keybd_event(byte vk, uint scan, int flags, uint extraInfo);

        [DllImport("user32.dll")]
        internal static extern void SetCursorPos(int x, int y);
    }
    
    public class PacketServerControl : IPacket
    {
        private bool _control;
        internal PacketServerControl() {}
        public PacketServerControl(bool control)
        {
            _control = control;
        }
        public void Write(ByteBuf buf)
        {
            buf.WriteVarInt((int) PacketType.ServerControl);
            buf.WriteBool(_control);
        }

        public void Read(NetworkManager networkManager, ByteBuf buf)
        {
            networkManager.UpdateServerControl(buf.ReadBool());
        }
    }
    
    public class PacketMouseMove : IPacket
    {
        private double _percentX, _percentY;
        internal PacketMouseMove() {}
        public PacketMouseMove(double percentX, double percentY)
        {
            _percentX = percentX;
            _percentY = percentY;
        }
        public PacketMouseMove(Vector pos) : this(pos.X, pos.Y) {}
        public void Write(ByteBuf buf)
        {
            buf.WriteVarInt((int) PacketType.MouseMove);
            buf.WriteDouble(_percentX);
            buf.WriteDouble(_percentY);
        }

        public void Read(NetworkManager networkManager, ByteBuf buf)
        {
            var size = ScreenThreadManager.GetScreenSize();
            var posX = (int) (size.X * buf.ReadDouble());
            var posY = (int) (size.Y * buf.ReadDouble());
            
            if(networkManager.IsAuthenticate && (RemoteServer.Instance?.ServerControl ?? false))
                ServerControl.SetCursorPos(posX, posY);
        }
    }

    public class PacketMouseEvent : IPacket
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

        public void Write(ByteBuf buf)
        {
            buf.WriteVarInt((int) PacketType.MouseEvent);
            buf.WriteVarInt((int) _id);
            buf.WriteVarInt((int) _data);
        }

        public void Read(NetworkManager networkManager, ByteBuf buf)
        {
            if (networkManager.IsAuthenticate && (RemoteServer.Instance?.ServerControl ?? false))
            {
                ServerControl.mouse_event((uint) buf.ReadVarInt(), 0, 0, (uint) buf.ReadVarInt(), 0);
            }
        }
    }

    public class PacketKeyEvent : IPacket
    {
        public const int KeyDown = 0;
        public const int KeyUp = 0x02;
        
        private byte _id;
        private int _flag;

        internal PacketKeyEvent() {}
        public PacketKeyEvent(byte id, int flag)
        {
            _id = id;
            _flag = flag;
        }

        public void Write(ByteBuf buf)
        {
            buf.WriteVarInt((int) PacketType.KeyEvent);
            buf.WriteByte(_id);
            buf.WriteVarInt(_flag);
        }

        public void Read(NetworkManager networkManager, ByteBuf buf)
        {
            var vk = buf.ReadByte();
            var flag = buf.ReadVarInt();
            if(networkManager.IsAuthenticate && (RemoteServer.Instance?.ServerControl ?? false))
                ServerControl.keybd_event((byte) vk, 0, flag, 0);
        }
    }
}
