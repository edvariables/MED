using DevDecoder.HIDDevices.Controllers;
using DynamicData;
using Emgu.CV;
using MED.Core;
using MED.EDJoystick;
using MED.EDWebCam;
using MED.Imaging;
using MED.Properties;
using Microsoft.Win32;
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
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;

namespace MED
{
    public partial class FStudio : ProcessForm
    {
        private int childFormNumber = 0;
        public static FStudio? Current { get; private set; }

        public FStudio()
        {
            InitializeComponent();

            Project.Name = Name = "Studio";
            Project.ProcessIcon = "MED";

            ActiveProcessChanged(null);

            Current = this;
        }


        private void FStudio_Load(object sender, EventArgs e)
        {
            LoadSettings();

            LoadChilds();

            LoadLastProcess();
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

        public override void LoadSettings(ProcessSettings? settings = null, string fileName = "")
        {
            base.LoadSettings(settings, fileName);

            string settingsSection = this.SettingsSection;
            Core.Settings.ClearCache(true, true, settingsSection);

            object v = Core.Settings.GetValue("Location", settingsSection, this.Location);
            this.Location = (Point)v;

            v = Core.Settings.GetValue("Size", settingsSection, this.Size);
            this.Size = (Size)v;

            EnsureFormLocationAndSize();

            LoadFavorites(settingsSection);

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

            if (ActiveProcess != null && !String.IsNullOrEmpty(ActiveProcess.ProcessSettings?.FileName))
                Core.Settings.SetValue("ActiveProcess", settingsSection, ActiveProcess.ProcessSettings.FileName);
            else
                Core.Settings.SetValue("ActiveProcess", settingsSection, "");

            SaveFavorites(settingsSection);

            FLogger.Current?.SaveSettings();

            Core.Settings.Save();
        }

        protected void SaveFavorites(string settingsSection)
        {
            StringBuilder favorites = new();
            foreach (var item in toolStrip.Items)
                if (item is ToolStripButton
                    && ((ToolStripButton)item).Name.StartsWith("btnFavorite["))
                {
                    if (favorites.Length > 0)
                        favorites.Append(';');
                    favorites.Append(((ToolStripButton)item).Tag?.ToString());
                }
            Core.Settings.SetValue("Favorites", settingsSection, favorites.ToString());
        }

        protected void LoadFavorites(string settingsSection)
        {
            var v = Core.Settings.GetValue("Favorites", settingsSection, "");
            if (v == null)
                return;

            string favorites = (string)v;
            foreach (var fileName in favorites.Split(";"))
                if (fileName != "" && File.Exists(fileName))
                    CreateProcessFavorite(fileName, Path.GetFileNameWithoutExtension(fileName), "Process");
        }

        private void EnsureFormLocationAndSize()
        {
            var screen = Screen.FromHandle(this.Handle);
            if (screen == null)
                screen = Screen.PrimaryScreen;
            if (screen == null)
                return;
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

            if (FLogger.Current != null)
            {
                FLogger.Current.Show();
                FLogger.Current.SizeChanged += FormChild_SizeChanged;
            }
            FProperties.Current.SizeChanged += FormChild_SizeChanged;

            FProperties.Current.ShowProperties((object[])[this.Project]);
        }

        public void LoadLastProcess()
        {
            string settingsSection = this.SettingsSection;

            string processFile = (string)Core.Settings.GetValue("ActiveProcess", settingsSection, "");
            if (!String.IsNullOrEmpty(processFile) && File.Exists(processFile))
                GetNewProcessForm(processFile);
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



        #region ProcessForm
        private void ShowNewForm(object sender, EventArgs e)
        {
            GetNewProcessForm();
        }
        /**
         * 
         * */
        private ProcessForm GetNewProcessForm(string? fileName = null, ProcessForm? processForm = null)
        {
            if (processForm == null)
                processForm = new("Projet " + childFormNumber++);
            processForm.MdiParent = this;
            processForm.OnProcessStateChanged += ProcessStateChanged;
            processForm.Activated += ProcessForm_Activated;

            processForm.Logger = FLogger.Current?.Logger;

            PictureBox? pictureBox = null;

            if (processForm.GetType() == typeof(ProcessForm) || processForm.Processes.Count == 0)
            {

                ProcessControl controller = new();
                controller.BackColor = System.Drawing.Color.Transparent;
                controller.Dock = DockStyle.Top;
                controller.ActiveProcess = processForm;
                controller.Show();
                processForm.Controls.Add(controller);

                pictureBox = new();
                pictureBox.BackColor = System.Drawing.Color.LightSteelBlue;
                pictureBox.Size = processForm.ClientSize;
                pictureBox.Dock = DockStyle.Fill;
                processForm.Controls.Add(pictureBox);
            }
            if (!string.IsNullOrEmpty(fileName))
            {
                processForm.LoadSettings(null, fileName);
                processForm.Text = processForm.Name = processForm.Project.Name;
            }
            else if (processForm.Processes.Count == 0)
            {
                var render = new Render(
                    "Render"
                    , new Performance("Render", FLogger.Current?.Logger)
                    , pictureBox
                );
                processForm.Processes.Add(render);

                var videoCapture = new EDVideoCapture(
                    "VideoCapture"
                    , new Performance("VideoCapture", FLogger.Current?.Logger)
                    , processForm
                    , (IImageConsumer)processForm.Processes.Last()
                );
                processForm.Processes.Add(videoCapture);

            }

            processForm.Icon = MEDIcon.GetIcon(processForm.ProcessIcon);

            processForm.Dock = DockStyle.Fill;

            processForm.Show();

            Size size = processForm.Size;
            Point location = processForm.Location;
            processForm.SuspendLayout();
            processForm.Dock = DockStyle.None;
            processForm.Location = location;
            processForm.Size = size;
            processForm.ResumeLayout();


            if (processForm.Processes.Count > 0 && processForm.Processes.First() is ImageProcess)
            {
                if (processForm.Processes.First() is Render)
                    ((Render)processForm.Processes.First()).RenderImageControl = pictureBox;
                else
                    ((ImageProcess)processForm.Processes.First()).InvokeHandler = pictureBox;
                ((ImageProcess)processForm.Processes.First()).OnImageChanged += ProcessForm_ImageChanged;
            }

            Processes.Add(processForm);
            FProperties.CurrentProperties = (object[])[this.Project];

            ActiveProcess = processForm;

            return processForm;
        }
        private void ProcessForm_Activated(object? sender, EventArgs e)
        {
            var activeProcess = ActiveProcessForm;
        }
        /**
         * Image
         * */
        private void ProcessForm_ImageChanged(IImageProvider sender, EventArgs e)
        {
            if (this.Disposing || this.IsDisposed || FLogger.Current == null)
                return;
            try
            {
                FLogger.Current.RefreshProgress((ImageProcess)sender);

                FLogger.Current.ProgressMessage = $"{((Process)sender).Name} [{((Process)sender).Performance?.Counter}]";

                if (btnProcessStartOneStep.Checked)
                    btnProcessPause_Click(sender, e);
            }
            catch (Exception ex)
            {
                Performance?.Error("ProcessForm_ImageChanged", ex);
            }
        }

        #endregion

        private void OpenFile(object sender, EventArgs e)
        {
            System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog();
            openFileDialog.InitialDirectory = Settings.MyProjectsDirectory;
            var extension = Settings.ProcessFileExtension;
            openFileDialog.Filter = $"Fichiers de projets MED (*{extension})|*{extension}|Tous les fichiers (*.*)|*.*";
            if (openFileDialog.ShowDialog(this) == DialogResult.OK)
            {
                string fileName = openFileDialog.FileName;

                GetNewProcessForm(fileName);
            }
        }

        private void saveToolStripButton_Click(object sender, EventArgs e)
        {
            SaveSettings();
            if (ActiveProcess is ProcessForm)
            {
                if (String.IsNullOrEmpty(((IProcess)ActiveProcess).ProcessSettings?.FileName))
                {
                    SaveAsToolStripMenuItem_Click(sender, e);
                    return;
                }
                ((ProcessForm)ActiveProcess).SaveSettings();
                toolStripStatusLabel.Text = ActiveProcess.Name + " enregistrée";
            }
            else
                toolStripStatusLabel.Text = "Aucun process à sauvegarder !";
        }

        private void SaveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ActiveProcess == null)
            {
                MessageBox.Show("Aucun projet actif à enregistrer", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            System.Windows.Forms.SaveFileDialog saveFileDialog = new System.Windows.Forms.SaveFileDialog();
            saveFileDialog.InitialDirectory = Settings.MyProjectsDirectory;
            saveFileDialog.FileName = ActiveProcess.ProcessSettings?.FileName;
            if (saveFileDialog.FileName == "")
                saveFileDialog.FileName = ActiveProcess.Name;
            var extension = Settings.ProcessFileExtension;
            saveFileDialog.Filter = $"Fichiers de projets MED (*{extension})|*{extension}|Tous les fichiers (*.*)|*.*";
            if (saveFileDialog.ShowDialog(this) == DialogResult.OK)
            {
                string fileName = saveFileDialog.FileName;
                if (ActiveProcess != null)
                {
                    ActiveProcess.SaveSettings(null, fileName);

                    if (ActiveProcess.ProcessSettings != null)
                        ActiveProcess.ProcessSettings.FileName = fileName;
                }
            }
        }


        #region Processes

        private void toolStripBtnAddToFavorites_Click(object sender, EventArgs e) => AddProcessToFavorites();

        private void AddProcessToFavorites()
        {
            var processForm = ActiveProcessForm;
            if (processForm == null)
                return;
            if (String.IsNullOrEmpty(ActiveProcessForm.ProcessSettings?.FileName))
                return;

            CreateProcessFavorite(ActiveProcessForm.ProcessSettings.FileName, ActiveProcessForm.Name, processForm.ProcessIcon);
        }
        private void CreateProcessFavorite(string fileName, string name, string processIcon)
        {
            ToolStripButton btnFavorite = new ToolStripButton();
            btnFavorite.Image = MEDIcon.GetImage(processIcon);
            btnFavorite.Name = $"btnFavorite[{name}]";
            btnFavorite.Tag = fileName;
            btnFavorite.Size = new Size(76, 22);
            btnFavorite.Text = name;
            btnFavorite.AutoSize = true;
            btnFavorite.Click += BtnFavorite_Click;

            toolStrip.Items.Add(btnFavorite);
        }

        private void BtnFavorite_Click(object? sender, EventArgs e)
        {
            if (sender == null)
                return;
            string? fileName = (string?)((ToolStripButton)sender).Tag;
            ProcessForm? processForm = null;
            foreach (Form form in MdiChildren)
            {
                if (form is ProcessForm
                && ((ProcessForm)form).ProcessSettings != null
                && ((ProcessForm)form).ProcessSettings.FileName == fileName)
                {
                    processForm = (ProcessForm)form;
                    break;
                }
            }
            if (processForm != null)
            {
                if (!processForm.Visible)
                    processForm.Visible = true;
                if (processForm.WindowState == FormWindowState.Minimized)
                    processForm.WindowState = FormWindowState.Normal;
                processForm.BringToFront();
                processForm.Show();
                return;
            }

            GetNewProcessForm(fileName);
        }

        public ProcessForm GetProcessorForm(Type type)
        {
            //CreateInstance
            try
            {
                IProcess proc = (IProcess)Activator.CreateInstance(type);
                if (proc is ProcessForm)
                {

                    ProcessForm form = (ProcessForm)proc;

                    return GetNewProcessForm("", form);


                    //form.MdiParent = this;
                    //form.Dock = DockStyle.Fill;
                    //form.Show();
                    //form.OnProcessStateChanged += ProcessStateChanged;
                    //form.Activated += ProcessForm_Activated;
                    //if (form.Processes.First() is ImageProcess)
                    //    (form.Processes.First() as ImageProcess).OnImageChanged += ProcessForm_ImageChanged;

                    //return form;
                }

                throw new Exception($"{type.Name} is not a ProcessForm type");
            }
            catch
            {
                throw new Exception($"{type.Name} is not a Form type");
            }
            //return null;
        }


        public ProcessForm ActiveProcessForm
        {
            get
            {
                var activeProcess = ActiveProcess;
                if (activeProcess is ProcessForm)
                    return (ProcessForm)activeProcess;
                return null;
            }
        }
        private IProcess? _active_Process;
        public IProcess? ActiveProcess
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
                {
                    var type = _active_Process.GetType();
                    return _active_Process = GetProcessorForm(type);
                }
                return _active_Process;
            }
            set
            {
                _active_Process = value;
                if (_active_Process != null)
                    if (_active_Process is Form)
                        ((Form)_active_Process).Activate();
                ActiveProcessChanged(_active_Process);
            }
        }

