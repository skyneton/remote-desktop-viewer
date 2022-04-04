using System.Windows;
using RemoteDesktopViewer.Utils;

namespace RemoteDesktopViewer.Networks.Packet.Data
{
    public class PacketDisconnect : IPacket
    {
        public string Reason { get; private set; }
        public PacketDisconnect(){}
        public PacketDisconnect(string reason)
        {
            Reason = reason;
        }

        public void Write(ByteBuf buf)
        {
            buf.WriteVarInt((int) PacketType.Disconnect);
            buf.WriteString(Reason);
        }

        public void Read(ByteBuf buf)
        {
            Reason = buf.ReadString();
        }
    }
}