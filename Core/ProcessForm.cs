using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MED
{
    public class ProcessForm : Form, IProcess, IConsumer
    {
        public ProcessForm() : base()
        {
            this.FormClosed += Form_FormClosed;

            Project = new(this.GetType().Name, null, this);

            Project.OnProcessStateChanged += Invoke_ProcessStateChanged;
        }

        protected override void WndProc(ref Message m)
        {
            FormWindowState org = this.WindowState;
            base.WndProc(ref m);
            if (this.WindowState != org)
                this.ProcessForm_WindowStateChanged(null, EventArgs.Empty);
        }

        protected virtual void Form_FormClosed(object sender, FormClosedEventArgs e)
        {
            Stop();
        }


        #region Form

        private Form _MdiParent;
        private void ProcessForm_WindowStateChanged(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Maximized)
            {
                _MdiParent = MdiParent;
                MdiParent = null;
            }
            else if(_MdiParent!=null) 
            {
                MdiParent = _MdiParent;
                if (WindowState == FormWindowState.Normal)
                    Dock = DockStyle.Fill;
            }
        }

        private Size DockedSize;
        private void ProcessForm_DockChanged(object sender, EventArgs e)
        {
            if (MdiParent == null)
                return;
            if (this.Dock == DockStyle.Fill)
                DockedSize = this.Size;
            else if (!DockedSize.IsEmpty)
                this.Size = DockedSize;

        }
        #endregion

        [Browsable(true)]
        [ReadOnly(true)]
        public Processes Project { get; protected set; }

        public Logger Logger { get => Project.Logger; set => Project.Logger = value; }

        #region Settings


        [ReadOnly(true)]
        public bool IsAsynchrone { get => Project.IsAsynchrone; set => Project.IsAsynchrone = value; }

        [ReadOnly(true)]
        public string SettingsPath { get => Project.SettingsPath; set => Project.SettingsPath = value; }

        public virtual void LoadSettings(bool loadChildren = true)
        {
            Core.Settings.ClearCache(true, true, this.Name);
            Performance.LoadSettings(Name + ".Perf");
            Project.LoadSettings(loadChildren);
        }
        public virtual void SaveSettings(bool saveChildren = true)
        {
            if (saveChildren && Processes != null)
                foreach (var proc in Processes)
                    proc.SaveSettings();
            Performance.SaveSettings(Name + ".Perf", saveChildren);

            Core.Settings.Save();
        }
        #endregion


        [Browsable(true)]
        public virtual List<IProcess> Processes { get => Project.Items; }

        public static ProcessForm FindProcessForm(IProcess proc)
        {
            if (proc is ProcessForm)
                return (ProcessForm)proc;
            if (proc is Processes)
                if ((proc as Processes).InvokeHandler is ProcessForm)
                    return (ProcessForm)((proc as Processes).InvokeHandler);

            if (proc is IProvider)
                if ((proc as IProvider).InvokeHandler is ProcessForm)
                    return (ProcessForm)((proc as IProvider).InvokeHandler);
                else if ((proc as IProvider).InvokeHandler is Control)
                {
                    var f = ((proc as IProvider).InvokeHandler).FindForm();
                    if (f is ProcessForm)
                        return (ProcessForm)f;
                }

            return null;
        }

        protected virtual void DisposeProcesses() => Project.DisposeProcesses();

        protected virtual void InitializeProcesses(bool resetAll = false) => Project.InitializeProcesses(resetAll);

        public Performance Performance { get => Project.Performance; }

        public bool IsRunning { get => Project.ProcessState == ThreadState.Running || Project.ProcessState == ThreadState.Suspended; }


        public IProcess.ProcessStateChangedDelegate OnProcessStateChanged;

        public System.Threading.ThreadState ProcessState { get => Project.ProcessState; set => Project.ProcessState = value; }

        public void Invoke_ProcessStateChanged(IProcess sender, System.Threading.ThreadState state) => OnProcessStateChanged?.Invoke(sender, state);

        #region Process

        /**
         * 
         * 
         */
        public virtual void Start() => Project.Start();

        /**
         * 
         * 
         */
        public virtual void Stop() => Project.Stop();

        public virtual void Resume() => Project.Resume();

        public virtual void Pause() => Project.Pause();

        #endregion

        public virtual Dictionary<string, object> ObjectsProperties
        {
            get
            {
                var dict = Project.ObjectsProperties;
                dict.Add(this.Name, this);

                return dict;
            }
        }
    }
}
