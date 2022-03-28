using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using RemoteDesktopViewer.Network.Packet.Data;
using RemoteDesktopViewer.Utils;

namespace RemoteDesktopViewer.Network
{
    public class RemoteServer
    {
        private const int ThreadEmptyDelay = 500;
        private const int ThreadDelay = 50;
        
        internal static RemoteServer Instance { get; private set; }
        private TcpListener _listener;
        public bool IsAvailable { get; private set; }
        private readonly ConcurrentBag<NetworkManager> _networkManagers = new();

        private readonly int _port;
        internal string Password { get; private set; }

        private const long Timeout = 20 * 1000;

        public int ClientLength => _networkManagers.Count;

        private readonly ThreadFactory _threadFactory = new();

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

            _listener.BeginAcceptTcpClient(AcceptSocket, null);
            
            _threadFactory.LaunchThread(new Thread(ClientUpdateWorker), false).Name = "Client Update Thread";
            _threadFactory.LaunchThread(new Thread(ScreenThreadManager.Worker), false).Name = "Screen Thread";
        }

        private void AcceptSocket(IAsyncResult result)
        {
            try
            {
                _networkManagers.Add(new NetworkManager(_listener.EndAcceptTcpClient(result)));
                _listener.BeginAcceptTcpClient(AcceptSocket, null);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
            }
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

        internal void Broadcast(Packet.IPacket packet, bool authenticate = true)
        {
            var buf = new ByteBuf();
            packet.Write(buf);

            var beforeLength = buf.WriteLength;

            var data = NetworkManager.CompressionEnabled ? NetworkManager.Compress(buf) : buf.Flush();
            
            Debug.WriteLine($"Before: {beforeLength}, After: {data.Length}");
            
            foreach (var networkManager in _networkManagers)
            {
                if(authenticate && !networkManager.IsAuthenticate) continue;
                networkManager.SendBytes(data);
            }
        }
    }
}