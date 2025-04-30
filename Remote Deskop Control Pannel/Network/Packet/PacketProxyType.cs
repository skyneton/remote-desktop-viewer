using NetworkLibrary.Networks.Packet;
using NetworkLibrary.Utils;

namespace RemoteDeskopControlPannel.Network.Packet
{
    internal class PacketProxyType : IPacket
    {
        public int PacketPrimaryKey => 2;
        public bool IsServer { get; private set; } = false;
        public PacketProxyType() { }
        public PacketProxyType(bool isServer)
        {
            IsServer = isServer;
        }

        public void Read(ByteBuf buf)
        {
            IsServer = buf.ReadBool();
        }

        public void Write(ByteBuf buf)
        {
            buf.WriteBool(IsServer);
        }
    }
}
