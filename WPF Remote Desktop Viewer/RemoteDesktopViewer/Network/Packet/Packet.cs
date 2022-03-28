using RemoteDesktopViewer.Utils;

namespace RemoteDesktopViewer.Network.Packet
{
    public interface IPacket
    {
        void Write(ByteBuf buf);

        void Read(NetworkManager networkManager, ByteBuf buf);
    }
}