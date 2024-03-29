﻿using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using RemoteDesktopViewer.Utils;

namespace RemoteDesktopViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public static MainWindow Instance { get; private set; }
        private const string LatestGithubUrl =
            "https://github.com/skyneton/remote-desktop-viewer/releases/latest/download/";
        private const string ClientViewer = "RemoteClientViewer.exe";
        private const string ClientViewerDirectory = "client";

        private const string NetworkDll = "RemoteDesktopViewer.Networks.dll";
        private const string UtilsDll = "RemoteDesktopViewer.Utils.dll";
        
        private const string NetworkAlreadyBind = "Network port already bind.";
        private const string FormCloseServerAlive = "Please server close.";
        private const string Error = "Error.";

        private const int DefaultPort = 33062;

        internal readonly ClipboardHelper ClipboardHelper = new();

        public MainWindow()
        {
            Instance = this;
            InitializeComponent();
            Task.Run(() =>
            {
                Dispatcher.Invoke(() =>
                {
                    ServerControl.IsEnabled = false;
                    Connect.IsEnabled = false;
                    ServerOnOff.IsEnabled = false;
                });
                CreateDll();
                CreateClient();
                Dispatcher.Invoke(() =>
                {
                    ServerControl.IsEnabled = true;
                    Connect.IsEnabled = true;
                    ServerOnOff.IsEnabled = true;
                });
            });
        }

        private static bool CreateDll()
        {
            var result = GitDownload(Path.Combine(Environment.CurrentDirectory, NetworkDll));
            result |= GitDownload(Path.Combine(Environment.CurrentDirectory, UtilsDll));
            return result;
        }

        private static void CreateClient()
        {
            var path = Path.Combine(Environment.CurrentDirectory, ClientViewerDirectory);
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            GitDownload(Path.Combine(path, ClientViewer));
            
            Copy(Path.Combine(Environment.CurrentDirectory, NetworkDll), Path.Combine(path, NetworkDll));
            Copy(Path.Combine(Environment.CurrentDirectory, UtilsDll), Path.Combine(path, UtilsDll));
        }

        private static bool GitDownload(string filePath)
        {
            if (File.Exists(filePath)) return false;
            var name = Path.GetFileName(filePath);

            using var wc = new WebClient();
            wc.DownloadFile($"{LatestGithubUrl}{name}", filePath);
            return true;
        }

        private static void Copy(string origin, string path)
        {
            if (File.Exists(path)) return;
            File.Copy(origin, path);
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
            
            if(!(button.IsChecked ?? false))
                Off();
            else
                On();
        }

        private void On()
        {
            ServerOnOff.Content = "Server Opened";
            
            if (string.IsNullOrEmpty(ServerPort.Text))
                ServerPort.Text = DefaultPort.ToString();

            try
            {
                new RemoteServer(int.Parse(ServerPort.Text), ServerPassword.Password).UpdateServerControl(ServerControl.IsChecked ?? false);
                ClipboardHelper.Create(this);
            }
            catch (SocketException err)
            {
                MessageBox.Show(err.SocketErrorCode == SocketError.AddressAlreadyInUse ? NetworkAlreadyBind : Error);
                ServerOnOff.IsChecked = false;
            }
            catch (Exception)
            {
                MessageBox.Show(Error);
                ServerOnOff.IsChecked = false;
            }
        }

        private void Off()
        {
            ServerOnOff.Content = "Server Closed";
            ClipboardHelper.Close();
            RemoteServer.Instance?.Close();
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

        private void ServerPassword_OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            RemoteServer.Instance?.UpdatePassword(ServerPassword.Password);
        }
    }
}