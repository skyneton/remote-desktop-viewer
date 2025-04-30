using System.Collections.Concurrent;
using System.Windows;
using System.Windows.Controls.Primitives;
using NetworkLibrary.Networks;
using NetworkLibrary.Networks.Packet;
using NetworkLibrary.Utils;
using RemoteDeskopControlPannel.ImageProcessing;
using RemoteDeskopControlPannel.Network.Handler;
using RemoteDeskopControlPannel.Network.Packet;
using RemoteDeskopControlPannel.Utils;

namespace RemoteDeskopControlPannel.Network
{
    internal class Server
    {
        public const int DefaultPort = 33062;
        public static readonly PacketFactory Factory = new PacketFactory()
            .RegisterPacket(
            new PacketKeepAlive(),
            new PacketProxyType(),
            new PacketLogin(),
            new PacketScreenSize(),
            new PacketScreenInfo(),
            new PacketFullScreen(),
            new PacketScreenChunk(),
            new PacketProxyConnected(),
            new PacketCursorType(),
            new PacketKeyboardInput(),
            new PacketMousePosition(),
            new PacketMouseEvent(),
            new PacketSoundInfo(),
            new PacketSoundChunk()
            );
        public readonly bool IsProxy = false;
        private readonly NetworkClient? client;
        private readonly NetworkListener? listener;
        private NetworkLibrary.Networks.Network? host = null;
        private readonly HashSet<NetworkLibrary.Networks.Network> clients = [];
        internal readonly string Password;
        internal bool proxyConnected = false;
        public bool IsAvailable => client != null ? proxyConnected : clients.Count > 0;
        public ImageProcess ScreenProcess { get; private set; } = ImageProcess.Byte3RGB;
        public readonly ConcurrentQueue<NetworkLibrary.Networks.Network> AcceptedClients = [];
        public PacketSoundInfo? SoundInfo { get; private set; } = null;
        private ScreenSize cachedScreenSize = ScreenSize.Zero;
        public Server(int port, bool isProxy, string password)
        {
            IsProxy = isProxy;
            Password = password;
            listener = new NetworkListener(Factory, port);

            listener.SetNetworkInstance(typeof(TimeoutNetwork));
            listener.OnAcceptEventHandler += OnAccept;
            listener.OnDisconnectEventHandler += OnDisconnect;
            listener.DefaultPacketFactory = Factory;
            listener.Listen(port);
        }

        public Server(string proxy, string password)
        {
            Password = password;
            var host = proxy;
            var port = DefaultPort;
            var column = proxy.LastIndexOf(':');
            if (column != -1)
            {
                host = proxy[..column];
                if (!int.TryParse(proxy.AsSpan(column + 1), out port))
                    port = DefaultPort;
            }
            client = new NetworkClient(Factory, host, port, timeout: 10000, networkInstance: typeof(TimeoutNetwork));
            client.Network.PacketHandler = new ServerPacketHandler(true);
            client.Network.Compression.CompressionEnabled = true;
            client.OnConnected += OnConnectedToProxy;
            client.OnConnectFailed += (sender, e) =>
            {
                MessageBox.Show("Can't connect to the proxy server.");
                //MainWindow.Instance.ServerProxyButtonOnOff(false);

                MainWindow.Instance.ServerProxyStartButton.IsChecked = false;
                MainWindow.Instance.ServerProxyStartButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            };
            client.OnDisconnect += (sender, e) =>
            {
                MessageBox.Show("Server disconnected. Server down or login failed.");

                MainWindow.Instance.ServerProxyStartButton.IsChecked = false;
                MainWindow.Instance.ServerProxyStartButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            };
            client.Connect();
        }

        public void Close()
        {
            client?.Close();
            listener?.Close();
        }

        private void OnConnectedToProxy(object? sender, NetworkEventArgs e)
        {
            e.network?.SendPacket(new PacketLogin(Password));
            e.network?.SendPacket(new PacketProxyType(true));
            e.network?.SendPacket(new PacketScreenInfo(ScreenProcess.Quality));
        }

        private void OnAccept(object? sender, NetworkEventArgs e)
        {
            var network = e.network;
            if (network?.Connected != true) return;
            network.Compression.CompressionEnabled = true;
            if (IsProxy)
            {
                network.PacketHandler = new ProxyPacketHandler();
                return;
            }
            network.PacketHandler = new ServerPacketHandler(false);
        }

        private void OnDisconnect(object? sender, NetworkEventArgs e)
        {
            var network = e.network;
            if (host == network)
            {
                lock (clients)
                {
                    foreach (var client in clients)
                    {
                        client.Disconnect();
                    }
                    clients.Clear();
                }
                host = null;
            }
            lock (clients)
            {
                clients.Remove(network);
            }
            if (clients.Count <= 0)
            {
                host?.SendPacket(new PacketProxyConnected(false));
                lock (cachedScreenSize)
                {
                    if (cachedScreenSize != ScreenSize.Zero)
                        DisplaySettings.ChangeDisplayResolution(cachedScreenSize.Width, cachedScreenSize.Height);
                    cachedScreenSize.Set(0, 0);
                }
            }
        }

        public void Broadcast(IPacket packet)
        {
            if (client != null)
            {
                client?.SendPacket(packet);
                return;
            }
            foreach (var client in clients)
            {
                client.SendPacket(packet);
            }
        }

        public void SendToHost(IPacket packet)
        {
            if (listener == null || !IsProxy || host?.IsAvailable != true) return;
            host.SendPacket(packet);
        }

        internal void UpdateScreenSize(int width, int height)
        {
            lock (cachedScreenSize)
            {
                if (cachedScreenSize == ScreenSize.Zero)
                {
                    cachedScreenSize.Set(width, height);
                    DisplaySettings.ChangeDisplayResolution(width, height);
                }
            }
        }

        internal void UpdateSoundInfo(PacketSoundInfo soundInfo)
        {
            SoundInfo = soundInfo;
        }

        internal void UpdateScreenProcessor(QualityMode quality)
        {
            ScreenProcess = quality switch
            {
                QualityMode.Byte2RGB => ImageProcess.Byte2RGB,
                _ => ImageProcess.Byte3RGB,
            };
        }

        internal void ReceiveClient(NetworkLibrary.Networks.Network network, ActiveMode mode)
        {
            if (IsProxy && host?.IsAvailable != true && mode != ActiveMode.Server || !IsProxy && mode != ActiveMode.Client)
            {
                network.Disconnect();
                return;
            }
            if (mode == ActiveMode.Server)
            {
                host = network;
                return;
            }
            lock (clients)
            {
                clients.Add(network);
            }
            if (host != null)
            {
                host.SendPacket(new PacketProxyConnected(true));
                host.SendPacket(new PacketFullScreen());
            }
            AcceptedClients.Enqueue(network);
            network.SendPacket(new PacketScreenInfo(ScreenProcess.Quality));
            if (SoundInfo != null) network.SendPacket(SoundInfo);
        }
    }
}
