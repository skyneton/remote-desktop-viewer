using System;
using System.Security.Cryptography;
using System.Text;

namespace RemoteDesktopViewer.Utils
{
    public class CryptoHelper
    {
        public static string ToSha256(string str)
        {
            using var sha256 = new SHA256Managed();
            var encrypt = sha256.ComputeHash(Encoding.UTF8.GetBytes(str));

            return Convert.ToBase64String(encrypt);
        }
    }
}