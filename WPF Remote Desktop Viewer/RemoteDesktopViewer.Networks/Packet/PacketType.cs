namespace RemoteDesktopViewer.Networks.Packet
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
        CursorEvent,
        FileNameEvent,
        FileChunkEvent,
        FileFinishedEvent,
    }
}