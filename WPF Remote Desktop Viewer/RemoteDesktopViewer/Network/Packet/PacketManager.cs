using System.Collections.Generic;
using System.Collections.ObjectModel;
using RemoteDesktopViewer.Network.Packet.Data;
using RemoteDesktopViewer.Utils;

namespace RemoteDesktopViewer.Network.Packet
{
    public static class PacketManager
    {
        private static readonly ReadOnlyDictionary<int, IPacket> Packets;

        static PacketManager()
        {
            var packets = new Dictionary<int, IPacket>
            {
                {(int) PacketType.Disconnect, new PacketDisconnect()},
                {(int) PacketType.Login, new PacketLogin()},
                {(int) PacketType.Screen, new PacketScreen()},
                {(int) PacketType.ScreenChunk, new PacketScreenChunk()},
                {(int) PacketType.ServerControl, new PacketServerControl()},
                {(int) PacketType.MouseMove, new PacketMouseMove()},
                {(int) PacketType.MouseEvent, new PacketMouseEvent()},
                {(int) PacketType.KeyEvent, new PacketKeyEvent()},
                {(int) PacketType.CursorEvent, new PacketCursorType()},
                {(int) PacketType.FileNameEvent, new PacketFileName()},
                {(int) PacketType.FileChunkEvent, new PacketFileChunk()},
                {(int) PacketType.FileFinishedEvent, new PacketFileFinished()},
            };
            Packets = new ReadOnlyDictionary<int, IPacket>(packets);
        }
        public static void Handle(NetworkManager networkManager, ByteBuf buf)
        {
            if (!Packets.TryGetValue(buf.ReadVarInt(), out var packet)) return;
            packet.Read(networkManager, buf);
        }
    }
}