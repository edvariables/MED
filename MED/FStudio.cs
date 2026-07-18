using DynamicData;
using MED.EDJoystick;
using MED.EDWebCam;
using MotionDetectionWinFormsApp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
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
        public static FStudio Current { get; private set; }

        public FStudio()
        {
            InitializeComponent();

            ActiveProcessChanged(null);

            Current = this;
        }


        private void FStudio_Load(object sender, EventArgs e)
        {
            LoadSettings();

            Init_ProcessorTypes();

            LoadChilds();

            //MotionDetectionForm f = new();
            //f.Show();
        }

        private void FStudio_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveSettings();
        }

        /**
         * Settings
         * */
        #region Settings
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
            Core.Settings.ClearCache(true, true, settingsSection);

            object v = Core.Settings.GetValue("Location", settingsSection, this.Location);
            this.Location = (Point)v;

            v = Core.Settings.GetValue("Size", settingsSection, this.Size);
            this.Size = (Size)v;

            EnsureFormLocationAndSize();

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

            Core.Settings.SetValue("FProperties.Width", settingsSection, FProperties.Current.Width);

            FLogger.Current.SaveSettings();

            Core.Settings.Save();
        }

        private void EnsureFormLocationAndSize()
        {
            var screen = Screen.FromHandle(this.Handle);
            if (screen == null)
                screen = Screen.PrimaryScreen;
            if (this.Width >= screen.WorkingArea.Width)
                this.Width = screen.WorkingArea.Width;
            if (this.Height >= screen.WorkingArea.Height)
                this.Width = screen.WorkingArea.Height;

            if (this.Left >= screen.WorkingArea.Width)
                this.Left = Math.Max(0, screen.WorkingArea.Right - this.Size.Width);

            if (this.Top >= screen.WorkingArea.Width)
                this.Top = Math.Max(0, screen.WorkingArea.Top - this.Size.Height);
        }
        #endregion


        private void LoadChilds()
        {
            Form f = new FLogger();
            f.MdiParent = this;
            f.Dock = DockStyle.Bottom;

            f = new FProperties();
            f.MdiParent = this;
            f.Width = (int)(Core.Settings.GetValue("FProperties.Width", SettingsSection, f.Width));
            f.Dock = DockStyle.Right;

            FProperties.Current.Show();
            FLogger.Current.Show();
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
            ProcessForm form = GetProcessorForm(typeof(FWebCam));
            form.Activate();
        }

        private void btnJoystick_Click(object sender, EventArgs e)
        {
            ProcessForm form = GetProcessorForm(typeof(FJoystick));
            form.Activate();
        }


        #region Processes
        public List<Type> ProcessorTypes;
        void Init_ProcessorTypes()
        {
            ProcessorTypes = new();
            ProcessorTypes.Add(typeof(FWebCam));
            ProcessorTypes.Add(typeof(FJoystick));
        }
        public List<IProcess> Processors = new();
        public void CleanProcessors()
        {
            foreach (var proc in Processors.ToArray())
            {
                if (proc is ProcessForm && (proc as ProcessForm).IsDisposed)
                    Processors.Remove(proc);
                else if (proc is Process && (proc as Process).IsDisposed)
                    Processors.Remove(proc);
            }
        }
        public ProcessForm GetProcessorForm(Type type)
        {
            CleanProcessors();

            //Existing
            foreach (var proc in Processors)
            {
                if (proc.GetType().Equals(type))
                    if (proc is ProcessForm)
                        return (ProcessForm)proc;
                    else 
                        throw new Exception($"{type.Name} is not a ProcessForm type");
            }

            //CreateInstance
            try
            {
                IProcess proc = (IProcess)Activator.CreateInstance(type);
                if (proc is ProcessForm)
                {
                    Processors.Add(proc);

                    ProcessForm form = (ProcessForm)proc;
                    form.MdiParent = this;
                    form.Dock = DockStyle.Fill;
                    form.Show();
                    form.ProcessStateChanged += ProcessStateChanged;
                    return form;
                }

                throw new Exception($"{type.Name} is not a ProcessForm type");
            }
            catch
            {
                throw new Exception($"{type.Name} is not a Form type");
            }
            //return null;
        }

        private IProcess _active_Process;
        public IProcess ActiveProcess
        {
            get
            {
                if (this.ActiveMdiChild is IProcess)
                {
                    if (this.ActiveMdiChild is ProcessForm && (this.ActiveMdiChild as ProcessForm).IsDisposed)
                        return _active_Process = null;
                    return _active_Process = (this.ActiveMdiChild as IProcess);
                }

                if (_active_Process is ProcessForm && (_active_Process as ProcessForm).IsDisposed)
                    return _active_Process = null;
                return _active_Process;
            }
            set
            {
                _active_Process = value;
                if (_active_Process != null)
                    if (_active_Process is Form)
                        (_active_Process as Form).Activate();
                ActiveProcessChanged(_active_Process);
            }
        }
        private void FStudio_MdiChildActivate(object sender, EventArgs e)
        {
            if (this.ActiveMdiChild is IProcess)
                ActiveProcess = (IProcess)this.ActiveMdiChild;
        }
        private void ActiveProcessChanged(IProcess sender, System.Threading.ThreadState state = System.Threading.ThreadState.Unstarted)
        {
            if (sender == null)
            {
                btnProcessStart.Enabled = false;
                btnProcessPause.Enabled = false;
                btnProcessPause.Checked = false;
                btnProcessStop.Enabled = false;
                return;
            }

            if (state == System.Threading.ThreadState.Unstarted)
                state = sender.ProcessState;
            bool isRunning = state == System.Threading.ThreadState.Running;
            bool isPaused = state == System.Threading.ThreadState.Suspended;

            btnProcessStart.Enabled = !isRunning && !isPaused;
            btnProcessPause.Enabled = isRunning || isPaused;
            btnProcessPause.Checked = isPaused;
            btnProcessPause.Font = new Font(btnProcessPause.Font, isPaused ? FontStyle.Bold : FontStyle.Regular);
            btnProcessStop.Enabled = isRunning || isPaused;
        }

        void ProcessStateChanged(IProcess sender, System.Threading.ThreadState state)
        {
            if (sender == ActiveProcess)
                ActiveProcessChanged(sender, state);
        }
        #endregion

        #region Process
        private void btnProcessStart_Click(object sender, EventArgs e)
        {
            var p = ActiveProcess;
            if (p != null)
                p.Start();
            else
                MessageBox.Show("Aucun process actif. Sélectionnez une fenêtre.");
            ActiveProcessChanged(p);
        }

        private void btnProcessPause_Click(object sender, EventArgs e)
        {
            var p = ActiveProcess;
            if (p != null)
            {
                if (p.ProcessState == System.Threading.ThreadState.Running)
                    p.Pause();
                else if (p.ProcessState == System.Threading.ThreadState.Suspended)
                    p.Resume();
            }
            else
                MessageBox.Show("Aucun process actif. Sélectionnez une fenêtre.");
            ActiveProcessChanged(p);
        }

        private void btnProcessStop_Click(object sender, EventArgs e)
        {
            var p = ActiveProcess;
            if (p != null)
                p.Stop();
            else
                MessageBox.Show("Aucun process actif. Sélectionnez une fenêtre.");
            ActiveProcessChanged(p);
        }
        #endregion

        private void saveToolStripButton_Click(object sender, EventArgs e)
        {
            if (ActiveMdiChild is ProcessForm)
            {
                ((ProcessForm)ActiveMdiChild).SaveSettings();
                toolStripStatusLabel.Text = ActiveMdiChild.Name + " enregistrée";
            }
            else
                toolStripStatusLabel.Text = "Rien à sauvegarder !";
        }

    }
}
