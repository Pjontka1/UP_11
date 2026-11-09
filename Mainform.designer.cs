#nullable disable
namespace ModemTerminal
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            panelPort = new FlowLayoutPanel();
            labelPort = new Label();
            cbPort = new ComboBox();
            labelBaud = new Label();
            cbBaud = new ComboBox();
            chkRts = new CheckBox();
            btnOpen = new Button();
            lblStatus = new Label();
            panelCmd = new FlowLayoutPanel();
            btnInit = new Button();
            labelNumber = new Label();
            txtPhone = new TextBox();
            btnDial = new Button();
            btnAnswer = new Button();
            btnAuto = new Button();
            btnEsc = new Button();
            btnAto = new Button();
            btnHang = new Button();
            panelFile = new FlowLayoutPanel();
            btnSendFile = new Button();
            btnRecvFile = new Button();
            btnCancel = new Button();
            panelInput = new Panel();
            txtIn = new TextBox();
            txtLog = new TextBox();
            panelPort.SuspendLayout();
            panelCmd.SuspendLayout();
            panelFile.SuspendLayout();
            panelInput.SuspendLayout();
            SuspendLayout();
            // 
            // panelPort
            // 
            panelPort.AutoSize = true;
            panelPort.Controls.Add(labelPort);
            panelPort.Controls.Add(cbPort);
            panelPort.Controls.Add(labelBaud);
            panelPort.Controls.Add(cbBaud);
            panelPort.Controls.Add(chkRts);
            panelPort.Controls.Add(btnOpen);
            panelPort.Controls.Add(lblStatus);
            panelPort.Dock = DockStyle.Top;
            panelPort.Location = new Point(0, 0);
            panelPort.Margin = new Padding(3, 4, 3, 4);
            panelPort.Name = "panelPort";
            panelPort.Size = new Size(1102, 48);
            panelPort.TabIndex = 4;
            // 
            // labelPort
            // 
            labelPort.AutoSize = true;
            labelPort.Location = new Point(3, 12);
            labelPort.Margin = new Padding(3, 12, 3, 0);
            labelPort.Name = "labelPort";
            labelPort.Size = new Size(38, 20);
            labelPort.TabIndex = 0;
            labelPort.Text = "Port:";
            // 
            // cbPort
            // 
            cbPort.DropDownStyle = ComboBoxStyle.DropDownList;
            cbPort.FormattingEnabled = true;
            cbPort.Location = new Point(47, 4);
            cbPort.Margin = new Padding(3, 4, 3, 4);
            cbPort.Name = "cbPort";
            cbPort.Size = new Size(91, 28);
            cbPort.TabIndex = 0;
            // 
            // labelBaud
            // 
            labelBaud.AutoSize = true;
            labelBaud.Location = new Point(144, 12);
            labelBaud.Margin = new Padding(3, 12, 3, 0);
            labelBaud.Name = "labelBaud";
            labelBaud.Size = new Size(71, 20);
            labelBaud.TabIndex = 1;
            labelBaud.Text = "Prędkość:";
            // 
            // cbBaud
            // 
            cbBaud.FormattingEnabled = true;
            cbBaud.Items.AddRange(new object[] { "1200", "2400", "9600", "19200", "38400", "57600", "115200" });
            cbBaud.Location = new Point(221, 4);
            cbBaud.Margin = new Padding(3, 4, 3, 4);
            cbBaud.Name = "cbBaud";
            cbBaud.Size = new Size(91, 28);
            cbBaud.TabIndex = 1;
            cbBaud.Text = "9600";
            // 
            // chkRts
            // 
            chkRts.AutoSize = true;
            chkRts.Location = new Point(318, 9);
            chkRts.Margin = new Padding(3, 9, 3, 0);
            chkRts.Name = "chkRts";
            chkRts.Size = new Size(86, 24);
            chkRts.TabIndex = 2;
            chkRts.Text = "RTS/CTS";
            chkRts.UseVisualStyleBackColor = true;
            // 
            // btnOpen
            // 
            btnOpen.AutoSize = true;
            btnOpen.Location = new Point(410, 4);
            btnOpen.Margin = new Padding(3, 4, 3, 4);
            btnOpen.Name = "btnOpen";
            btnOpen.Size = new Size(113, 40);
            btnOpen.TabIndex = 3;
            btnOpen.Text = "Otwórz port";
            btnOpen.UseVisualStyleBackColor = true;
            btnOpen.Click += btnOpen_Click;
            // 
            // lblStatus
            // 
            lblStatus.AutoSize = true;
            lblStatus.Location = new Point(535, 12);
            lblStatus.Margin = new Padding(9, 12, 3, 0);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(106, 20);
            lblStatus.TabIndex = 4;
            lblStatus.Text = "Port zamknięty";
            // 
            // panelCmd
            // 
            panelCmd.AutoSize = true;
            panelCmd.Controls.Add(btnInit);
            panelCmd.Controls.Add(labelNumber);
            panelCmd.Controls.Add(txtPhone);
            panelCmd.Controls.Add(btnDial);
            panelCmd.Controls.Add(btnAnswer);
            panelCmd.Controls.Add(btnAuto);
            panelCmd.Controls.Add(btnEsc);
            panelCmd.Controls.Add(btnAto);
            panelCmd.Controls.Add(btnHang);
            panelCmd.Dock = DockStyle.Top;
            panelCmd.Location = new Point(0, 48);
            panelCmd.Margin = new Padding(3, 4, 3, 4);
            panelCmd.Name = "panelCmd";
            panelCmd.Size = new Size(1102, 48);
            panelCmd.TabIndex = 3;
            // 
            // btnInit
            // 
            btnInit.AutoSize = true;
            btnInit.Location = new Point(3, 4);
            btnInit.Margin = new Padding(3, 4, 3, 4);
            btnInit.Name = "btnInit";
            btnInit.Size = new Size(86, 40);
            btnInit.TabIndex = 0;
            btnInit.Text = "ATZ";
            btnInit.UseVisualStyleBackColor = true;
            btnInit.Click += btnInit_Click;
            // 
            // labelNumber
            // 
            labelNumber.AutoSize = true;
            labelNumber.Location = new Point(101, 12);
            labelNumber.Margin = new Padding(9, 12, 3, 0);
            labelNumber.Name = "labelNumber";
            labelNumber.Size = new Size(57, 20);
            labelNumber.TabIndex = 1;
            labelNumber.Text = "Numer:";
            // 
            // txtPhone
            // 
            txtPhone.Location = new Point(164, 4);
            txtPhone.Margin = new Padding(3, 4, 3, 4);
            txtPhone.Name = "txtPhone";
            txtPhone.Size = new Size(125, 27);
            txtPhone.TabIndex = 1;
            txtPhone.Text = "123";
            // 
            // btnDial
            // 
            btnDial.AutoSize = true;
            btnDial.Location = new Point(295, 4);
            btnDial.Margin = new Padding(3, 4, 3, 4);
            btnDial.Name = "btnDial";
            btnDial.Size = new Size(130, 40);
            btnDial.TabIndex = 2;
            btnDial.Text = "Dzwoń (ATDT)";
            btnDial.UseVisualStyleBackColor = true;
            btnDial.Click += btnDial_Click;
            // 
            // btnAnswer
            // 
            btnAnswer.AutoSize = true;
            btnAnswer.Location = new Point(431, 4);
            btnAnswer.Margin = new Padding(3, 4, 3, 4);
            btnAnswer.Name = "btnAnswer";
            btnAnswer.Size = new Size(128, 40);
            btnAnswer.TabIndex = 3;
            btnAnswer.Text = "Odbierz (ATA)";
            btnAnswer.UseVisualStyleBackColor = true;
            btnAnswer.Click += btnAnswer_Click;
            // 
            // btnAuto
            // 
            btnAuto.AutoSize = true;
            btnAuto.Location = new Point(565, 4);
            btnAuto.Margin = new Padding(3, 4, 3, 4);
            btnAuto.Name = "btnAuto";
            btnAuto.Size = new Size(191, 40);
            btnAuto.TabIndex = 4;
            btnAuto.Text = "Auto-odbiór (ATS0=1)";
            btnAuto.UseVisualStyleBackColor = true;
            btnAuto.Click += btnAuto_Click;
            // 
            // btnEsc
            // 
            btnEsc.AutoSize = true;
            btnEsc.Location = new Point(762, 4);
            btnEsc.Margin = new Padding(3, 4, 3, 4);
            btnEsc.Name = "btnEsc";
            btnEsc.Size = new Size(101, 40);
            btnEsc.TabIndex = 5;
            btnEsc.Text = "+++ (ESC)";
            btnEsc.UseVisualStyleBackColor = true;
            btnEsc.Click += btnEsc_Click;
            // 
            // btnAto
            // 
            btnAto.AutoSize = true;
            btnAto.Location = new Point(869, 4);
            btnAto.Margin = new Padding(3, 4, 3, 4);
            btnAto.Name = "btnAto";
            btnAto.Size = new Size(86, 40);
            btnAto.TabIndex = 6;
            btnAto.Text = "ATO";
            btnAto.UseVisualStyleBackColor = true;
            btnAto.Click += btnAto_Click;
            // 
            // btnHang
            // 
            btnHang.AutoSize = true;
            btnHang.Location = new Point(961, 4);
            btnHang.Margin = new Padding(3, 4, 3, 4);
            btnHang.Name = "btnHang";
            btnHang.Size = new Size(128, 40);
            btnHang.TabIndex = 7;
            btnHang.Text = "Rozłącz (ATH)";
            btnHang.UseVisualStyleBackColor = true;
            btnHang.Click += btnHang_Click;
            // 
            // panelFile
            // 
            panelFile.AutoSize = true;
            panelFile.Controls.Add(btnSendFile);
            panelFile.Controls.Add(btnRecvFile);
            panelFile.Controls.Add(btnCancel);
            panelFile.Dock = DockStyle.Top;
            panelFile.Location = new Point(0, 96);
            panelFile.Margin = new Padding(3, 4, 3, 4);
            panelFile.Name = "panelFile";
            panelFile.Size = new Size(1102, 48);
            panelFile.TabIndex = 2;
            // 
            // btnSendFile
            // 
            btnSendFile.AutoSize = true;
            btnSendFile.Location = new Point(3, 4);
            btnSendFile.Margin = new Padding(3, 4, 3, 4);
            btnSendFile.Name = "btnSendFile";
            btnSendFile.Size = new Size(189, 40);
            btnSendFile.TabIndex = 0;
            btnSendFile.Text = "Wyślij plik (XMODEM)";
            btnSendFile.UseVisualStyleBackColor = true;
            btnSendFile.Click += btnSendFile_Click;
            // 
            // btnRecvFile
            // 
            btnRecvFile.AutoSize = true;
            btnRecvFile.Location = new Point(198, 4);
            btnRecvFile.Margin = new Padding(3, 4, 3, 4);
            btnRecvFile.Name = "btnRecvFile";
            btnRecvFile.Size = new Size(205, 40);
            btnRecvFile.TabIndex = 1;
            btnRecvFile.Text = "Odbierz plik (XMODEM)";
            btnRecvFile.UseVisualStyleBackColor = true;
            btnRecvFile.Click += btnRecvFile_Click;
            // 
            // btnCancel
            // 
            btnCancel.AutoSize = true;
            btnCancel.Enabled = false;
            btnCancel.Location = new Point(409, 4);
            btnCancel.Margin = new Padding(3, 4, 3, 4);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(131, 40);
            btnCancel.TabIndex = 2;
            btnCancel.Text = "Anuluj transfer";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += btnCancel_Click;
            // 
            // panelInput
            // 
            panelInput.Controls.Add(txtIn);
            panelInput.Dock = DockStyle.Bottom;
            panelInput.Location = new Point(0, 764);
            panelInput.Margin = new Padding(3, 4, 3, 4);
            panelInput.Name = "panelInput";
            panelInput.Size = new Size(1102, 37);
            panelInput.TabIndex = 1;
            // 
            // txtIn
            // 
            txtIn.Dock = DockStyle.Fill;
            txtIn.Font = new Font("Consolas", 10F);
            txtIn.Location = new Point(0, 0);
            txtIn.Margin = new Padding(3, 4, 3, 4);
            txtIn.Name = "txtIn";
            txtIn.Size = new Size(1102, 27);
            txtIn.TabIndex = 0;
            txtIn.KeyDown += txtIn_KeyDown;
            // 
            // txtLog
            // 
            txtLog.BackColor = Color.White;
            txtLog.Dock = DockStyle.Fill;
            txtLog.Font = new Font("Consolas", 10F);
            txtLog.Location = new Point(0, 144);
            txtLog.Margin = new Padding(3, 4, 3, 4);
            txtLog.Multiline = true;
            txtLog.Name = "txtLog";
            txtLog.ReadOnly = true;
            txtLog.ScrollBars = ScrollBars.Vertical;
            txtLog.Size = new Size(1102, 620);
            txtLog.TabIndex = 0;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1102, 801);
            Controls.Add(txtLog);
            Controls.Add(panelInput);
            Controls.Add(panelFile);
            Controls.Add(panelCmd);
            Controls.Add(panelPort);
            Margin = new Padding(3, 4, 3, 4);
            Name = "MainForm";
            Text = "Modem – terminal i XMODEM";
            panelPort.ResumeLayout(false);
            panelPort.PerformLayout();
            panelCmd.ResumeLayout(false);
            panelCmd.PerformLayout();
            panelFile.ResumeLayout(false);
            panelFile.PerformLayout();
            panelInput.ResumeLayout(false);
            panelInput.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.FlowLayoutPanel panelPort;
        private System.Windows.Forms.Label labelPort;
        private System.Windows.Forms.ComboBox cbPort;
        private System.Windows.Forms.Label labelBaud;
        private System.Windows.Forms.ComboBox cbBaud;
        private System.Windows.Forms.CheckBox chkRts;
        private System.Windows.Forms.Button btnOpen;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.FlowLayoutPanel panelCmd;
        private System.Windows.Forms.Button btnInit;
        private System.Windows.Forms.Label labelNumber;
        private System.Windows.Forms.TextBox txtPhone;
        private System.Windows.Forms.Button btnDial;
        private System.Windows.Forms.Button btnAnswer;
        private System.Windows.Forms.Button btnAuto;
        private System.Windows.Forms.Button btnEsc;
        private System.Windows.Forms.Button btnAto;
        private System.Windows.Forms.Button btnHang;
        private System.Windows.Forms.FlowLayoutPanel panelFile;
        private System.Windows.Forms.Button btnSendFile;
        private System.Windows.Forms.Button btnRecvFile;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Panel panelInput;
        private System.Windows.Forms.TextBox txtIn;
        private System.Windows.Forms.TextBox txtLog;
    }
}