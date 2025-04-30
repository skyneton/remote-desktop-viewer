using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using NetworkLibrary.Networks;
using NetworkLibrary.Utils;
using RemoteDeskopControlPannel.ImageProcessing;
using RemoteDeskopControlPannel.Network.Handler;
using RemoteDeskopControlPannel.Network.Packet;
using RemoteDeskopControlPannel.Utils;

namespace RemoteDeskopControlPannel.Network
{
    internal class Client
    {
        internal readonly ClientWindow Window = new();
        private readonly NetworkClient client;
        private readonly KeyboardHook keyboardHook = new();
        private readonly HashSet<VirtualKey> pressedKeys = [];
        public string Password { get; private set; }
        public ImageProcess ScreenProcess { get; private set; } = ImageProcess.Byte3RGB;
        public int Cursor { get; private set; } = -1;
        internal WriteableBitmap? Bitmap = null;
        private SoundTrack? soundTrack = null;
        public Client(string address, string password)
        {
            Password = password;
            var host = address;
            var port = Server.DefaultPort;
            var column = address.LastIndexOf(':');
            if (column != -1)
            {
                host = address[..column];
                if (!int.TryParse(address.AsSpan(column + 1), out port))
                    port = Server.DefaultPort;
            }
            client = new NetworkClient(Server.Factory, host, port, timeout: 10000);
            client.Network.PacketHandler = new ClientPacketHandler(this);
            client.Network.Compression.CompressionEnabled = true;
            client.OnConnected += OnConnect;
            client.OnConnectFailed += (sender, e) =>
            {
                MessageBox.Show("Can't connect to the server.");
            };
            client.OnDisconnect += (sender, e) =>
            {
                if (Window.IsOpened)
                    MessageBox.Show("Server disconnected. Server down or login failed.");
            };
            client.Connect();

            Window.Loaded += (sender, e) =>
            {
                Window.IsOpened = true;
                keyboardHook.SetupHook(new WindowInteropHelper(Window).EnsureHandle());
                keyboardHook.AddCallback(KeyHookCallback);
            };
            Window.Closed += (sender, e) =>
            {
                Window.IsOpened = false;
                keyboardHook.ShutdownHook();
                Close();
            };
            Window.Deactivated += (sender, e) =>
            {
                foreach (var key in pressedKeys)
                {
                    client.SendPacket(new PacketKeyboardInput(key.VK, key.SystemKey ? 0x3 : 0x2));
                }
                pressedKeys.Clear();
            };
            Window.Canvas.MouseMove += (sender, e) =>
            {
                var pos = e.GetPosition(Window.Canvas);
                var x = (int)Math.Round(pos.X / Window.Canvas.RenderSize.Width * Bitmap?.PixelWidth ?? 0);
                var y = (int)Math.Round(pos.Y / Window.Canvas.RenderSize.Height * Bitmap?.PixelHeight ?? 0);
                client.SendPacket(new PacketMousePosition(x, y));
                if (Cursor != -1) NativeKeyboardMouse.SetCursor(Cursor);
            };
            Window.Canvas.MouseWheel += (sender, e) =>
            {
                client.SendPacket(new PacketMouseEvent((int)NativeKeyboardMouse.MouseType.Wheel, e.Delta));
                e.Handled = true;
            };
            Window.Canvas.MouseDown += (sender, e) =>
            {
                switch (e.ChangedButton)
                {
                    case MouseButton.Left:
                        client.SendPacket(new PacketMouseEvent((int)NativeKeyboardMouse.MouseType.LeftButtonDown, 0));
                        break;
                    case MouseButton.Middle:
                        client.SendPacket(new PacketMouseEvent((int)NativeKeyboardMouse.MouseType.MiddleDown, 0));
                        break;
                    case MouseButton.Right:
                        client.SendPacket(new PacketMouseEvent((int)NativeKeyboardMouse.MouseType.RightButtonDown, 0));
                        break;
                    case MouseButton.XButton1:
                        client.SendPacket(new PacketMouseEvent((int)NativeKeyboardMouse.MouseType.XButtonDown, 1));
                        break;
                    case MouseButton.XButton2:
                        client.SendPacket(new PacketMouseEvent((int)NativeKeyboardMouse.MouseType.XButtonDown, 2));
                        break;
                }
            };
            Window.Canvas.MouseUp += (sender, e) =>
            {
                switch (e.ChangedButton)
                {
                    case MouseButton.Left:
                        client.SendPacket(new PacketMouseEvent((int)NativeKeyboardMouse.MouseType.LeftButtonUp, 0));
                        break;
                    case MouseButton.Middle:
                        client.SendPacket(new PacketMouseEvent((int)NativeKeyboardMouse.MouseType.MiddleUp, 0));
                        break;
                    case MouseButton.Right:
                        client.SendPacket(new PacketMouseEvent((int)NativeKeyboardMouse.MouseType.RightButtonUp, 0));
                        break;
                    case MouseButton.XButton1:
                        client.SendPacket(new PacketMouseEvent((int)NativeKeyboardMouse.MouseType.XButtonUp, 1));
                        break;
                    case MouseButton.XButton2:
                        client.SendPacket(new PacketMouseEvent((int)NativeKeyboardMouse.MouseType.XButtonUp, 2));
                        break;
                }
            };
            Window.Title = address;
            Window.Show();
        }

        public void Close()
        {
            client.Close();
            Window.Close();
        }

        internal void StartSoundTrack(int sampleRate, int bitsPerSample, int channels)
        {
            soundTrack = new SoundTrack(sampleRate, bitsPerSample, channels);
        }

        internal void ReceiveSoundChunk(byte[] buffer)
        {
            soundTrack?.AddChunk(buffer);
        }

        internal void CursorUpdate(int cursor)
        {
            Cursor = cursor;
            NativeKeyboardMouse.SetCursor(cursor);
        }

        private bool KeyHookCallback(int code, int wParam, int vk)
        {
            if (!Window.IsActive) return false;
            var key = new VirtualKey(vk, wParam == KeyboardHook.SystemKeyDown || wParam == KeyboardHook.SystemKeyUp);
            int flag;
            if (wParam == KeyboardHook.KeyDown || wParam == KeyboardHook.SystemKeyDown)
            {
                flag = (int)NativeKeyboardMouse.KeyType.KeyDown;
                pressedKeys.Add(key);
            }
            else
            {
                flag = (int)NativeKeyboardMouse.KeyType.KeyUp;
                pressedKeys.Remove(key);
            }
            if (key.SystemKey)
                flag |= (int)NativeKeyboardMouse.KeyType.System;

            client.SendPacket(new PacketKeyboardInput(vk, flag));
            return true;
        }

        private void OnConnect(object? sender, NetworkEventArgs e)
        {
            e.network?.SendPacket(new PacketLogin(Password));
            e.network?.SendPacket(new PacketProxyType(false));
            var res = DisplaySettings.GetResolution();
            e.network?.SendPacket(new PacketScreenSize(res.Width, res.Height));
        }

        internal void UpdateScreenProcessor(QualityMode quality)
        {
            ScreenProcess = quality switch
            {
                QualityMode.Byte2RGB => ImageProcess.Byte2RGB,
                _ => ImageProcess.Byte3RGB,
            };
        }
    }
}
