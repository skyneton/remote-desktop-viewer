using RemoteDesktopViewer.Utils;

namespace RemoteDesktopViewer.Networks.Packet
{
    public interface IPacket
    {
        void Write(ByteBuf buf);

        void Read(ByteBuf buf);
    }
}