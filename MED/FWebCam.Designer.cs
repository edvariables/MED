

namespace MED.EDWebCam
{
    partial class FWebCam
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FWebCam));
            cboCameras = new ComboBox();
            rtbLog = new RichTextBox();
            panBottom = new Panel();
            chkClearLogOnRun = new CheckBox();
            chkLogColored = new CheckBox();
            chkVideoCaptureLogger = new CheckBox();
            chkRenderLogger = new CheckBox();
            picRender = new PictureBox();
            chkRun = new CheckBox();
            cmdSaveSettings = new Button();
            panTopTools = new Panel();
            cboCaptureSize = new ComboBox();
            tableLayoutPan = new TableLayoutPanel();
            splitterLog = new Splitter();
            toolTip1 = new ToolTip(components);
            panBottom.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)picRender).BeginInit();
            panTopTools.SuspendLayout();
            tableLayoutPan.SuspendLayout();
            SuspendLayout();
            // 
            // cboCameras
            // 
            cboCameras.FormattingEnabled = true;
            cboCameras.Location = new Point(108, 6);
            cboCameras.Name = "cboCameras";
            cboCameras.Size = new Size(121, 23);
            cboCameras.TabIndex = 2;
            // 
            // rtbLog
            // 
            rtbLog.BackColor = SystemColors.WindowText;
            rtbLog.Dock = DockStyle.Bottom;
            rtbLog.Font = new Font("Consolas", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            rtbLog.ForeColor = SystemColors.Window;
            rtbLog.Location = new Point(0, 577);
            rtbLog.MaximumSize = new Size(0, 200);
            rtbLog.Name = "rtbLog";
            rtbLog.Size = new Size(925, 100);
            rtbLog.TabIndex = 3;
            rtbLog.Text = "";
            // 
            // panBottom
            // 
            panBottom.Controls.Add(chkClearLogOnRun);
            panBottom.Controls.Add(chkLogColored);
            panBottom.Controls.Add(chkVideoCaptureLogger);
            panBottom.Controls.Add(chkRenderLogger);
            panBottom.Dock = DockStyle.Bottom;
            panBottom.Location = new Point(0, 677);
            panBottom.Name = "panBottom";
            panBottom.Size = new Size(925, 26);
            panBottom.TabIndex = 7;
            // 
            // chkClearLogOnRun
            // 
            chkClearLogOnRun.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            chkClearLogOnRun.Appearance = Appearance.Button;
            chkClearLogOnRun.AutoSize = true;
            chkClearLogOnRun.BackColor = SystemColors.Control;
            chkClearLogOnRun.Checked = true;
            chkClearLogOnRun.CheckState = CheckState.Checked;
            chkClearLogOnRun.FlatAppearance.BorderSize = 0;
            chkClearLogOnRun.FlatStyle = FlatStyle.Flat;
            chkClearLogOnRun.ForeColor = SystemColors.ControlText;
            chkClearLogOnRun.Image = (Image)resources.GetObject("chkClearLogOnRun.Image");
            chkClearLogOnRun.Location = new Point(896, 2);
            chkClearLogOnRun.Name = "chkClearLogOnRun";
            chkClearLogOnRun.Size = new Size(22, 22);
            chkClearLogOnRun.TabIndex = 5;
            toolTip1.SetToolTip(chkClearLogOnRun, "Cocher pour vider le log à chaque lancement.\r\nDouble-clic pour effacer maintenant.");
            chkClearLogOnRun.UseVisualStyleBackColor = false;
            chkClearLogOnRun.CheckedChanged += chkClearLogOnRun_CheckedChanged;
            chkClearLogOnRun.CheckStateChanged += ChkClearLogOnRun_CheckStateChanged;
            // 
            // chkLogColored
            // 
            chkLogColored.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            chkLogColored.Appearance = Appearance.Button;
            chkLogColored.AutoSize = true;
            chkLogColored.BackColor = SystemColors.Control;
            chkLogColored.FlatAppearance.BorderSize = 0;
            chkLogColored.FlatAppearance.CheckedBackColor = Color.FromArgb(255, 192, 192);
            chkLogColored.FlatStyle = FlatStyle.Flat;
            chkLogColored.ForeColor = SystemColors.ControlText;
            chkLogColored.Image = (Image)resources.GetObject("chkLogColored.Image");
            chkLogColored.Location = new Point(852, 2);
            chkLogColored.Name = "chkLogColored";
            chkLogColored.Size = new Size(22, 22);
            chkLogColored.TabIndex = 5;
            toolTip1.SetToolTip(chkLogColored, "Colorise les traces.\r\nAttention, cela consomme des ressources.");
            chkLogColored.UseVisualStyleBackColor = false;
            chkLogColored.CheckedChanged += chkLogColoredNot_CheckedChanged;
            // 
            // chkVideoCaptureLogger
            // 
            chkVideoCaptureLogger.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            chkVideoCaptureLogger.AutoSize = true;
            chkVideoCaptureLogger.BackColor = SystemColors.Control;
            chkVideoCaptureLogger.Checked = true;
            chkVideoCaptureLogger.CheckState = CheckState.Checked;
            chkVideoCaptureLogger.FlatStyle = FlatStyle.Flat;
            chkVideoCaptureLogger.ForeColor = SystemColors.ControlText;
            chkVideoCaptureLogger.Location = new Point(753, 4);
            chkVideoCaptureLogger.Name = "chkVideoCaptureLogger";
            chkVideoCaptureLogger.Size = new Size(95, 19);
            chkVideoCaptureLogger.TabIndex = 5;
            chkVideoCaptureLogger.Text = "VideoCapture";
            toolTip1.SetToolTip(chkVideoCaptureLogger, "Active la trace");
            chkVideoCaptureLogger.UseVisualStyleBackColor = false;
            chkVideoCaptureLogger.CheckedChanged += chkVideoCaptureLogger_CheckedChanged;
            // 
            // chkRenderLogger
            // 
            chkRenderLogger.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            chkRenderLogger.AutoSize = true;
            chkRenderLogger.BackColor = SystemColors.Control;
            chkRenderLogger.Checked = true;
            chkRenderLogger.CheckState = CheckState.Checked;
            chkRenderLogger.FlatStyle = FlatStyle.Flat;
            chkRenderLogger.ForeColor = SystemColors.ControlText;
            chkRenderLogger.Location = new Point(687, 4);
            chkRenderLogger.Name = "chkRenderLogger";
            chkRenderLogger.Size = new Size(60, 19);
            chkRenderLogger.TabIndex = 5;
            chkRenderLogger.Text = "Render";
            toolTip1.SetToolTip(chkRenderLogger, "Active la trace");
            chkRenderLogger.UseVisualStyleBackColor = false;
            chkRenderLogger.CheckedChanged += chkRenderLogger_CheckedChanged;
            // 
            // picRender
            // 
            picRender.BackColor = SystemColors.WindowFrame;
            tableLayoutPan.SetColumnSpan(picRender, 2);
            picRender.Dock = DockStyle.Fill;
            picRender.Location = new Point(20, 67);
            picRender.Margin = new Padding(20);
            picRender.Name = "picRender";
            picRender.Padding = new Padding(30);
            picRender.Size = new Size(885, 486);
            picRender.TabIndex = 1;
            picRender.TabStop = false;
            // 
            // chkRun
            // 
            chkRun.Appearance = Appearance.Button;
            chkRun.AutoSize = true;
            chkRun.FlatStyle = FlatStyle.Popup;
            chkRun.Location = new Point(12, 7);
            chkRun.Name = "chkRun";
            chkRun.Size = new Size(66, 25);
            chkRun.TabIndex = 4;
            chkRun.Text = "Démarrer";
            chkRun.UseVisualStyleBackColor = true;
            chkRun.CheckedChanged += chkRun_CheckedChanged;
            // 
            // cmdSaveSettings
            // 
            cmdSaveSettings.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            cmdSaveSettings.Location = new Point(858, 6);
            cmdSaveSettings.Name = "cmdSaveSettings";
            cmdSaveSettings.Size = new Size(52, 27);
            cmdSaveSettings.TabIndex = 6;
            cmdSaveSettings.Text = "Save";
            cmdSaveSettings.UseVisualStyleBackColor = true;
            cmdSaveSettings.Click += cmdSaveSettings_Click;
            // 
            // panTopTools
            // 
            panTopTools.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            panTopTools.BackColor = SystemColors.ControlLight;
            tableLayoutPan.SetColumnSpan(panTopTools, 2);
            panTopTools.Controls.Add(cboCaptureSize);
            panTopTools.Controls.Add(cboCameras);
            panTopTools.Controls.Add(chkRun);
            panTopTools.Controls.Add(cmdSaveSettings);
            panTopTools.Location = new Point(3, 3);
            panTopTools.Name = "panTopTools";
            panTopTools.Size = new Size(919, 41);
            panTopTools.TabIndex = 10;
            // 
            // cboCaptureSize
            // 
            cboCaptureSize.FormattingEnabled = true;
            cboCaptureSize.Items.AddRange(new object[] { "800x600", "640x480", "320x240", "160x120", "80x60" });
            cboCaptureSize.Location = new Point(235, 6);
            cboCaptureSize.Name = "cboCaptureSize";
            cboCaptureSize.Size = new Size(71, 23);
            cboCaptureSize.TabIndex = 10;
            cboCaptureSize.SelectedIndexChanged += cboCaptureSize_SelectedIndexChanged;
            // 
            // tableLayoutPan
            // 
            tableLayoutPan.ColumnCount = 2;
            tableLayoutPan.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPan.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPan.Controls.Add(panTopTools, 0, 0);
            tableLayoutPan.Controls.Add(picRender, 0, 1);
            tableLayoutPan.Dock = DockStyle.Top;
            tableLayoutPan.Location = new Point(0, 0);
            tableLayoutPan.Name = "tableLayoutPan";
            tableLayoutPan.RowCount = 5;
            tableLayoutPan.RowStyles.Add(new RowStyle());
            tableLayoutPan.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPan.RowStyles.Add(new RowStyle());
            tableLayoutPan.RowStyles.Add(new RowStyle());
            tableLayoutPan.RowStyles.Add(new RowStyle());
            tableLayoutPan.Size = new Size(925, 573);
            tableLayoutPan.TabIndex = 11;
            // 
            // splitterLog
            // 
            splitterLog.BackColor = SystemColors.ActiveCaption;
            splitterLog.Dock = DockStyle.Bottom;
            splitterLog.Location = new Point(0, 573);
            splitterLog.MinimumSize = new Size(10, 3);
            splitterLog.Name = "splitterLog";
            splitterLog.Size = new Size(925, 4);
            splitterLog.TabIndex = 11;
            splitterLog.TabStop = false;
            // 
            // FWebCam
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(925, 703);
            Controls.Add(splitterLog);
            Controls.Add(rtbLog);
            Controls.Add(panBottom);
            Controls.Add(tableLayoutPan);
            Name = "FWebCam";
            Text = "WebCam";
            FormClosed += Form1_FormClosed;
            Load += Form1_Load;
            panBottom.ResumeLayout(false);
            panBottom.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)picRender).EndInit();
            panTopTools.ResumeLayout(false);
            panTopTools.PerformLayout();
            tableLayoutPan.ResumeLayout(false);
            ResumeLayout(false);
        }


        #endregion
        private ComboBox cboCameras;
        private RichTextBox rtbLog;
        private PictureBox picRender;
        private CheckBox chkRun;
        private CheckBox chkVideoCaptureLogger;
        private CheckBox chkRenderLogger;
        private Button cmdSaveSettings;
        private Panel panBottom;
        private CheckBox chkLogColored;
        private Panel panTopTools;
        private TableLayoutPanel tableLayoutPan;
        private Splitter splitterLog;
        private ComboBox cboCaptureSize;
        private CheckBox chkClearLogOnRun;
        private ToolTip toolTip1;
    }
}
