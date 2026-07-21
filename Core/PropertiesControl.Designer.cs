namespace MED
{
    partial class PropertiesControl
    {
        /// <summary> 
        /// Variable nécessaire au concepteur.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Nettoyage des ressources utilisées.
        /// </summary>
        /// <param name="disposing">true si les ressources managées doivent être supprimées ; sinon, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Code généré par le Concepteur de composants

        /// <summary> 
        /// Méthode requise pour la prise en charge du concepteur - ne modifiez pas 
        /// le contenu de cette méthode avec l'éditeur de code.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PropertiesControl));
            propertyGrid = new PropertyGrid();
            cboObjectsList = new ComboBox();
            panCboObjects = new Panel();
            cmdRefresh = new Button();
            processesControl1 = new ProcessesControl();
            splitContainer1 = new SplitContainer();
            panCboObjects.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            SuspendLayout();
            // 
            // propertyGrid
            // 
            propertyGrid.Dock = DockStyle.Fill;
            propertyGrid.Location = new Point(0, 23);
            propertyGrid.Margin = new Padding(3, 6, 3, 3);
            propertyGrid.Name = "propertyGrid";
            propertyGrid.Size = new Size(259, 296);
            propertyGrid.TabIndex = 3;
            // 
            // cboObjectsList
            // 
            cboObjectsList.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            cboObjectsList.DropDownStyle = ComboBoxStyle.DropDownList;
            cboObjectsList.FormattingEnabled = true;
            cboObjectsList.Location = new Point(0, 0);
            cboObjectsList.Name = "cboObjectsList";
            cboObjectsList.Size = new Size(235, 23);
            cboObjectsList.TabIndex = 1;
            cboObjectsList.SelectedIndexChanged += cboObjectsList_SelectedIndexChanged;
            // 
            // panCboObjects
            // 
            panCboObjects.Controls.Add(cboObjectsList);
            panCboObjects.Controls.Add(cmdRefresh);
            panCboObjects.Dock = DockStyle.Top;
            panCboObjects.Location = new Point(0, 0);
            panCboObjects.Margin = new Padding(3, 3, 3, 8);
            panCboObjects.Name = "panCboObjects";
            panCboObjects.Size = new Size(259, 23);
            panCboObjects.TabIndex = 4;
            // 
            // cmdRefresh
            // 
            cmdRefresh.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            cmdRefresh.Image = (Image)resources.GetObject("cmdRefresh.Image");
            cmdRefresh.Location = new Point(235, 0);
            cmdRefresh.Name = "cmdRefresh";
            cmdRefresh.Size = new Size(24, 23);
            cmdRefresh.TabIndex = 2;
            cmdRefresh.UseVisualStyleBackColor = true;
            cmdRefresh.Click += cmdRefresh_Click;
            // 
            // processesControl1
            // 
            processesControl1.Dock = DockStyle.Fill;
            processesControl1.ImageIndex = 0;
            processesControl1.Location = new Point(0, 0);
            processesControl1.Name = "processesControl1";
            processesControl1.SelectedImageIndex = 0;
            processesControl1.Size = new Size(259, 322);
            processesControl1.TabIndex = 5;
            processesControl1.BeforeSelect += ProcessesControl1_BeforeSelect;
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = DockStyle.Fill;
            splitContainer1.Location = new Point(0, 0);
            splitContainer1.Name = "splitContainer1";
            splitContainer1.Orientation = Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(processesControl1);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(propertyGrid);
            splitContainer1.Panel2.Controls.Add(panCboObjects);
            splitContainer1.Size = new Size(259, 645);
            splitContainer1.SplitterDistance = 322;
            splitContainer1.TabIndex = 6;
            // 
            // PropertiesControl
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(splitContainer1);
            Name = "PropertiesControl";
            Size = new Size(259, 645);
            panCboObjects.ResumeLayout(false);
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private PropertyGrid propertyGrid;
        private ComboBox cboObjectsList;
        private Panel panCboObjects;
        private Button cmdRefresh;
        private ProcessesControl processesControl1;
        private SplitContainer splitContainer1;
    }
}
