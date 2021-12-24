using RemoteDesktopViewer.Utils;

namespace RemoteDesktopViewer.Network.Packet
{
    public class Packet
    {
        internal virtual void Write(ByteBuf buf)
        {
        }

        internal virtual void Read(NetworkManager networkManager, ByteBuf buf)
        {
        }
    }
}