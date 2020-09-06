using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace RemoteServer
{
    public partial class Form1 : Form
    {
        [DllImport("user32.dll")]
        static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, int dwExtraInfo);
        [DllImport("user32.dll")]
        static extern void keybd_event(uint vk, uint scan, uint flags, uint extraInfo);
        [DllImport("user32.dll")]
        static extern uint MapVirtualKey(int wCode, int wMapType);

        const uint LBUTTON_UP = 0x04, LBUTTON_DOWN = 0x02;
        const uint RBUTTON_UP = 0x10, RBUTTON_DOWN = 0x08;
        const uint WHEEL_UP = 0x0040, WHEEL_DOWN = 0x0020, WHEEL = 0x0800;

        //private static String ImgbyteLast;
        private static byte[] buf;
        private static Thread thread;

        public Form1()
        {
            InitializeComponent();
        }

        #region [Event]
        private void Form1_Load(object sender, EventArgs e)
        {
            ControlSetting(IsStarted);
        }

        private void btnOpen_Click(object sender, EventArgs e)
        {
            this.id = SHA256Hash(txtId.Text);
            this.pw = SHA256Hash(txtPw.Text);
            this.ServerPort = int.Parse(txtPort.Text);

            if (!IsStarted)
                this.startServer();
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            if (IsStarted)
                this.stopServer();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (IsStarted)
                this.stopServer();
        }


        private void txtPort_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
                e.Handled = true;
        }

        private void txtLog_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = true;
        }
        #endregion [~Event]

        #region [Socket]
        public static bool IsStarted = false;
        public int ServerPort = 33062;

        private static Dictionary<String, Socket> client;

        private Socket server;

        String id = null;
        String pw = null;

        public void startServer()
        {
            try
            {
                thread = new Thread(TimerImg);
                client = new Dictionary<String, Socket>();
                //ImgbyteLast = null;

                server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                server.Bind(new IPEndPoint(IPAddress.Any, ServerPort)); //포트 설정

                server.Listen(20);

                IsStarted = true;

                SocketAsyncEventArgs socketAsync = new SocketAsyncEventArgs();
                socketAsync.Completed += new EventHandler<SocketAsyncEventArgs>(Server_Connected);
                server.AcceptAsync(socketAsync);

                TextSet(this.txtLog, DateTime.Now.ToString() + " : 서버 오픈 이벤트");

                thread.Start();

                ControlSetting(IsStarted);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public void stopServer()
        {
            try
            {
                foreach (KeyValuePair<String, Socket> items in client)
                {
                    TextSet(this.txtLog, DateTime.Now.ToString() + " : 종료됨 [" + items.Value.RemoteEndPoint.ToString() + "]");
                    items.Value.Close();
                }

                server.Close();
                if(thread.IsAlive) thread.Abort();
                IsStarted = false;

                TextSet(this.txtLog, DateTime.Now.ToString() + " : 서버 중지 이벤트");

                ControlSetting(IsStarted);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void Server_Connected(object sender, SocketAsyncEventArgs e)
        {
            try
            {
                Socket cl = e.AcceptSocket;
                byte[] idpw = new byte[200];
                cl.Receive(idpw);

                String a = SHA256Hash((id + "+" + pw));
                String b = SHA256Hash(Encoding.UTF8.GetString(idpw).Replace("\0", "").Replace("\r", "").Replace("\n", ""));

                if (b.Equals(a))
                {

                    byte[] msg = Encoding.UTF8.GetBytes("연결되었습니다.");
                    cl.Send(BitConverter.GetBytes(msg.Length));
                    cl.Send(msg, msg.Length, SocketFlags.None);

                    SocketAsyncEventArgs recieveAsync = new SocketAsyncEventArgs();
                    recieveAsync.SetBuffer(new byte[4], 0, 4);
                    recieveAsync.UserToken = cl;
                    recieveAsync.Completed += new EventHandler<SocketAsyncEventArgs>(Server_Recieve);

                    cl.ReceiveAsync(recieveAsync);

                    //연결됨
                    this.Invoke(new Action(delegate () { TextSet(this.txtLog, DateTime.Now.ToString() + " : 연결됨 [" + cl.RemoteEndPoint.ToString() + "]"); }));

                    cl.Send(BitConverter.GetBytes(buf.Length));
                    cl.Send(buf, buf.Length, SocketFlags.None);

                    client.Add(cl.RemoteEndPoint.ToString(), cl);
                }
                else
                {
                    byte[] error = Encoding.UTF8.GetBytes("아이디 혹은 패스워드가 일치하지 않습니다.");

                    cl.Send(BitConverter.GetBytes(error.Length));
                    cl.Send(error, error.Length, SocketFlags.None);

                    cl.Shutdown(SocketShutdown.Both);
                }

                e.AcceptSocket = null;
                server.AcceptAsync(e);
            }
            catch { }
        }

        private void Server_Recieve(object sender, SocketAsyncEventArgs e)
        {
            Socket cl = (Socket) sender;
            try
            {
                if(cl.Connected && e.BytesTransferred > 0 && this.ckbControl.Checked)
                {
                    int length = BitConverter.ToInt32(e.Buffer, 0);
                    byte[] data = new byte[length];
                    cl.Receive(data, length, SocketFlags.None);
                    String msg = Encoding.UTF8.GetString(data).Replace("\0", "").Replace("\r", "").Replace("\n", "");

                    if (msg.Contains("MouseMove"))
                    {
                        double x = double.Parse(msg.Split('+')[1].Split(',')[0]);
                        double y = double.Parse(msg.Split('+')[1].Split(',')[1]);

                        Rectangle ScreenSize = Screen.PrimaryScreen.Bounds;
                        int MouseX = (int) (ScreenSize.Width * x);
                        int MouseY = (int) (ScreenSize.Height * y);

                        Cursor.Position = new Point(MouseX, MouseY);
                    }
                    else if (msg.Contains("MouseDown"))
                    {
                        switch(msg.Split('+')[1])
                        {
                            case "Left":
                                mouse_event(LBUTTON_DOWN, 0, 0, 0, 0);
                                break;
                            case "Right":
                                mouse_event(RBUTTON_DOWN, 0, 0, 0, 0);
                                break;
                            case "Middle":
                                mouse_event(WHEEL_DOWN, 0, 0, 0, 0);
                                break;
                        }
                    }
                    else if (msg.Contains("MouseUp"))
                    {
                        switch (msg.Split('+')[1])
                        {
                            case "Left":
                                mouse_event(LBUTTON_UP, 0, 0, 0, 0);
                                break;
                            case "Right":
                                mouse_event(RBUTTON_UP, 0, 0, 0, 0);
                                break;
                            case "Middle":
                                mouse_event(WHEEL_UP, 0, 0, 0, 0);
                                break;
                        }
                    }
                    else if (msg.Contains("KeyDown"))
                    {
                        int code = int.Parse(msg.Split('+')[1]);
                        try
                        {
                            keybd_event((byte) code, 0, 0, 0);
                        }
                        catch(Exception ex)
                        {
                            MessageBox.Show(ex.Message);
                        }
                    }
                    else if (msg.Contains("KeyUp"))
                    {
                        int code = int.Parse(msg.Split('+')[1]);
                        keybd_event((byte)code, 0, 0x02, 0);
                    }
                    else if (msg.Contains("MouseW"))
                    {
                        int delta = int.Parse(msg.Split('+')[1]);
                        mouse_event(WHEEL, 0, 0, (uint) delta, 0);
                    }
                }

            }
            catch { }
            
            try { cl.ReceiveAsync(e); } catch { }
        }

        #endregion [~Socket]

        private static void TextSet(Control aControl, String aString)
        {
            (aControl as TextBox).AppendText(aString + "\r\n");
        }

        private void ControlSetting(Boolean aServerStatus)
        {
            this.ckbControl.Enabled = !aServerStatus;
            this.txtPort.Enabled = !aServerStatus;
            this.btnOpen.Enabled = !aServerStatus;
            this.txtId.Enabled = !aServerStatus;
            this.txtPw.Enabled = !aServerStatus;
            this.btnClose.Enabled = aServerStatus;
        }

        private String SHA256Hash(String data)
        {
            SHA256 sha = new SHA256Managed();
            byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(data));
            StringBuilder stringBuilder = new StringBuilder();
            foreach (byte b in hash)
                stringBuilder.AppendFormat("{0:x2}", b);
            sha.Dispose();

            return stringBuilder.ToString();
        }

        #region [ImageSend]
        private Bitmap GrabDesktop()
        {
            Rectangle ScreenSize = Screen.PrimaryScreen.Bounds;
            Bitmap ScreenMap = new Bitmap(ScreenSize.Width, ScreenSize.Height, PixelFormat.Format16bppRgb565);
            Graphics gr = Graphics.FromImage(ScreenMap);
            gr.CopyFromScreen(ScreenSize.X, ScreenSize.Y, 0, 0, ScreenSize.Size);

            gr.Dispose();

            return ScreenMap;
        }

        private byte[] imageTobyteArray(Bitmap image)
        {
            MemoryStream ms = new MemoryStream();
            image.Save(ms, ImageFormat.Jpeg);
            byte[] data = ms.ToArray();
            ms.Dispose();
            image.Dispose();

            return data;

            /*ImageConverter converter = new ImageConverter();

            return (byte[])converter.ConvertTo(image, typeof(byte[]));*/
        }

        /*private byte[] Compress(byte[] data)
        {
            MemoryStream ms = new MemoryStream();
            DeflateStream ds = new DeflateStream(ms, CompressionMode.Compress);
            ds.Write(data, 0, data.Length);
            byte[] output = ms.ToArray();
            ds.Dispose();
            ms.Dispose();

            return output;
        }*/

        //string key { get; set; } = "A!9HHhi%XjjYY4YP2@Nob009X";
        private byte[] key = { 0x13, 0x31, 0x21, 0x11, 0x33, 0x14, 0x12, 0x22, 0x06, 0x09, 0x19, 0x02, 0x32 };

        private byte[]/*String*/ Encrypt(byte[] buf) {

            byte[] result = new byte[buf.Length];
            for (int i = 0; i < buf.Length; i++)
                result[i] = (byte)(buf[i] ^ key[i % key.Length]);

            return result;
            /*MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
            TripleDESCryptoServiceProvider tdes = new TripleDESCryptoServiceProvider();
            tdes.Key = md5.ComputeHash(UTF8Encoding.UTF8.GetBytes(key));
            tdes.Mode = CipherMode.ECB;
            tdes.Padding = PaddingMode.PKCS7;

            ICryptoTransform transform = tdes.CreateEncryptor();

            byte[] bytes = transform.TransformFinalBlock(buf, 0, buf.Length);
            md5.Dispose();
            tdes.Dispose();
            return Convert.ToBase64String(bytes, 0, bytes.Length);*/
        }
        /*private String MD5Hash(byte[] byt)
        {
            MD5 md5 = MD5.Create();
            byte[] data = md5.ComputeHash(byt);
            StringBuilder stringBuilder = new StringBuilder();
            foreach (byte b in data)
                stringBuilder.AppendFormat(b.ToString("x2"));

            md5.Dispose();
            return stringBuilder.ToString();
        }*/

        private void SplitImgSend()
        {
            Dictionary<String, Socket> remove = new Dictionary<String, Socket>();

            /*String md = MD5Hash(buf);
            //String md = Encrypt(buf);
            if (ImgbyteLast != null && ImgbyteLast.Equals(md))
                return;

            ImgbyteLast = md;*/

            byte[] data = Encrypt(buf);
            //byte[] data = Encoding.UTF8.GetBytes(md);

            try {
                foreach (KeyValuePair<String, Socket> items in client)
                {
                    try
                    {
                        items.Value.Send(BitConverter.GetBytes(data.Length));
                        items.Value.Send(data, data.Length, SocketFlags.None);
                    }catch(Exception)
                    {
                        if (!remove.ContainsKey(items.Key))
                            remove.Add(items.Key, items.Value);
                    }
                }
            } catch { }

            foreach (KeyValuePair<String, Socket> items in remove)
            {
                try
                {
                    this.Invoke(new Action(delegate () { try { TextSet(this.txtLog, DateTime.Now.ToString() + " : 종료됨 [" + items.Value.RemoteEndPoint.ToString() + "]"); } catch { } }));
                    items.Value.Close();
                    client.Remove(items.Key);
                }
                catch { }
            }
        }

        private void TimerImg()
        {

            while (IsStarted)
            {
                //buf = Compress(imageTobyteArray(GrabDesktop()));
                buf = imageTobyteArray(GrabDesktop());

                SplitImgSend();
            }
        }
        #endregion [~ImageSend]
    }
}
