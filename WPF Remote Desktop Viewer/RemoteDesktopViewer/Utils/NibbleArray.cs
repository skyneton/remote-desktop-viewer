using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace RemoteDesktopViewer.Utils
{
    public class NibbleArray
    {
        private const double Div = 4.047619047619048;
        public byte[] Data { get; }
        public int Length => Data.Length / 3 * 4;

        public NibbleArray(int length)
        {
            Data = new byte[length / 4 * 3 + (length % 4 == 0 ? 0 : 3)];
        }

        public NibbleArray(IReadOnlyList<byte> arr)
        {
            var length = arr.Count;
            Data = new byte[length / 4 * 3 + (length % 4 == 0 ? 0 : 3)];
            for (var i = 0; i < length; i++)
            {
                this[i] = arr[i];
                // var test = i - i % 3;
                // if (arr[test] == 255 && arr[test + 1] == 255 && arr[test + 2] == 255)
                // {
                //     Debug.WriteLine("ASDF");
                // }
            }
        }

        public NibbleArray(byte[] bytes)
        {
            Data = bytes;
        }

        public byte this[int i]
        {
            get
            {
                // var index = i / 4 * 3;
                // var shift = i % 4 * 2;
                // var result = (byte) ((Data[index] >> shift) & 0b_00000011);
                // result |= (byte) (((Data[index + 1] >> shift) & 0b_00000011) << 2);
                // result |= (byte) (((Data[index + 2] >> shift) & 0b_00000011) << 4);
                // return (byte) Math.Round(result * Div);
                return (byte) Math.Round((Data[i / 2] >> (i % 2 * 4) & 0xF) * 17.0);
                // return (byte) ((((Data[index] & target) >> shift) | (((Data[index + 1] & target) >> shift) << 2) | (((Data[index + 2] & target) >> shift) << 4)) * 4);
            }
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
                
                value = (byte) Math.Round(value / 17.0);
                value &= 0xF;
                Data[i / 2] &= (byte) (0xF << ((i + 1) % 2 * 4));
                Data[i / 2] |= (byte) (value << (i % 2 * 4));
            }
        }

        public static explicit operator byte[](NibbleArray nibbleArray) => nibbleArray.Data;
    }
}