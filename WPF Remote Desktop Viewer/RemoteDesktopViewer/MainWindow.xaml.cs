using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using RemoteDesktopViewer.Utils;
using RemoteDesktopViewer.Utils.Compress;

namespace RemoteDesktopViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private const string LatestGithubUrl =
            "https://github.com/skyneton/remote-desktop-viewer/releases/latest/download/";
        private const string ClientViewer = "RemoteClientViewer.exe";
        private const string ClientViewerDirectory = "client";
        private const string NetworkAlreadyBind = "Network port already bind.";
        private const string FormCloseServerAlive = "Please server close.";
        private const string Error = "Error.";

        private const int DefaultPort = 33062;

        public MainWindow()
        {
            InitializeComponent();
            ClientCreate();
        }

        private static void ClientCreate()
        {
            var path = Path.Combine(Environment.CurrentDirectory, ClientViewerDirectory);
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            path = Path.Combine(path, ClientViewer);
            if (!File.Exists(path))
            {
                
                Task.Run(() =>
                {
                    using var wc = new WebClient();
                    wc.DownloadFile($"{LatestGithubUrl}{ClientViewer}", path);
                });
            }
        }

        internal void InvokeAction(Action func)
        {
            if (!Dispatcher.CheckAccess())
                Dispatcher.Invoke(func);
            else
                func();
        }

        private void ServerPort_OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if(!(char.IsDigit((char) KeyInterop.VirtualKeyFromKey(e.Key)) || e.Key == Key.Back))
                e.Handled = true;
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            if (RemoteServer.Instance?.IsAvailable ?? false)
            {
                MessageBox.Show(FormCloseServerAlive);
                e.Cancel = true;
            }
            else
            {
                ThreadFactory.Close();
            }
        }

        private void ServerOnOff_OnCheckedChanged(object sender, RoutedEventArgs e)
        {
            var button = (ToggleButton) sender;
            
            ServerPort.IsEnabled = !(button.IsChecked ?? false);
            
            if (!(button.IsChecked ?? false))
            {
                button.Content = "Server Closed";
                RemoteServer.Instance?.Close();
                return;
            }
            
            button.Content = "Server Opened";
            
            if (string.IsNullOrEmpty(ServerPort.Text))
                ServerPort.Text = DefaultPort.ToString();

            try
            {
                new RemoteServer(int.Parse(ServerPort.Text), ServerPassword.Password).UpdateServerControl(ServerControl.IsChecked ?? false);
            }
            catch (SocketException err)
            {
                MessageBox.Show(err.SocketErrorCode == SocketError.AddressAlreadyInUse ? NetworkAlreadyBind : Error);
                button.IsChecked = false;
            }
            catch (Exception)
            {
                MessageBox.Show(Error);
                button.IsChecked = false;
            }
        }

        private void ServerControl_OnCheckedChanged(object sender, RoutedEventArgs e)
        {
            var button = (ToggleButton) sender;
            RemoteServer.Instance?.UpdateServerControl(button.IsChecked ?? false);
            
            if (button.IsChecked ?? false)
            {
                button.Content = "ON";
            }
            else
            {
                button.Content = "OFF";
            }
        }

        private void IpAddress_OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key is Key.Oem1 && (Keyboard.Modifiers & ModifierKeys.Shift) == 0 || e.Key is Key.OemQuestion or Key.Oem5 or Key.Space)
                e.Handled = true;
        }

        private void Connect_OnClick(object sender, RoutedEventArgs e)
        {
            var address = IpAddress.Text.Split(':');
            var ip = address[0];
            var port = DefaultPort;
            if (address.Length >= 2)
                if (int.TryParse(address[1], out var temp))
                    port = temp;

            ConnectAsync(ip, port, ClientPassword.Password);
        }

        private void ConnectAsync(string ip, int port, string password)
        {
            var argument = $"-ip={ip} -port={port} -password={password}";
            var clientPath = Path.Combine(Environment.CurrentDirectory, "client", "RemoteClientViewer.exe");
            
            ProcessHelper.RunAsDesktopUser(clientPath, argument);
        }
    }
}