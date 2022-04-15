namespace RemoteDesktopViewer.Utils.Clipboard
{
    public class ClipboardTypeFile
    {
        public string[] Name { get; private set; }

        public ClipboardTypeFile(params string[] name)
        {
            Name = name;
        }

        public override string ToString()
        {
            return string.Join(", ", Name);
        }
    }
}