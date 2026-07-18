namespace MED
{
    partial class ProcessControl
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ProcessControl));
            cmdStart = new Button();
            cmdStop = new Button();
            chkPause = new CheckBox();
            SuspendLayout();
            // 
            // cmdStart
            // 
            cmdStart.Image = (Image)resources.GetObject("cmdStart.Image");
            cmdStart.Location = new Point(0, 0);
            cmdStart.Dock = DockStyle.Left;
            cmdStart.Name = "cmdStart";
            cmdStart.Size = new Size(32, 29);
            cmdStart.TabIndex = 0;
            cmdStart.UseVisualStyleBackColor = true;
            cmdStart.Click += cmdStart_Click;
            // 
            // chkPause
            // 
            chkPause.Appearance = Appearance.Button;
            chkPause.Dock = DockStyle.Right;
            chkPause.Image = (Image)resources.GetObject("chkPause.Image");
            chkPause.Location = new Point(33, 0);
            chkPause.Name = "chkPause";
            chkPause.Size = new Size(32, 29);
            chkPause.TabIndex = 1;
            chkPause.UseVisualStyleBackColor = true;
            chkPause.CheckedChanged += chkPause_CheckedChanged;
            // 
            // cmdStop
            // 
            cmdStop.Dock = DockStyle.Right;
            cmdStop.Image = (Image)resources.GetObject("cmdStop.Image");
            cmdStop.Location = new Point(65, 0);
            cmdStop.Name = "cmdStop";
            cmdStop.Size = new Size(32, 29);
            cmdStop.TabIndex = 2;
            cmdStop.UseVisualStyleBackColor = true;
            cmdStop.Click += cmdStop_Click;
            // 
            // ProcessControl
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(chkPause);
            Controls.Add(cmdStop);
            Controls.Add(cmdStart);
            Name = "ProcessControl";
            Size = new Size(97, 29);
            VisibleChanged += ProcessControl_VisibleChanged;
            ResumeLayout(false);
        }


        #endregion

        private Button cmdStart;
        private Button cmdStop;
        private CheckBox chkPause;
    }
}
