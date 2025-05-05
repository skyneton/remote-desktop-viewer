using System.Net.Sockets;
using NetworkLibrary.Networks.Multi;
using RemoteDeskopControlPannel.Network.Packet;

namespace RemoteDeskopControlPannel.Network
{
    internal class TimeoutNetwork : MultiNetwork
    {
        public TimeoutNetwork(Socket socket) : base(socket)
        {
            KeepAliveTimeout = 60 * 1000;
            socket.ReceiveBufferSize = 1024 * 18;
        }

        protected override void TimeoutHandler(long deltaTime)
        {
            SendPacket(new PacketKeepAlive());
        }

        protected override void ExceptionHandler(Exception e)
        {
            //MessageBox.Show(e.ToString());
        }
    }
}
