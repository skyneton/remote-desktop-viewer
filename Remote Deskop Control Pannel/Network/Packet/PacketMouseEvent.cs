using NetworkLibrary.Networks.Packet;
using NetworkLibrary.Utils;

namespace RemoteDeskopControlPannel.Network.Packet
{
    internal class PacketMouseEvent : IPacket
    {
        public int PacketPrimaryKey => 11;
        public int Type { get; private set; }
        public int Flag { get; private set; }
        public PacketMouseEvent() { }
        public PacketMouseEvent(int type, int flag)
        {
            Type = type;
            Flag = flag;
        }

        public void Read(ByteBuf buf)
        {
            Type = buf.ReadVarInt();
            Flag = buf.ReadVarInt();
        }

        public void Write(ByteBuf buf)
        {
            buf.WriteVarInt(Type);
            buf.WriteVarInt(Flag);
        }
    }
}
