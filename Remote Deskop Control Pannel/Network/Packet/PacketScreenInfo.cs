using NetworkLibrary.Networks.Packet;
using NetworkLibrary.Utils;
using RemoteDeskopControlPannel.Utils;

namespace RemoteDeskopControlPannel.Network.Packet
{
    internal class PacketScreenInfo : IPacket
    {
        public int PacketPrimaryKey => 4;
        public QualityMode Quality { get; private set; }
        public PacketScreenInfo() { }
        public PacketScreenInfo(QualityMode quality)
        {
            Quality = quality;
        }

        public void Read(ByteBuf buf)
        {
            Quality = (QualityMode)buf.ReadVarInt();
        }

        public void Write(ByteBuf buf)
        {
            buf.WriteVarInt((int)Quality);
        }
    }
}
