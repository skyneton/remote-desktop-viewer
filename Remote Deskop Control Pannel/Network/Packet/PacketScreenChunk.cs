using NetworkLibrary.Networks.Packet;
using NetworkLibrary.Utils;

namespace RemoteDeskopControlPannel.Network.Packet
{
    internal class PacketScreenChunk : IPacket
    {
        public int PacketPrimaryKey => 6;
        public byte[] PixelPos { get; private set; } = [];
        public int CompressType { get; private set; }
        public byte[] PixelData { get; private set; } = [];
        public PacketScreenChunk() { }
        public PacketScreenChunk(byte[] pixelPos, int compressType, byte[] pixelData)
        {
            PixelPos = pixelPos;
            CompressType = compressType;
            PixelData = pixelData;
        }

        public void Read(ByteBuf buf)
        {
            PixelPos = buf.ReadByteArray();
            CompressType = buf.ReadVarInt();
            PixelData = buf.Read(buf.Length);
        }

        public void Write(ByteBuf buf)
        {
            buf.WriteByteArray(PixelPos);
            buf.WriteVarInt(CompressType);
            buf.Write(PixelData);
        }
    }
}
