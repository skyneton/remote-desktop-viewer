using NetworkLibrary.Networks.Packet;
using NetworkLibrary.Utils;

namespace RemoteDeskopControlPannel.Network.Packet
{
    internal class PacketKeepAlive : IPacket
    {
        public int PacketPrimaryKey => 0;

        public void Read(ByteBuf buf) { }

        public void Write(ByteBuf buf) { }
    }
}
