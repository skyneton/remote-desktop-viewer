using RemoteDesktopViewer.Utils;

namespace RemoteDesktopViewer.Network.Packet.Data
{
    public class PacketLogin : Packet
    {
        private string _password;
        internal PacketLogin(){}
        public PacketLogin(string password)
        {
            _password = password;
        }

        internal override void Write(ByteBuf buf)
        {
            buf.WriteVarInt((int) PacketType.Login);
            buf.WriteString(_password);
        }

        internal override void Read(NetworkManager networkManager, ByteBuf buf)
        {
            networkManager.ServerLogin(buf.ReadString());
        }
    }
}