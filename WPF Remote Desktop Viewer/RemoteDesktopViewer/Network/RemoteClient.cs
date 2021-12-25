using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using RemoteDesktopViewer.Network.Packet.Data;
using RemoteDesktopViewer.Utils;

namespace RemoteDesktopViewer.Network
{
    public class RemoteClient
    {
        private const int ConnectTime = 1000;
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
        
        internal async Task<NetworkManager> Connect(string ip, int port, string password)
        {
            var client = await ConnectTimeout(ip, port, ConnectTime);
            if (client == null)
                return null;
            
            var networkManager = new NetworkManager(client);
            _networkManagers.Add(networkManager);
            networkManager.SendPacket(new PacketLogin(password));

            return networkManager;
        }

        private Task<TcpClient> ConnectTimeout(string ip, int port, int timeout)
        {
            return Task.Run(() =>
            {
                var client = new TcpClient(AddressFamily.InterNetwork);
                var result = client.BeginConnect(ip, port, null, null);
                var connected = result.AsyncWaitHandle.WaitOne(timeout, true);
                try
                {
                    client.EndConnect(result);
                    if (connected)
                    {
                        return client;
                    }
                }
                catch (Exception)
                {
                    // ignored
                }

                return null;
            });
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