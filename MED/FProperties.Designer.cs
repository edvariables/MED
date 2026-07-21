namespace MED
{
    partial class FProperties
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
            propertiesControl1 = new PropertiesControl();
            SuspendLayout();
            // 
            // propertiesControl1
            // 
            propertiesControl1.CurrentProperty = null;
            propertiesControl1.Dock = DockStyle.Fill;
            propertiesControl1.Location = new Point(0, 0);
            propertiesControl1.Name = "propertiesControl1";
            propertiesControl1.Size = new Size(327, 812);
            propertiesControl1.TabIndex = 0;
            // 
            // FProperties
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(327, 812);
            ControlBox = false;
            Controls.Add(propertiesControl1);
            FormBorderStyle = FormBorderStyle.SizableToolWindow;
            Name = "FProperties";
            Text = "Propriétés";
            Load += FProperties_Load;
            ResumeLayout(false);
        }

        #endregion

        private PropertiesControl propertiesControl1;
    }
}