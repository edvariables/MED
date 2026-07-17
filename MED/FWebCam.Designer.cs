

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
            picRender = new PictureBox();
            chkRun = new CheckBox();
            panTopTools = new Panel();
            cboCaptureSize = new ComboBox();
            tableLayoutPan = new TableLayoutPanel();
            toolTip1 = new ToolTip(components);
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
            // picRender
            // 
            picRender.BackColor = SystemColors.WindowFrame;
            tableLayoutPan.SetColumnSpan(picRender, 2);
            picRender.Dock = DockStyle.Fill;
            picRender.Location = new Point(20, 67);
            picRender.Margin = new Padding(20);
            picRender.Name = "picRender";
            picRender.Padding = new Padding(30);
            picRender.Size = new Size(885, 616);
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
            // panTopTools
            // 
            panTopTools.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            panTopTools.BackColor = SystemColors.ControlLight;
            tableLayoutPan.SetColumnSpan(panTopTools, 2);
            panTopTools.Controls.Add(cboCaptureSize);
            panTopTools.Controls.Add(cboCameras);
            panTopTools.Controls.Add(chkRun);
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
            tableLayoutPan.Dock = DockStyle.Fill;
            tableLayoutPan.Location = new Point(0, 0);
            tableLayoutPan.Name = "tableLayoutPan";
            tableLayoutPan.RowCount = 5;
            tableLayoutPan.RowStyles.Add(new RowStyle());
            tableLayoutPan.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPan.RowStyles.Add(new RowStyle());
            tableLayoutPan.RowStyles.Add(new RowStyle());
            tableLayoutPan.RowStyles.Add(new RowStyle());
            tableLayoutPan.Size = new Size(925, 703);
            tableLayoutPan.TabIndex = 11;
            // 
            // FWebCam
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(925, 703);
            Controls.Add(tableLayoutPan);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "FWebCam";
            Text = "WebCam";
            Activated += FWebCam_Activated;
            FormClosed += Form1_FormClosed;
            Load += Form1_Load;
            ((System.ComponentModel.ISupportInitialize)picRender).EndInit();
            panTopTools.ResumeLayout(false);
            panTopTools.PerformLayout();
            tableLayoutPan.ResumeLayout(false);
            ResumeLayout(false);
        }



        #endregion
        private ComboBox cboCameras;
        private PictureBox picRender;
        private CheckBox chkRun;
        private Panel panTopTools;
        private TableLayoutPanel tableLayoutPan;
        private ComboBox cboCaptureSize;
        private ToolTip toolTip1;
    }
}
