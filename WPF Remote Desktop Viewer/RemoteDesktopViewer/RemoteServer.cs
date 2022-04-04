using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using RemoteDesktopViewer.Networks;
using RemoteDesktopViewer.Networks.Packet;
using RemoteDesktopViewer.Networks.Packet.Data;
using RemoteDesktopViewer.Threading;
using RemoteDesktopViewer.Utils;

namespace RemoteDesktopViewer
{
    public class RemoteServer
    {
        private const int ThreadEmptyDelay = 500;
        private const int ThreadDelay = 50;
        
        internal static RemoteServer Instance { get; private set; }
        private TcpListener _listener;
        public bool IsAvailable { get; private set; }
        private readonly ConcurrentBag<Network.Network> _networks = new();

        private readonly int _port;
        internal string Password { get; private set; }

        private const long Timeout = 20 * 1000;

        public int ClientLength => _networks.Count;

        private readonly ThreadFactory _threadFactory = new();

        public bool ServerControl { get; private set; }

        internal RemoteServer(int port, string password)
        {
            Instance = this;
            
            _port = port;
            UpdatePassword(password);
            
            InitServer();
            Start();
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
            foreach (var networkManager in _networks)
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
                _networks.Add(new Network.Network(_listener.EndAcceptTcpClient(result)));
                _listener.BeginAcceptTcpClient(AcceptSocket, null);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
            }
        }

        internal void UpdatePassword(string password)
        {
            Password = CryptoHelper.ToSha256(password);
        }

        internal void UpdateServerControl(bool control)
        {
            ServerControl = control;
            Broadcast(new PacketServerControl(ServerControl));
        }

        private void ClientUpdateWorker()
        {
            while (IsAvailable)
            {
                try
                {
                    if (_networks.IsEmpty)
                    {
                        Thread.Sleep(ThreadEmptyDelay);
                        continue;
                    }

                    var destroy = new Queue<Network.Network>();
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
        

        private void ClientForEachUpdate(Queue<Network.Network> destroy)
        {
            foreach (var networkManager in _networks)
            {
                if (!(networkManager?.IsAvailable ?? false))
                {
                    destroy.Enqueue(networkManager);
                    continue;
                }
                networkManager.Update();
            }
        }

        private void ClientForEachDestroy(Queue<Network.Network> destroy)
        {
            while(destroy.Count > 0)
            {
                var networkManager = destroy.Dequeue();
            
                networkManager.Close();
                _networks.Remove(networkManager);
            }
        }

        internal void Broadcast(IPacket packet, bool authenticate = true)
        {
            var buf = new ByteBuf();
            packet.Write(buf);

            var data = NetworkManager.CompressionEnabled ? NetworkManager.Compress(buf) : buf.Flush();

            foreach (var network in _networks)
            {
                if(authenticate && !network.IsAuthenticate) continue;
                network.SendBytes(data);
            }
        }
    }
}