using RemoteDesktopViewer.Utils;

namespace RemoteDesktopViewer.Network.Packet.Data
{
    public class PacketFileName : IPacket
    {
        private readonly int _id;
        private readonly string _data;
        
        public PacketFileName() {}

        public PacketFileName(int id, string data)
        {
            _id = id;
            _data = data;
        }
        
        public void Write(ByteBuf buf)
        {
            buf.WriteVarInt((int) PacketType.FileNameEvent);
            buf.WriteVarInt(_id);
            buf.WriteString(_data);
        }

        public void Read(NetworkManager networkManager, ByteBuf buf)
        {
            networkManager.FileChunkCreate(buf.ReadVarInt(), buf.ReadString());
        }
    }
    
    public class PacketFileChunk : IPacket
    {
        private readonly int _id;
        private readonly byte[] _data;
        
        public PacketFileChunk() {}

        public PacketFileChunk(int id, byte[] data)
        {
            _id = id;
            _data = data;
        }
        
        public void Write(ByteBuf buf)
        {
            buf.WriteVarInt((int) PacketType.FileChunkEvent);
            buf.WriteVarInt(_id);
            buf.Write(_data);
        }

        public void Read(NetworkManager networkManager, ByteBuf buf)
        {
            networkManager.FileChunkReceived(buf.ReadVarInt(), buf.Read(buf.Length));
        }
    }
    
    public class PacketFileFinished : IPacket
    {
        private readonly int _id;
        
        public PacketFileFinished() {}

        public PacketFileFinished(int id)
        {
            _id = id;
        }
        
        public void Write(ByteBuf buf)
        {
            buf.WriteVarInt((int) PacketType.FileFinishedEvent);
            buf.WriteVarInt(_id);
        }

        public void Read(NetworkManager networkManager, ByteBuf buf)
        {
            networkManager.FileChunkFinished(buf.ReadVarInt());
        }
    }
}