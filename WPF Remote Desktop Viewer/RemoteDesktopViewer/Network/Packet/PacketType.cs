namespace RemoteDesktopViewer.Network.Packet
{
    public enum PacketType
    {
        KeepAlive,
        Disconnect,
        Login,
        Screen,
        ScreenChunk,
        ServerControl,
        MouseMove,
        MouseEvent,
        KeyEvent,
        CursorEvent
    }
}