        /**
         * 
         * 
         * */
        private void ActiveProcessChanged(IProcess? sender, System.Threading.ThreadState state = System.Threading.ThreadState.Unstarted)
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

            if (sender is ProcessForm)
            {
                if (!(sender as ProcessForm).Project.IsDisposed)
                    FProperties.CurrentProperties = (object[])[(sender as ProcessForm).Project];
                //else
                //    FProperties.RemoveProperties((object[])[(sender as ProcessForm).Project]);
            }
            else if (!(sender is Process && (sender as Process).IsDisposed))
                FProperties.CurrentProperties = (object[])[sender];
            //else
            //     FProperties.RemoveProperties((object[])[(sender as ProcessForm).Project]);
        }

        private void FStudio_MdiChildActivate(object sender, EventArgs e)
        {
            if (this.ActiveMdiChild is IProcess)
                ActiveProcess = (IProcess)this.ActiveMdiChild;
        }

        void ProcessStateChanged(IProcess sender, System.Threading.ThreadState state)
        {
            if (ProcessForm.FindProcessForm(sender) == ActiveProcess)
                ActiveProcessChanged(sender, state);
            if (state == System.Threading.ThreadState.Running)
                FLogger.Current?.Start();
            else if (state == System.Threading.ThreadState.Stopped)
            {
                FLogger.Current?.Stop();
                btnProcessStartOneStep.Checked = false;
            }
        }
        #endregion

