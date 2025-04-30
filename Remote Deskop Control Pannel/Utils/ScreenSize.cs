namespace RemoteDeskopControlPannel.Utils
{
    public class ScreenSize(int width, int height)
    {
        public int Width { get; private set; } = width;
        public int Height { get; private set; } = height;
        public static readonly ScreenSize Zero = new(0, 0);

        public void Set(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public override bool Equals(object? obj)
        {
            return (obj as ScreenSize)?.Width == Width && (obj as ScreenSize)?.Height == Height;
        }

        public override int GetHashCode()
        {
            return Width << 16 | Height;
        }
    }
}
