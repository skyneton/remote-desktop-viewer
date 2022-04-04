using RemoteDesktopViewer.Utils;

namespace RemoteDesktopViewer.Networks.Packet.Data
{   
    public class PacketServerControl : IPacket
    {
        public bool Control { get; private set; }
        public PacketServerControl() {}
        public PacketServerControl(bool control)
        {
            Control = control;
        }
        public void Write(ByteBuf buf)
        {
            buf.WriteVarInt((int) PacketType.ServerControl);
            buf.WriteBool(Control);
        }

        public void Read(ByteBuf buf)
        {
            Control = buf.ReadBool();
        }
    }
    
    public class PacketMouseMove : IPacket
    {
        public float PercentX { get; private set; }
        public float PercentY { get; private set; }
        public PacketMouseMove() {}
        public PacketMouseMove(float percentX, float percentY)
        {
            PercentX = percentX;
            PercentY = percentY;
        }
        public PacketMouseMove(DoubleKey<float, float> percent)
        {
            PercentX = percent.X;
            PercentY = percent.Y;
        }
        public void Write(ByteBuf buf)
        {
            buf.WriteVarInt((int) PacketType.MouseMove);
            buf.WriteFloat(PercentX);
            buf.WriteFloat(PercentY);
        }

        public void Read(ByteBuf buf)
        {
            PercentX = buf.ReadFloat();
            PercentY = buf.ReadFloat();
        }
    }

    public class PacketCursorType : IPacket
    {
        public int Cursor { get; private set; }
        public PacketCursorType() {}

        public PacketCursorType(int cursor)
        {
            Cursor = cursor;
        }
        
        public void Write(ByteBuf buf)
        {
            buf.WriteVarInt((int) PacketType.CursorEvent);
            buf.WriteVarInt(Cursor);
        }

        public void Read(ByteBuf buf)
        {
            Cursor = buf.ReadVarInt();
        }
    }

    public class PacketMouseEvent : IPacket
    {
        public int Id { get; private set; }
        public int Data { get; private set; }

        public PacketMouseEvent() {}

        public PacketMouseEvent(int id, int data)
        {
            Id = id;
            Data = data;
        }

        public void Write(ByteBuf buf)
        {
            buf.WriteVarInt((int) PacketType.MouseEvent);
            buf.WriteVarInt(Id);
            buf.WriteVarInt(Data);
        }

        public void Read(ByteBuf buf)
        {
            Id = buf.ReadVarInt();
            Data = buf.ReadVarInt();
        }
    }

    public class PacketKeyEvent : IPacket
    {
        public byte Id { get; private set; }
        public int Flag { get; private set; }

        public PacketKeyEvent() {}
        public PacketKeyEvent(byte id, int flag)
        {
            Id = id;
            Flag = flag;
        }

        public void Write(ByteBuf buf)
        {
            buf.WriteVarInt((int) PacketType.KeyEvent);
            buf.WriteByte(Id);
            buf.WriteVarInt(Flag);
        }

        public void Read(ByteBuf buf)
        {
            Id = (byte) buf.ReadByte();
            Flag = buf.ReadVarInt();
        }
    }
}
