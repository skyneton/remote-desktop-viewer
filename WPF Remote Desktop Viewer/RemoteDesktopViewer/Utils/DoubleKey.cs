using System.Collections.Generic;

namespace RemoteDesktopViewer.Utils
{
    public class DoubleKey<T1, T2> where T1: struct where T2 : struct {
        public readonly T1 X;
        public readonly T2 Y;
        public DoubleKey(T1 x, T2 y) { X = x; Y = y;}

        public override string ToString()
        {
            return $"<{X}, {Y}>";
        }

        public override bool Equals(object obj)
        {
            return obj is DoubleKey<T1, T2> o && o.X.Equals(X) && o.Y.Equals(Y);
        }
        
        public static bool operator ==(DoubleKey<T1, T2> c1, DoubleKey<T1, T2> c2)
        {
            if (c1 is null || c2 is null) return c1 is null && c2 is null;
            return c1.Equals(c2);
        }

        public static bool operator !=(DoubleKey<T1, T2> c1, DoubleKey<T1, T2> c2) => !(c1 == c2);

        public override int GetHashCode()
        {
            return X.GetHashCode() << 6 | Y.GetHashCode();
        }
    }
}