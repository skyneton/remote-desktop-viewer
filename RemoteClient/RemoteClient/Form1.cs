using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace RemoteClient
{
    public partial class Form1 : Form
    {
        private static Form2 form;
        private static byte[] buf;
        private static Boolean UserLogin = false;

        public Form1()
        {
            InitializeComponent();
        }

        #region [Event]
        private void Form1_Load(object sender, EventArgs e)
        {
            ControlSetting(false);
        }

        private void btnConnect_click(object sender, EventArgs e)
        {
            String ip;
            int port = 33062;
            if (txtIp.Text.Contains(":"))
            {
                ip = txtIp.Text.Split(':')[0];
                port = int.Parse(txtIp.Text.Split(':')[1]);
            }
            else
                ip = txtIp.Text;

            startClient(ip, port);
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            stopClient();
        }

        private void txtIP_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && (e.KeyChar != '.'))
            {
                e.Handled = true;
                if (!txtIp.Text.Contains(":") && (e.KeyChar == ':'))
                    e.Handled = false;
            }
            else if (txtIp.Text.Contains(":") && (e.KeyChar == '.'))
                e.Handled = true;
        }


        private void btnDisConnect_Click(object sender, EventArgs e)
        {
            stopClient();
        }
        #endregion [~Event]

        #region [Socket]
        private static Socket client;

        private void startClient(String ip, int port)
        {
            try {
                buf = null;
                form = new Form2();
                form.form = this;

                client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                client.Connect(IPAddress.Parse(ip), port);

                byte[] buffer = Encoding.UTF8.GetBytes(SHA256Hash(txtId.Text) + "+" + SHA256Hash(txtPw.Text));

                client.Send(buffer);

                ControlSetting(true);

                SocketAsyncEventArgs recieveAsync = new SocketAsyncEventArgs();
                recieveAsync.Completed += new EventHandler<SocketAsyncEventArgs>(Client_Connected);
                recieveAsync.SetBuffer(new byte[4], 0, 4);
                recieveAsync.UserToken = client;
                client.ReceiveAsync(recieveAsync);

            } catch(Exception ex) { MessageBox.Show(ex.Message); }
        }

        public void stopClient()
        {
            try {
                UserLogin = false;

                client.Close();

                //form.Stop();

                this.Invoke(new Action(delegate () { ControlSetting(false); form.Close(); }));
            } catch { }
        }

        private void Client_Connected(object sender, SocketAsyncEventArgs e)
        {
            try
            {
                Socket server = (Socket) sender;
                if (!UserLogin)
                {
                    int length = BitConverter.ToInt32(e.Buffer, 0);
                    byte[] data = new byte[length];
                    server.Receive(data, length, SocketFlags.None);

                    String msg = Encoding.UTF8.GetString(data);

                    MessageBox.Show(msg);

                    if(msg.Contains("연결되었습니다."))
                    {
                        UserLogin = true;
                        this.Invoke(new Action(delegate () { form.Show(); }));
                    } else
                        stopClient();
                } else {
                    int length = BitConverter.ToInt32(e.Buffer, 0);
                    //buf = new Byte[length];
                    //server.Receive(buf, length, SocketFlags.None);
                    int readedBlockSize = 0;
                    int readedBlock = 0;

                    MemoryStream ms = new MemoryStream();

                    while(readedBlockSize < length)
                    {
                        buf = new Byte[length];
                        readedBlock = server.Receive(buf, length, SocketFlags.None);

                        readedBlockSize += readedBlock;
                        ms.Write(buf, 0, readedBlock);
                        ms.Flush();
                    }

                    byte[] data = ms.ToArray();
                    ms.Dispose();

                    form.drawImage(byteArrayToImage(Decrypt(buf)));
                    //form.drawImage(byteArrayToImage(Decrypt(Encoding.UTF8.GetString(data))));
                }


            } catch { }

            try
            {
                client.ReceiveAsync(e);
            }
            catch { }
        }

        public void Send(String msg)
        {
            try
            {
                byte[] buf = Encoding.UTF8.GetBytes(msg);
                client.Send(BitConverter.GetBytes(buf.Length));
                client.Send(buf, buf.Length, SocketFlags.None);
            }
            catch
            {
                MessageBox.Show("서버가 종료되었거나 연결에 실패하였습니다.");
                stopClient();
            }
        }
        #endregion [~Socket]

        private void ControlSetting(Boolean aServerStatus)
        {
            this.btnDisConnect.Enabled = aServerStatus;
            this.btnConnect.Enabled = !aServerStatus;
            this.txtIp.Enabled = !aServerStatus;
            this.txtId.Enabled = !aServerStatus;
            this.txtPw.Enabled = !aServerStatus;
        }

        private String SHA256Hash(String data)
        {
            SHA256 sha = new SHA256Managed();
            byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(data));
            StringBuilder stringBuilder = new StringBuilder();
            foreach (byte b in hash)
                stringBuilder.AppendFormat("{0:x2}", b);

            return stringBuilder.ToString();
        }
        //string key { get; set; } = "A!9HHhi%XjjYY4YP2@Nob009X";
        private byte[] key = { 0x13, 0x31, 0x21, 0x11, 0x33, 0x14, 0x12, 0x22, 0x06, 0x09, 0x19, 0x02, 0x32 };

        private byte[] Decrypt(byte[] buf/*String cipher*/)
        {
            byte[] result = new byte[buf.Length];
            for (int i = 0; i < buf.Length; i++)
                result[i] = (byte)(buf[i] ^ key[i % key.Length]);

            return result;
            /*MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
            TripleDESCryptoServiceProvider tdes = new TripleDESCryptoServiceProvider();
            
            tdes.Key = md5.ComputeHash(UTF8Encoding.UTF8.GetBytes(key));
            tdes.Mode = CipherMode.ECB;
            tdes.Padding = PaddingMode.PKCS7;

            ICryptoTransform transform = tdes.CreateDecryptor();
            
            byte[] cipherBytes = Convert.FromBase64String(cipher);

            tdes.Dispose();
            md5.Dispose();
            return transform.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);*/
        }

        private Image byteArrayToImage(Byte[] byteArray)
        {
            MemoryStream ms = new MemoryStream(byteArray);
            Image data = Image.FromStream(ms);
            ms.Close();

            return data;
            /*ImageConverter ic = new ImageConverter();

            return (Image) ic.ConvertFrom(byteArray);*/
        }

        /*private byte[] Decompress(Byte[] buffer)
        {
            MemoryStream result = new MemoryStream();
            MemoryStream ms = new MemoryStream(buffer);
            DeflateStream ds = new DeflateStream(ms, CompressionMode.Decompress);
            ds.CopyTo(result);
            ds.Close();

            byte[] re = result.ToArray();
            result.Close();
            ms.Close();

            return re;
        }*/
    }
}
