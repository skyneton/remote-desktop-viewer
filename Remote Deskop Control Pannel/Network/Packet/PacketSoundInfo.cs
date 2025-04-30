using NetworkLibrary.Networks.Packet;
using NetworkLibrary.Utils;

namespace RemoteDeskopControlPannel.Network.Packet
{
    internal class PacketSoundInfo : IPacket
    {
        public int PacketPrimaryKey => 12;
        public int SampleRate { get; private set; }
        public int BitsPerSample { get; private set; }
        public int Channels { get; private set; }

        public void Read(ByteBuf buf)
        {
            SampleRate = buf.ReadVarInt();
            BitsPerSample = buf.ReadVarInt();
            Channels = buf.ReadVarInt();
        }

        public void Write(ByteBuf buf)
        {
            buf.WriteVarInt(SampleRate);
            buf.WriteVarInt(BitsPerSample);
            buf.WriteVarInt(Channels);
        }
    }
}
