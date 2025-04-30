using NetworkLibrary.Networks.Packet;
using NetworkLibrary.Utils;

namespace RemoteDeskopControlPannel.Network.Packet
{
    internal class PacketFullScreen : IPacket
    {
        public int PacketPrimaryKey => 5;
        public byte[] Data { get; private set; } = [];
        public PacketFullScreen() { }
        public PacketFullScreen(byte[] data)
        {
            Data = data;
        }

        public void Read(ByteBuf buf)
        {
            Data = buf.Read(buf.Length);
        }

        public void Write(ByteBuf buf)
        {
            buf.Write(Data);
        }
    }
}
