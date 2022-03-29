using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
using RemoteDesktopViewer.Utils;

namespace RemoteDesktopViewer.Network.Packet.Data
{
    public class PacketScreen : IPacket
    {
        /*
        // Compress
        private readonly int _width, _height;
        private readonly int _format;
        private readonly double _dpiX, _dpiY;
        private readonly int _size;
        */
        
        private readonly byte[] _data;
        internal PacketScreen() {}

         // JPEG
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
        
        /*
         // Compress
        internal PacketScreen(Bitmap bitmap)
        {
            _width = bitmap.Width;
            _height = bitmap.Height;
            _format = bitmap.PixelFormat.ToWpfPixelFormat().ToId();
            _dpiX = bitmap.HorizontalResolution;
            _dpiY = bitmap.VerticalResolution;
            var pixels = bitmap.ToPixelArray();
            _size = pixels.Length;

            // _data = new NibbleArray((IReadOnlyList<byte>) pixels.Loss()).Data.Compress();
            _data = pixels.ImageCompress().Data;
        }
        */

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