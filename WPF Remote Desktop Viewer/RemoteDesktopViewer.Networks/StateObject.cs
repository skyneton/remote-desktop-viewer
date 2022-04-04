using System.Net.Sockets;

namespace RemoteDesktopViewer.Networks
{
    public class StateObject
    {
        public const int BufferSize = 1024 * 12;
        public readonly byte[] Buffer = new byte[BufferSize];
        public Socket TargetSocket { get; }

        public StateObject(Socket socket)
        {
            TargetSocket = socket;
        }
    }
}