using System;
using System.Net.Sockets;
using System.Windows;
using RemoteDesktopViewer.Networks;
using RemoteDesktopViewer.Networks.Packet;
using RemoteDesktopViewer.Networks.Packet.Data;
using RemoteDesktopViewer.Networks.Threading;
using RemoteDesktopViewer.Threading;
using RemoteDesktopViewer.Utils;

namespace RemoteDesktopViewer.Network
{
    public class Network : NetworkManager
    {
        private readonly FileReceiveHelper _helper = new();
        public bool IsAuthenticate { get; private set; }
        
        public int BeforeCursor { get; internal set; } = -1;
        
        public Network(Socket client) : base(client)
        {
        }

        protected override void PacketHandle(IPacket iPacket)
        {
            switch (iPacket)
            {
                case PacketLogin packet:
                    Login(packet.Password);
                    break;
                case PacketMouseMove packet:
                    MouseMove(packet);
                    break;
                case PacketMouseEvent packet:
                    ServerControlAction(() => LowHelper.mouse_event(packet.Id, 0, 0, packet.Data, 0));
                    break;
                case PacketKeyEvent packet:
                    ServerControlAction(() => LowHelper.keybd_event(packet.Id, 0, packet.Flag, 0));
                    break;
                case PacketFileName packet:
                    _helper.FileChunkCreate(packet.Id, packet.Data, packet.IsDirectory);
                    break;
                case PacketFileChunk packet:
                    _helper.FileChunkReceived(packet.Id, packet.Data);
                    break;
                case PacketFileFinished packet:
                    _helper.FileChunkFinished(packet.Id);
                    break;
                case PacketClipboard packet:
                    MainWindow.Instance.ClipboardHelper.SetClipboard(ClipboardThreadManager.GetData(packet.DataType, packet.Data));
                    break;
            }
        }

        private void Login(string password)
        {
            if (RemoteServer.Instance.Password.Equals(CryptoHelper.ToSha256(password)))
            {
                IsAuthenticate = true;
                ScreenThreadManager.SendFullScreen(this);
                SendPacket(new PacketServerControl(RemoteServer.Instance.ServerControl));
                return;
            }
            
            SendPacket(new PacketDisconnect("Password wrong."));
            Disconnect();
        }

        private void MouseMove(PacketMouseMove packet)
        {
            if (!(IsAuthenticate && RemoteServer.Instance.ServerControl)) return;
            
            var x = ScreenThreadManager.CurrentSize.X * packet.PercentX;
            var y = ScreenThreadManager.CurrentSize.Y * packet.PercentY;
            
            LowHelper.SetCursorPos((int) x, (int) y);

            var pci = LowHelper.GetCursorInfo(out var success);
            if (!success || (int) pci.hCursor == BeforeCursor) return;
            BeforeCursor = (int) pci.hCursor;
            SendPacket(new PacketCursorType(BeforeCursor));
        }

        private void ServerControlAction(Action action)
        {
            if(IsAuthenticate && RemoteServer.Instance.ServerControl)
                action.Invoke();
        }
    }
}