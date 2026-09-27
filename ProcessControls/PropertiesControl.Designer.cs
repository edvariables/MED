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
            contextMenuProcesses = new ContextMenuStrip(components);
            toolStripMenuProcAdd = new ToolStripMenuItem();
            toolStripMenuItemProcessEnabled = new ToolStripMenuItem();
            toolStripMenuProcRemove = new ToolStripMenuItem();
            toolStripMenuItem1 = new ToolStripSeparator();
            toolStripMenuItemMoveBefore = new ToolStripMenuItem();
            toolStripMenuItemMoveAfter = new ToolStripMenuItem();
            contextMenuAddProcess = new ContextMenuStrip(components);
            openFileDialog1 = new OpenFileDialog();
            panCboObjects.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            contextMenuProcesses.SuspendLayout();
            SuspendLayout();
            // 
            // propertyGrid
            // 
            propertyGrid.BackColor = SystemColors.Control;
            propertyGrid.Dock = DockStyle.Fill;
            propertyGrid.Location = new Point(0, 31);
            propertyGrid.Margin = new Padding(3, 8, 3, 4);
            propertyGrid.Name = "propertyGrid";
            propertyGrid.Size = new Size(296, 395);
            propertyGrid.TabIndex = 3;
            // 
            // cboObjectsList
            // 
            cboObjectsList.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            cboObjectsList.DisplayMember = "Key";
            cboObjectsList.DropDownStyle = ComboBoxStyle.DropDownList;
            cboObjectsList.FormattingEnabled = true;
            cboObjectsList.Location = new Point(0, 0);
            cboObjectsList.Margin = new Padding(3, 4, 3, 4);
            cboObjectsList.Name = "cboObjectsList";
            cboObjectsList.Size = new Size(268, 28);
            cboObjectsList.TabIndex = 1;
            cboObjectsList.ValueMember = "Value";
            cboObjectsList.SelectedIndexChanged += cboObjectsList_SelectedIndexChanged;
            // 
            // panCboObjects
            // 
            panCboObjects.Controls.Add(cboObjectsList);
            panCboObjects.Controls.Add(cmdRefresh);
            panCboObjects.Dock = DockStyle.Top;
            panCboObjects.Location = new Point(0, 0);
            panCboObjects.Margin = new Padding(3, 4, 3, 11);
            panCboObjects.Name = "panCboObjects";
            panCboObjects.Size = new Size(296, 31);
            panCboObjects.TabIndex = 4;
            // 
            // cmdRefresh
            // 
            cmdRefresh.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            cmdRefresh.Image = (Image)resources.GetObject("cmdRefresh.Image");
            cmdRefresh.Location = new Point(269, 0);
            cmdRefresh.Margin = new Padding(3, 4, 3, 4);
            cmdRefresh.Name = "cmdRefresh";
            cmdRefresh.Size = new Size(27, 31);
            cmdRefresh.TabIndex = 2;
            cmdRefresh.UseVisualStyleBackColor = true;
            cmdRefresh.Click += cmdRefresh_Click;
            // 
            // processesControl1
            // 
            processesControl1.Dock = DockStyle.Fill;
            processesControl1.HideSelection = false;
            processesControl1.ImageIndex = 0;
            processesControl1.Location = new Point(0, 0);
            processesControl1.Margin = new Padding(3, 4, 3, 4);
            processesControl1.Name = "processesControl1";
            processesControl1.SelectedImageIndex = 0;
            processesControl1.Size = new Size(296, 429);
            processesControl1.TabIndex = 5;
            processesControl1.BeforeSelect += ProcessesControl1_BeforeSelect;
            processesControl1.NodeMouseClick += processesControl1_NodeMouseClick;
            processesControl1.NodeMouseDoubleClick += ProcessesControl1_NodeMouseDoubleClick;
            processesControl1.MouseClick += processesControl1_MouseClick;
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = DockStyle.Fill;
            splitContainer1.Location = new Point(0, 0);
            splitContainer1.Margin = new Padding(3, 4, 3, 4);
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
            splitContainer1.Size = new Size(296, 860);
            splitContainer1.SplitterDistance = 429;
            splitContainer1.SplitterWidth = 5;
            splitContainer1.TabIndex = 6;
            // 
            // contextMenuProcesses
            // 
            contextMenuProcesses.ImageScalingSize = new Size(20, 20);
            contextMenuProcesses.Items.AddRange(new ToolStripItem[] { toolStripMenuProcAdd, toolStripMenuItemProcessEnabled, toolStripMenuProcRemove, toolStripMenuItem1, toolStripMenuItemMoveBefore, toolStripMenuItemMoveAfter });
            contextMenuProcesses.Name = "contextMenuProcesses";
            contextMenuProcesses.Size = new Size(215, 140);
            contextMenuProcesses.Text = "Processes";
            // 
            // toolStripMenuProcAdd
            // 
            toolStripMenuProcAdd.Image = (Image)resources.GetObject("toolStripMenuProcAdd.Image");
            toolStripMenuProcAdd.Name = "toolStripMenuProcAdd";
            toolStripMenuProcAdd.Size = new Size(214, 26);
            toolStripMenuProcAdd.Text = "Ajouter un process...";
            toolStripMenuProcAdd.Click += toolStripMenuProcAdd_Click;
            // 
            // toolStripMenuItemProcessEnabled
            // 
            toolStripMenuItemProcessEnabled.Image = (Image)resources.GetObject("toolStripMenuItemProcessEnabled.Image");
            toolStripMenuItemProcessEnabled.Name = "toolStripMenuItemProcessEnabled";
            toolStripMenuItemProcessEnabled.Size = new Size(214, 26);
            toolStripMenuItemProcessEnabled.Text = "Process actif";
            toolStripMenuItemProcessEnabled.Click += toolStripMenuItemProcessEnabled_Click;
            // 
            // toolStripMenuProcRemove
            // 
            toolStripMenuProcRemove.Image = (Image)resources.GetObject("toolStripMenuProcRemove.Image");
            toolStripMenuProcRemove.Name = "toolStripMenuProcRemove";
            toolStripMenuProcRemove.Size = new Size(214, 26);
            toolStripMenuProcRemove.Text = "Supprimer...";
            toolStripMenuProcRemove.Click += toolStripMenuProcRemove_Click;
            // 
            // toolStripMenuItem1
            // 
            toolStripMenuItem1.Name = "toolStripMenuItem1";
            toolStripMenuItem1.Size = new Size(211, 6);
            // 
            // toolStripMenuItemMoveBefore
            // 
            toolStripMenuItemMoveBefore.Image = (Image)resources.GetObject("toolStripMenuItemMoveBefore.Image");
            toolStripMenuItemMoveBefore.Name = "toolStripMenuItemMoveBefore";
            toolStripMenuItemMoveBefore.Size = new Size(214, 26);
            toolStripMenuItemMoveBefore.Text = "Avant";
            toolStripMenuItemMoveBefore.Click += toolStripMenuItemMoveBefore_Click;
            // 
            // toolStripMenuItemMoveAfter
            // 
            toolStripMenuItemMoveAfter.Image = (Image)resources.GetObject("toolStripMenuItemMoveAfter.Image");
            toolStripMenuItemMoveAfter.Name = "toolStripMenuItemMoveAfter";
            toolStripMenuItemMoveAfter.Size = new Size(214, 26);
            toolStripMenuItemMoveAfter.Text = "Après";
            toolStripMenuItemMoveAfter.Click += toolStripMenuItemMoveAfter_Click;
            // 
            // contextMenuAddProcess
            // 
            contextMenuAddProcess.ImageScalingSize = new Size(20, 20);
            contextMenuAddProcess.Name = "contextMenuAddProcess";
            contextMenuAddProcess.Size = new Size(61, 4);
            // 
            // openFileDialog1
            // 
            openFileDialog1.FileName = "openFileDialog1";
            // 
            // PropertiesControl
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(splitContainer1);
            Margin = new Padding(3, 4, 3, 4);
            Name = "PropertiesControl";
            Size = new Size(296, 860);
            panCboObjects.ResumeLayout(false);
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            contextMenuProcesses.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private PropertyGrid propertyGrid;
        private ComboBox cboObjectsList;
        private Panel panCboObjects;
        private Button cmdRefresh;
        private ProcessesControl processesControl1;
        private SplitContainer splitContainer1;
        private ContextMenuStrip contextMenuProcesses;
        private ToolStripMenuItem toolStripMenuProcAdd;
        private ToolStripMenuItem toolStripMenuProcRemove;
        private ContextMenuStrip contextMenuAddProcess;
        private ToolStripSeparator toolStripMenuItem1;
        private ToolStripMenuItem toolStripMenuItemMoveBefore;
        private ToolStripMenuItem toolStripMenuItemMoveAfter;
        private ToolStripMenuItem toolStripMenuItemProcessEnabled;
        private OpenFileDialog openFileDialog1;
    }
}
