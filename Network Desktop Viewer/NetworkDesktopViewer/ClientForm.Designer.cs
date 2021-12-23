using System.ComponentModel;

namespace RemoteDesktopViewer
{
    partial class ClientForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }

            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // ClientForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Name = "ClientForm";
            this.Text = "ClientForm";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.ClientForm_FormClosed);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.ClientForm_KeyDown);
            this.KeyUp += new System.Windows.Forms.KeyEventHandler(this.ClientForm_KeyUp);
            this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.ClientForm_MouseDown);
            this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.ClientForm_MouseMove);
            this.MouseUp += new System.Windows.Forms.MouseEventHandler(this.ClientForm_MouseUp);
            this.MouseWheel += new System.Windows.Forms.MouseEventHandler(this.ClientForm_MouseWheel);
            this.Resize += new System.EventHandler(this.ClientForm_Resize);
            this.ResumeLayout(false);
        }

        #endregion
    }
}