using System.Windows;
using RemoteDesktopViewer.Utils;

namespace RemoteDesktopViewer.Network.Packet.Data
{
    public class PacketDisconnect : IPacket
    {
        private string _reason;
        internal PacketDisconnect(){}
        public PacketDisconnect(string reason)
        {
            _reason = reason;
        }

        public void Write(ByteBuf buf)
        {
            buf.WriteVarInt((int) PacketType.Disconnect);
            buf.WriteString(_reason);
        }

        public void Read(NetworkManager networkManager, ByteBuf buf)
        {
            MessageBox.Show(buf.ReadString());
            networkManager.Disconnect();
        }
    }
}