using NetworkLibrary.Networks.Packet;
using NetworkLibrary.Utils;

namespace RemoteDeskopControlPannel.Network.Packet
{
    internal class PacketSoundChunk : IPacket
    {
        public int PacketPrimaryKey => 13;
        public byte[] Chunk { get; private set; } = [];
        public PacketSoundChunk() { }
        public PacketSoundChunk(byte[] chunk)
        {
            Chunk = chunk;
        }

        public void Read(ByteBuf buf)
        {
            Chunk = buf.ReadByteArray();
        }

        public void Write(ByteBuf buf)
        {
            buf.WriteByteArray(Chunk);
        }
    }
}
