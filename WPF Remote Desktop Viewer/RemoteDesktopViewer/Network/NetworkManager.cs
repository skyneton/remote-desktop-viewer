using System;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Input;
using RemoteDesktopViewer.Network.Packet;
using RemoteDesktopViewer.Network.Packet.Data;
using RemoteDesktopViewer.Utils;

namespace RemoteDesktopViewer.Network
{
    public class NetworkManager
    {
        private const long KeepAliveTime = 1000;
        public const bool CompressionEnabled = true;
        private const int CompressionThreshold = 50;
        
        private readonly TcpClient _client;
        public bool Connected => _client?.Connected ?? false;
        public bool IsAvailable { get; private set; }
        private long _lastPacketMillis = TimeManager.CurrentTimeMillis;
        public bool IsAuthenticate { get; private set; }
        
        public ClientWindow ClientWindow { get; private set; }
        public bool ServerControl { get; private set; }

        private NetworkBuf _receiveBuf = new();

        internal NetworkManager(TcpClient client)
        {
            IsAvailable = true;
            client.SendTimeout = 500;
            client.NoDelay = true;
            _client = client;
            
            var so = new StateObject(_client.Client);
            so.TargetSocket.BeginReceive(so.Buffer, 0, StateObject.BufferSize, SocketFlags.None, ReceiveAsync, so);
        }

        public void Disconnect(bool remove = true)
        {
            IsAvailable = false;
            if (remove && ClientWindow != null)
                MainWindow.Instance?.InvokeAction(ClientWindow.Close);
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

        private void ReceiveAsync(IAsyncResult rs)
        {
            var so = (StateObject) rs.AsyncState;
            try
            {
                var read = so!.TargetSocket.EndReceive(rs);
                if (read > 0)
                {
                    _lastPacketMillis = TimeManager.CurrentTimeMillis;
                    
                    _receiveBuf.Read(so.Buffer, read);

                    var result = _receiveBuf.ReadPacket();
                    while (result != null)
                    {
                        PacketHandle(result);
                        result = _receiveBuf.ReadPacket();
                    }

                    so.TargetSocket.BeginReceive(so.Buffer, 0, StateObject.BufferSize, 0, ReceiveAsync, so);
                }

                else
                {
                    if (ClientWindow != null) MainWindow.Instance?.InvokeAction(() => MessageBox.Show($@"{ClientWindow?.Title} server closed."));
                    Disconnect();
                }
            }
            catch (Exception e)
            {
                SocketExceptionCheck(e);
            }
        }

        private void SocketExceptionCheck(Exception e)
        {
            while (true)
            {
                if (e.InnerException != null)
                {
                    e = e.InnerException;
                    continue;
                }

                if (_client is not {Connected: true}) return;
                    if (ClientWindow != null) MainWindow.Instance?.InvokeAction(() => MessageBox.Show(e.Message));
                
                Disconnect();

                break;
            }
        }

        private void PacketHandle(byte[] packet)
        {
            if (CompressionEnabled)
                packet = Decompress(packet);
            
            try
            {
                PacketManager.Handle(this, new ByteBuf(packet));
            }
            catch (Exception e)
            {
                SocketExceptionCheck(e);
            }
        }

        private static byte[] Decompress(byte[] buf)
        {
            var result = new ByteBuf(buf);
            var length = result.ReadVarInt();

            var compressed = result.Read(result.Length);
            if (length == 0)
                return compressed;

            return compressed.Decompress();
        }


        public static byte[] Compress(ByteBuf buf)
        {
            var result = new ByteBuf();
            if(buf.WriteLength >= CompressionThreshold)
            {
                var compressed = buf.GetBytes().Compress();
                result.WriteVarInt(buf.WriteLength);
                result.Write(compressed);
            }else
            {
                result.WriteVarInt(0);
                result.Write(buf.GetBytes());
            }

            return result.Flush();
        }

        public void Update()
        {
            KeepAliveUpdate();
        }

        private void KeepAliveUpdate()
        {
            if (TimeManager.CurrentTimeMillis - _lastPacketMillis < KeepAliveTime) return;
            SendPacket(new PacketKeepAlive());
        }

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

        internal void SendPacket(IPacket packet)
        {
            if (_client is not {Connected: true}) return;
            
            var buf = new ByteBuf();
            packet.Write(buf);

            var data = CompressionEnabled ? Compress(buf) : buf.Flush();

            try
            {
                _client.GetStream().Write(data, 0, data.Length);
                _lastPacketMillis = TimeManager.CurrentTimeMillis;
            }
            catch (Exception e)
            {
                if (ClientWindow != null) MainWindow.Instance?.InvokeAction(() => MessageBox.Show(e.Message));
                Disconnect();
            }
        }

        internal void SendBytes(byte[] packet)
        {
            if (!_client.Connected) return;

            try
            {
                _client.GetStream().Write(packet, 0, packet.Length);
                // _client.GetStream().Flush();
                // _client.Client.Send(packet);
            
                _lastPacketMillis = TimeManager.CurrentTimeMillis;
            }
            catch (Exception)
            {
                _client.Close();
            }
        }

        private class StateObject
        {
            public const int BufferSize = 1024 * 10;
            public readonly byte[] Buffer = new byte[BufferSize];
            public Socket TargetSocket { get; }

            public StateObject(Socket socket)
            {
                TargetSocket = socket;
            }
        }
    }
}
