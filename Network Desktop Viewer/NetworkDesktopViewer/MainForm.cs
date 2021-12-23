using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Windows.Forms;
using RemoteDesktopViewer.Network;
using RemoteDesktopViewer.Utils;

namespace RemoteDesktopViewer
{
    public partial class MainForm : Form
    {
        public static MainForm Instance { get; private set; }
        private const string NetworkAlreadyBind = "Network port already bind.";
        private const string FormCloseServerAlive = "Please server close.";
        private const string Error = "Error.";

        private const int DefaultPort = 33062;
        
        public MainForm()
        {
            Instance = this;
            InitializeComponent();
        }

        private void serverPort_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!(char.IsDigit(e.KeyChar) || e.KeyChar == Convert.ToChar(Keys.Back)))
                e.Handled = true;
        }

        private void connectIpAddress_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == Convert.ToChar(Keys.Space))
                e.Handled = true;
        }

        private void serverConnect_Click(object sender, EventArgs e)
        {
            var address = connectIpAddress.Text.Split(':');
            var ip = address[0];
            var port = DefaultPort;
            if (address.Length >= 2)
                if (int.TryParse(address[1], out var temp))
                    port = temp;

            try
            {
                RemoteClient.Instance.Connect(ip, port, connectPassword.Text).CreateClientForm().Show();
            }
            catch (Exception err)
            {
                MessageBox.Show(err.Message);
            }
        }

        private void RemoteDesktopViewer_Closing(object sender, FormClosingEventArgs e)
        {
            if (RemoteServer.Instance?.IsAvailable ?? false)
            {
                MessageBox.Show(FormCloseServerAlive);
                e.Cancel = true;
            }
            else
            {
                RemoteClient.Instance?.Close();
                ThreadFactory.Close();
            }
        }

        private void serverOnOff_CheckedChanged(object sender, EventArgs e)
        {
            serverPort.Enabled = !serverOnOff.Checked;
            
            if (!serverOnOff.Checked)
            {
                RemoteServer.Instance?.Close();
                return;
            }
            
            if (string.IsNullOrEmpty(serverPort.Text))
                serverPort.Text = DefaultPort.ToString();

            try
            {
                new RemoteServer(int.Parse(serverPort.Text), serverPassword.Text);
            }
            catch (SocketException err)
            {
                MessageBox.Show(err.SocketErrorCode == SocketError.AddressAlreadyInUse ? NetworkAlreadyBind : Error);
            }
            catch (Exception ec)
            {
                MessageBox.Show(ec.Message);
            }
        }

        internal void InvokeAction(Action func)
        {
            if (InvokeRequired)
                Invoke(func);
            else
                func();
        }

        private void serverPassword_TextChanged(object sender, EventArgs e)
        {
            RemoteServer.Instance?.UpdatePassword(serverPassword.Text);
        }

        private void serverControl_CheckedChanged(object sender, EventArgs e)
        {
            RemoteServer.Instance?.UpdateServerControl(serverControl.Checked);
        }
    }
}