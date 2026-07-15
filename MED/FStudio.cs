using DynamicData;
using MED.EDJoystick;
using MED.EDWebCam;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MED
{
    public partial class FStudio : Form
    {
        private int childFormNumber = 0;

        public FStudio()
        {
            InitializeComponent();

            Current = this;
        }


        private void FStudio_Load(object sender, EventArgs e)
        {
            LoadSettings();
            LoadChilds();
        }

        private void FStudio_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveSettings();
        }

        private string SettingsSection
        {
            get
            {
                return this.GetType().Name;
            }
        }

        private void LoadSettings()
        {
            string settingsSection = this.SettingsSection;
            object v = Core.Settings.GetValue("Location", settingsSection, this.Location);
            this.Location = (Point)v;

            v = Core.Settings.GetValue("Size", settingsSection, this.Size);
            this.Size = (Size)v;

            this.WindowState = Enum.Parse<FormWindowState>(Core.Settings.GetValue("WindowState", settingsSection, this.WindowState).ToString());
        }

        private void SaveSettings()
        {
            string settingsSection = SettingsSection;
            if (this.WindowState == FormWindowState.Normal)
            {
                Core.Settings.SetValue("Location", settingsSection, this.Location);
                Core.Settings.SetValue("Size", settingsSection, this.Size);
            }
            Core.Settings.SetValue("WindowState", settingsSection, this.WindowState);

            FLogger.Current.SaveSettings();

            Core.Settings.Save();
        }

        public static FStudio Current { get; private set; }

        private void LoadChilds()
        {
            var f = new FLogger();
            f.MdiParent = this;
            f.Show();
            f.Dock = DockStyle.Bottom;
        }

        /*
         * 
         * 
         */

        private void ExitToolsStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveSettings();
            this.Close();
        }


        private void ShowNewForm(object sender, EventArgs e)
        {
            Form childForm = new Form();
            childForm.MdiParent = this;
            childForm.Text = "Fenêtre " + childFormNumber++;
            childForm.Show();
        }

        private void OpenFile(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            openFileDialog.Filter = "Fichiers texte (*.txt)|*.txt|Tous les fichiers (*.*)|*.*";
            if (openFileDialog.ShowDialog(this) == DialogResult.OK)
            {
                string FileName = openFileDialog.FileName;
            }
        }

        private void SaveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            saveFileDialog.Filter = "Fichiers texte (*.txt)|*.txt|Tous les fichiers (*.*)|*.*";
            if (saveFileDialog.ShowDialog(this) == DialogResult.OK)
            {
                string FileName = saveFileDialog.FileName;
            }
        }

        private void CutToolStripMenuItem_Click(object sender, EventArgs e)
        {
        }

        private void CopyToolStripMenuItem_Click(object sender, EventArgs e)
        {
        }

        private void PasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
        }

        private void ToolBarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            toolStrip.Visible = toolBarToolStripMenuItem.Checked;
        }

        private void StatusBarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            statusStrip.Visible = statusBarToolStripMenuItem.Checked;
        }

        private void CascadeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LayoutMdi(MdiLayout.Cascade);
        }

        private void TileVerticalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LayoutMdi(MdiLayout.TileVertical);
        }

        private void TileHorizontalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LayoutMdi(MdiLayout.TileHorizontal);
        }

        private void ArrangeIconsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LayoutMdi(MdiLayout.ArrangeIcons);
        }

        private void CloseAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Form childForm in MdiChildren)
            {
                childForm.Close();
            }
        }

        private void btnWebCam_Click(object sender, EventArgs e)
        {
            FWebCam form = new();
            form.MdiParent = this;
            form.Dock = DockStyle.Fill;
            form.Show();

        }

        private void btnJoystick_Click(object sender, EventArgs e)
        {
            FJoystick form = new();
            form.MdiParent = this;
            form.Dock = DockStyle.Fill;
            form.Show();
        }

        private void saveToolStripButton_Click(object sender, EventArgs e)
        {
            if (ActiveMdiChild is FWebCam)
            {
                ((FWebCam)ActiveMdiChild).SaveSettings();
                toolStripStatusLabel.Text = "WebCam enregistrée";
            }
            else
                toolStripStatusLabel.Text = "Rien à sauvegarder !";
        }
    }
}
