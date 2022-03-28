using RemoteDesktopViewer.Utils;

namespace RemoteDesktopViewer.Network.Packet.Data
{
    public class PacketLogin : IPacket
    {
        private string _password;
        internal PacketLogin(){}
        public PacketLogin(string password)
        {
            _password = password;
        }

        public void Write(ByteBuf buf)
        {
            buf.WriteVarInt((int) PacketType.Login);
            buf.WriteString(_password);
        }

        public void Read(NetworkManager networkManager, ByteBuf buf)
        {
            networkManager.ServerLogin(buf.ReadString());
        }
    }
}