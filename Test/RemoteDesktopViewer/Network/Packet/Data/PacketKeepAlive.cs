using RemoteDesktopViewer.Utils;

namespace RemoteDesktopViewer.Network.Packet.Data
{
    public class PacketKeepAlive : Packet
    {
        internal override void Write(ByteBuf buf)
        {
            buf.WriteVarInt((int) PacketType.KeepAlive);
        }
    }
}