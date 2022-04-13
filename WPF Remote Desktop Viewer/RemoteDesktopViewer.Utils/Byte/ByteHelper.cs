using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization.Formatters.Binary;

namespace RemoteDesktopViewer.Utils.Byte
{
    public static class ByteHelper
    {

        public static byte[] Compress(byte[] input)
        {
            using var stream = new MemoryStream();
            using (var zip = new DeflateStream(stream, CompressionMode.Compress))
            {
                zip.Write(input, 0, input.Length);
                zip.Flush();
            }

            return stream.ToArray();
        }

        public static byte[] Decompress(byte[] input)
        {
            using var stream = new MemoryStream(input);
            using var zip = new DeflateStream(stream, CompressionMode.Decompress);
            using var result = new MemoryStream();
            zip.CopyTo(result);

            return result.ToArray();
        }

        public static byte[] GZipCompress(byte[] input)
        {
            using var ms = new MemoryStream();
            using (var zip = new GZipStream(ms, CompressionMode.Compress))
            {
                zip.Write(input, 0, input.Length);
                zip.Flush();
            }

            return ms.ToArray();
        }

        public static byte[] DirectoryCompress(string path)
        {
            var tempPath = FileHelper.GetFileName(Path.GetTempPath(), "rdv_compressed.zip");
            ZipFile.CreateFromDirectory(path, tempPath);
            
            return File.ReadAllBytes(tempPath);
        }

        public static void DirectoryDecompress(string path, byte[] zip)
        {
            var tempPath = FileHelper.GetFileName(Path.GetTempPath(), "rdv_decompressed.zip");
            File.WriteAllBytes(tempPath, zip);
            ZipFile.ExtractToDirectory(tempPath, path);
        }

        public static void CreateCompactArray(MemoryStream ms, int bitsPerBlock, ByteBuf pixelPalette)
        {
            ushort buffer = 0;
            var bitIndex = 0;
            var length = pixelPalette.Length >> 1;
            //Write(ms, ByteBuf.GetVarInt((pixelPalette.Length * bitsPerBlock) >> 3));

            for (var i = 0; i < length; i++)
            {
                var value = (ushort)pixelPalette.ReadShort();
                buffer |= (ushort)(value << bitIndex);
                var remaining = bitsPerBlock - (16 - bitIndex);
                if (remaining >= 0)
                {
                    //ms.Write(BitConverter.GetBytes((short)IPAddress.HostToNetworkOrder(buffer)), 0, 2);
                    ms.WriteByte((byte)(buffer >> 8));
                    ms.WriteByte((byte)buffer);
                    buffer = (ushort)(value >> (bitsPerBlock - remaining));
                    bitIndex = remaining;
                }
                else
                    bitIndex += bitsPerBlock;
            }

            if (bitIndex > 0)
            {
                ms.WriteByte((byte)(buffer >> 8));
                ms.WriteByte((byte)buffer);
            }
        }

        public static void IterateCompactArray(int bitsPerBlock, int length, byte[] lowData, Action<int, int> action)
        {
            var buf = new ByteBuf(lowData);
            var lowLength = lowData.Length >> 1;
            var data = new ushort[lowLength];

            for (var i = 0; i < lowLength; i++)
                data[i] = (ushort)buf.ReadShort();

            var maxEntryValue = (1L << bitsPerBlock) - 1;
            for (var i = 0; i < length; i++)
            {
                var bitIndex = i * bitsPerBlock;
                var start = bitIndex >> 4;
                var end = (bitIndex + bitsPerBlock - 1) >> 4;
                var startBitSub = bitIndex & 15;
                int value;
                if (start == end)
                {
                    value = (int)(data[start] >> startBitSub & maxEntryValue);
                }
                else
                {
                    var endBitSubIndex = 16 - startBitSub;
                    value = (int)((data[start] >> startBitSub | data[end] << endBitSubIndex) & maxEntryValue);
                }
                action.Invoke(i, value);
            }
        }

        public static byte GetBitsPerBlock(int paletteSize)
        {
            byte bitsPerBlock = 4;
            while (paletteSize > 1 << bitsPerBlock)
                bitsPerBlock++;

            return bitsPerBlock;
        }

        public static byte[] Object2ByteArray(object obj)
        {
            var bf = new BinaryFormatter();
            using var ms = new MemoryStream();
            bf.Serialize(ms, obj);
            
            return ms.ToArray();
        }

        public static object ByteArray2Object(byte[] array)
        {
            var bf = new BinaryFormatter(); 
            using var ms = new MemoryStream(array);
            return bf.Deserialize(ms);
        }
    }
}