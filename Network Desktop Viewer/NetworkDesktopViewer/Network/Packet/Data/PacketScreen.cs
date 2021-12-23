using System.Drawing;
using RemoteDesktopViewer.Utils;

namespace RemoteDesktopViewer.Network.Packet.Data
{
    public class PacketScreen : Packet
    {
        private readonly byte[] _data;
        internal PacketScreen() {}

        internal PacketScreen(Bitmap bitmap)
        {
            _data = bitmap.ToByteArray().GZipCompress();
        }

        internal override void Write(ByteBuf buf)
        {
            buf.WriteVarInt((int) PacketType.Screen);
            // buf.WriteVarInt(_width);
            // buf.WriteVarInt(_height);
            buf.Write(_data);
        }

        internal override void Read(NetworkManager networkManager, ByteBuf buf)
        {
            networkManager.ClientForm?.DrawFullScreen(buf.Read(buf.Length - buf.Position).GZipDecompress());
        }
    }

    public class PacketScreenChunk : Packet
    {
        private int _x, _y;
        private byte[] _data;
        internal PacketScreenChunk() {}

        internal PacketScreenChunk(int x, int y, Bitmap bitmap)
        {
            _x = x;
            _y = y;
            _data = bitmap.ToByteArray().GZipCompress();
        }

        internal PacketScreenChunk(int x, int y, int width, int height, byte[] data)
        {
            _x = x;
            _y = y;
            _data = data.GZipCompress();
        }

        internal override void Write(ByteBuf buf)
        {
            buf.WriteVarInt((int) PacketType.ScreenChunk);
            buf.WriteVarInt(_x);
            buf.WriteVarInt(_y);
            buf.Write(_data);
        }

        internal override void Read(NetworkManager networkManager, ByteBuf buf)
        {
            networkManager.ClientForm?.DrawScreenChunk(buf.ReadVarInt(), buf.ReadVarInt(),
                buf.Read(buf.Length - buf.Position).GZipDecompress());
        }
    }
}