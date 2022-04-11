using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using RemoteDesktopViewer.Utils;

namespace RemoteClientViewer
{
    public class RemoteClient
    {
        private const int ConnectTime = 600;
        private const int ThreadDelay = 50;
        public bool IsAvailable { get; private set; }
        
        public Network.Network Network { get; private set; }

        private readonly ThreadFactory _threadFactory = new();

        public RemoteClient()
        {
            IsAvailable = true;
        }
        
        internal async Task<Network.Network> Connect(string ip, int port)
        {
            var client = await ConnectTimeout(ip, port, ConnectTime);
            if (client == null)
                return null;
            
            Network = new Network.Network(client);
            _threadFactory.LaunchThread(new Thread(ClientUpdateWorker), false).Name = "Client Update Thread";

            return Network;
        }

        private Task<Socket> ConnectTimeout(string host, int port, int timeout)
        {
            return Task.Run(() =>
            {
                var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                var result = client.BeginConnect(host, port, null, null);
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
                    if (!Network.IsAvailable)
                    {
                        Close();
                        break;
                    }
                    
                    Network.Update();
                    Thread.Sleep(ThreadDelay);
                }
                catch (Exception)
                {
                    //ignored
                }
            }
        }
    }
}