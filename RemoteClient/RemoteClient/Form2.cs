using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace RemoteClient
{
    public partial class Form2 : Form
    {
        //private Image Img;
        private Graphics graphics;
        public Form1 form;

        Boolean FormEntered = true;

        //Dictionary<String, String> lastImageMD5 = new Dictionary<String, String>();

        public Form2()
        {
            InitializeComponent();
            graphics = this.CreateGraphics();
        }

        public void drawImage(Image image)
        {
            /*try
            {
                Bitmap newImage = new Bitmap(image, new System.Drawing.Size(this.ClientSize.Width, this.ClientSize.Height));
                Mat img = OpenCvSharp.Extensions.BitmapConverter.ToMat(newImage);
                pictureBoxIpl1.ImageIpl = img;
                newImage.Dispose();
                img.Dispose();
                image.Dispose();
            }
            catch { }*/

            Bitmap newImage = new Bitmap(image, new Size(this.ClientSize.Width, this.ClientSize.Height));
            graphics.DrawImage(newImage, 0, 0);
            newImage.Dispose();
            image.Dispose();


            /*if (Img == null)
            {
                Img = image;
                //graphics.DrawImage(newImage, 0, 0);
                return;
            }
            Bitmap newImage = new Bitmap(image, new Size(this.ClientSize.Width, this.ClientSize.Height));

            int ImgSplitX = newImage.Width / 4;
            int ImgSplitY = newImage.Height / 4;

            for (int y = 0; y < newImage.Height / ImgSplitY; y++)
            {

                for (int x = 0; x < newImage.Width / ImgSplitX; x++)
                {

                    int marginX = (x * ImgSplitX + ImgSplitX > newImage.Width) ? newImage.Width - x * ImgSplitX : ImgSplitX;
                    int marginY = (y * ImgSplitY + ImgSplitY > newImage.Height) ? newImage.Height - y * ImgSplitY : ImgSplitY;

                    Bitmap SplitNew = newImage.Clone(new Rectangle(x * ImgSplitX, y * ImgSplitY, marginX, marginY), PixelFormat.DontCare);

                    String md5 = MD5Hash(imageTobyteArray(SplitNew));

                    if (lastImageMD5.ContainsKey(x + "," + y))
                    {
                        if (!lastImageMD5.Equals(md5))
                        {n
                            graphics.DrawImage(SplitNew, x * ImgSplitX, y * ImgSplitY);
                            lastImageMD5[x + "," + y] = md5;
                        }
                    }else {
                        graphics.DrawImage(SplitNew, x * ImgSplitX, y * ImgSplitY);
                        lastImageMD5.Add(x + "," + y, md5);
                    }

                    SplitNew.Dispose();
                }
            }

            newImage.Dispose();

            Img = image;
            image.Dispose();*/
        }
        /*private byte[] imageTobyteArray(Image image)
        {
            ImageConverter converter = new ImageConverter();

            return (byte[])converter.ConvertTo(image, typeof(byte[]));
        }
        private String MD5Hash(byte[] byt)
        {
            MD5 md5 = MD5.Create();
            byte[] data = md5.ComputeHash(byt);
            StringBuilder stringBuilder = new StringBuilder();
            foreach (byte b in data)
                stringBuilder.AppendFormat(b.ToString("x2"));

            md5.Dispose();
            return stringBuilder.ToString();
        }*/

        private void Form2_Closing(object sender, FormClosingEventArgs e) { }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (FormEntered)
            {
                form.Send("KeyDown+" + e.KeyValue);
            }
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (FormEntered)
            {
                form.Send("KeyUp+" + e.KeyValue);
            }
        }

        private void Window_MouseDown(object sender, MouseEventArgs e)
        {
            if (FormEntered)
            {
                form.Send("MouseDown+" + e.Button.ToString());
            }
        }

        private void Window_MouseUp(object sender, MouseEventArgs e)
        {
            if (FormEntered)
            {
                form.Send("MouseUp+" + e.Button.ToString());
            }
        }

        private void Form_Resize(object sender, EventArgs e)
        {
            //lastImageMD5.Clear();
            graphics = this.CreateGraphics();
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (FormEntered)
            {
                form.Send("MouseMove+" + (e.Location.X * 1.0) / (this.ClientSize.Width * 1.0) + "," + (e.Location.Y * 1.0) / (this.ClientSize.Height * 1.0));
            }
        }

        private void Window_MouseLeave(object sender, EventArgs e)
        {
            FormEntered = false;
        }

        private void Window_MouseEnter(object sender, EventArgs e)
        {
            FormEntered = true;
        }

        private void Window_MouseWheel(object sender, MouseEventArgs e)
        {
            if (FormEntered)
                form.Send("MouseW+" + e.Delta);
        }
    }
}
