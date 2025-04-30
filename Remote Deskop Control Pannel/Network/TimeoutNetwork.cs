using System.Net.Sockets;
using RemoteDeskopControlPannel.Network.Packet;

namespace RemoteDeskopControlPannel.Network
{
    internal class TimeoutNetwork : NetworkLibrary.Networks.Network
    {
        public TimeoutNetwork(Socket socket) : base(socket)
        {
            KeepAliveTimeout = 60 * 1000;
        }

        protected override void TimeoutHandler(long deltaTime)
        {
            SendPacket(new PacketKeepAlive());
        }

        //protected override void ExceptionHandler(Exception e)
        //{
        //    MessageBox.Show(e.ToString());
        //}
    }
}
