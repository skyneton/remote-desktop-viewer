using NetworkLibrary.Networks.Packet;
using NetworkLibrary.Utils;

namespace RemoteDeskopControlPannel.Network.Packet
{
    internal class PacketProxyConnected : IPacket
    {
        public int PacketPrimaryKey => 7;
        public bool IsConnected { get; private set; }
        public PacketProxyConnected() { }
        public PacketProxyConnected(bool isConnected)
        {
            IsConnected = isConnected;
        }

        public void Read(ByteBuf buf)
        {
            IsConnected = buf.ReadBool();
        }

        public void Write(ByteBuf buf)
        {
            buf.WriteBool(IsConnected);
        }
    }
}
