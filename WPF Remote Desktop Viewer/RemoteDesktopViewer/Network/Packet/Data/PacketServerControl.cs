using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
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

        [DllImport("user32.dll")]
        internal static extern bool GetCursorInfo(out CURSORINFO pci);
        
        [DllImport("user32.dll")]
        internal static extern int LoadCursor(IntPtr hInstance, int hCursor);
        
        [DllImport("user32.dll")]
        internal static extern int SetCursor(int hCursor);
        
        [StructLayout(LayoutKind.Sequential)]
        public struct CURSORINFO
        {
            public Int32 cbSize;
            public Int32 flags;
            public IntPtr hCursor;
            public POINT ptScreenPos;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public Int32 x;
            public Int32 y;
        }

        public enum CursorType
        {
            None = 0,
            Arrow = 65539,
            IBeam = 65541,
            Wait = 65543,
            Cross = 65545,
            ScrollNW = 65549,
            ScrollWE = 65553,
            ScrollNE = 65551,
            ScrollW = 65553,
            ScrollNS = 65555,
            ScrollAll = 65557,
            No = 65559,
            Progress = 65561,
            Pointer = 65563,
            
            Grabbing = 13896596,
            Alias = 31327887,
            ColResize = 32770565,
            VerticalText = 38668561,
            ZoomIn = 62917193,
            Cell = 64882867,
            Grab = 69339379,
            RowResize = 85000401,
            Copy = 132646983,
            ZoomOut = 186320971
        }
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

            if (!networkManager.IsAuthenticate || (!(RemoteServer.Instance?.ServerControl ?? false))) return;
            
            ServerControl.SetCursorPos(posX, posY);
            
            ServerControl.CURSORINFO pci;
            pci.cbSize =  Marshal.SizeOf(typeof(ServerControl.CURSORINFO));
            if (!ServerControl.GetCursorInfo(out pci) || (int) pci.hCursor == networkManager.BeforeCursor) return;
            networkManager.BeforeCursor = (int) pci.hCursor;
            networkManager.SendPacket(new PacketCursorType((int) pci.hCursor));
        }
    }

    public class PacketCursorType : IPacket
    {
        private int _cursor;
        public PacketCursorType()
        {
            ServerControl.CURSORINFO pci;
            pci.cbSize =  Marshal.SizeOf(typeof(ServerControl.CURSORINFO));
            if (!ServerControl.GetCursorInfo(out pci)) return;
            _cursor = (int) pci.hCursor;
        }

        public PacketCursorType(int cursor)
        {
            _cursor = cursor;
        }
        
        public void Write(ByteBuf buf)
        {
            buf.WriteVarInt((int) PacketType.CursorEvent);
            buf.WriteVarInt(_cursor);
        }

        public void Read(NetworkManager networkManager, ByteBuf buf)
        {
            _cursor = buf.ReadVarInt();
            if(networkManager.ClientWindow != null)
                networkManager.ClientWindow.CursorValue = _cursor;
            
            MainWindow.Instance.Dispatcher.Invoke(() => ServerControl.SetCursor(_cursor));
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
            if (networkManager.IsAuthenticate && (RemoteServer.Instance?.ServerControl ?? false))
            {
                ServerControl.keybd_event((byte) vk, 0, flag, 0);
            }
        }
    }
}
