namespace ER_StationAgent
{
    partial class ER_StationAgent_UI
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle2 = new DataGridViewCellStyle();
            connLabel = new Label();
            ConnIndi = new Label();
            splitter1 = new Splitter();
            cmbStation = new ComboBox();
            btnStart = new Button();
            btnStop = new Button();
            rtbLog = new RichTextBox();
            rtbContextMenu = new ContextMenuStrip(components);
            toolStripMenuItem1 = new ToolStripMenuItem();
            lblMulticastIp = new Label();
            label6 = new Label();
            lblPort = new Label();
            label7 = new Label();
            label9 = new Label();
            lblAssetPath = new Label();
            label12 = new Label();
            groupBox1 = new GroupBox();
            groupBox2 = new GroupBox();
            groupBox3 = new GroupBox();
            listBox1 = new ListBox();
            groupBox4 = new GroupBox();
            label1 = new Label();
            dataGridViewEn = new DataGridView();
            label2 = new Label();
            tbSendCycle = new TextBox();
            label3 = new Label();
            btnSetSendCycle = new Button();
            rtbContextMenu.SuspendLayout();
            groupBox1.SuspendLayout();
            groupBox2.SuspendLayout();
            groupBox3.SuspendLayout();
            groupBox4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridViewEn).BeginInit();
            SuspendLayout();
            // 
            // connLabel
            // 
            connLabel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            connLabel.FlatStyle = FlatStyle.Flat;
            connLabel.Font = new Font("Segoe UI", 11.25F, FontStyle.Bold);
            connLabel.ForeColor = Color.FromArgb(230, 234, 242);
            connLabel.Location = new Point(232, 21);
            connLabel.Margin = new Padding(0);
            connLabel.Name = "connLabel";
            connLabel.Size = new Size(115, 28);
            connLabel.TabIndex = 7;
            connLabel.Text = "Disconnected";
            connLabel.TextAlign = ContentAlignment.MiddleRight;
            // 
            // ConnIndi
            // 
            ConnIndi.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            ConnIndi.BackColor = Color.FromArgb(239, 68, 68);
            ConnIndi.FlatStyle = FlatStyle.Flat;
            ConnIndi.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            ConnIndi.ForeColor = Color.FromArgb(230, 234, 242);
            ConnIndi.Location = new Point(345, 25);
            ConnIndi.Margin = new Padding(0);
            ConnIndi.Name = "ConnIndi";
            ConnIndi.Size = new Size(5, 20);
            ConnIndi.TabIndex = 8;
            ConnIndi.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // splitter1
            // 
            splitter1.Location = new Point(0, 0);
            splitter1.Name = "splitter1";
            splitter1.Size = new Size(3, 592);
            splitter1.TabIndex = 9;
            splitter1.TabStop = false;
            // 
            // cmbStation
            // 
            cmbStation.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            cmbStation.BackColor = Color.FromArgb(17, 19, 23);
            cmbStation.FlatStyle = FlatStyle.Flat;
            cmbStation.ForeColor = SystemColors.Menu;
            cmbStation.FormattingEnabled = true;
            cmbStation.Location = new Point(29, 28);
            cmbStation.Name = "cmbStation";
            cmbStation.Size = new Size(300, 29);
            cmbStation.TabIndex = 11;
            cmbStation.SelectedIndexChanged += cmbStation_SelectedIndexChanged;
            // 
            // btnStart
            // 
            btnStart.BackColor = Color.FromArgb(20, 60, 34);
            btnStart.BackgroundImageLayout = ImageLayout.None;
            btnStart.FlatAppearance.BorderColor = Color.FromArgb(34, 197, 94);
            btnStart.FlatStyle = FlatStyle.Flat;
            btnStart.Font = new Font("Segoe UI", 11.25F, FontStyle.Bold);
            btnStart.ForeColor = SystemColors.ButtonFace;
            btnStart.Location = new Point(29, 66);
            btnStart.Margin = new Padding(0);
            btnStart.Name = "btnStart";
            btnStart.Size = new Size(145, 36);
            btnStart.TabIndex = 12;
            btnStart.Text = "Start Service";
            btnStart.UseVisualStyleBackColor = false;
            btnStart.Click += btnStart_Click;
            // 
            // btnStop
            // 
            btnStop.BackColor = Color.FromArgb(59, 24, 24);
            btnStop.FlatAppearance.BorderColor = Color.FromArgb(239, 68, 68);
            btnStop.FlatStyle = FlatStyle.Flat;
            btnStop.Font = new Font("Segoe UI", 11.25F, FontStyle.Bold);
            btnStop.ForeColor = SystemColors.ButtonFace;
            btnStop.Location = new Point(184, 66);
            btnStop.Margin = new Padding(0);
            btnStop.Name = "btnStop";
            btnStop.Size = new Size(145, 36);
            btnStop.TabIndex = 13;
            btnStop.Text = "Stop Service";
            btnStop.UseVisualStyleBackColor = false;
            btnStop.Click += btnStop_Click;
            // 
            // rtbLog
            // 
            rtbLog.BackColor = Color.FromArgb(21, 23, 30);
            rtbLog.BorderStyle = BorderStyle.None;
            rtbLog.Dock = DockStyle.Fill;
            rtbLog.Font = new Font("Segoe UI", 8F, FontStyle.Regular, GraphicsUnit.Point, 0);
            rtbLog.ForeColor = SystemColors.Info;
            rtbLog.Location = new Point(3, 25);
            rtbLog.Margin = new Padding(10);
            rtbLog.Name = "rtbLog";
            rtbLog.ReadOnly = true;
            rtbLog.Size = new Size(825, 151);
            rtbLog.TabIndex = 14;
            rtbLog.Text = "";
            // 
            // rtbContextMenu
            // 
            rtbContextMenu.Items.AddRange(new ToolStripItem[] { toolStripMenuItem1 });
            rtbContextMenu.Name = "rtbContextMenu";
            rtbContextMenu.Size = new Size(102, 26);
            rtbContextMenu.Text = "Clear";
            rtbContextMenu.Click += clearToolStripMenuItem_Click;
            // 
            // toolStripMenuItem1
            // 
            toolStripMenuItem1.Name = "toolStripMenuItem1";
            toolStripMenuItem1.Size = new Size(101, 22);
            toolStripMenuItem1.Text = "Clear";
            // 
            // lblMulticastIp
            // 
            lblMulticastIp.BackColor = Color.FromArgb(17, 19, 23);
            lblMulticastIp.BorderStyle = BorderStyle.FixedSingle;
            lblMulticastIp.FlatStyle = FlatStyle.Flat;
            lblMulticastIp.Font = new Font("Segoe UI", 11F);
            lblMulticastIp.ForeColor = Color.FromArgb(230, 234, 242);
            lblMulticastIp.Location = new Point(167, 86);
            lblMulticastIp.Margin = new Padding(0);
            lblMulticastIp.Name = "lblMulticastIp";
            lblMulticastIp.Size = new Size(162, 25);
            lblMulticastIp.TabIndex = 17;
            lblMulticastIp.Text = "-";
            lblMulticastIp.TextAlign = ContentAlignment.MiddleRight;
            // 
            // label6
            // 
            label6.BackColor = Color.FromArgb(21, 23, 30);
            label6.FlatStyle = FlatStyle.Flat;
            label6.Font = new Font("Segoe UI", 11.25F, FontStyle.Bold);
            label6.ForeColor = Color.FromArgb(230, 234, 242);
            label6.Location = new Point(29, 86);
            label6.Margin = new Padding(0);
            label6.Name = "label6";
            label6.Size = new Size(138, 25);
            label6.TabIndex = 16;
            label6.Text = "Multicast IP:";
            label6.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblPort
            // 
            lblPort.BackColor = Color.FromArgb(17, 19, 23);
            lblPort.BorderStyle = BorderStyle.FixedSingle;
            lblPort.FlatStyle = FlatStyle.Flat;
            lblPort.Font = new Font("Segoe UI", 11F);
            lblPort.ForeColor = Color.FromArgb(230, 234, 242);
            lblPort.Location = new Point(167, 113);
            lblPort.Margin = new Padding(0);
            lblPort.Name = "lblPort";
            lblPort.Size = new Size(162, 25);
            lblPort.TabIndex = 19;
            lblPort.Text = "-";
            lblPort.TextAlign = ContentAlignment.MiddleRight;
            // 
            // label7
            // 
            label7.BackColor = Color.FromArgb(21, 23, 30);
            label7.FlatStyle = FlatStyle.Flat;
            label7.Font = new Font("Segoe UI", 11.25F, FontStyle.Bold);
            label7.ForeColor = Color.FromArgb(230, 234, 242);
            label7.Location = new Point(29, 113);
            label7.Margin = new Padding(0);
            label7.Name = "label7";
            label7.Size = new Size(138, 25);
            label7.TabIndex = 18;
            label7.Text = "Port:";
            label7.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // label9
            // 
            label9.BackColor = Color.FromArgb(21, 23, 30);
            label9.FlatStyle = FlatStyle.Flat;
            label9.Font = new Font("Segoe UI", 11.25F, FontStyle.Bold);
            label9.ForeColor = Color.FromArgb(230, 234, 242);
            label9.Location = new Point(29, 140);
            label9.Margin = new Padding(0);
            label9.Name = "label9";
            label9.Size = new Size(138, 25);
            label9.TabIndex = 20;
            label9.Text = "OSC Addresses:";
            label9.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblAssetPath
            // 
            lblAssetPath.BackColor = Color.FromArgb(17, 19, 23);
            lblAssetPath.BorderStyle = BorderStyle.FixedSingle;
            lblAssetPath.FlatStyle = FlatStyle.Flat;
            lblAssetPath.Font = new Font("Segoe UI", 11F);
            lblAssetPath.ForeColor = Color.FromArgb(230, 234, 242);
            lblAssetPath.Location = new Point(29, 59);
            lblAssetPath.Margin = new Padding(0);
            lblAssetPath.Name = "lblAssetPath";
            lblAssetPath.Size = new Size(300, 25);
            lblAssetPath.TabIndex = 25;
            lblAssetPath.Text = "-";
            lblAssetPath.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // label12
            // 
            label12.BackColor = Color.FromArgb(21, 23, 30);
            label12.FlatStyle = FlatStyle.Flat;
            label12.Font = new Font("Segoe UI", 11.25F, FontStyle.Bold);
            label12.ForeColor = Color.FromArgb(230, 234, 242);
            label12.Location = new Point(29, 32);
            label12.Margin = new Padding(0);
            label12.Name = "label12";
            label12.Size = new Size(138, 25);
            label12.TabIndex = 24;
            label12.Text = "Assets Folder:";
            label12.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // groupBox1
            // 
            groupBox1.BackColor = Color.FromArgb(21, 23, 30);
            groupBox1.Controls.Add(rtbLog);
            groupBox1.FlatStyle = FlatStyle.Flat;
            groupBox1.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            groupBox1.ForeColor = SystemColors.ButtonHighlight;
            groupBox1.Location = new Point(406, 393);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(831, 179);
            groupBox1.TabIndex = 28;
            groupBox1.TabStop = false;
            groupBox1.Text = "System Log | Event Stream";
            // 
            // groupBox2
            // 
            groupBox2.BackColor = Color.FromArgb(21, 23, 30);
            groupBox2.BackgroundImageLayout = ImageLayout.None;
            groupBox2.Controls.Add(cmbStation);
            groupBox2.Controls.Add(btnStart);
            groupBox2.Controls.Add(btnStop);
            groupBox2.FlatStyle = FlatStyle.Flat;
            groupBox2.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            groupBox2.ForeColor = SystemColors.ButtonHighlight;
            groupBox2.Location = new Point(25, 67);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new Size(358, 117);
            groupBox2.TabIndex = 29;
            groupBox2.TabStop = false;
            groupBox2.Text = "Station Selection";
            // 
            // groupBox3
            // 
            groupBox3.BackColor = Color.FromArgb(21, 23, 30);
            groupBox3.Controls.Add(listBox1);
            groupBox3.Controls.Add(lblMulticastIp);
            groupBox3.Controls.Add(label6);
            groupBox3.Controls.Add(label7);
            groupBox3.Controls.Add(lblPort);
            groupBox3.Controls.Add(label9);
            groupBox3.Controls.Add(lblAssetPath);
            groupBox3.Controls.Add(label12);
            groupBox3.FlatStyle = FlatStyle.Flat;
            groupBox3.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            groupBox3.ForeColor = SystemColors.ButtonHighlight;
            groupBox3.Location = new Point(25, 235);
            groupBox3.Name = "groupBox3";
            groupBox3.Size = new Size(358, 224);
            groupBox3.TabIndex = 29;
            groupBox3.TabStop = false;
            groupBox3.Text = "Network and Storage Info";
            // 
            // listBox1
            // 
            listBox1.BackColor = Color.FromArgb(17, 19, 23);
            listBox1.BorderStyle = BorderStyle.FixedSingle;
            listBox1.Font = new Font("Segoe UI", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            listBox1.ForeColor = Color.FromArgb(230, 234, 242);
            listBox1.FormattingEnabled = true;
            listBox1.ItemHeight = 13;
            listBox1.Items.AddRange(new object[] { "/MESSAGES", "/MESSAGES_OVER", "/ARCHIVE_EN", "/ARCHIVE_AR", "/DEPLOYMENT_TEMPLATE" });
            listBox1.Location = new Point(167, 140);
            listBox1.Name = "listBox1";
            listBox1.SelectionMode = SelectionMode.None;
            listBox1.Size = new Size(162, 67);
            listBox1.TabIndex = 26;
            // 
            // groupBox4
            // 
            groupBox4.BackColor = Color.FromArgb(21, 23, 30);
            groupBox4.Controls.Add(ConnIndi);
            groupBox4.Controls.Add(connLabel);
            groupBox4.FlatStyle = FlatStyle.Flat;
            groupBox4.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            groupBox4.ForeColor = SystemColors.ButtonHighlight;
            groupBox4.Location = new Point(25, 510);
            groupBox4.Name = "groupBox4";
            groupBox4.Size = new Size(358, 60);
            groupBox4.TabIndex = 30;
            groupBox4.TabStop = false;
            groupBox4.Text = "API Connection Status";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.BackColor = Color.FromArgb(21, 23, 30);
            label1.FlatStyle = FlatStyle.Flat;
            label1.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
            label1.ForeColor = Color.FromArgb(230, 234, 242);
            label1.Location = new Point(25, 23);
            label1.Margin = new Padding(0);
            label1.Name = "label1";
            label1.Size = new Size(301, 32);
            label1.TabIndex = 26;
            label1.Text = "Station Agent Dashboard";
            label1.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // dataGridViewEn
            // 
            dataGridViewEn.BackgroundColor = Color.FromArgb(21, 23, 30);
            dataGridViewCellStyle1.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = Color.FromArgb(15, 17, 42);
            dataGridViewCellStyle1.Font = new Font("Segoe UI", 10F);
            dataGridViewCellStyle1.ForeColor = SystemColors.Control;
            dataGridViewCellStyle1.SelectionBackColor = SystemColors.HotTrack;
            dataGridViewCellStyle1.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = DataGridViewTriState.True;
            dataGridViewEn.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            dataGridViewEn.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle2.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = Color.FromArgb(21, 23, 30);
            dataGridViewCellStyle2.Font = new Font("Segoe UI", 8F);
            dataGridViewCellStyle2.ForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle2.SelectionBackColor = SystemColors.HotTrack;
            dataGridViewCellStyle2.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = DataGridViewTriState.False;
            dataGridViewEn.DefaultCellStyle = dataGridViewCellStyle2;
            dataGridViewEn.GridColor = Color.DarkGray;
            dataGridViewEn.Location = new Point(406, 79);
            dataGridViewEn.Name = "dataGridViewEn";
            dataGridViewEn.Size = new Size(817, 294);
            dataGridViewEn.TabIndex = 0;
            dataGridViewEn.CellEndEdit += dataGridViewEn_CellEndEdit;
            // 
            // label2
            // 
            label2.BackColor = Color.FromArgb(21, 23, 30);
            label2.FlatStyle = FlatStyle.Flat;
            label2.Font = new Font("Segoe UI", 11.25F, FontStyle.Bold);
            label2.ForeColor = Color.FromArgb(230, 234, 242);
            label2.Location = new Point(991, 41);
            label2.Margin = new Padding(0);
            label2.Name = "label2";
            label2.Size = new Size(87, 35);
            label2.TabIndex = 27;
            label2.Text = "Read Time:";
            label2.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // tbSendCycle
            // 
            tbSendCycle.Font = new Font("Segoe UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            tbSendCycle.Location = new Point(1081, 45);
            tbSendCycle.Name = "tbSendCycle";
            tbSendCycle.Size = new Size(59, 27);
            tbSendCycle.TabIndex = 31;
            tbSendCycle.Text = "6000";
            // 
            // label3
            // 
            label3.FlatStyle = FlatStyle.Flat;
            label3.Font = new Font("Segoe UI", 11.25F);
            label3.ForeColor = Color.FromArgb(230, 234, 242);
            label3.Location = new Point(1139, 41);
            label3.Margin = new Padding(0);
            label3.Name = "label3";
            label3.Size = new Size(37, 35);
            label3.TabIndex = 32;
            label3.Text = "ms";
            label3.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // btnSetSendCycle
            // 
            btnSetSendCycle.BackColor = Color.FromArgb(20, 34, 60);
            btnSetSendCycle.BackgroundImageLayout = ImageLayout.None;
            btnSetSendCycle.FlatAppearance.BorderColor = Color.FromArgb(34, 94, 197);
            btnSetSendCycle.FlatStyle = FlatStyle.Flat;
            btnSetSendCycle.Font = new Font("Segoe UI", 11.25F, FontStyle.Bold);
            btnSetSendCycle.ForeColor = SystemColors.ButtonFace;
            btnSetSendCycle.Location = new Point(1180, 41);
            btnSetSendCycle.Margin = new Padding(0);
            btnSetSendCycle.Name = "btnSetSendCycle";
            btnSetSendCycle.Size = new Size(43, 35);
            btnSetSendCycle.TabIndex = 14;
            btnSetSendCycle.Text = "Set";
            btnSetSendCycle.UseVisualStyleBackColor = false;
            btnSetSendCycle.Click += btnSetSendCycle_Click;
            // 
            // ER_StationAgent_UI
            // 
            AutoScaleDimensions = new SizeF(11F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(21, 23, 30);
            ClientSize = new Size(1257, 592);
            Controls.Add(btnSetSendCycle);
            Controls.Add(label3);
            Controls.Add(tbSendCycle);
            Controls.Add(label2);
            Controls.Add(dataGridViewEn);
            Controls.Add(label1);
            Controls.Add(groupBox4);
            Controls.Add(groupBox3);
            Controls.Add(groupBox2);
            Controls.Add(groupBox1);
            Controls.Add(splitter1);
            Font = new Font("Segoe UI", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            Margin = new Padding(5);
            Name = "ER_StationAgent_UI";
            Text = "Etihad Rail Station Agent";
            FormClosing += ER_StationAgent_UI_FormClosing;
            Shown += ER_StationAgent_UI_Shown;
            rtbContextMenu.ResumeLayout(false);
            groupBox1.ResumeLayout(false);
            groupBox2.ResumeLayout(false);
            groupBox3.ResumeLayout(false);
            groupBox4.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dataGridViewEn).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private Label connLabel;
        private Label ConnIndi;
        private Splitter splitter1;
        private ComboBox cmbStation;
        private Button btnStart;
        private Button btnStop;
        private RichTextBox rtbLog;
        private ContextMenuStrip rtbContextMenu;
        private ToolStripMenuItem toolStripMenuItem1;
        private Label lblPort;
        //private Label label5;
        private Label label6;
        private Label label7;
        private Label label9;
        private Label lblMulticastIp;
        private Label lblAssetPath;
        private Label label12;
        private GroupBox groupBox1;
        private GroupBox groupBox2;
        private GroupBox groupBox3;
        private GroupBox groupBox4;
        private Label label1;
        private ListBox listBox1;
        private DataGridView dataGridViewEn;
        private Label label2;
        private TextBox tbSendCycle;
        private Label label3;
        private Button btnSetSendCycle;
    }
}
