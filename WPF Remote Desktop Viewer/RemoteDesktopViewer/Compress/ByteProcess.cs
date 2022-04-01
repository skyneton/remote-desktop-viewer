using System.IO;
using System.IO.Compression;

namespace RemoteDesktopViewer.Compress
{
    public class ByteProcess
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
    }
}