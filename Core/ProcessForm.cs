using MED.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace MED
{
    public class ProcessForm : Form, IProcess, IConsumer
    {
        public ProcessForm() : this("ProcessForm") { }

        public ProcessForm(string name) : base()
        {
            Text = Name = name;

#pragma warning disable CS8622 // La nullabilité des types référence dans le type du paramètre ne correspond pas au délégué cible (probablement en raison des attributs de nullabilité).
            this.FormClosed += Form_FormClosed;
            this.DockChanged += ProcessForm_DockChanged;
#pragma warning restore CS8622 // La nullabilité des types référence dans le type du paramètre ne correspond pas au délégué cible (probablement en raison des attributs de nullabilité).

            Project = new(name, null, this, this);

            ProcessIcon = Project.ProcessIcon;

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
            Project.Dispose();
        }


        #region Form

        private Form? _MdiParent;
        private void ProcessForm_WindowStateChanged(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Maximized)
            {
                if (MdiParent != null)
                {
                    _MdiParent = MdiParent;
                    MdiParent = null;
                }
            }
            else if (_MdiParent != null && MdiParent == null)
            {
                MdiParent = _MdiParent;
                if (WindowState == FormWindowState.Normal)
                {
                    Dock = DockStyle.Fill;

                    Size size = this.Size;
                    Point location = this.Location;
                    this.Dock = DockStyle.None;
                    this.Location = location;
                    this.Size = size;
                }
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

        [Browsable(true)]
        public ProcessSettings ProcessSettings { get => Project.ProcessSettings; set => Project.ProcessSettings = value; }

        public virtual void LoadSettings(ProcessSettings? processSettings = null, string fileName = "")
        {
            Project.LoadSettings(processSettings, fileName);

            Size = (Size)ProcessSettings.GetValue("Size", Size);
            Location = (Point)ProcessSettings.GetValue("Location", Location);

            ProcessIcon = Project.ProcessIcon;
        }
        public virtual void LoadProcess(JsonNode node) => Project.LoadProcess(node);

        public virtual void SaveSettings(ProcessSettings? settings = null, string fileName = "") => Project.SaveSettings(settings, fileName);

        public virtual JsonObject SaveProcess(JsonObject? node = null)
        {
            if (node == null)
                node = new JsonObject();
            node["ProcessClass"] = this.GetType().FullName;
            node["Name"] = Name;
            node["IsAsynchrone"] = IsAsynchrone;
            if (Visible)
            {
                node["Size"] = Parser.ObjectToString(Size);
                node["Location"] = Parser.ObjectToString(Location);
            }
            node["Perf"] = Performance.SaveNode();

            return node;
        }
        #endregion

        //public virtual void LoadSettings(bool loadChildren = true)
        //{
        //    Core.Settings.ClearCache(true, true, this.Name);
        //    Performance.LoadSettings(Name + ".Perf");
        //    Project.LoadSettings(loadChildren);
        //}
        //public virtual void SaveSettings(bool saveChildren = true)
        //{
        //    if (saveChildren && Processes != null)
        //        foreach (var proc in Processes)
        //            proc.SaveSettings();
        //    Performance.SaveSettings(Name + ".Perf", saveChildren);

        //    Core.Settings.Save();
        //}
        //#endregion


        [Browsable(true)]
        public virtual List<IProcess> Processes { get => Project.Items; }

        public static ProcessForm FindProcessForm(IProcess proc)
        {
            if (proc is ProcessForm)
                return (ProcessForm)proc;
            if (proc is IProcesses)
                if ((proc as Process).InvokeHandler is ProcessForm)
                    return (ProcessForm)((proc as Process).InvokeHandler);

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


        public IProcess.ProcessStateChangedDelegate? OnProcessStateChanged;

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
                return dict;
            }
        }

        public string ProcessIcon
        {
            get => Project.ProcessIcon;
            set
            {
                Project.ProcessIcon = value;
                if (!String.IsNullOrEmpty(value))
                    this.Icon = Core.Settings.GetIcon(value);
            }
        }
    }
}
