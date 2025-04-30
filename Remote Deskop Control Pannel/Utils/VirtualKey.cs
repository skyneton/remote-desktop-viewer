namespace RemoteDeskopControlPannel.Utils
{
    internal class VirtualKey(int vK, bool systemKey)
    {
        public readonly int VK = vK;
        public readonly bool SystemKey = systemKey;

        public override int GetHashCode()
        {
            return VK;
        }
        public override bool Equals(object? obj)
        {
            return (obj as VirtualKey)?.VK == VK && (obj as VirtualKey)?.SystemKey == SystemKey;
        }
    }
}
