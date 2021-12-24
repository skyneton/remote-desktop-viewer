using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;
using RemoteDesktopViewer.Network.Packet;
using RemoteDesktopViewer.Network.Packet.Data;
using RemoteDesktopViewer.Utils;

namespace RemoteDesktopViewer.Network
{
    public class NetworkManager
    {
        public const long KeepAliveTime = 100;
        private TcpClient _client;
        public bool Connected => _client?.Connected ?? false;
        public bool IsAvailable { get; private set; }
        public long LastPacketMillis { get; private set; } = TimeManager.CurrentTimeMillis;
        public bool IsAuthenticate { get; private set; }
        
        public ClientForm ClientForm { get; private set; }
        public bool ServerControl { get; private set; }
        
        internal NetworkManager(TcpClient client)
        {
            IsAvailable = true;
            client.SendTimeout = 500;
            client.NoDelay = true;
            _client = client;
        }

        public void Disconnect(bool remove = true)
        {
            IsAvailable = false;
            if (remove && ClientForm != null)
            {
                MainForm.Instance?.InvokeAction(() =>
                {
                    MessageBox.Show($@"{ClientForm.Text} server closed.");
                    ClientForm?.Close();
                    ClientForm?.Dispose();
                });
            }
        }

        public void Close()
        {
            _client.Close();
            _client.Dispose();
        }

        internal void UpdateServerControl(bool control)
        {
            ServerControl = control;
        }

        public void Update()
        {
            PacketUpdate();
            KeepAliveUpdate();
        }

        private void KeepAliveUpdate()
        {
            if (TimeManager.CurrentTimeMillis - LastPacketMillis < KeepAliveTime) return;
            SendPacket(new PacketKeepAlive());
        }
        

        private void PacketUpdate()
        {
            if (!_client.Connected || _client.Available <= 0) return;
            
            LastPacketMillis = TimeManager.CurrentTimeMillis;
            
            var bytes = new byte[ByteBuf.ReadVarInt(_client.GetStream())];
            _client.GetStream().Read(bytes, 0, bytes.Length);

            try
            {
                PacketManager.Handle(this, new ByteBuf(bytes));
            }
            catch (Exception)
            {
                Disconnect();
            }
        }

        internal Form CreateClientForm()
        {
            ClientForm = new ClientForm(this);
            ClientForm.Text = ((IPEndPoint) _client.Client.RemoteEndPoint).ToString();

            return ClientForm;
        }

        internal void ServerLogin(string password)
        {
            if (RemoteServer.Instance?.Password.Equals(password.ToSha256()) ?? false)
            {
                IsAuthenticate = true;
                ScreenThreadManager.SendFullScreen(this);
                SendPacket(new PacketServerControl(RemoteServer.Instance?.ServerControl ?? false));
            }
            else
            {
                SendPacket(new PacketDisconnect("Password error."));
                Disconnect();
            }
        }

        internal void SendPacket(Packet.Packet packet)
        {
            if (!_client.Connected) return;
            
            var buf = new ByteBuf();
            packet.Write(buf);

            var data = buf.Flush();

            try
            {
                _client.GetStream().WriteAsync(data, 0, data.Length);
                _client.GetStream().Flush();
            
                LastPacketMillis = TimeManager.CurrentTimeMillis;
            }
            catch (Exception)
            {
                _client.Close();
            }
        }
    }
}
