using RemoteDesktopViewer.Utils;

namespace RemoteDesktopViewer.Networks.Packet.Data
{
    public class PacketKeepAlive : IPacket
    {
        public void Write(ByteBuf buf)
        {
            buf.WriteVarInt((int) PacketType.KeepAlive);
        }

        public void Read(ByteBuf buf)
        {
        }
    }
}