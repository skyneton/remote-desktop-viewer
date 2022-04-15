using RemoteDesktopViewer.Utils;

namespace RemoteDesktopViewer.Networks.Packet.Data
{
    public class PacketClipboard : IPacket
    {
        public string Id { get; private set; }
        public byte Type { get; private set; }
        public byte[] Data { get; private set; }
        public PacketClipboard() { }

        public PacketClipboard(string id, byte type, byte[] data)
        {
            Id = id;
            Type = type;
            Data = data;
        }
        public void Write(ByteBuf buf)
        {
            buf.WriteVarInt((int) PacketType.ClipboardEvent);
            buf.WriteString(Id);
            buf.WriteByte(Type);
            if(Data != null)
                buf.Write(Data);
        }

        public void Read(ByteBuf buf)
        {
            Id = buf.ReadString();
            Type = (byte) buf.ReadByte();
            if(buf.Length > 0)
                Data = buf.Read(buf.Length);
        }
    }
}