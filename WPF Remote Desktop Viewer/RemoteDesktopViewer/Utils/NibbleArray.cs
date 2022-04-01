using System.Collections.Generic;

namespace RemoteDesktopViewer.Utils
{
    public class NibbleArray
    {
        public byte[] Data { get; }
        // public int Length => Data.Length / 3 * 4;
        public int Length => Data.Length << 1;
        
        public NibbleArray(int length)
        {
            Data = new byte[(length >> 1) + (length & 1)];
            // Data = new byte[length / 4 * 3 + (length % 4 == 0 ? 0 : 3)];
        }

        public NibbleArray(byte[] bytes)
        {
            Data = bytes;
        }

        public byte this[int i]
        {
            get =>
                // var index = i / 4 * 3;
                // var shift = i % 4 * 2;
                // var result = (byte) ((Data[index] >> shift) & 0b_00000011);
                // result |= (byte) (((Data[index + 1] >> shift) & 0b_00000011) << 2);
                // result |= (byte) (((Data[index + 2] >> shift) & 0b_00000011) << 4);
                // return (byte) Math.Round(result * Div);
                (byte) ((Data[i >> 1] >> ((i & 1) << 2) & 0xF) << 4);
            // return (byte) ((((Data[index] & target) >> shift) | (((Data[index + 1] & target) >> shift) << 2) | (((Data[index + 2] & target) >> shift) << 4)) * 4);
            set
            {
                // value = (byte) Math.Round(value / Div);
                // var index = i / 4 * 3;
                // var shift = i % 4 * 2;
                // var target = (byte) ~(0b_00000011 << shift);
                // Data[index] &= target;
                // Data[index] |= (byte) ((value & 0b_00000011) << shift);
                //
                // value >>= 2;
                // Data[index + 1] &= target;
                // Data[index + 1] |= (byte) ((value & 0b_00000011) << shift);
                //
                // value >>= 2;
                // Data[index + 2] &= target;
                // Data[index + 2] |= (byte) ((value & 0b_00000011) << shift);

                value >>= 4;
                Data[i >> 1] &= (byte) (0xF << (((i + 1) & 1) << 2));
                Data[i >> 1] |= (byte) (value << ((i & 1) << 2));
            }
        }
    }
}