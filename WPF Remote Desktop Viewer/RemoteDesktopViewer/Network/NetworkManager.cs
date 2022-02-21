using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using RemoteDesktopViewer.Network.Packet;
using RemoteDesktopViewer.Network.Packet.Data;
using RemoteDesktopViewer.Utils;

namespace RemoteDesktopViewer.Network
{
    public class NetworkManager
    {
        public const long KeepAliveTime = 1000;
        private TcpClient _client;
        public bool Connected => _client?.Connected ?? false;
        public bool IsAvailable { get; private set; }
        public long LastPacketMillis { get; private set; } = TimeManager.CurrentTimeMillis;
        public bool IsAuthenticate { get; private set; }
        
        public ClientWindow ClientWindow { get; private set; }
        public bool ServerControl { get; private set; }

        private NetworkBuf _networkBuf = new NetworkBuf();
        
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
            if (remove && ClientWindow != null)
            {
                MainWindow.Instance?.InvokeAction(() =>
                {
                    MessageBox.Show($@"{ClientWindow?.Title} server closed.");
                    ClientWindow?.Close();
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
            PacketHandleUpdate();
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

            _networkBuf.Buf ??= new byte[ByteBuf.ReadVarInt(_client.GetStream())];

            _networkBuf.Offset += _client.GetStream().Read(_networkBuf.Buf, _networkBuf.Offset,
                _networkBuf.Buf.Length - _networkBuf.Offset);
        }

        private void PacketHandleUpdate()
        {
            if (_networkBuf.Buf == null || _networkBuf.Offset != _networkBuf.Buf.Length) return;
            
            try
            {
                PacketManager.Handle(this, new ByteBuf(_networkBuf.Buf));
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                Disconnect();
            }

            _networkBuf.Buf = null;
        }
        

        /*
        private void PacketUpdate()
        {
            if (!_client.Connected || _client.Available <= 0) return;
            
            LastPacketMillis = TimeManager.CurrentTimeMillis;

            var packetSize = 0;
            if (_networkBuf.Buf != null)
            {
                var pos = _networkBuf.Position;
                packetSize = _networkBuf.ReadVarInt();
                packetSize = _networkBuf.Available < packetSize ? packetSize - _networkBuf.Available : 0;

                _networkBuf.Position = pos;
                _networkBuf.Offset += _client.GetStream().Read(_networkBuf.Buf, _networkBuf.Offset,
                    _networkBuf.Buf.Length - _networkBuf.Offset);
            }
            
            if(packetSize == 0)
            {
                packetSize = ByteBuf.ReadVarInt(_client.GetStream());
                _networkBuf.WriteVarInt(packetSize);
            }
            
            var bytes = new byte[packetSize];
            var recvByteSizeAcc = _client.GetStream().Read(bytes, 0, bytes.Length);
            
            _networkBuf.Write(bytes, recvByteSizeAcc);
            
            // while (recvByteSizeAcc != bytes.Length)
            // {
            //     var recvByteSize = _client.GetStream().Read(bytes, recvByteSizeAcc, bytes.Length - recvByteSizeAcc);
            //     recvByteSizeAcc += recvByteSize;
            // }
            //
            // _networkBuf.WriteVarInt(bytes.Length);
            // _networkBuf.Write(bytes);
        }

        private void PacketHandleUpdate()
        {
            var count = 0;
            while (_networkBuf.Available > 2)
            {
                var pos = _networkBuf.Position;
                var size = _networkBuf.ReadVarInt();
                // Debug.WriteLine(size);
                if (_networkBuf.Available < size)
                {
                    // Debug.WriteLine(_networkBuf.Available +", " + size);
                    _networkBuf.Position = pos;
                    break;
                }
                try
                {
                    PacketManager.Handle(this, new ByteBuf(_networkBuf.Read(size)));
                    count++;
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                    Disconnect();
                    return;
                }
            }

            if (count > 0)
            {
                _networkBuf = new ByteBuf(_networkBuf.Read(_networkBuf.Available));
            }
        }
        */

        internal ClientWindow CreateClientWindow()
        {
            ClientWindow = new ClientWindow(this);
            ClientWindow.Title = ((IPEndPoint) _client.Client.RemoteEndPoint).ToString();

            return ClientWindow;
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
                // _client.GetStream().Flush();
                // _client.Client.Send(data);
            
                LastPacketMillis = TimeManager.CurrentTimeMillis;
            }
            catch (Exception)
            {
                _client.Close();
            }
        }

        internal void SendBytes(byte[] packet)
        {
            if (!_client.Connected) return;

            try
            {
                _client.GetStream().WriteAsync(packet, 0, packet.Length);
                // _client.GetStream().Flush();
                // _client.Client.Send(packet);
            
                LastPacketMillis = TimeManager.CurrentTimeMillis;
            }
            catch (Exception)
            {
                _client.Close();
            }
        }
    }

    class NetworkBuf
    {
        public byte[] Buf;
        public int Offset;
    }
}
