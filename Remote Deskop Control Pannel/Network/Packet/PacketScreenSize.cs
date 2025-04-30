using NetworkLibrary.Networks.Packet;
using NetworkLibrary.Utils;

namespace RemoteDeskopControlPannel.Network.Packet
{
    internal class PacketScreenSize : IPacket
    {
        public int PacketPrimaryKey => 3;
        public int Width { get; private set; }
        public int Height { get; private set; }
        public PacketScreenSize() { }
        public PacketScreenSize(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public void Read(ByteBuf buf)
        {
            Width = buf.ReadVarInt();
            Height = buf.ReadVarInt();
        }

        public void Write(ByteBuf buf)
        {
            buf.WriteVarInt(Width);
            buf.WriteVarInt(Height);
        }
    }
}
