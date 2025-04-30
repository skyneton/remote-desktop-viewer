using NetworkLibrary.Networks.Packet;
using NetworkLibrary.Utils;

namespace RemoteDeskopControlPannel.Network.Packet
{
    internal class PacketCursorType : IPacket
    {
        public int PacketPrimaryKey => 8;
        public int Cursor { get; private set; }
        public PacketCursorType() { }
        public PacketCursorType(int cursor)
        {
            Cursor = cursor;
        }

        public void Read(ByteBuf buf)
        {
            Cursor = buf.ReadVarInt();
        }

        public void Write(ByteBuf buf)
        {
            buf.WriteVarInt(Cursor);
        }
    }
}
