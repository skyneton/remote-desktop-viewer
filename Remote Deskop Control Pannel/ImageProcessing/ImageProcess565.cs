using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using RemoteDeskopControlPannel.Utils;
namespace RemoteDeskopControlPannel.ImageProcessing
{
    class ImageProcess565 : ImageProcess
    {
        public override QualityMode Quality => QualityMode.Byte2RGB;
        public override PixelFormat Format => PixelFormat.Format16bppRgb565;
        public override byte PixelBytes => 2;

        protected override bool ConvertInput(MemoryStream changedPixelStream, nint ptr, byte[] pixelData, int offset)
        {
            var rg = Marshal.ReadByte(ptr);
            ptr++;
            var gb = Marshal.ReadByte(ptr);
            if (pixelData[offset] != rg || pixelData[offset + 1] != gb)
            {
                pixelData[offset++] = rg;
                pixelData[offset] = gb;
                changedPixelStream.WriteByte(rg);
                changedPixelStream.WriteByte(gb);
                return true;
            }
            return false;
        }
    }
}
