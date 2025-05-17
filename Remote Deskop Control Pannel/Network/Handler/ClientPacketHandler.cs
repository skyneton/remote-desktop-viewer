using NetworkLibrary.Networks.Multi;
using NetworkLibrary.Networks.Packet;
using RemoteDeskopControlPannel.ImageProcessing;
using RemoteDeskopControlPannel.Network.Packet;
using RemoteDeskopControlPannel.Utils;

namespace RemoteDeskopControlPannel.Network.Handler
{
    internal class ClientPacketHandler(Client client) : IMultiPacketHandler
    {
        public void Handle(MultiNetwork network, IPacket packet)
        {
            switch (packet)
            {
                case PacketFullScreen:
                    ReceivePacketFullScreen((PacketFullScreen)packet);
                    break;
                case PacketScreenChunk:
                    ReceivePacketScreenChunk((PacketScreenChunk)packet);
                    break;
                case PacketScreenInfo:
                    ReceivePacketScreenInfo((PacketScreenInfo)packet);
                    break;
                case PacketCursorType:
                    ReceivePacketCursorType((PacketCursorType)packet);
                    break;
                case PacketSoundInfo:
                    ReceivePacketSoundInfo((PacketSoundInfo)packet);
                    break;
                case PacketSoundChunk:
                    ReceivePacketSoundChunk((PacketSoundChunk)packet);
                    break;
            }
            ;
        }

        private void ReceivePacketSoundChunk(PacketSoundChunk packet)
        {
            client.ReceiveSoundChunk(packet.Chunk);
        }

        private void ReceivePacketSoundInfo(PacketSoundInfo packet)
        {
            client.StartSoundTrack(packet.SampleRate, packet.BitsPerSample, packet.Channels);
        }

        private void ReceivePacketCursorType(PacketCursorType packet)
        {
            client.CursorUpdate(packet.Cursor);

        }

        private void ReceivePacketScreenChunk(PacketScreenChunk packet)
        {
            if (client.Source.Source.Length == 0 || client.Source.Width <= 0 || client.Source.Height <= 0) return;
            Task.Run(() =>
            {
                client.ScreenProcess.Deprocess(client.Source.Source, packet.CompressType, packet.PixelPos, packet.PixelData);
                client.Source.OnUpdate();
                //client.Window.Dispatcher.Invoke(() =>
                //{
                //    client.ScreenProcess.Deprocess(client.Bitmap, packet.CompressType, packet.PixelPos, packet.PixelData);
                //});
            });
        }

        private void ReceivePacketScreenInfo(PacketScreenInfo packet)
        {
            client.UpdateScreenProcessor(packet.Quality);
        }

        private void ReceivePacketFullScreen(PacketFullScreen packet)
        {
            Task.Run(() =>
            {
                using var source = packet.CompressType != 2 ? ImageCompress.ArrayToBitmap(packet.Data) : WebP.Decode(packet.Data);
                client.Source.Set(ImageCompress.ToPixelArrray(source!), source!.Width, source.Height);
                //client.Window.Dispatcher.Invoke(() =>
                //{
                //    client.Bitmap = new WriteableBitmap(source.Width, source.Height, 96, 96, ImageCompress.ToWpfPixelFormat(source.PixelFormat), null);

                //    var bitmapData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);
                //    client.Bitmap.WritePixels(new Int32Rect(0, 0, source.Width, source.Height), bitmapData.Scan0, bitmapData.Height * bitmapData.Stride, bitmapData.Stride);
                //    source.UnlockBits(bitmapData);

                //    client.Window.Screen.BeginInit();
                //    client.Window.Screen.Source = client.Bitmap;
                //    client.Window.Screen.EndInit();
                //});
            });
        }
    }
}
