using NetworkLibrary.Networks.Packet;
using NetworkLibrary.Utils;

namespace RemoteDeskopControlPannel.Network.Packet
{
    internal class PacketLogin : IPacket
    {
        public int PacketPrimaryKey => 1;
        public string Password { get; private set; } = string.Empty;
        public PacketLogin() { }
        public PacketLogin(string password)
        {
            Password = password;
        }

        public void Read(ByteBuf buf)
        {
            Password = buf.ReadString();
        }

        public void Write(ByteBuf buf)
        {
            buf.WriteString(Password);
        }
    }
}
