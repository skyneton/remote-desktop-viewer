using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using RemoteDesktopViewer.Networks.Packet.Data;
using RemoteDesktopViewer.Utils;

namespace RemoteDesktopViewer.Networks.Packet
{
    public static class PacketManager
    {
        private static readonly ReadOnlyDictionary<int, Type> Packets;

        static PacketManager()
        {
            var packets = new Dictionary<int, Type>
            {
                {(int) PacketType.Disconnect, typeof(PacketDisconnect)},
                {(int) PacketType.Login, typeof(PacketLogin)},
                {(int) PacketType.Screen, typeof(PacketScreen)},
                {(int) PacketType.ScreenChunk, typeof(PacketScreenChunk)},
                {(int) PacketType.ServerControl, typeof(PacketServerControl)},
                {(int) PacketType.MouseMove, typeof(PacketMouseMove)},
                {(int) PacketType.MouseEvent, typeof(PacketMouseEvent)},
                {(int) PacketType.KeyEvent, typeof(PacketKeyEvent)},
                {(int) PacketType.CursorEvent, typeof(PacketCursorType)},
                {(int) PacketType.FileNameEvent, typeof(PacketFileName)},
                {(int) PacketType.FileChunkEvent, typeof(PacketFileChunk)},
                {(int) PacketType.FileFinishedEvent, typeof(PacketFileFinished)},
            };
            Packets = new ReadOnlyDictionary<int, Type>(packets);
        }
        public static IPacket Handle(ByteBuf buf)
        {
            if (!Packets.TryGetValue(buf.ReadVarInt(), out var type)) return null;
            var packet = (IPacket) Activator.CreateInstance(type);
            packet.Read(buf);
            return packet;
        }
    }
}