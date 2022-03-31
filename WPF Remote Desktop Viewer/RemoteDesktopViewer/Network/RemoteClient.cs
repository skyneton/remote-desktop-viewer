using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using RemoteDesktopViewer.Network.Packet.Data;
using RemoteDesktopViewer.Utils;

namespace RemoteDesktopViewer.Network
{
    public class RemoteClient
    {
        private const int ConnectTime = 600;
        private const int ThreadEmptyDelay = 500;
        private const int ThreadDelay = 50;
        internal static readonly RemoteClient Instance = new();
        public bool IsAvailable { get; private set; }
        
        private readonly ConcurrentBag<NetworkManager> _networkManagers = new();

        private const long Timeout = 20 * 1000;

        public int ClientLength => _networkManagers.Count;

        private readonly ThreadFactory _threadFactory = new();

        private RemoteClient()
        {
            IsAvailable = true;
            _threadFactory.LaunchThread(new Thread(ClientUpdateWorker), false).Name = "Client Update Thread";
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
                try
                {
                    if (_networkManagers.IsEmpty)
                    {
                        Thread.Sleep(ThreadEmptyDelay);
                        continue;
                    }

                    var destroy = new Queue<NetworkManager>();
                    ClientForEachUpdate(destroy);
                    ClientForEachDestroy(destroy);

                    Thread.Sleep(ThreadDelay);
                }
                catch (Exception)
                {
                    //ignored
                }
            }
        }
        

        private void ClientForEachUpdate(Queue<NetworkManager> destroy)
        {
            foreach (var networkManager in _networkManagers)
            {
                if (!(networkManager?.IsAvailable ?? false))
                {
                    destroy.Enqueue(networkManager);
                    continue;
                }
                networkManager.Update();
            }
        }

        private void ClientForEachDestroy(Queue<NetworkManager> destroy)
        {
            while(destroy.Count > 0)
            {
                var networkManager = destroy.Dequeue();
            
                networkManager.Close();
                _networkManagers.Remove(networkManager);
            }
        }
    }
}