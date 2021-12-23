using System.Drawing;

namespace RemoteDesktopViewer
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

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
            this.serverOnOff = new RemoteDesktopViewer.CustomControls.ToggleButton();
            this.serverLayout = new RemoteDesktopViewer.CustomControls.LayoutBox();
            this.serverPassword = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.serverPort = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.serverControl = new RemoteDesktopViewer.CustomControls.ToggleButton();
            this.layoutBox1 = new RemoteDesktopViewer.CustomControls.LayoutBox();
            this.serverConnect = new System.Windows.Forms.Button();
            this.connectPassword = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.connectIpAddress = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.serverLayout.SuspendLayout();
            this.layoutBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // serverOnOff
            // 
            this.serverOnOff.BackColor = System.Drawing.Color.Transparent;
            this.serverOnOff.Font = new System.Drawing.Font("Tahoma", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte) (0)));
            this.serverOnOff.ForeColor = System.Drawing.Color.Black;
            this.serverOnOff.Location = new System.Drawing.Point(11, 20);
            this.serverOnOff.MaximumSize = new System.Drawing.Size(200, 40);
            this.serverOnOff.Name = "serverOnOff";
            this.serverOnOff.OffBackColor = System.Drawing.Color.Gray;
            this.serverOnOff.OffFont = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte) (0)));
            this.serverOnOff.OffForeColor = System.Drawing.Color.White;
            this.serverOnOff.OffText = "Server Closed";
            this.serverOnOff.OffToggleColor = System.Drawing.Color.Gainsboro;
            this.serverOnOff.OnBackColor = System.Drawing.Color.LightSteelBlue;
            this.serverOnOff.OnFont = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold);
            this.serverOnOff.OnForeColor = System.Drawing.Color.Black;
            this.serverOnOff.OnText = "Server Opened";
            this.serverOnOff.OnToggleColor = System.Drawing.Color.DodgerBlue;
            this.serverOnOff.Size = new System.Drawing.Size(200, 40);
            this.serverOnOff.TabIndex = 1;
            this.serverOnOff.Text = "Remote Server ON/OFF";
            this.serverOnOff.UseVisualStyleBackColor = false;
            this.serverOnOff.CheckedChanged += new System.EventHandler(this.serverOnOff_CheckedChanged);
            // 
            // serverLayout
            // 
            this.serverLayout.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.serverLayout.BorderColor = System.Drawing.Color.Black;
            this.serverLayout.BorderStyle = System.Windows.Forms.ButtonBorderStyle.Solid;
            this.serverLayout.Controls.Add(this.serverPassword);
            this.serverLayout.Controls.Add(this.label2);
            this.serverLayout.Controls.Add(this.serverPort);
            this.serverLayout.Controls.Add(this.label1);
            this.serverLayout.Controls.Add(this.serverControl);
            this.serverLayout.Controls.Add(this.serverOnOff);
            this.serverLayout.Dock = System.Windows.Forms.DockStyle.Right;
            this.serverLayout.Location = new System.Drawing.Point(440, 0);
            this.serverLayout.Margin = new System.Windows.Forms.Padding(5);
            this.serverLayout.Name = "serverLayout";
            this.serverLayout.Size = new System.Drawing.Size(360, 450);
            this.serverLayout.TabIndex = 2;
            this.serverLayout.TabStop = false;
            this.serverLayout.Text = "layoutBox1";
            // 
            // serverPassword
            // 
            this.serverPassword.Font = new System.Drawing.Font("Tahoma", 12F);
            this.serverPassword.Location = new System.Drawing.Point(20, 286);
            this.serverPassword.MaxLength = 27;
            this.serverPassword.Name = "serverPassword";
            this.serverPassword.PasswordChar = '*';
            this.serverPassword.Size = new System.Drawing.Size(269, 27);
            this.serverPassword.TabIndex = 6;
            this.serverPassword.TextChanged += new System.EventHandler(this.serverPassword_TextChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte) (0)));
            this.label2.Location = new System.Drawing.Point(20, 271);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(63, 16);
            this.label2.TabIndex = 5;
            this.label2.Text = "Password";
            // 
            // serverPort
            // 
            this.serverPort.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte) (0)));
            this.serverPort.ImeMode = System.Windows.Forms.ImeMode.Disable;
            this.serverPort.Location = new System.Drawing.Point(20, 210);
            this.serverPort.MaxLength = 5;
            this.serverPort.Name = "serverPort";
            this.serverPort.Size = new System.Drawing.Size(140, 27);
            this.serverPort.TabIndex = 4;
            this.serverPort.Text = "33062";
            this.serverPort.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.serverPort_KeyPress);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Tahoma", 9.75F);
            this.label1.Location = new System.Drawing.Point(20, 195);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(31, 16);
            this.label1.TabIndex = 3;
            this.label1.Text = "Port";
            // 
            // serverControl
            // 
            this.serverControl.Font = new System.Drawing.Font("Tahoma", 9.75F);
            this.serverControl.Location = new System.Drawing.Point(20, 78);
            this.serverControl.Name = "serverControl";
            this.serverControl.OffBackColor = System.Drawing.Color.Gray;
            this.serverControl.OffFont = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte) (0)));
            this.serverControl.OffForeColor = System.Drawing.Color.White;
            this.serverControl.OffText = "OFF";
            this.serverControl.OffToggleColor = System.Drawing.Color.Gainsboro;
            this.serverControl.OnBackColor = System.Drawing.Color.LightSteelBlue;
            this.serverControl.OnFont = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte) (0)));
            this.serverControl.OnForeColor = System.Drawing.Color.Black;
            this.serverControl.OnText = "ON";
            this.serverControl.OnToggleColor = System.Drawing.Color.DodgerBlue;
            this.serverControl.Size = new System.Drawing.Size(63, 34);
            this.serverControl.TabIndex = 2;
            this.serverControl.Text = "Control";
            this.serverControl.UseVisualStyleBackColor = true;
            this.serverControl.CheckedChanged += new System.EventHandler(this.serverControl_CheckedChanged);
            // 
            // layoutBox1
            // 
            this.layoutBox1.BorderColor = System.Drawing.Color.Orange;
            this.layoutBox1.BorderStyle = System.Windows.Forms.ButtonBorderStyle.Solid;
            this.layoutBox1.Controls.Add(this.serverConnect);
            this.layoutBox1.Controls.Add(this.connectPassword);
            this.layoutBox1.Controls.Add(this.label4);
            this.layoutBox1.Controls.Add(this.connectIpAddress);
            this.layoutBox1.Controls.Add(this.label3);
            this.layoutBox1.Dock = System.Windows.Forms.DockStyle.Left;
            this.layoutBox1.Location = new System.Drawing.Point(0, 0);
            this.layoutBox1.Margin = new System.Windows.Forms.Padding(5);
            this.layoutBox1.Name = "layoutBox1";
            this.layoutBox1.Size = new System.Drawing.Size(435, 450);
            this.layoutBox1.TabIndex = 7;
            this.layoutBox1.TabStop = false;
            this.layoutBox1.Text = "layoutBox1";
            // 
            // serverConnect
            // 
            this.serverConnect.Location = new System.Drawing.Point(114, 321);
            this.serverConnect.Name = "serverConnect";
            this.serverConnect.Size = new System.Drawing.Size(189, 52);
            this.serverConnect.TabIndex = 4;
            this.serverConnect.Text = "Connect";
            this.serverConnect.UseVisualStyleBackColor = true;
            this.serverConnect.Click += new System.EventHandler(this.serverConnect_Click);
            // 
            // connectPassword
            // 
            this.connectPassword.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.connectPassword.Font = new System.Drawing.Font("Tahoma", 12F);
            this.connectPassword.Location = new System.Drawing.Point(51, 205);
            this.connectPassword.Name = "connectPassword";
            this.connectPassword.PasswordChar = '*';
            this.connectPassword.Size = new System.Drawing.Size(320, 27);
            this.connectPassword.TabIndex = 3;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Tahoma", 9.75F);
            this.label4.Location = new System.Drawing.Point(51, 186);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(63, 16);
            this.label4.TabIndex = 2;
            this.label4.Text = "Password";
            // 
            // connectIpAddress
            // 
            this.connectIpAddress.Font = new System.Drawing.Font("Tahoma", 12F);
            this.connectIpAddress.Location = new System.Drawing.Point(51, 117);
            this.connectIpAddress.MaxLength = 50;
            this.connectIpAddress.Name = "connectIpAddress";
            this.connectIpAddress.Size = new System.Drawing.Size(320, 27);
            this.connectIpAddress.TabIndex = 1;
            this.connectIpAddress.Text = "127.0.0.1";
            this.connectIpAddress.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.connectIpAddress_KeyPress);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Tahoma", 9.75F);
            this.label3.Location = new System.Drawing.Point(51, 98);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(69, 16);
            this.label3.TabIndex = 0;
            this.label3.Text = "IP Address";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.layoutBox1);
            this.Controls.Add(this.serverLayout);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.Text = "Remote Desktop Viewer";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.RemoteDesktopViewer_Closing);
            this.serverLayout.ResumeLayout(false);
            this.serverLayout.PerformLayout();
            this.layoutBox1.ResumeLayout(false);
            this.layoutBox1.PerformLayout();
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.TextBox connectIpAddress;

        private System.Windows.Forms.TextBox connectPassword;

        private System.Windows.Forms.Button serverConnect;

        private System.Windows.Forms.Label label4;

        private System.Windows.Forms.Label label3;

        private System.Windows.Forms.TextBox serverPassword;

        private System.Windows.Forms.Label label2;

        private System.Windows.Forms.TextBox serverPort;

        private System.Windows.Forms.Label label1;

        private RemoteDesktopViewer.CustomControls.ToggleButton serverControl;

        private RemoteDesktopViewer.CustomControls.LayoutBox serverLayout;

        private RemoteDesktopViewer.CustomControls.LayoutBox layoutBox1;

        private RemoteDesktopViewer.CustomControls.ToggleButton serverOnOff;

        #endregion
    }
}