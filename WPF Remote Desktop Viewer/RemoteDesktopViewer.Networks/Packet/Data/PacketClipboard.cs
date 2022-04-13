using RemoteDesktopViewer.Utils;

namespace RemoteDesktopViewer.Networks.Packet.Data
{
    public class PacketClipboard : IPacket
    {
        public byte DataType { get; private set; }
        public byte[] Data { get; private set; }
        public PacketClipboard() { }

        public PacketClipboard(byte dataType, byte[] data)
        {
            DataType = dataType;
            Data = data;
        }
        public void Write(ByteBuf buf)
        {
            buf.WriteVarInt((int) PacketType.ClipboardEvent);
            buf.WriteByte(DataType);
            buf.Write(Data);
        }

        public void Read(ByteBuf buf)
        {
            DataType = (byte) buf.ReadByte();
            Data = buf.Read(buf.Length);
        }
    }
}