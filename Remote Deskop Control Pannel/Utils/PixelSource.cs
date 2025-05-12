namespace RemoteDeskopControlPannel.Utils
{
    internal class PixelSource
    {
        public int Width { get; set; } = 0;
        public int Height { get; set; } = 0;
        public byte[] Source { get; set; } = [];
        private int _updated = 0;
        public bool Updated
        {
            get
            {
                var value = _updated != 0;
                Interlocked.Exchange(ref _updated, 0);
                return value;
            }
        }
        public PixelSource() { }
        public void Set(byte[] source, int width, int height)
        {
            lock (this)
            {
                Source = source;
                Width = width;
                Height = height;
                _updated = 1;
            }
        }

        public void OnUpdate()
        {
            Interlocked.Exchange(ref _updated, 1);
        }
    }
}
