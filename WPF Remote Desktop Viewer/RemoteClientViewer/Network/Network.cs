using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows;
using RemoteDesktopViewer.Networks;
using RemoteDesktopViewer.Networks.Packet;
using RemoteDesktopViewer.Networks.Packet.Data;
using RemoteDesktopViewer.Utils;

namespace RemoteClientViewer.Network
{
    public class Network : NetworkManager
    {
        public Network(Socket client) : base(client)
        {
        }

        public override void Disconnect()
        {
            base.Disconnect();
            MainWindow.Instance.Invoke(MainWindow.Instance.Close);
        }

        protected override void ExceptionHandle(Exception e)
        {
            MainWindow.Instance.Invoke(() => MessageBox.Show(e.Message));
        }

        protected override void PacketHandle(IPacket packet)
        {
            Task.Run(() => Handle(packet));
        }

        private void Handle(IPacket data)
        {
            switch (data)
            {
                case PacketDisconnect packet:
                    MessageBox.Show(packet.Reason);
                    Disconnect();
                    break;
                case PacketScreen packet:
                    MainWindow.Instance.DrawFullScreen(packet.Width, packet.Height, packet.Format, packet.Data);
                    break;
                case PacketScreenChunk packet:
                    MainWindow.Instance.DrawScreenChunk(new ByteBuf(packet.Data));
                    break;
                case PacketServerControl packet:
                    MainWindow.Instance.ServerControl = packet.Control;
                    break;
                case PacketCursorType packet:
                    MainWindow.Instance.CursorValue = packet.Cursor;
                    MainWindow.Instance.Invoke(() => LowHelper.SetCursor(packet.Cursor));
                    break;
                case PacketClipboard packet:
                    MainWindow.Instance.ClipboardHelper.ClipboardChunkReceived(packet.Id, packet.Type, packet.Data);
                    break;
            }
        }
    }
}