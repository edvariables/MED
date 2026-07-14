

namespace MED.EDWebCam
{
    partial class Form1
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            cboCameras = new ComboBox();
            rtbLog = new RichTextBox();
            splitterLog = new Splitter();
            panBottom = new Panel();
            chkLogColored = new CheckBox();
            chkVideoCaptureLogger = new CheckBox();
            chkRenderLogger = new CheckBox();
            picRender = new PictureBox();
            chkRun = new CheckBox();
            cmdSaveSettings = new Button();
            cmdNewForm = new Button();
            panTopTools = new Panel();
            panBottom.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)picRender).BeginInit();
            panTopTools.SuspendLayout();
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
            rtbLog.Location = new Point(0, 448);
            rtbLog.Margin = new Padding(3, 20, 3, 3);
            rtbLog.MaximumSize = new Size(0, 200);
            rtbLog.Name = "rtbLog";
            rtbLog.Size = new Size(925, 102);
            rtbLog.TabIndex = 3;
            rtbLog.Text = "";
            // 
            // splitterLog
            // 
            splitterLog.BackColor = SystemColors.ActiveCaption;
            splitterLog.Dock = DockStyle.Bottom;
            splitterLog.Location = new Point(0, 444);
            splitterLog.Margin = new Padding(3, 10, 3, 10);
            splitterLog.Name = "splitterLog";
            splitterLog.Size = new Size(925, 4);
            splitterLog.TabIndex = 8;
            splitterLog.TabStop = false;
            splitterLog.SplitterMoved += splitterLog_SplitterMoved;
            splitterLog.SplitterMoving += SplitterLog_SplitterMoving;
            // 
            // panBottom
            // 
            panBottom.Controls.Add(chkLogColored);
            panBottom.Controls.Add(chkVideoCaptureLogger);
            panBottom.Controls.Add(chkRenderLogger);
            panBottom.Dock = DockStyle.Bottom;
            panBottom.Location = new Point(0, 550);
            panBottom.Name = "panBottom";
            panBottom.Size = new Size(925, 23);
            panBottom.TabIndex = 7;
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
            chkLogColored.Location = new Point(894, 1);
            chkLogColored.Name = "chkLogColored";
            chkLogColored.Size = new Size(22, 22);
            chkLogColored.TabIndex = 5;
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
            chkVideoCaptureLogger.Location = new Point(795, 3);
            chkVideoCaptureLogger.Name = "chkVideoCaptureLogger";
            chkVideoCaptureLogger.Size = new Size(95, 19);
            chkVideoCaptureLogger.TabIndex = 5;
            chkVideoCaptureLogger.Text = "VideoCapture";
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
            chkRenderLogger.Location = new Point(729, 3);
            chkRenderLogger.Name = "chkRenderLogger";
            chkRenderLogger.Size = new Size(60, 19);
            chkRenderLogger.TabIndex = 5;
            chkRenderLogger.Text = "Render";
            chkRenderLogger.UseVisualStyleBackColor = false;
            chkRenderLogger.CheckedChanged += chkRenderLogger_CheckedChanged;
            // 
            // picRender
            // 
            picRender.BackColor = SystemColors.WindowFrame;
            picRender.Dock = DockStyle.Top;
            picRender.Location = new Point(0, 42);
            picRender.Margin = new Padding(20);
            picRender.Name = "picRender";
            picRender.Padding = new Padding(30);
            picRender.Size = new Size(925, 209);
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
            cmdSaveSettings.Location = new Point(864, 6);
            cmdSaveSettings.Name = "cmdSaveSettings";
            cmdSaveSettings.Size = new Size(52, 27);
            cmdSaveSettings.TabIndex = 6;
            cmdSaveSettings.Text = "Save";
            cmdSaveSettings.UseVisualStyleBackColor = true;
            cmdSaveSettings.Click += cmdSaveSettings_Click;
            // 
            // cmdNewForm
            // 
            cmdNewForm.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            cmdNewForm.Location = new Point(802, 6);
            cmdNewForm.Name = "cmdNewForm";
            cmdNewForm.Size = new Size(56, 25);
            cmdNewForm.TabIndex = 9;
            cmdNewForm.Text = "New";
            cmdNewForm.UseVisualStyleBackColor = true;
            cmdNewForm.Click += cmdNewForm_Click;
            // 
            // panTopTools
            // 
            panTopTools.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            panTopTools.BackColor = SystemColors.ControlLight;
            panTopTools.Controls.Add(cboCameras);
            panTopTools.Controls.Add(cmdNewForm);
            panTopTools.Controls.Add(chkRun);
            panTopTools.Controls.Add(cmdSaveSettings);
            panTopTools.Dock = DockStyle.Top;
            panTopTools.Location = new Point(0, 0);
            panTopTools.Name = "panTopTools";
            panTopTools.Size = new Size(925, 42);
            panTopTools.TabIndex = 10;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(925, 573);
            Controls.Add(picRender);
            Controls.Add(panTopTools);
            Controls.Add(splitterLog);
            Controls.Add(rtbLog);
            Controls.Add(panBottom);
            Name = "Form1";
            Text = "Form1";
            FormClosed += Form1_FormClosed;
            Load += Form1_Load;
            panBottom.ResumeLayout(false);
            panBottom.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)picRender).EndInit();
            panTopTools.ResumeLayout(false);
            panTopTools.PerformLayout();
            ResumeLayout(false);
        }


        #endregion
        private ComboBox cboCameras;
        private RichTextBox rtbLog;
        private Splitter splitterLog;
        private PictureBox picRender;
        private CheckBox chkRun;
        private CheckBox chkVideoCaptureLogger;
        private CheckBox chkRenderLogger;
        private Button cmdSaveSettings;
        private Panel panBottom;
        private CheckBox chkLogColored;
        private Button cmdNewForm;
        private Panel panTopTools;
    }
}
