using System.Windows;
using RemoteDesktopViewer.Utils;

namespace RemoteDesktopViewer.Network.Packet.Data
{
    public class PacketDisconnect : Packet
    {
        private string _reason;
        internal PacketDisconnect(){}
        public PacketDisconnect(string reason)
        {
            _reason = reason;
        }

        internal override void Write(ByteBuf buf)
        {
            buf.WriteVarInt((int) PacketType.Disconnect);
            buf.WriteString(_reason);
        }

        internal override void Read(NetworkManager networkManager, ByteBuf buf)
        {
            MessageBox.Show(buf.ReadString());
            networkManager.Disconnect();
        }
    }
}