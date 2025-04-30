using NetworkLibrary.Networks.Packet;
using NetworkLibrary.Utils;

namespace RemoteDeskopControlPannel.Network.Packet
{
    internal class PacketKeyboardInput : IPacket
    {
        public int PacketPrimaryKey => 9;
        public int KeyCode { get; private set; }
        public int Flag { get; private set; }
        public PacketKeyboardInput() { }
        public PacketKeyboardInput(int keyCode, int flag)
        {
            KeyCode = keyCode;
            Flag = flag;
        }

        public void Read(ByteBuf buf)
        {
            KeyCode = buf.ReadVarInt();
            Flag = buf.ReadVarInt();
        }

        public void Write(ByteBuf buf)
        {
            buf.WriteVarInt(KeyCode);
            buf.WriteVarInt(Flag);
        }
    }
}
