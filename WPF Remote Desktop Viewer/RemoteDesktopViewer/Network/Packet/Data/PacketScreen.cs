using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using RemoteDesktopViewer.Image;
using RemoteDesktopViewer.Utils;

namespace RemoteDesktopViewer.Network.Packet.Data
{
    public class PacketScreen : IPacket
    {
        private readonly int _format;
        private readonly int _width, _height;
        private readonly float _dpiX, _dpiY;
        
        private readonly byte[] _data;
        internal PacketScreen() {}

        internal PacketScreen(PixelFormat format, int width, int height, DoubleKey<float, float> dpi, byte[] data)
        {
            _format = format.ToWpfPixelFormat().ToId();
            _width = width;
            _height = height;
            _dpiX = dpi.X;
            _dpiY = dpi.Y;

            _data = ImageProcess.ToGifImage(width, height, PixelFormat.Format8bppIndexed, data);
            // Debug.WriteLine($"Before: {data.Length} Compressed: {_data.Length}");
            // _data = data;
        }

        public void Write(ByteBuf buf)
        {
            buf.WriteVarInt((int) PacketType.Screen);
            buf.WriteVarInt(_format);
            buf.WriteVarInt(_width);
            buf.WriteVarInt(_height);
            buf.WriteFloat(_dpiX);
            buf.WriteFloat(_dpiY);
            buf.WriteVarInt(_data.Length);
            buf.Write(_data);
        }

        public void Read(NetworkManager networkManager, ByteBuf buf)
        {
            // networkManager.ClientWindow?.DrawFullScreen(buf.Read(buf.Length));
            // networkManager.ClientWindow?.DrawFullScreen(buf.ReadVarInt(), buf.ReadVarInt(), buf.ReadDouble(),
            //     buf.ReadDouble(), buf.ReadVarInt().ToPixelFormat(), buf.ReadVarInt(),
            //     new NibbleArray(buf.Read(buf.Length)));
            networkManager.ClientWindow?.DrawFullScreen(buf.ReadVarInt().ToPixelFormat(), buf.ReadVarInt(), buf.ReadVarInt(), buf.ReadFloat(), buf.ReadFloat(), buf.Read(buf.ReadVarInt()));
        }
    }

    public class PacketScreenChunk : IPacket
    {
        private byte[] _data;
        internal PacketScreenChunk() {}

        internal PacketScreenChunk(byte[] chunk)
        {
            _data = chunk;
        }

        public void Write(ByteBuf buf)
        {
            buf.WriteVarInt((int) PacketType.ScreenChunk);
            buf.Write(_data);
        }

        public void Read(NetworkManager networkManager, ByteBuf buf)
        {
            networkManager.ClientWindow?.DrawScreenChunk(buf);
            // networkManager.ClientWindow?.DrawScreenChunkCompress(buf);
        }
    }
}