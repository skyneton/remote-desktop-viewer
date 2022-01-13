namespace RemoteDesktopViewer.Utils
{
    public readonly struct Tuple<T1, T2> {
        public readonly T1 X;
        public readonly T2 Y;
        public Tuple(T1 x, T2 y) { X = x; Y = y;}

        public override string ToString()
        {
            return $"<{X}, {Y}>";
        }
    }

    public static class Tuple { // for type-inference goodness.
        public static Tuple<T1,T2> Create<T1,T2>(T1 item1, T2 item2) { 
            return new Tuple<T1,T2>(item1, item2); 
        }
    }
}