using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using RemoteDeskopControlPannel.ImageProcessing;
using RemoteDeskopControlPannel.Network;
using RemoteDeskopControlPannel.Network.Packet;
using RemoteDeskopControlPannel.Utils;

namespace RemoteDeskopControlPannel.Capture
{
    internal class ScreenCapture
    {
        private const int ImageThreading = 32767; // maximum gdi+ jpeg width
        private static int beforeWidth, beforeHeight;
        private static byte[] pixels = [];
        private static int lastCursorInfo = 65539;
        public static void Run(Server server)
        {
            if (!server.IsAvailable) return;
            //using var screen = GetCompressedScreen(server.ScreenProcess.Format);
            using var screen = DisplaySettings.Screenshot(server.ScreenProcess.Format);

            var bitmapData = screen.LockBits(new Rectangle(0, 0, screen.Width, screen.Height), ImageLockMode.ReadOnly, server.ScreenProcess.Format);
            var force = screen.Width != beforeWidth || screen.Height != beforeHeight;
            if (force)
                pixels = new byte[screen.Width * screen.Height * server.ScreenProcess.PixelBytes];

            beforeWidth = screen.Width;
            beforeHeight = screen.Height;

            var list = new List<Task>();
            for (int i = 0, end = screen.Width * screen.Height; i < end; i += ImageThreading)
            {
                var start = i;
                list.Add(Task.Run(() =>
                {
                    var packet = server.ScreenProcess.Process(bitmapData, pixels, start, Math.Min(end, start + ImageThreading), force);
                    if (packet == null) return;
                    server.Broadcast(packet);
                }));
            }
            Task.WaitAll(list);
            screen.UnlockBits(bitmapData);

            if (force)
            {
                Debug.WriteLine($"{screen.Width} {screen.Height}");
                server.Broadcast(new PacketFullScreen(ImageCompress.PixelToImage(screen, ImageFormat.Jpeg)));
            }
            else if (!server.AcceptedClients.IsEmpty)
            {
                var packet = new PacketFullScreen(ImageCompress.PixelToImage(screen, ImageFormat.Jpeg));
                while (!server.AcceptedClients.IsEmpty)
                {
                    if (!server.AcceptedClients.TryDequeue(out var client)) continue;
                    client.SendPacket(packet);
                }
            }

            var cursorInfo = NativeKeyboardMouse.GetCursorInfo(out var success);
            if (success && lastCursorInfo != cursorInfo.hCursor)
            {
                lastCursorInfo = (int)cursorInfo.hCursor;
                server.Broadcast(new PacketCursorType(lastCursorInfo));
            }
        }

        private static Bitmap GetCompressedScreen(PixelFormat format)
        {
            using var screen = DisplaySettings.Screenshot(format);
            return ImageCompress.ArrayToBitmap(ImageCompress.PixelToImage(screen, ImageFormat.Jpeg));
        }
    }
}
