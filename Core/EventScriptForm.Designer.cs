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
            RichEditor = new RichTextBoxMED();
            statusStrip = new StatusStrip();
            dropDownVariables = new ToolStripDropDownButton();
            cmdPlayTest = new ToolStripStatusLabel();
            statusStripItem = new ToolStripStatusLabel();
            cmdSave = new ToolStripStatusLabel();
            cmdClose = new ToolStripStatusLabel();
            LineNumberTextBox = new RichTextBox();
            statusStrip.SuspendLayout();
            SuspendLayout();
            // 
            // RTBEditor
            // 
            RichEditor.AcceptsTab = true;
            RichEditor.BackColor = Color.Black;
            RichEditor.BorderStyle = BorderStyle.None;
            RichEditor.CodeColors = (Dictionary<string, Color>)resources.GetObject("RTBEditor.CodeColors");
            RichEditor.Dock = DockStyle.Fill;
            RichEditor.EventScript = null;
            RichEditor.Font = new Font("Cascadia Code", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            RichEditor.ForeColor = Color.White;
            RichEditor.LineNumbersTextBox = null;
            RichEditor.Location = new Point(34, 0);
            RichEditor.Name = "RTBEditor";
            RichEditor.ScrollBars = RichTextBoxScrollBars.ForcedBoth;
            RichEditor.ShowSelectionMargin = true;
            RichEditor.Size = new Size(766, 641);
            RichEditor.TabIndex = 0;
            RichEditor.Text = "";
            RichEditor.WordWrap = false;
            RichEditor.KeyUp += Editor_KeyUp;
            // 
            // statusStrip
            // 
            statusStrip.Items.AddRange(new ToolStripItem[] { dropDownVariables, cmdPlayTest, statusStripItem, cmdSave, cmdClose });
            statusStrip.Location = new Point(0, 641);
            statusStrip.Name = "statusStrip";
            statusStrip.ShowItemToolTips = true;
            statusStrip.Size = new Size(800, 25);
            statusStrip.SizingGrip = false;
            statusStrip.TabIndex = 1;
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
            // cmdPlayTest
            // 
            cmdPlayTest.Image = (Image)resources.GetObject("cmdPlayTest.Image");
            cmdPlayTest.Name = "cmdPlayTest";
            cmdPlayTest.Size = new Size(42, 20);
            cmdPlayTest.Text = "test";
            cmdPlayTest.IsLink = true;
            cmdPlayTest.Click += cmdPlayTest_Click;
            // 
            // statusStripItem
            // 
            statusStripItem.Name = "statusStripItem";
            statusStripItem.Size = new Size(561, 20);
            statusStripItem.Spring = true;
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
            // LineNumberTextBox
            // 
            LineNumberTextBox.BorderStyle = BorderStyle.None;
            LineNumberTextBox.Dock = DockStyle.Left;
            LineNumberTextBox.Location = new Point(0, 0);
            LineNumberTextBox.Name = "LineNumberTextBox";
            LineNumberTextBox.Size = new Size(34, 641);
            LineNumberTextBox.TabIndex = 2;
            LineNumberTextBox.Text = "";
            // 
            // EventScriptForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 666);
            Controls.Add(RichEditor);
            Controls.Add(LineNumberTextBox);
            Controls.Add(statusStrip);
            FormBorderStyle = FormBorderStyle.SizableToolWindow;
            Name = "EventScriptForm";
            Text = "EventScriptForm";
            statusStrip.ResumeLayout(false);
            statusStrip.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private RichTextBoxMED RichEditor;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel cmdSave;
        private ToolStripStatusLabel cmdClose;
        private ToolStripDropDownButton dropDownVariables;
        private ToolStripStatusLabel statusStripItem;
        private RichTextBox LineNumberTextBox;
        private ToolStripStatusLabel cmdPlayTest;
    }
}