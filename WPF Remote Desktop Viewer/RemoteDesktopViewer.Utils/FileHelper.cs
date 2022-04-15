using System;
using System.IO;
using RemoteDesktopViewer.Utils.Byte;

namespace RemoteDesktopViewer.Utils
{
    public class FileHelper
    {
        public static string GetDesktopFilePath(string name)
        {
            // var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            // if (!File.Exists(Path.Combine(desktop, name)))
            //     return Path.Combine(desktop, name);
            //
            // var withOutExtension = Path.GetFileNameWithoutExtension(name);
            // var extension = Path.GetExtension(name);
            // var i = 0;
            //
            // var path = Path.Combine(desktop, $"{withOutExtension} ({i}){extension}");
            //
            // while (File.Exists(path))
            // {
            //     i++;
            //     path = Path.Combine(desktop, $"{withOutExtension} ({i}){extension}");
            // }

            // return path;
            return GetFileName(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), name);
        }

        public static string GetFileName(string path, string name)
        {
            if (!File.Exists(Path.Combine(path, name)))
                return Path.Combine(path, name);

            var withOutExtension = Path.GetFileNameWithoutExtension(name);
            var extension = Path.GetExtension(name);
            var i = 0;

            var result = Path.Combine(path, $"{withOutExtension} ({i}){extension}");

            while (File.Exists(result))
            {
                i++;
                result = Path.Combine(path, $"{withOutExtension} ({i}){extension}");
            }

            return result;
        }

        public static byte[] GetData(string path) => Directory.Exists(path)
            ? ByteHelper.DirectoryCompress(path)
            : ByteHelper.Compress(File.ReadAllBytes(path));
    }
}