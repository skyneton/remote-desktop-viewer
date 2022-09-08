using RemoteDesktopViewer.Utils;

namespace RemoteDesktopViewer.Networks.Packet.Data
{
    public class PacketScreen : IPacket
    {
        #region values
        public int Width { get; private set; }
        public int Height { get; private set; }
        public byte Format { get; private set; }
        public byte[] Data { get; private set; }
        #endregion
        public PacketScreen() {}

        public PacketScreen(int width, int height, byte format, byte[] data)
        {
            Width = width;
            Height = height;
            Format = format;
            Data = data;
        }

        public void Write(ByteBuf buf)
        {
            buf.WriteVarInt((int) PacketType.Screen);
            buf.WriteVarInt(Width);
            buf.WriteVarInt(Height);
            buf.WriteByte(Format);
            buf.Write(Data);
        }

        public void Read(ByteBuf buf)
        {
            Width = buf.ReadVarInt();
            Height = buf.ReadVarInt();
            Format = (byte) buf.ReadByte();
            Data = buf.Read(buf.Length);
        }
    }

    public class PacketScreenChunk : IPacket
    {
        public byte[] Data { get; private set; }
        public PacketScreenChunk() {}

        public PacketScreenChunk(byte[] chunk)
        {
            Data = chunk;
        }

        public void Write(ByteBuf buf)
        {
            buf.WriteVarInt((int) PacketType.ScreenChunk);
            buf.Write(Data);
        }

        public void Read(ByteBuf buf)
        {
            Data = buf.Read(buf.Length);
        }
    }
}