        #region Process
        private void btnProcessStart_Click(object sender, EventArgs e)
        {
            btnProcessStartOneStep.Checked = false;
            var p = ActiveProcess;
            if (p != null)
                p.Start();
            else
                MessageBox.Show("Aucun process actif. Sélectionnez une fenêtre.");
            ActiveProcessChanged(p);
        }

        private void btnProcessStartOneStep_Click(object sender, EventArgs e)
        {
            if (btnProcessStartOneStep.Checked)
            {
                btnProcessStartOneStep.Checked = false;
                return;
            }
            btnProcessStartOneStep.Checked = true;
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

        private void FormChild_SizeChanged(object? sender, EventArgs e)
        {
            Rectangle bounds = this.ClientRectangle;
            bounds.Width -= (int)this.AutoScaleDimensions.Width;// - this.DefaultMargin.Left - this.DefaultMargin.Right;
            bounds.Height -= (int)this.AutoScaleDimensions.Height;// - this.DefaultMargin.Top - -this.DefaultMargin.Bottom;
            if (statusStrip.Visible)
                bounds.Height -= statusStrip.Height;
            if (toolsMenu.Visible)
            {
                bounds.Height -= toolsMenu.Height;
            }
            if (menuStrip.Visible)
            {
                bounds.Height -= menuStrip.Height;
            }
            foreach (Form form in MdiChildren)
            {
                if (form is ProcessForm)
                    continue;
                if (!form.Visible)
                    continue;//TODO
                if (form.Dock == DockStyle.Left)
                {
                    bounds.X += form.Width;
                    bounds.Width -= form.Width;
                }
                else if (form.Dock == DockStyle.Top)
                {
                    bounds.Y += form.Height;
                    bounds.Height -= form.Height;
                }
                else if (form.Dock == DockStyle.Right)
                {
                    bounds.Width -= form.Width;
                }
                else if (form.Dock == DockStyle.Bottom)
                {
                    bounds.Height -= form.Height;
                }
            }
            foreach (Form processForm in MdiChildren)
            {
                if (processForm is not ProcessForm)
                    continue;
                if (processForm.WindowState != FormWindowState.Normal)
                    continue;//TODO

                processForm.Location = bounds.Location;
                processForm.Size = bounds.Size;
            }
        }
    }
}
