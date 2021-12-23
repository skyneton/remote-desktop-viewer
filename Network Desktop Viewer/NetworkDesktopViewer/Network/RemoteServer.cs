using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using RemoteDesktopViewer.Network.Packet.Data;
using RemoteDesktopViewer.Utils;

namespace RemoteDesktopViewer.Network
{
    public class RemoteServer
    {
        internal static RemoteServer Instance { get; private set; }
        private TcpListener _listener;
        public bool IsAvailable { get; private set; }
        private readonly ConcurrentBag<NetworkManager> _networkManagers = new ConcurrentBag<NetworkManager>();
        private readonly ConcurrentQueue<NetworkManager> _destroyNetworks = new ConcurrentQueue<NetworkManager>();

        private readonly int _port;
        internal string Password { get; private set; }

        private const long Timeout = 20 * 1000;

        public int ClientLength => _networkManagers.Count;

        private readonly ThreadFactory _threadFactory = new ThreadFactory();

        public bool ServerControl { get; private set; }

        internal RemoteServer(int port, string password)
        {
            Instance = this;
            
            _port = port;
            Password = password.ToSha256();
            
            InitServer();
            Start();
        }

        internal void UpdatePassword(string password)
        {
            Password = password.ToSha256();
        }

        internal void UpdateServerControl(bool control)
        {
            ServerControl = control;
            Broadcast(new PacketServerControl(ServerControl));
        }

        private void InitServer()
        {
            _listener = new TcpListener(IPAddress.Any, _port)
            {
                Server =
                {
                    NoDelay = true,
                    SendTimeout = 500
                }
            };
        }

        public void Close()
        {
            IsAvailable = false;
            foreach (var networkManager in _networkManagers)
            {
                networkManager?.Close();
            }

            foreach (var networkManager in _destroyNetworks)
            {
                networkManager?.Close();
            }
            
            try {_listener.Stop();}
            catch (Exception)
            {
                // ignored
            }

            Instance = null;
            
            _threadFactory.KillAll();
        }

        private void Start()
        {
            IsAvailable = true;

            try
            {
                _listener.Start();
            }
            catch (Exception)
            {
                _threadFactory.KillAll();
                throw;
            }

            _threadFactory.LaunchThread(new Thread(AcceptSocketWorker), false).Name = "Client Bind Thread";
            _threadFactory.LaunchThread(new Thread(ClientUpdateWorker), false).Name = "Client Update Thread";
            _threadFactory.LaunchThread(new Thread(ClientDestroyWorker), false).Name = "Client Destroy Thread";
            _threadFactory.LaunchThread(new Thread(ScreenThreadManager.Worker), false).Name = "Screen Thread";
        }

        private void AcceptSocketWorker()
        {
            while (IsAvailable)
            {
                try
                {
                    _networkManagers.Add(new NetworkManager(_listener.AcceptTcpClient()));
                }
                catch (Exception)
                {
                    // ignored
                }
            }
        }

        private void ClientUpdateWorker()
        {
            while (IsAvailable)
            {
                var currentTimeMillis = TimeManager.CurrentTimeMillis;
                foreach (var networkManager in _networkManagers)
                {
                    if (!(networkManager?.IsAvailable ?? false))
                    {
                        if(!_destroyNetworks.Contains(networkManager))
                            _destroyNetworks.Enqueue(networkManager);
                        continue;
                    }

                    if (!networkManager.Connected ||
                        currentTimeMillis - networkManager.LastPacketMillis > Timeout)
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

        internal void Broadcast(Packet.Packet packet, bool authenticate = true)
        {
            foreach (var networkManager in _networkManagers)
            {
                if(authenticate && !networkManager.IsAuthenticate) continue;
                networkManager.SendPacket(packet);
            }
        }
    }
}