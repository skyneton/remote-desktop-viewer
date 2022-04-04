using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.IO.Packaging;

namespace RemoteDesktopViewer.Utils.Compress
{
    public class ByteHelper
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
            Debug.WriteLine(tempPath);
            ZipFile.ExtractToDirectory(tempPath, path);
        }
    }
}