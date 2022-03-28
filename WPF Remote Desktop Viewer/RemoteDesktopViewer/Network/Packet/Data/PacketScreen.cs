using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using RemoteDesktopViewer.Utils;

namespace RemoteDesktopViewer.Network.Packet.Data
{
    public class PacketScreen : IPacket
    {
        // private readonly int _width, _height;
        // private readonly int _format;
        // private readonly double _dpiX, _dpiY;
        // private readonly int _size;
        private readonly byte[] _data;
        internal PacketScreen() {}

        internal PacketScreen(Bitmap bitmap)
        {
            // _width = bitmap.Width;
            // _height = bitmap.Height;
            // _format = bitmap.PixelFormat.ToWpfPixelFormat().ToId();
            // _dpiX = bitmap.HorizontalResolution;
            // _dpiY = bitmap.VerticalResolution;
            // var pixels = bitmap.ToPixelArray();
            // _size = pixels.Length;

            // _data = new NibbleArray((IReadOnlyList<byte>) pixels.Loss()).Data.Compress();
            // _data = pixels.ImageCompress().Data;
            // var test = new NibbleArray((IReadOnlyList<byte>) pixels);
            
            _data = bitmap.ToByteArray();
        }

        public void Write(ByteBuf buf)
        {
            buf.WriteVarInt((int) PacketType.Screen);
            // buf.WriteVarInt(_width);
            // buf.WriteVarInt(_height);
            // buf.WriteDouble(_dpiX);
            // buf.WriteDouble(_dpiY);
            // buf.WriteVarInt(_format);
            // buf.WriteVarInt(_size);
            buf.Write(_data);
        }

        public void Read(NetworkManager networkManager, ByteBuf buf)
        {
            networkManager.ClientWindow?.DrawFullScreen(buf.Read(buf.Length));
            // networkManager.ClientWindow?.DrawFullScreen(buf.ReadVarInt(), buf.ReadVarInt(), buf.ReadDouble(),
            //     buf.ReadDouble(), buf.ReadVarInt().ToPixelFormat(), buf.ReadVarInt(),
            //     new NibbleArray(buf.Read(buf.Length)));
        }
    }

    public class PacketScreenChunk : IPacket
    {
        private int _x, _y;
        // private readonly int _width, _height;
        // private int _size;
        private byte[] _data;
        internal PacketScreenChunk() {}

        internal PacketScreenChunk(int x, int y, Bitmap bitmap)
        {
            _x = x;
            _y = y;
            // _width = bitmap.Width;
            // _height = bitmap.Height;
            var pixels = bitmap.ToPixelArray();
            // _size = pixels.Length;

            _data = new NibbleArray((IReadOnlyList<byte>) pixels.Loss()).Data;
            // Debug.WriteLine(_data.Length +", ");
            // _data = bitmap.ToByteArray().Compress();
        }

        internal PacketScreenChunk(int x, int y, byte[] bitmap)
        {
            _x = x;
            _y = y;

            _data = bitmap;
            // Debug.WriteLine(_data.Length +", ");
            // _data = bitmap.ToByteArray().Compress();
        }

        internal PacketScreenChunk(int x, int y, int width, int height, int size, byte[] nibbleData)
        {
            _x = x;
            _y = y;
            // _width = width;
            // _height = height;
            // _size = size;

            _data = nibbleData;
            // _data = bitmap.ToByteArray().Compress();
        }

        public void Write(ByteBuf buf)
        {
            buf.WriteVarInt((int) PacketType.ScreenChunk);
            buf.WriteVarInt(_x);
            buf.WriteVarInt(_y);
            // buf.WriteVarInt(_width);
            // buf.WriteVarInt(_height);
            // buf.WriteVarInt(_size);
            buf.Write(_data);
        }

        public void Read(NetworkManager networkManager, ByteBuf buf)
        {
            // networkManager.ClientWindow?.DrawScreenChunk(buf.ReadVarInt(), buf.ReadVarInt(),
            //     buf.ReadVarInt(),buf.ReadVarInt(), buf.ReadVarInt(), new NibbleArray(buf.Read(buf.Length)));
            networkManager.ClientWindow?.DrawScreenChunk(buf.ReadVarInt(), buf.ReadVarInt(), buf.Read(buf.Length));
        }
    }
}