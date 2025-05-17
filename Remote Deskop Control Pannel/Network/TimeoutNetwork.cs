using System.IO;
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
        }

        protected override void TimeoutHandler(long deltaTime)
        {
            SendPacket(new PacketKeepAlive());
        }

        protected override void ExceptionHandler(Exception e)
        {
            if (e is SocketException { SocketErrorCode: SocketError.OperationAborted } || e is SocketException { SocketErrorCode: SocketError.ConnectionReset }) return;
            try
            {
                File.AppendAllText("err.log", $"{e}\n");
            }
            catch (Exception) { }
        }
    }
}
