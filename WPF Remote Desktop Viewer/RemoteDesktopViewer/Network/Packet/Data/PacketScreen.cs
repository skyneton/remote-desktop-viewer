using System.Collections.Generic;
using System.Drawing;
using RemoteDesktopViewer.Utils;

namespace RemoteDesktopViewer.Network.Packet.Data
{
    public class PacketScreen : Packet
    {
        private readonly int _width, _height;
        private readonly int _format;
        private readonly double _dpiX, _dpiY;
        private readonly int _size;
        private readonly byte[] _data;
        internal PacketScreen() {}

        internal PacketScreen(Bitmap bitmap)
        {
            _width = bitmap.Width;
            _height = bitmap.Height;
            _format = bitmap.PixelFormat.ToWpfPixelFormat().ToId();
            _dpiX = bitmap.HorizontalResolution;
            _dpiY = bitmap.VerticalResolution;
            var pixels = bitmap.ToPixelArray();
            _size = pixels.Length;

            _data = new NibbleArray((IReadOnlyList<byte>) pixels).Data.Compress();
            // var test = new NibbleArray((IReadOnlyList<byte>) pixels);
            // Debug.WriteLine(string.Join(", ", pixels[0], pixels[1], pixels[2], pixels[3], pixels[4], pixels[5], pixels[6], pixels[7]));
            // Debug.WriteLine(string.Join(", ", test[0], test[1], test[2], test[3], test[4], test[5], test[6], test[7]));
            
            // _data = bitmap.ToByteArray().GZipCompress();
            // Debug.WriteLine(bitmap.ToByteArray().Compress().Length +", " + new NibbleArray((IReadOnlyList<byte>) bitmap.ToPixelArray()).Data.Compress().Length);
            // var array = new NibbleArray(10);
            // array[0] = 0;
            // array[1] = 127;
            // array[2] = 255;
            // array[3] = 255;
            // array[4] = 127;
            // array[5] = 255;
            // array[6] = 255;
            // array[7] = 0;
            // array[8] = 0;
            // array[9] = 0;
            // array[10] = 0;
            // Debug.WriteLine(array[0] +", "+array[1] +", " + array[2] +", " + array[3] +", " + array[4] +", " + array[5]);
            // Debug.WriteLine(Convert.ToString(array.Data[0], 2).PadLeft(8, '0'));
            // Debug.WriteLine(Convert.ToString(array.Data[1], 2).PadLeft(8, '0'));
            // Debug.WriteLine(Convert.ToString(array.Data[2], 2).PadLeft(8, '0'));
        }

        internal override void Write(ByteBuf buf)
        {
            buf.WriteVarInt((int) PacketType.Screen);
            buf.WriteVarInt(_width);
            buf.WriteVarInt(_height);
            buf.WriteDouble(_dpiX);
            buf.WriteDouble(_dpiY);
            buf.WriteVarInt(_format);
            buf.WriteVarInt(_size);
            buf.Write(_data);
        }

        internal override void Read(NetworkManager networkManager, ByteBuf buf)
        {
            // networkManager.ClientWindow?.DrawFullScreen(buf.ReadVarInt(), new NibbleArray(buf.Read(buf.Available).Decompress()));
            networkManager.ClientWindow?.DrawFullScreen(buf.ReadVarInt(), buf.ReadVarInt(), buf.ReadDouble(),
                buf.ReadDouble(), buf.ReadVarInt().ToPixelFormat(), buf.ReadVarInt(),
                new NibbleArray(buf.Read(buf.Available).Decompress()));
        }
    }

    public class PacketScreenChunk : Packet
    {
        private int _x, _y;
        private readonly int _width, _height;
        private int _size;
        private byte[] _data;
        internal PacketScreenChunk() {}

        internal PacketScreenChunk(int x, int y, Bitmap bitmap)
        {
            _x = x;
            _y = y;
            _width = bitmap.Width;
            _height = bitmap.Height;
            var pixels = bitmap.ToPixelArray();
            _size = pixels.Length;

            _data = new NibbleArray((IReadOnlyList<byte>) pixels).Data.Compress();
            // _data = bitmap.ToByteArray().Compress();
        }

        internal PacketScreenChunk(int x, int y, int width, int height, int size, byte[] nibbleData)
        {
            _x = x;
            _y = y;
            _width = width;
            _height = height;
            _size = size;

            _data = nibbleData.Compress();
            // _data = bitmap.ToByteArray().Compress();
        }

        internal override void Write(ByteBuf buf)
        {
            buf.WriteVarInt((int) PacketType.ScreenChunk);
            buf.WriteVarInt(_x);
            buf.WriteVarInt(_y);
            buf.WriteVarInt(_width);
            buf.WriteVarInt(_height);
            buf.WriteVarInt(_size);
            buf.Write(_data);
        }

        internal override void Read(NetworkManager networkManager, ByteBuf buf)
        {
            // networkManager.ClientWindow?.DrawScreenChunk(buf.ReadVarInt(), buf.ReadVarInt(),
            //     buf.Read(buf.Available).Decompress());
            networkManager.ClientWindow?.DrawScreenChunk(buf.ReadVarInt(), buf.ReadVarInt(), buf.ReadVarInt(),
                buf.ReadVarInt(), buf.ReadVarInt(),
                new NibbleArray(buf.Read(buf.Available).Decompress()));
        }
    }
}