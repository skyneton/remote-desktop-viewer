using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading;
using RemoteDesktopViewer.Network.Packet.Data;
using RemoteDesktopViewer.Utils;

namespace RemoteDesktopViewer.Network
{
    public class RemoteClient
    {
        internal static RemoteClient Instance { get; private set; } = new RemoteClient();
        public bool IsAvailable { get; private set; }
        
        private readonly ConcurrentBag<NetworkManager> _networkManagers = new ConcurrentBag<NetworkManager>();
        private readonly ConcurrentQueue<NetworkManager> _destroyNetworks = new ConcurrentQueue<NetworkManager>();

        private const long Timeout = 20 * 1000;

        public int ClientLength => _networkManagers.Count;

        private readonly ThreadFactory _threadFactory = new ThreadFactory();

        private RemoteClient()
        {
            IsAvailable = true;
            _threadFactory.LaunchThread(new Thread(ClientUpdateWorker), false).Name = "Client Update Thread";
            _threadFactory.LaunchThread(new Thread(ClientDestroyWorker), false).Name = "Client Destroy Thread";
        }
        
        internal NetworkManager Connect(string ip, int port, string password)
        {
            var networkManager = new NetworkManager(new TcpClient(ip, port));
            _networkManagers.Add(networkManager);
            networkManager.SendPacket(new PacketLogin(password));

            return networkManager;
        }

        public void Close()
        {
            IsAvailable = false;
            
            _threadFactory.KillAll();
        }

        private void ClientUpdateWorker()
        {
            while (IsAvailable)
            {
                var currentTimeMillis = TimeManager.CurrentTimeMillis;
                foreach (var networkManager in _networkManagers)
                {
                    if (!(networkManager?.IsAvailable ?? false))
                        continue;

                    if (!networkManager.Connected || currentTimeMillis - networkManager.LastPacketMillis > Timeout)
                    {
                        networkManager.Disconnect();
                        _destroyNetworks.Enqueue(networkManager);
                        continue;
                    }

                    networkManager.Update();
                }
            }
        }

        private void ClientDestroyWorker()
        {
            while (IsAvailable)
            {
                while (!_destroyNetworks.IsEmpty)
                {
                    if(!_destroyNetworks.TryDequeue(out var networkManager)) continue;

                    if (networkManager.Connected)
                        networkManager.Close();
                    
                    _networkManagers.Remove(networkManager);
                }
            }
        }
    }
}