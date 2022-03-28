using RemoteDesktopViewer.Utils;

namespace RemoteDesktopViewer.Network.Packet.Data
{
    public class PacketKeepAlive : IPacket
    {
        public void Write(ByteBuf buf)
        {
            buf.WriteVarInt((int) PacketType.KeepAlive);
        }

        public void Read(NetworkManager networkManager, ByteBuf buf)
        {
            throw new System.NotImplementedException();
        }
    }
}