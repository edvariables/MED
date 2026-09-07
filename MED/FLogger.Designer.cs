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
            RTGBAppendRegex = null;
            Performance = null;
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FLogger));
            rtbLog = new RichTextBox();
            panBottom = new Panel();
            cmdLastErrors = new Button();
            cmdSave = new Button();
            lblProgressMessage = new Label();
            chkClearLogOnRun = new CheckBox();
            chkLogColored = new CheckBox();
            saveFileDialog1 = new SaveFileDialog();
            toolTip1 = new ToolTip(components);
            chkEnabled = new CheckBox();
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
            rtbLog.Name = "rtbLog";
            rtbLog.Size = new Size(1137, 110);
            rtbLog.TabIndex = 8;
            rtbLog.Text = "";
            rtbLog.KeyUp += RtbLog_KeyUp;
            // 
            // panBottom
            // 
            panBottom.Controls.Add(chkEnabled);
            panBottom.Controls.Add(cmdLastErrors);
            panBottom.Controls.Add(cmdSave);
            panBottom.Controls.Add(lblProgressMessage);
            panBottom.Controls.Add(chkClearLogOnRun);
            panBottom.Controls.Add(chkLogColored);
            panBottom.Dock = DockStyle.Bottom;
            panBottom.Location = new Point(0, 110);
            panBottom.Name = "panBottom";
            panBottom.Size = new Size(1137, 26);
            panBottom.TabIndex = 9;
            // 
            // cmdLastErrors
            // 
            cmdLastErrors.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            cmdLastErrors.FlatAppearance.BorderSize = 0;
            cmdLastErrors.FlatStyle = FlatStyle.Flat;
            cmdLastErrors.Image = (Image)resources.GetObject("cmdLastErrors.Image");
            cmdLastErrors.ImageAlign = ContentAlignment.BottomLeft;
            cmdLastErrors.Location = new Point(970, 3);
            cmdLastErrors.Name = "cmdLastErrors";
            cmdLastErrors.Size = new Size(45, 21);
            cmdLastErrors.TabIndex = 8;
            cmdLastErrors.Text = "0";
            cmdLastErrors.TextAlign = ContentAlignment.MiddleRight;
            toolTip1.SetToolTip(cmdLastErrors, "Last errors");
            cmdLastErrors.UseVisualStyleBackColor = true;
            cmdLastErrors.Click += cmdLastErrors_Click;
            // 
            // cmdSave
            // 
            cmdSave.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            cmdSave.FlatAppearance.BorderSize = 0;
            cmdSave.FlatStyle = FlatStyle.Flat;
            cmdSave.Image = (Image)resources.GetObject("cmdSave.Image");
            cmdSave.Location = new Point(1054, 2);
            cmdSave.Name = "cmdSave";
            cmdSave.Size = new Size(27, 20);
            cmdSave.TabIndex = 7;
            toolTip1.SetToolTip(cmdSave, "Save to file. +Ctrl = select a file name. +Shift : Save and open log file in external editor.");
            cmdSave.UseVisualStyleBackColor = true;
            cmdSave.Click += cmdSave_Click;
            // 
            // lblProgressMessage
            // 
            lblProgressMessage.AutoSize = true;
            lblProgressMessage.Location = new Point(3, 4);
            lblProgressMessage.Name = "lblProgressMessage";
            lblProgressMessage.Size = new Size(16, 15);
            lblProgressMessage.TabIndex = 6;
            lblProgressMessage.Text = "...";
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
            chkClearLogOnRun.FlatAppearance.CheckedBackColor = Color.FromArgb(255, 255, 192);
            chkClearLogOnRun.FlatStyle = FlatStyle.Flat;
            chkClearLogOnRun.ForeColor = SystemColors.ControlText;
            chkClearLogOnRun.Image = (Image)resources.GetObject("chkClearLogOnRun.Image");
            chkClearLogOnRun.Location = new Point(1112, 0);
            chkClearLogOnRun.Name = "chkClearLogOnRun";
            chkClearLogOnRun.Size = new Size(22, 22);
            chkClearLogOnRun.TabIndex = 5;
            toolTip1.SetToolTip(chkClearLogOnRun, "Check to clear log when process start. Double-click to clear now.");
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
            // chkEnabled
            // 
            chkEnabled.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            chkEnabled.Appearance = Appearance.Button;
            chkEnabled.AutoSize = true;
            chkEnabled.BackColor = SystemColors.Control;
            chkEnabled.Checked = true;
            chkEnabled.CheckState = CheckState.Checked;
            chkEnabled.FlatAppearance.BorderSize = 0;
            chkEnabled.FlatAppearance.CheckedBackColor = Color.FromArgb(255, 255, 192);
            chkEnabled.FlatStyle = FlatStyle.Flat;
            chkEnabled.ForeColor = SystemColors.ControlText;
            chkEnabled.Image = (Image)resources.GetObject("chkEnabled.Image");
            chkEnabled.Location = new Point(1026, 1);
            chkEnabled.Name = "chkEnabled";
            chkEnabled.Size = new Size(22, 22);
            chkEnabled.TabIndex = 9;
            toolTip1.SetToolTip(chkEnabled, "Check to enable logger");
            chkEnabled.UseVisualStyleBackColor = false;
            chkEnabled.CheckedChanged += chkEnabled_CheckedChanged;
            // 
            // FLogger
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1137, 136);
            ControlBox = false;
            Controls.Add(rtbLog);
            Controls.Add(panBottom);
            FormBorderStyle = FormBorderStyle.SizableToolWindow;
            Name = "FLogger";
            ShowInTaskbar = false;
            Text = "Traces et Performances";
            Activated += FLogger_Activated;
            panBottom.ResumeLayout(false);
            panBottom.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private RichTextBox rtbLog;
        private Panel panBottom;
        private CheckBox chkClearLogOnRun;
        private CheckBox chkLogColored;
        private Label lblProgressMessage;
        private Button cmdSave;
        private SaveFileDialog saveFileDialog1;
        private ToolTip toolTip1;
        private Button cmdLastErrors;
        private CheckBox chkEnabled;
    }
}