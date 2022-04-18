using System;
using System.Runtime.InteropServices;

namespace RemoteDesktopViewer.Utils
{
    public class NibbleArray
    {
        private byte[] _data;
        // public int Length => Data.Length / 3 * 4;
        public int Length => _length << 1;

        private int _length;

        public byte[] Data
        {
            get
            {
                var d = new byte[_length];
                Marshal.Copy(Marshal.UnsafeAddrOfPinnedArrayElement(_data, 0), d, 0, _length);
                return d;
            }
        }
        
        public NibbleArray(int capacity)
        {
            _data = new byte[(capacity >> 1) + (capacity & 1)];
            // Data = new byte[length / 4 * 3 + (length % 4 == 0 ? 0 : 3)];
        }

        public NibbleArray(byte[] bytes)
        {
            _data = bytes;
            _length = _data.Length;
        }

        public byte[] GetData(int length)
        {
            length = (length >> 1) + (length & 1);
            var d = new byte[length];
            Buffer.BlockCopy(_data, 0, d, 0, length);
            // Marshal.Copy(Marshal.UnsafeAddrOfPinnedArrayElement(_data, 0), d, 0, length);
            return d;
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
                (byte) ((_data[i >> 1] >> ((i & 1) << 2) & 0xF) << 4);
            // return (byte) ((((Data[index] & target) >> shift) | (((Data[index + 1] & target) >> shift) << 2) | (((Data[index + 2] & target) >> shift) << 4)) * 4);
            set
            {
                // CheckCapacity(i >> 1);
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
                _data[i >> 1] &= (byte) (0xF << (((i + 1) & 1) << 2));
                _data[i >> 1] |= (byte) (value << ((i & 1) << 2));

                //_length = Math.Max(_length, (i >> 1) + 1);
            }
        }

        private void CheckCapacity(int index)
        {
            var length = _data.Length;
            if (length > index) return;
            var d = new byte[index * 2 | 1];
            Marshal.Copy(Marshal.UnsafeAddrOfPinnedArrayElement(_data, 0), d, 0, _length);
            _data = d;
        }
    }
}