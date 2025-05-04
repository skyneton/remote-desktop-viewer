using System.Windows;
using System.Windows.Controls.Primitives;
using RemoteDeskopControlPannel.Capture;
using RemoteDeskopControlPannel.Network;

namespace RemoteDeskopControlPannel
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        internal static MainWindow Instance { get; private set; }
        internal Server? Server { get; private set; }
        private Worker? worker = null;
        private SoundCapture? soundCapture = null;
        public MainWindow()
        {
            InitializeComponent();
            Instance = this;
        }

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            foreach (var command in Environment.GetCommandLineArgs())
            {
                var data = command.Split('=');
                var lower = command.ToLower();
                if (lower == "server-on")
                {
                    ServerProxyStartButton.IsChecked = true;
                    ServerProxyStartButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                }
                else if (lower == "server-type=proxy")
                {
                    ServerProxyToggleButton.IsChecked = true;
                    ServerProxyToggleButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                }
                else if (lower == "proxy")
                {
                    ServerUseProxyToggleButton.IsChecked = true;
                    ServerUseProxyToggleButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                }
                else if (lower == "client-on") ClientConnectButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                if (data.Length != 2) continue;
                var k = data[0].ToLower();
                var v = data[1];
                if (k == "client-ip") ClientIPAddress.Text = v;
                else if (k == "client-password") ClientPassword.Password = v;
                else if (k == "server-ip") ServerProxyAddress.Text = v;
                else if (k == "server-password") ServerPassword.Text = v;
                else if (k == "server-port") ServerPort.Text = v;
            }
        }

        private void OnServerProxyToggleButton(object sender, RoutedEventArgs e)
        {
            var isChecked = ((ToggleButton)sender).IsChecked ?? false;
            ServerProxyToggleLabel.Content = isChecked ? "Proxy" : "Server";
            if (isChecked)
            {
                ServerUseProxyLabel.Visibility = ServerUseProxyToggleButton.Visibility = Visibility.Collapsed;
                ServerProxyAddressLabel.Visibility = ServerProxyAddress.Visibility = Visibility.Collapsed;
                ServerPortLabel.Visibility = ServerPort.Visibility = Visibility.Visible;
            }
            else
            {
                if (ServerUseProxyToggleButton.IsChecked ?? false)
                {
                    ServerProxyAddressLabel.Visibility = ServerProxyAddress.Visibility = Visibility.Visible;
                    ServerPortLabel.Visibility = ServerPort.Visibility = Visibility.Collapsed;
                }
                else
                {
                    ServerProxyAddressLabel.Visibility = ServerProxyAddress.Visibility = Visibility.Collapsed;
                    ServerPortLabel.Visibility = ServerPort.Visibility = Visibility.Visible;
                }
                ServerUseProxyLabel.Visibility = ServerUseProxyToggleButton.Visibility = Visibility.Visible;
            }
        }

        private void OnClientConnectButton(object sender, RoutedEventArgs e)
        {
            var ip = ClientIPAddress.Text ?? "";
            var password = ClientPassword.Password ?? "";
            new Client(ip, password);
        }

        private void OnServerUseProxyToggleButton(object sender, RoutedEventArgs e)
        {
            var isChecked = ((ToggleButton)sender).IsChecked ?? false;
            var isCheckOn = isChecked ? Visibility.Visible : Visibility.Collapsed;
            var isCheckOff = isChecked ? Visibility.Collapsed : Visibility.Visible;
            ServerProxyAddressLabel.Visibility = ServerProxyAddress.Visibility = isCheckOn;
            ServerPortLabel.Visibility = ServerPort.Visibility = isCheckOff;
        }

        private void OnServerProxyStartButton(object sender, RoutedEventArgs e)
        {
            var isChecked = ((ToggleButton)sender).IsChecked ?? false;
            ServerProxyButtonOnOff(isChecked);
        }

        internal void ServerProxyButtonOnOff(bool isChecked)
        {
            ServerProxyStartButton.Content = isChecked ? "ON" : "OFF";
            ServerProxyToggleButton.IsEnabled = ServerUseProxyToggleButton.IsEnabled = ServerProxyAddress.IsEnabled = ServerPort.IsEnabled = ServerPassword.IsEnabled = !isChecked;
            worker?.Stop();
            Server?.Close();
            soundCapture?.Close();
            worker = null;
            Server = null;
            soundCapture = null;
            if (isChecked)
            {
                var isProxy = ServerProxyToggleButton.IsChecked ?? false;
                if (!int.TryParse(ServerPort.Text, out var port)) port = Server.DefaultPort;
                ServerPort.Text = port.ToString();
                var proxy = ServerProxyAddress.Text ?? "";
                var password = ServerPassword.Text ?? "";
                var useProxy = ServerUseProxyToggleButton.IsChecked ?? false;
                if (isProxy || !useProxy)
                    Server = new Server(port, isProxy, password);
                else
                    Server = new Server(proxy, password);

                if (!isProxy)
                {
                    worker = new Worker(20);
                    worker.Execute(Server);

                    soundCapture = new SoundCapture(44100, 16, 2);
                }
            }
        }
    }
}