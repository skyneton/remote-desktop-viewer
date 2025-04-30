using NetworkLibrary.Networks.Packet;
using RemoteDeskopControlPannel.Network.Packet;
using RemoteDeskopControlPannel.Utils;

namespace RemoteDeskopControlPannel.Network.Handler
{
    internal class ServerPacketHandler(bool skipLogin) : IPacketHandler
    {
        public bool IsLogined { get; private set; } = false;
        public ActiveMode ActiveType { get; private set; } = ActiveMode.None;
        public void Handle(NetworkLibrary.Networks.Network network, IPacket packet)
        {
            switch (packet)
            {
                case PacketLogin:
                    LoginPacketReceive(network, (PacketLogin)packet);
                    break;
                case PacketProxyType:
                    ProxyTypeReceive(network, (PacketProxyType)packet);
                    break;
                case PacketProxyConnected:
                    ProxyConnectedReceive(network, (PacketProxyConnected)packet);
                    break;
                case PacketScreenSize:
                    ScreenSizeReceive((PacketScreenSize)packet);
                    break;
                case PacketFullScreen:
                    FullScreenReqReceive(network);
                    break;
                case PacketKeyboardInput:
                    KeyboardInputReceive((PacketKeyboardInput)packet);
                    break;
                case PacketMousePosition:
                    MousePositionReceive((PacketMousePosition)packet);
                    break;
                case PacketMouseEvent:
                    MouseEventReceive((PacketMouseEvent)packet);
                    break;
            }
            ;
        }

        private static void ScreenSizeReceive(PacketScreenSize packet)
        {
            MainWindow.Instance.Server?.UpdateScreenSize(packet.Width, packet.Height);
        }

        private static void MouseEventReceive(PacketMouseEvent packet)
        {
            NativeKeyboardMouse.mouse_event(packet.Type, 0, 0, packet.Flag, 0);
        }

        private static void MousePositionReceive(PacketMousePosition packet)
        {
            NativeKeyboardMouse.SetCursorPos(packet.X, packet.Y);
        }

        private static void KeyboardInputReceive(PacketKeyboardInput packet)
        {
            NativeKeyboardMouse.keybd_event(packet.KeyCode, 0, packet.Flag, 0);
        }

        private static void FullScreenReqReceive(NetworkLibrary.Networks.Network network)
        {
            MainWindow.Instance.Server?.AcceptedClients?.Enqueue(network);
        }

        private void ProxyConnectedReceive(NetworkLibrary.Networks.Network network, PacketProxyConnected packet)
        {
            if (!skipLogin)
            {
                network.Disconnect();
                return;
            }
            if (MainWindow.Instance.Server != null)
                MainWindow.Instance.Server.proxyConnected = packet.IsConnected;
        }

        private void ProxyTypeReceive(NetworkLibrary.Networks.Network network, PacketProxyType packet)
        {
            if (ActiveType != ActiveMode.Logined)
            {
                network.Disconnect();
                return;
            }
            ActiveType = packet.IsServer ? ActiveMode.Server : ActiveMode.Client;
            MainWindow.Instance.Server?.ReceiveClient(network, ActiveType);
        }

        private void LoginPacketReceive(NetworkLibrary.Networks.Network network, PacketLogin packet)
        {
            if (ActiveType != ActiveMode.None) return;
            if (MainWindow.Instance.Server?.Password != packet.Password)
            {
                network.Disconnect();
                return;
            }
            ActiveType = ActiveMode.Logined;
        }
    }
}
