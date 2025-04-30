namespace RemoteDeskopControlPannel.Utils
{
    enum CompressType
    {
        None = 0,
        Jpeg = 0b1,
        Png = 0b10,
        Webp = 0b11,
        Broti = 0b10000,
        Gzip = 0b100000,
        Deflate = 0b110000,
    }
}
