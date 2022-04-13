using System;
using System.Diagnostics;
using System.Net.Sockets;
using RemoteDesktopViewer.Networks.Packet;
using RemoteDesktopViewer.Networks.Packet.Data;
using RemoteDesktopViewer.Utils;
using RemoteDesktopViewer.Utils.Byte;

namespace RemoteDesktopViewer.Networks
{
    public class NetworkManager
    {
        private const long KeepAliveTime = 1000;
        public const bool CompressionEnabled = true;
        private const int CompressionThreshold = 50;
        
        private readonly Socket _client;
        public bool Connected => (_client?.Connected ?? false) || IsAvailable;
        public bool IsAvailable { get; private set; }
        private long _lastPacketMillis = TimeManager.CurrentTimeMillis;

        private readonly NetworkBuf _receiveBuf = new();

        public NetworkManager(Socket client)
        {
            IsAvailable = true;
            client.NoDelay = true;
            _client = client;
            
            var so = new StateObject(_client);
            so.TargetSocket.BeginReceive(so.Buffer, 0, StateObject.BufferSize, SocketFlags.None, ReceiveAsync, so);
        }

        public virtual void Disconnect()
        {
            IsAvailable = false;
        }

        public void Close()
        {
            _client.Close();
            _client.Dispose();
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
            }
            catch (Exception e)
            {
                ExceptionCheck(e);
            }
        }

        private void ExceptionCheck(Exception e)
        {
            while (true)
            {
                if (e.InnerException != null)
                {
                    e = e.InnerException;
                    continue;
                }
                
                Debug.WriteLine(e);
                ExceptionHandle(e);
                Disconnect();

                break;
            }
        }
        
        protected virtual void ExceptionHandle(Exception e) {}

        private void PacketHandle(byte[] data)
        {
            if (CompressionEnabled)
                data = Decompress(data);
            
            try
            {
                var packet = PacketManager.Handle(new ByteBuf(data));
                if(packet != null) PacketHandle(packet);
            }
            catch (Exception e)
            {
                ExceptionCheck(e);
            }
        }
        
        protected virtual void PacketHandle(IPacket packet){}

        private static byte[] Decompress(byte[] buf)
        {
            var result = new ByteBuf(buf);
            var length = result.ReadVarInt();

            var compressed = result.Read(result.Length);
            return length == 0 ? compressed : ByteHelper.Decompress(compressed);
        }


        public static byte[] Compress(ByteBuf buf)
        {
            var result = new ByteBuf();
            if(buf.WriteLength >= CompressionThreshold)
            {
                var compressed = ByteHelper.Compress(buf.GetBytes());
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

        public void SendPacket(IPacket packet)
        {
            if (_client is not {Connected: true}) return;
            
            try
            {
                var buf = new ByteBuf();
                packet.Write(buf);

                var data = CompressionEnabled ? Compress(buf) : buf.Flush();

                _client.Send(data);
                _lastPacketMillis = TimeManager.CurrentTimeMillis;
            }
            catch (Exception e)
            {
                ExceptionCheck(e);
            }
        }

        public void SendBytes(byte[] packet)
        {
            if (!_client.Connected) return;

            try
            {
                _client.Send(packet);
                // Debug.WriteLine($"{packet.Length} {TimeManager.CurrentTimeMillis - packetSend}ms");
                // _client.GetStream().Flush();
                // _client.Client.Send(packet);
            
                _lastPacketMillis = TimeManager.CurrentTimeMillis;
            }
            catch (Exception e)
            {
                ExceptionCheck(e);
            }
        }
    }
}
