using RemoteDesktopViewer.Utils;

namespace RemoteDesktopViewer.Networks.Packet.Data
{
    public class PacketClipboard : IPacket
    {
        public string Format { get; private set; }
        public byte DataType { get; private set; }
        public byte[] Data { get; private set; }
        public PacketClipboard() { }

        public PacketClipboard(string format, byte dataType, byte[] data)
        {
            Format = format;
            DataType = dataType;
            Data = data;
        }
        public void Write(ByteBuf buf)
        {
            buf.WriteVarInt((int) PacketType.ClipboardEvent);
            buf.WriteString(Format);
            buf.WriteByte(DataType);
            buf.Write(Data);
        }

        public void Read(ByteBuf buf)
        {
            Format = buf.ReadString();
            DataType = (byte) buf.ReadByte();
            Data = buf.Read(buf.Length);
        }
    }
}