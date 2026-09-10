using MED.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace MED
{
    /**
     * class ProcessForm : Form, IProcess, IConsumer
     * <summary>A form that hosts a process</summary>
     * */
    public class ProcessForm : Form, IProcess, IConsumer, IUndo
    {
        public ProcessForm() : this("ProcessForm") { }

        public ProcessForm(string name) : base()
        {
            Text = Name = name;

            this.FormClosed += Form_FormClosed;
            this.DockChanged += ProcessForm_DockChanged;

            Project = new(name, null, this, this);

            ProcessIcon = ProcessIconDefault;

            Project.ProcessStateChanged += OnProcessStateChanged;
        }

        protected override void WndProc(ref Message m)
        {
            FormWindowState org = this.WindowState;
            base.WndProc(ref m);
            if (this.WindowState != org)
                this.ProcessForm_WindowStateChanged(this, EventArgs.Empty);
        }
        public IProcess Clone(Type? cloneType = null) => (IProcess)MemberwiseClone();

        protected virtual void Form_FormClosed(object? sender, FormClosedEventArgs e)
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
                try
                {
                    MdiParent = _MdiParent;
                }
                catch (Exception ex)
                {
                    Performance?.Error("ProcessForm_WindowStateChanged", ex);
                }
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
        private void ProcessForm_DockChanged(object? sender, EventArgs e)
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

        [Category("Process")]
        public Processes Project { get; protected set; }


        [Category("Process")]
        public Logger? Logger { get => Project.Logger; set => Project.Logger = value; }

        #region Settings


        [ReadOnly(true)]

        [Category("Process")]
        public bool IsAsynchrone { get => Project.IsAsynchrone; set => Project.IsAsynchrone = value; }

        [Browsable(true)]

        [Category("Process")]
        public ProcessSettings? ProcessSettings { get => Project.ProcessSettings; set => Project.ProcessSettings = value; }

        public virtual void LoadSettings(ProcessSettings? processSettings = null, string fileName = "")
        {
            Project.LoadSettings(processSettings, fileName);
            if (ProcessSettings == null)
                return;
            Size = (Size)(ProcessSettings.GetValue("Size", Size) ?? Size);
            Location = (Point)(ProcessSettings.GetValue("Location", Location) ?? Location);
            StartFullScreen = (bool)(ProcessSettings.GetValue("StartFullScreen", StartFullScreen) ?? StartFullScreen);

            ProcessIcon = Project.ProcessIcon;
        }
        public virtual void LoadProcess(JsonNode node) => Project.LoadProcess(node);
        public virtual void LoadSettingsDone(object? sender, EventArgs e) => throw new NotImplementedException();

        public virtual void SaveSettings(ProcessSettings? settings = null, string fileName = "") => Project.SaveSettings(settings, fileName);

        public virtual JsonObject SaveProcess(JsonObject? node = null)
        {
            node ??= [];
            node["ProcessClass"] = this.GetType().FullName;
            node["Name"] = Name;
            node["IsAsynchrone"] = IsAsynchrone;
            node["StartFullScreen"] = StartFullScreen;
            if (Visible)
            {
                node["Size"] = Parser.ObjectToString(Size);
                node["Location"] = Parser.ObjectToString(Location);
            }
            if (Performance != null)
                node["Perf"] = Performance.SaveNode();

            return node;
        }
        #endregion

        #region Processes

        [Browsable(true)]

        [Category("Process")]
        public virtual List<IProcess> Processes { get => Project.Items; }

        public static ProcessForm? FindProcessForm(IProcess proc)
        {
            if (proc is ProcessForm processForm)
                return processForm;
            if (proc is IProcesses)
                if (((Process)proc).InvokeHandler is ProcessForm invokeHandlerForm)
                    return invokeHandlerForm;

            if (proc is IProvider provider)
                if (provider.InvokeHandler is ProcessForm invokeHandlerForm)
                    return invokeHandlerForm;
                else if (((IProvider)proc).InvokeHandler is Control invokeHandlerControl)
                {
                    var f = invokeHandlerControl.FindForm();
                    if (f is ProcessForm processForm1)
                        return processForm1;
                }

            return null;
        }

        protected virtual void DisposeProcesses() => Project.DisposeProcesses();

        protected virtual void InitializeProcesses(bool resetAll = false) => Project.InitializeProcesses(resetAll);

        #endregion


        [Category("Process")]
        public Performance? Performance { get => Project.Performance; set => Project.Performance = value; }

        [Browsable(false)]
        public IConsumer? Consumer => Project.Consumer;

        public virtual void OnGameControllerChanged(IGameController gameController, PropertyChangedEventArgs eventArgs)
        {
            if (OnGameControllerScript == null)
                return;
            OnGameControllerScript.Eval(gameController, eventArgs);
        }

        [Browsable(true)]
        [Category("GameController")]
        [Editor(typeof(EventScriptEditor), typeof(UITypeEditor))]
        [TypeConverter(typeof(EventScriptConvertor))]
        public virtual GameControllerScript? OnGameControllerScript { get; set; }


        #region Process
        public bool IsRunning => Project.IsRunning;
        public bool IsPaused => Project.IsPaused;

        [Browsable(false)]
        public IProcess.ProcessStateChangedDelegate? ProcessStateChanged { get; set; }

        public virtual void OnProcessStateChanged(IProcess sender, System.Threading.ThreadState state)
        {
            ProcessStateChanged?.Invoke(this, state);
            if (sender != Project)
                Project.OnProcessStateChanged(this, ProcessState);
        }

        public System.Threading.ThreadState ProcessState { get => Project.ProcessState; set => Project.ProcessState = value; }

        /**
         * 
         * 
         */
        public virtual void Start()
        {
            if (StartFullScreen)
            {
                StartFullScreen_WindowState = this.WindowState;
                this.WindowState = FormWindowState.Maximized;
                StartFullScreen_BorderStyle = this.FormBorderStyle;
                this.FormBorderStyle = FormBorderStyle.None;
            }
            Project.Start();
        }

        /**
         * 
         * 
         */
        public virtual void Stop()
        {
            Project.Stop();
            if (StartFullScreen)
            {
                this.FormBorderStyle = StartFullScreen_BorderStyle;
                this.WindowState = StartFullScreen_WindowState;
            }
        }

        public virtual void Resume() => Project.Resume();

        public virtual void Pause() => Project.Pause();


        [Browsable(true)]

        [Category("Process")]
        public bool StartFullScreen { get; set; }

        private FormWindowState StartFullScreen_WindowState;
        private FormBorderStyle StartFullScreen_BorderStyle;

        #endregion


        [Category("Process")]
        public virtual Dictionary<string, object> ObjectsProperties
        {
            get
            {
                var dict = Project.ObjectsProperties;
                return dict;
            }
        }


        [Category("Process")]
        public virtual string ProcessIcon
        {
            get => Project.ProcessIcon;
            set
            {
                Project.ProcessIcon = value;
                if (!String.IsNullOrEmpty(value))
                    this.Icon = MEDIcon.GetIcon(value);
            }
        }


        [Category("Process")]
        public virtual string ProcessIconDefault { get; protected set; } = "Visual";


        [Category("Process")]
        public Dictionary<string, object?>? Data { get; set; }

        [Category("Process")]
        public new object? Tag { get; set; }


        #region IUndo
        public void UndoClear() => Project.UndoClear();
        public virtual Dictionary<string, object> UndoModeSaveProperties() => Project.UndoModeSaveProperties();
        public virtual Dictionary<string, object>? Undo(int length = 1) => Project.Undo(length);
        #endregion
    }
}
