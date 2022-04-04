using RemoteDesktopViewer.Utils;

namespace RemoteDesktopViewer.Networks.Packet.Data
{
    public class PacketFileName : IPacket
    {
        public int Id { get; private set; }
        public string Data { get; private set; }
        public bool IsDirectory { get; private set; }
        
        public PacketFileName() {}

        public PacketFileName(int id, string data, bool isDirectory)
        {
            Id = id;
            Data = data;
            IsDirectory = isDirectory;
        }
        
        public void Write(ByteBuf buf)
        {
            buf.WriteVarInt((int) PacketType.FileNameEvent);
            buf.WriteVarInt(Id);
            buf.WriteString(Data);
            buf.WriteBool(IsDirectory);
        }

        public void Read(ByteBuf buf)
        {
            Id = buf.ReadVarInt();
            Data = buf.ReadString();
            IsDirectory = buf.ReadBool();
        }
    }
    
    public class PacketFileChunk : IPacket
    {
        public int Id { get; private set; }
        public byte[] Data { get; private set; }
        
        public PacketFileChunk() {}

        public PacketFileChunk(int id, byte[] data)
        {
            Id = id;
            Data = data;
        }
        
        public void Write(ByteBuf buf)
        {
            buf.WriteVarInt((int) PacketType.FileChunkEvent);
            buf.WriteVarInt(Id);
            buf.Write(Data);
        }

        public void Read(ByteBuf buf)
        {
            Id = buf.ReadVarInt();
            Data = buf.Read(buf.Length);
        }
    }
    
    public class PacketFileFinished : IPacket
    {
        public int Id { get; private set; }
        
        public PacketFileFinished() {}

        public PacketFileFinished(int id)
        {
            Id = id;
        }
        
        public void Write(ByteBuf buf)
        {
            buf.WriteVarInt((int) PacketType.FileFinishedEvent);
            buf.WriteVarInt(Id);
        }

        public void Read(ByteBuf buf)
        {
            Id = buf.ReadVarInt();
        }
    }
}