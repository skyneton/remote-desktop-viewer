using RemoteDesktopViewer.Utils;

namespace RemoteDesktopViewer.Networks.Packet.Data
{
    public class PacketScreen : IPacket
    {
        public byte[] Data { get; private set; }
        public PacketScreen() {}

        public PacketScreen(byte[] jpeg)
        {
            Data = jpeg;
        }

        public void Write(ByteBuf buf)
        {
            buf.WriteVarInt((int) PacketType.Screen);
            buf.Write(Data);
        }

        public void Read(ByteBuf buf)
        {
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