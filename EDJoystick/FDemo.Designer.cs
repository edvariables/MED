// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.



namespace MED.EDJoystick
{
    partial class FDemo
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FDemo));
            tableLayoutPanel1 = new TableLayoutPanel();
            picRender = new PictureBox();
            txtLog = new RichTextBox();
            chkRun = new CheckBox();
            lvwJoystickControls = new ListView();
            columnHeader1 = new ColumnHeader();
            columnHeader2 = new ColumnHeader();
            imageListEDV = new ImageList(components);
            panel1 = new Panel();
            cboJoystickConfig = new ComboBox();
            cboUsages = new ComboBox();
            button1 = new Button();
            button2 = new Button();
            tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)picRender).BeginInit();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 2;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15.3443117F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 84.6556854F));
            tableLayoutPanel1.Controls.Add(picRender, 1, 1);
            tableLayoutPanel1.Controls.Add(txtLog, 1, 2);
            tableLayoutPanel1.Controls.Add(chkRun, 0, 0);
            tableLayoutPanel1.Controls.Add(lvwJoystickControls, 0, 1);
            tableLayoutPanel1.Controls.Add(panel1, 1, 0);
            tableLayoutPanel1.Dock = DockStyle.Fill;
            tableLayoutPanel1.Location = new Point(0, 0);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 4;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 8.037825F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 91.96217F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 286F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            tableLayoutPanel1.Size = new Size(1336, 730);
            tableLayoutPanel1.TabIndex = 0;
            // 
            // picRender
            // 
            picRender.Dock = DockStyle.Fill;
            picRender.Location = new Point(208, 37);
            picRender.Name = "picRender";
            picRender.Size = new Size(1125, 383);
            picRender.TabIndex = 0;
            picRender.TabStop = false;
            // 
            // txtLog
            // 
            txtLog.BackColor = SystemColors.WindowText;
            txtLog.Dock = DockStyle.Fill;
            txtLog.ForeColor = SystemColors.Window;
            txtLog.Location = new Point(208, 426);
            txtLog.Name = "txtLog";
            txtLog.Size = new Size(1125, 280);
            txtLog.TabIndex = 2;
            txtLog.Text = "";
            // 
            // chkRun
            // 
            chkRun.Appearance = Appearance.Button;
            chkRun.AutoSize = true;
            chkRun.Dock = DockStyle.Fill;
            chkRun.FlatAppearance.CheckedBackColor = Color.FromArgb(192, 255, 192);
            chkRun.FlatStyle = FlatStyle.Flat;
            chkRun.Location = new Point(3, 3);
            chkRun.Name = "chkRun";
            chkRun.Size = new Size(199, 28);
            chkRun.TabIndex = 3;
            chkRun.Text = "Activer";
            chkRun.TextAlign = ContentAlignment.MiddleCenter;
            chkRun.UseVisualStyleBackColor = true;
            chkRun.CheckedChanged += chkRun_CheckedChanged;
            // 
            // lvwJoystickControls
            // 
            lvwJoystickControls.Columns.AddRange(new ColumnHeader[] { columnHeader1, columnHeader2 });
            lvwJoystickControls.Dock = DockStyle.Fill;
            lvwJoystickControls.GridLines = true;
            lvwJoystickControls.Location = new Point(3, 37);
            lvwJoystickControls.Name = "lvwJoystickControls";
            tableLayoutPanel1.SetRowSpan(lvwJoystickControls, 2);
            lvwJoystickControls.Size = new Size(199, 669);
            lvwJoystickControls.SmallImageList = imageListEDV;
            lvwJoystickControls.StateImageList = imageListEDV;
            lvwJoystickControls.TabIndex = 4;
            lvwJoystickControls.UseCompatibleStateImageBehavior = false;
            lvwJoystickControls.View = View.Details;
            lvwJoystickControls.Invalidated += LvwJoystickControls_Invalidated;
            // 
            // columnHeader1
            // 
            columnHeader1.Text = "Control";
            columnHeader1.Width = 100;
            // 
            // columnHeader2
            // 
            columnHeader2.Text = "Value";
            columnHeader2.Width = 90;
            // 
            // imageListEDV
            // 
            imageListEDV.ColorDepth = ColorDepth.Depth32Bit;
            imageListEDV.ImageStream = (ImageListStreamer)resources.GetObject("imageListEDV.ImageStream");
            imageListEDV.TransparentColor = Color.Transparent;
            imageListEDV.Images.SetKeyName(0, "Add");
            imageListEDV.Images.SetKeyName(1, "Array");
            imageListEDV.Images.SetKeyName(2, "Boolean");
            imageListEDV.Images.SetKeyName(3, "Button");
            imageListEDV.Images.SetKeyName(4, "Client");
            imageListEDV.Images.SetKeyName(5, "Code");
            imageListEDV.Images.SetKeyName(6, "Color");
            imageListEDV.Images.SetKeyName(7, "Component");
            imageListEDV.Images.SetKeyName(8, "DataTable");
            imageListEDV.Images.SetKeyName(9, "DateTime");
            imageListEDV.Images.SetKeyName(10, "Dom");
            imageListEDV.Images.SetKeyName(11, "DomSys");
            imageListEDV.Images.SetKeyName(12, "EDV");
            imageListEDV.Images.SetKeyName(13, "False");
            imageListEDV.Images.SetKeyName(14, "File");
            imageListEDV.Images.SetKeyName(15, "Function");
            imageListEDV.Images.SetKeyName(16, "Image");
            imageListEDV.Images.SetKeyName(17, "LibVar");
            imageListEDV.Images.SetKeyName(18, "Link");
            imageListEDV.Images.SetKeyName(19, "Minus");
            imageListEDV.Images.SetKeyName(20, "Name");
            imageListEDV.Images.SetKeyName(21, "Null");
            imageListEDV.Images.SetKeyName(22, "Num");
            imageListEDV.Images.SetKeyName(23, "Object");
            imageListEDV.Images.SetKeyName(24, "Password");
            imageListEDV.Images.SetKeyName(25, "Plus");
            imageListEDV.Images.SetKeyName(26, "Print");
            imageListEDV.Images.SetKeyName(27, "Script");
            imageListEDV.Images.SetKeyName(28, "Selection");
            imageListEDV.Images.SetKeyName(29, "String");
            imageListEDV.Images.SetKeyName(30, "This");
            imageListEDV.Images.SetKeyName(31, "True");
            imageListEDV.Images.SetKeyName(32, "Type");
            imageListEDV.Images.SetKeyName(33, "User");
            imageListEDV.Images.SetKeyName(34, "Var");
            imageListEDV.Images.SetKeyName(35, "varAutoReset");
            imageListEDV.Images.SetKeyName(36, "VarReadOnly");
            imageListEDV.Images.SetKeyName(37, "Visual");
            imageListEDV.Images.SetKeyName(38, "VisualTrue");
            imageListEDV.Images.SetKeyName(39, "Web");
            // 
            // panel1
            // 
            panel1.Controls.Add(cboJoystickConfig);
            panel1.Controls.Add(cboUsages);
            panel1.Controls.Add(button1);
            panel1.Controls.Add(button2);
            panel1.Dock = DockStyle.Fill;
            panel1.Location = new Point(208, 3);
            panel1.Name = "panel1";
            panel1.Size = new Size(1125, 28);
            panel1.TabIndex = 7;
            // 
            // cboJoystickConfig
            // 
            cboJoystickConfig.DropDownStyle = ComboBoxStyle.DropDownList;
            cboJoystickConfig.FormattingEnabled = true;
            cboJoystickConfig.Items.AddRange(new object[] { "1 : Clavier", "2 : Clavier / Hook", "3 : Joystick", "4 : Joystick #2", "5 : Joystick #3" });
            cboJoystickConfig.Location = new Point(3, 2);
            cboJoystickConfig.Name = "cboJoystickConfig";
            cboJoystickConfig.Size = new Size(148, 23);
            cboJoystickConfig.TabIndex = 8;
            cboJoystickConfig.SelectedIndexChanged += this.cboJoystickConfig_SelectedIndexChanged;
            // 
            // cboUsages
            // 
            cboUsages.DropDownStyle = ComboBoxStyle.DropDownList;
            cboUsages.FormattingEnabled = true;
            cboUsages.Items.AddRange(new object[] { "(tous)", "X", "Y", "Z", "Select", "Start" });
            cboUsages.Location = new Point(157, 3);
            cboUsages.Name = "cboUsages";
            cboUsages.Size = new Size(69, 23);
            cboUsages.TabIndex = 7;
            cboUsages.SelectedIndexChanged += cboUsages_SelectedIndexChanged;
            // 
            // button1
            // 
            button1.Location = new Point(232, 2);
            button1.Name = "button1";
            button1.Size = new Size(107, 24);
            button1.TabIndex = 5;
            button1.Text = "Même joy";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // button2
            // 
            button2.Location = new Point(345, 2);
            button2.Name = "button2";
            button2.Size = new Size(107, 24);
            button2.TabIndex = 6;
            button2.Text = "New joy";
            button2.UseVisualStyleBackColor = true;
            button2.Click += button2_Click;
            // 
            // FMain
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1336, 730);
            Controls.Add(tableLayoutPanel1);
            Name = "FDemo";
            Text = "EDJoystick";
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)picRender).EndInit();
            panel1.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private TableLayoutPanel tableLayoutPanel1;
        private PictureBox picRender;
        private RichTextBox txtLog;
        private CheckBox chkRun;
        private ListView lvwJoystickControls;
        private ColumnHeader columnHeader1;
        private ColumnHeader columnHeader2;
        private ImageList imageListEDV;
        private Button button1;
        private Button button2;
        private Panel panel1;
        private ComboBox cboUsages;
        private ComboBox cboJoystickConfig;
    }
}