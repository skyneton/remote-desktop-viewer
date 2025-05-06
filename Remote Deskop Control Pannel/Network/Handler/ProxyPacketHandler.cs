using NetworkLibrary.Networks.Multi;
using NetworkLibrary.Networks.Packet;
using RemoteDeskopControlPannel.Network.Packet;
using RemoteDeskopControlPannel.Utils;

namespace RemoteDeskopControlPannel.Network.Handler
{
    internal class ProxyPacketHandler : IMultiPacketHandler
    {
        public bool IsLogined { get; private set; } = false;
        public ActiveMode ActiveType { get; private set; } = ActiveMode.None;
        public void Handle(MultiNetwork network, IPacket packet)
        {
            switch (packet)
            {
                case PacketFullScreen:
                    FullScreenReceive((PacketFullScreen)packet);
                    break;
                case PacketScreenChunk:
                case PacketCursorType:
                case PacketSoundChunk:
                    ActiveHostBroadcastToClients(network, packet);
                    break;
                case PacketScreenInfo:
                    ScreenInfoReceive((PacketScreenInfo)packet);
                    break;
                case PacketSoundInfo:
                    SoundInfoReceive((PacketSoundInfo)packet);
                    break;
                case PacketScreenSize:
                case PacketKeyboardInput:
                case PacketMousePosition:
                case PacketMouseEvent:
                    ActiveClientToHost(network, packet);
                    break;
                case PacketLogin:
                    LoginPacketReceive(network, (PacketLogin)packet);
                    break;
                case PacketProxyType:
                    ProxyTypeReceive(network, (PacketProxyType)packet);
                    break;
            }
            ;
        }

        private void SoundInfoReceive(PacketSoundInfo packet)
        {
            if (ActiveType != ActiveMode.Server) return;
            MainWindow.Instance.Server?.UpdateSoundInfo(packet);
        }

        private void FullScreenReceive(PacketFullScreen packet)
        {
            if (ActiveType != ActiveMode.Server) return;
            var clients = MainWindow.Instance.Server?.AcceptedClients;
            if (clients == null) return;
            while (!clients.IsEmpty)
            {
                if (!clients.TryDequeue(out var client)) continue;
                client.SendPacket(packet);
            }
        }

        private void ScreenInfoReceive(PacketScreenInfo packet)
        {
            if (ActiveType != ActiveMode.Server) return;
            MainWindow.Instance.Server?.UpdateScreenProcessor(packet.Quality);
        }

        private void ProxyTypeReceive(MultiNetwork network, PacketProxyType packet)
        {
            if (ActiveType != ActiveMode.Logined)
            {
                network.Disconnect();
                return;
            }
            ActiveType = packet.IsServer ? ActiveMode.Server : ActiveMode.Client;
            MainWindow.Instance.Server?.ReceiveClient(network, ActiveType);
        }

        private void LoginPacketReceive(MultiNetwork network, PacketLogin packet)
        {
            if (ActiveType != ActiveMode.None) return;
            if (MainWindow.Instance.Server?.Password != packet.Password)
            {
                network.Disconnect();
                return;
            }
            ActiveType = ActiveMode.Logined;
        }

        private void ActiveHostBroadcastToClients(MultiNetwork network, IPacket packet)
        {
            if (ActiveType != ActiveMode.Server) return;
            MainWindow.Instance.Server?.Broadcast(packet);
        }

        private void ActiveClientToHost(MultiNetwork network, IPacket packet)
        {
            if (ActiveType != ActiveMode.Client) return;
            MainWindow.Instance.Server?.SendToHost(packet);
        }
    }
}
