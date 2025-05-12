using NetworkLibrary.Networks.Packet;
using NetworkLibrary.Utils;

namespace RemoteDeskopControlPannel.Network.Packet
{
    internal class PacketFullScreen : IPacket
    {
        public int PacketPrimaryKey => 5;
        public int CompressType { get; private set; } = 0;
        public byte[] Data { get; private set; } = [];
        public PacketFullScreen() { }
        public PacketFullScreen(int compressType, byte[] data)
        {
            CompressType = compressType;
            Data = data;
        }

        public void Read(ByteBuf buf)
        {
            CompressType = buf.ReadVarInt();
            Data = buf.Read(buf.Length);
        }

        public void Write(ByteBuf buf)
        {
            buf.WriteVarInt(CompressType);
            buf.Write(Data);
        }
    }
}
