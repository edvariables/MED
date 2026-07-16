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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FProperties));
            propertyGrid = new PropertyGrid();
            cboObjectsList = new ComboBox();
            panel1 = new Panel();
            cmdRefresh = new Button();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // propertyGrid
            // 
            propertyGrid.Dock = DockStyle.Fill;
            propertyGrid.Location = new Point(0, 23);
            propertyGrid.Margin = new Padding(3, 6, 3, 3);
            propertyGrid.Name = "propertyGrid";
            propertyGrid.Size = new Size(327, 789);
            propertyGrid.TabIndex = 0;
            // 
            // cboObjectsList
            // 
            cboObjectsList.Dock = DockStyle.Fill;
            cboObjectsList.DropDownStyle = ComboBoxStyle.DropDownList;
            cboObjectsList.FormattingEnabled = true;
            cboObjectsList.Location = new Point(0, 0);
            cboObjectsList.Name = "cboObjectsList";
            cboObjectsList.Size = new Size(303, 23);
            cboObjectsList.TabIndex = 1;
            cboObjectsList.SelectedIndexChanged += cboObjectsList_SelectedIndexChanged;
            // 
            // panel1
            // 
            panel1.Controls.Add(cboObjectsList);
            panel1.Controls.Add(cmdRefresh);
            panel1.Dock = DockStyle.Top;
            panel1.Location = new Point(0, 0);
            panel1.Margin = new Padding(3, 3, 3, 6);
            panel1.Name = "panel1";
            panel1.Size = new Size(327, 23);
            panel1.TabIndex = 2;
            // 
            // cmdRefresh
            // 
            cmdRefresh.Dock = DockStyle.Right;
            cmdRefresh.Image = (Image)resources.GetObject("cmdRefresh.Image");
            cmdRefresh.Location = new Point(303, 0);
            cmdRefresh.Name = "cmdRefresh";
            cmdRefresh.Size = new Size(24, 23);
            cmdRefresh.TabIndex = 2;
            cmdRefresh.UseVisualStyleBackColor = true;
            cmdRefresh.Click += cmdRefresh_Click;
            // 
            // FProperties
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(327, 812);
            ControlBox = false;
            Controls.Add(propertyGrid);
            Controls.Add(panel1);
            FormBorderStyle = FormBorderStyle.SizableToolWindow;
            Name = "FProperties";
            Text = "Propriétés";
            Load += FProperties_Load;
            panel1.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private PropertyGrid propertyGrid;
        private ComboBox cboObjectsList;
        private Panel panel1;
        private Button cmdRefresh;
    }
}