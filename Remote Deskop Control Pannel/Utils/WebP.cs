using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace RemoteDeskopControlPannel.Utils
{
    public static class WebP
    {
        [DllImport("libwebp.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int WebPGetInfo(nint ptr, int size, out int width, out int height);
        [DllImport("libwebp.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int WebPEncodeBGR(nint ptr, int width, int height, int stride, float quality, out nint ptrOut);
        [DllImport("libwebp.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int WebPEncodeLosslessBGR(nint ptr, int width, int height, int stride, out nint ptrOut);
        [DllImport("libwebp.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern nint WebPDecodeBGR(nint ptr, int size, ref int width, ref int height);
        [DllImport("libwebp.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern nint WebPDecodeBGRInto(nint ptr, int size, nint outPtr, int outSize, int stride);
        [DllImport("libwebp.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int WebPFree(nint ptr);

        public static byte[] Encode(Bitmap bitmap, int quality)
        {
            var bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            var size = WebPEncodeBGR(bitmapData.Scan0, bitmapData.Width, bitmapData.Height, bitmapData.Stride, quality, out var ptr);
            var result = new byte[size];
            Marshal.Copy(ptr, result, 0, size);
            WebPFree(ptr);
            bitmap.UnlockBits(bitmapData);
            return result;
        }
        public static byte[] Encode(Bitmap bitmap)
        {
            var bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            var size = WebPEncodeLosslessBGR(bitmapData.Scan0, bitmapData.Width, bitmapData.Height, bitmapData.Stride, out var ptr);
            var result = new byte[size];
            Marshal.Copy(ptr, result, 0, size);
            WebPFree(ptr);
            bitmap.UnlockBits(bitmapData);
            return result;
        }
        public static Bitmap? Decode(byte[] data)
        {
            var pinned = GCHandle.Alloc(data, GCHandleType.Pinned);
            var addr = pinned.AddrOfPinnedObject();
            var result = Decode(addr, data.Length);
            pinned.Free();
            return result;
        }

        private static Bitmap? Decode(nint ptr, int length)
        {
            if (WebPGetInfo(ptr, length, out var width, out var height) == 0) return null;
            var bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            var bitmapData = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            var result = WebPDecodeBGRInto(ptr, length, bitmapData.Scan0, bitmapData.Stride * bitmapData.Height, bitmapData.Stride);
            var success = bitmapData.Scan0 == result;
            bitmap.UnlockBits(bitmapData);
            if (!success)
            {
                bitmap.Dispose();
                return null;
            }
            return bitmap;
        }
    }
}
