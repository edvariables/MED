namespace MED
{
    partial class EventScriptForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(EventScriptForm));
            RTBEditor = new RichTextBox();
            statusStrip1 = new StatusStrip();
            dropDownVariables = new ToolStripDropDownButton();
            toolStripStatusLabel1 = new ToolStripStatusLabel();
            cmdSave = new ToolStripStatusLabel();
            cmdClose = new ToolStripStatusLabel();
            statusStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // RTBEditor
            // 
            RTBEditor.Dock = DockStyle.Fill;
            RTBEditor.Font = new Font("Cascadia Code", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            RTBEditor.Location = new Point(0, 0);
            RTBEditor.Name = "RTBEditor";
            RTBEditor.Size = new Size(800, 641);
            RTBEditor.TabIndex = 0;
            RTBEditor.Text = "";
            RTBEditor.TextChanged += Editor_TextChanged;
            RTBEditor.KeyUp += Editor_KeyUp;
            // 
            // statusStrip1
            // 
            statusStrip1.Items.AddRange(new ToolStripItem[] { dropDownVariables, toolStripStatusLabel1, cmdSave, cmdClose });
            statusStrip1.Location = new Point(0, 641);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.ShowItemToolTips = true;
            statusStrip1.Size = new Size(800, 25);
            statusStrip1.SizingGrip = false;
            statusStrip1.TabIndex = 1;
            // 
            // dropDownVariables
            // 
            dropDownVariables.Image = (Image)resources.GetObject("dropDownVariables.Image");
            dropDownVariables.ImageTransparentColor = Color.Magenta;
            dropDownVariables.Name = "dropDownVariables";
            dropDownVariables.Size = new Size(52, 23);
            dropDownVariables.Text = "var";
            dropDownVariables.ToolTipText = "Variables disponibles dans ce script";
            // 
            // toolStripStatusLabel1
            // 
            toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            toolStripStatusLabel1.Size = new Size(603, 20);
            toolStripStatusLabel1.Spring = true;
            // 
            // cmdSave
            // 
            cmdSave.BorderSides = ToolStripStatusLabelBorderSides.Left | ToolStripStatusLabelBorderSides.Top | ToolStripStatusLabelBorderSides.Right | ToolStripStatusLabelBorderSides.Bottom;
            cmdSave.Image = (Image)resources.GetObject("cmdSave.Image");
            cmdSave.Name = "cmdSave";
            cmdSave.Size = new Size(83, 20);
            cmdSave.Text = "Enregistrer";
            cmdSave.Click += cmdSave_Click;
            // 
            // cmdClose
            // 
            cmdClose.Image = (Image)resources.GetObject("cmdClose.Image");
            cmdClose.Name = "cmdClose";
            cmdClose.Size = new Size(16, 20);
            cmdClose.Click += CmdClose_Click;
            // 
            // EventScriptForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 666);
            Controls.Add(RTBEditor);
            Controls.Add(statusStrip1);
            FormBorderStyle = FormBorderStyle.SizableToolWindow;
            Name = "EventScriptForm";
            Text = "EventScriptForm";
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private RichTextBox RTBEditor;
        private StatusStrip statusStrip1;
        private ToolStripStatusLabel cmdSave;
        private ToolStripStatusLabel cmdClose;
        private ToolStripDropDownButton dropDownVariables;
        private ToolStripStatusLabel toolStripStatusLabel1;
    }
}