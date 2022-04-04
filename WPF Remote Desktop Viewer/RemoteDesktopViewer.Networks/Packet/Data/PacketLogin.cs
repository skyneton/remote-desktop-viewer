using RemoteDesktopViewer.Utils;

namespace RemoteDesktopViewer.Networks.Packet.Data
{
    public class PacketLogin : IPacket
    {
        public string Password { get; private set; }
        public PacketLogin(){}
        public PacketLogin(string password)
        {
            Password = password;
        }

        public void Write(ByteBuf buf)
        {
            buf.WriteVarInt((int) PacketType.Login);
            buf.WriteString(Password);
        }

        public void Read(ByteBuf buf)
        {
            Password = buf.ReadString();
        }
    }
}