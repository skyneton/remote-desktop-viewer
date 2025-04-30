using NetworkLibrary.Networks.Packet;
using NetworkLibrary.Utils;

namespace RemoteDeskopControlPannel.Network.Packet
{
    internal class PacketMousePosition : IPacket
    {
        public int PacketPrimaryKey => 10;
        public int X { get; private set; }
        public int Y { get; private set; }
        public PacketMousePosition() { }
        public PacketMousePosition(int x, int y)
        {
            X = x;
            Y = y;
        }

        public void Read(ByteBuf buf)
        {
            X = buf.ReadVarInt();
            Y = buf.ReadVarInt();
        }

        public void Write(ByteBuf buf)
        {
            buf.WriteVarInt(X);
            buf.WriteVarInt(Y);
        }
    }
}
