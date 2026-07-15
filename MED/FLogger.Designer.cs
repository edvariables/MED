namespace MED
{
    partial class FLogger
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FLogger));
            rtbLog = new RichTextBox();
            panBottom = new Panel();
            chkClearLogOnRun = new CheckBox();
            chkLogColored = new CheckBox();
            chkVideoCaptureLogger = new CheckBox();
            chkRenderLogger = new CheckBox();
            panBottom.SuspendLayout();
            SuspendLayout();
            // 
            // rtbLog
            // 
            rtbLog.BackColor = SystemColors.WindowText;
            rtbLog.Dock = DockStyle.Fill;
            rtbLog.Font = new Font("Consolas", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            rtbLog.ForeColor = SystemColors.Window;
            rtbLog.Location = new Point(0, 0);
            rtbLog.MaximumSize = new Size(0, 200);
            rtbLog.Name = "rtbLog";
            rtbLog.Size = new Size(1137, 110);
            rtbLog.TabIndex = 8;
            rtbLog.Text = "";
            // 
            // panBottom
            // 
            panBottom.Controls.Add(chkClearLogOnRun);
            panBottom.Controls.Add(chkLogColored);
            panBottom.Controls.Add(chkVideoCaptureLogger);
            panBottom.Controls.Add(chkRenderLogger);
            panBottom.Dock = DockStyle.Bottom;
            panBottom.Location = new Point(0, 110);
            panBottom.Name = "panBottom";
            panBottom.Size = new Size(1137, 26);
            panBottom.TabIndex = 9;
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
            chkClearLogOnRun.Location = new Point(1112, 0);
            chkClearLogOnRun.Name = "chkClearLogOnRun";
            chkClearLogOnRun.Size = new Size(22, 22);
            chkClearLogOnRun.TabIndex = 5;
            chkClearLogOnRun.UseVisualStyleBackColor = false;
            chkClearLogOnRun.CheckedChanged += chkClearLogOnRun_CheckedChanged;
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
            chkLogColored.Location = new Point(1084, 1);
            chkLogColored.Name = "chkLogColored";
            chkLogColored.Size = new Size(22, 22);
            chkLogColored.TabIndex = 5;
            chkLogColored.UseVisualStyleBackColor = false;
            chkLogColored.CheckedChanged += chkLogColored_CheckedChanged;
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
            chkVideoCaptureLogger.Location = new Point(983, 3);
            chkVideoCaptureLogger.Name = "chkVideoCaptureLogger";
            chkVideoCaptureLogger.Size = new Size(95, 19);
            chkVideoCaptureLogger.TabIndex = 5;
            chkVideoCaptureLogger.Text = "VideoCapture";
            chkVideoCaptureLogger.UseVisualStyleBackColor = false;
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
            chkRenderLogger.Location = new Point(917, 3);
            chkRenderLogger.Name = "chkRenderLogger";
            chkRenderLogger.Size = new Size(60, 19);
            chkRenderLogger.TabIndex = 5;
            chkRenderLogger.Text = "Render";
            chkRenderLogger.UseVisualStyleBackColor = false;
            // 
            // FLogger
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1137, 136);
            ControlBox = false;
            Controls.Add(rtbLog);
            Controls.Add(panBottom);
            FormBorderStyle = FormBorderStyle.None;
            Name = "FLogger";
            Text = "Traces et Performances";
            panBottom.ResumeLayout(false);
            panBottom.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private RichTextBox rtbLog;
        private Panel panBottom;
        private CheckBox chkClearLogOnRun;
        private CheckBox chkLogColored;
        private CheckBox chkVideoCaptureLogger;
        private CheckBox chkRenderLogger;
    }
}