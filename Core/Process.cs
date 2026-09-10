using MED.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Diagnostics.CodeAnalysis;
using System.Drawing.Design;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace MED
{
    public class Process : IConsumer, IProvider
    {
        public IProcess Clone(Type? cloneType = null) => (IProcess)MemberwiseClone();

        public Process(string name, Performance? performance = null, Control? invokeHandler = null, IConsumer? consumer = null, bool isAsynchrone = false)
        {
            InvokeHandler = invokeHandler;
            IsAsynchrone = isAsynchrone;
            Consumer = consumer;
            ProcessIcon = ProcessIconDefault;

            if (name == "")
                name = this.GetType().Name;

            Name = name;

            Performance = performance ?? MED.Performance.Empty();

        }


        public virtual void Dispose()
        {
            Disposing = true;

            Stop();

            ProcessState = ThreadState.Aborted;

            RemovePropertyDelegateConsumers();

            Performance = MED.Performance.Empty();
            Consumer = null;
            InvokeHandler = null;

            if (Disposing)
                IsDisposed = true;
        }

        public override string ToString()
        {
            var typeName = GetType().Name;
            if (typeName == Name)
                return $"{Name}({ProcessState})";
            return $"{Name} as {typeName} ({ProcessState})";
        }

        #region Delegates ans consumers
        /**
         * Delegates ans consumers
         * 
         * */
        public virtual bool AddConsumer(IConsumer consumer, string property = "ProcessState", MulticastDelegate? consumerDelegate = null) => ProcessStatic.AddConsumer(this, consumer, property, consumerDelegate);

        public virtual bool RemoveConsumer(IConsumer consumer, string property, MulticastDelegate? consumerDelegate = null) => ProcessStatic.RemoveConsumer(this, consumer, property, consumerDelegate);

        /**
         * 
         * */
        protected Dictionary<IProcess, Delegate> GetConsumers(string propertyName = "") => GetPropertyDelegateConsumers(propertyName).Value ?? [];

        /**
         * 
         * */
        protected List<MulticastDelegate> GetOnChangedDelegates(string propertyName = "") => ProcessStatic.GetOnChangedDelegates(this, propertyName);

        /**
         * 
         * */
        protected MulticastDelegate? GetOnChangedDelegate(string propertyName)
        {
            var onChangedDelegates = GetOnChangedDelegates(propertyName);
            if (onChangedDelegates.Count == 0)
                return null;
            return onChangedDelegates.First();

        }
        /**
         * 
         * */
        protected Dictionary<IProcess, Delegate>? GetOnChangedConsumers(string propertyName = "") => ProcessStatic.GetOnChangedConsumers(GetOnChangedDelegate(propertyName));

        protected Dictionary<IProcess, Delegate>? GetOnChangedConsumers(MulticastDelegate onChangedDelegate) => ProcessStatic.GetOnChangedConsumers(onChangedDelegate);

        /**
         * 
         * */
        protected List<string> GetProperties(string propertyName = "") => [.. GetPropertiesDelegatesConsumers(propertyName).Keys];

        /**
         * 
         * */
        protected bool PropertyExists(string propertyName) => GetProperties(propertyName).Count > 0;

        /**
         * _PropertiesDelegatesConsumers
         * See ProcessStatic.PropertiesConsumersCacheReset()
         * */
        internal Dictionary<string, KeyValuePair<MulticastDelegate, Dictionary<IProcess, Delegate>>>? _PropertiesDelegatesConsumers;

        /**
         * 
         * */
        public void RemovePropertyDelegateConsumers(string propertyName = "") => ProcessStatic.RemovePropertyDelegateConsumers(this, propertyName);
        /**
         * 
         * */
        public void CleanPropertiesDelegatesConsumers(string propertyName = "") => ProcessStatic.CleanPropertiesDelegatesConsumers(this, propertyName);

        /**
         * 
         * */
        public KeyValuePair<MulticastDelegate, Dictionary<IProcess, Delegate>> GetPropertyDelegateConsumers(string propertyName = "", bool evenEmpty = true) => ProcessStatic.GetPropertyDelegateConsumers(this, propertyName, evenEmpty);
        /**
         * 
         * */
        public Dictionary<string, KeyValuePair<MulticastDelegate, Dictionary<IProcess, Delegate>>> GetPropertiesDelegatesConsumers(string propertyName = "", bool evenEmpty = true) => ProcessStatic.GetPropertiesDelegatesConsumers(this, propertyName, evenEmpty);

        /***
         * Invoke
         * 
         * */

        public bool IsInvokingPropertyChanged(Delegate delegateMethod) => ProcessStatic.IsInvokingPropertyChanged(this, delegateMethod);

        public virtual void InvokePropertyChanged(IProvider? sender, Delegate? delegateMethod, EventArgs? e, string? propertyDomain = null) => ProcessStatic.InvokePropertyChanged(this, sender, delegateMethod, e, propertyDomain);

        public void AddHandler(string handler_field, IConsumer consumer, Type consumer_type, string consumer_method) => ProcessStatic.AddHandler(this, handler_field, consumer, consumer_type, consumer_method);

        public void RemoveHandler(string handler_field, IConsumer consumer, Type consumer_type, string consumer_method) => ProcessStatic.RemoveHandler(this, handler_field, consumer, consumer_type, consumer_method);
        #endregion

        #region Properties
        [Category("Process")]
        [DefaultValue(true)]
        public virtual bool Enabled { get; set; } = true;

        [Category("Process")]
        [DefaultValue(false)]
        public virtual bool IsAsynchrone { get; set; }

        [Category("Process")]
        public virtual string Name { get; set; }

        [Editor(typeof(MEDIconSelectorEditor), typeof(UITypeEditor))]
        [TypeConverter(typeof(MEDIconNameConverter))]
        [Category("Process")]
        public virtual string ProcessIcon { get; set; }

        [Category("Process")]
        [DefaultValue("Process")]
        public virtual string ProcessIconDefault { get; protected set; } = "Process";

        [Browsable(false)]
        public virtual Control? InvokeHandler { get; set; }

        [Browsable(false)]
        public IConsumer? Consumer { get; set; }

        [Browsable(true)]
        [Category("Process")]
        public virtual Performance? Performance { get; set; }

        [Category("Process")]
        public Dictionary<string, object?>? Data { get; set; }

        [Category("Process")]
        public object? Tag { get; set; }

        #region GameController
        public virtual void OnGameControllerChanged(MED.GameController.IGameController gameController, PropertyChangedEventArgs eventArgs)
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

        #endregion

        /**
         * ObjectsProperties
         * */
        [Category("Process")]
        public virtual Dictionary<string, object> ObjectsProperties
        {
            get
            {
                var dict = new Dictionary<string, object>
                {
                    { this.Name, this }
                };
                if (Performance != null)
                    dict.Add(this.Name + ".Performance", Performance);

                return dict;
            }
        }
        #endregion

        #region Settings

        [Browsable(true)]
        [Category("Process")]
        public virtual ProcessSettings? ProcessSettings { get; set; }
        public virtual void LoadSettings(ProcessSettings? settings = null, string fileName = "")
        {
            if (settings == null)
                settings = ProcessSettings = ProcessSettings.FromFile(fileName);
            else
                ProcessSettings = settings;
            if (settings == null)
                return;
            Name = (string)(settings.GetValue("Name", Name) ?? Name);
            ProcessIcon = (string)(settings.GetValue("ProcessIcon", ProcessIcon) ?? ProcessIcon);
            IsAsynchrone = (bool)(settings.GetValue("IsAsynchrone", IsAsynchrone) ?? IsAsynchrone);
            Enabled = (bool)(settings.GetValue("Enabled", Enabled) ?? Enabled);

            if (Performance != null)
            {
                Performance.Name = Name;
                Performance.LoadSettings(settings.ChildSettings("Perf"));
            }
            if (settings.Root != null)
                LoadProcess(settings.Root);

            EventScript.LoadSetting(settings, this, nameof(OnGameControllerScript));
            EventScript.LoadSetting(settings, this, nameof(OnProcessStateScript));

            if (settings.OnLoadSettingsDone != null)
                settings.OnLoadSettingsDone(this, EventArgs.Empty);
        }

        public virtual void LoadProcess(JsonNode node) { }

        public virtual void LoadSettingsDone(object? sender, EventArgs e)
        {
            if (OnGameControllerScript != null)
                OnGameControllerScript.Script = OnGameControllerScript.Script;
            if (OnProcessStateScript != null)
                OnProcessStateScript.Script = OnProcessStateScript.Script;
        }

        public virtual void SaveSettings(ProcessSettings? settings = null, string fileName = "")
        {
            settings ??= ProcessSettings;

            if (settings != null && settings.Root != null)
            {
                SaveProcess(settings.Root.AsObject());

                if (fileName != "" || settings.FileName != "")
                {
                    settings.Save(fileName);
                }
            }
        }


        public virtual JsonObject SaveProcess(JsonObject? node = null)
        {
            node ??= [];
            var type = this.GetType();
            var assembly = type.Assembly.Location;
#pragma warning disable CS8602 // Déréférencement d'une éventuelle référence null.
            if (Directory.GetParent(assembly).FullName == Directory.GetParent(Assembly.GetExecutingAssembly().Location).FullName)
                assembly = "";// type.AssemblyQualifiedName;
#pragma warning restore CS8602 // Déréférencement d'une éventuelle référence null.

            node["ProcessClass"] = type.FullName;
            if (assembly != "")
                node["ProcessLib"] = assembly;
            node[nameof(Name)] = Name;
            node[nameof(IsAsynchrone)] = IsAsynchrone;
            node[nameof(Enabled)] = Enabled;
            if (ProcessIcon != ProcessIconDefault)
                node[nameof(ProcessIcon)] = ProcessIcon;

            if (OnGameControllerScript != null && !string.IsNullOrEmpty(OnGameControllerScript.Script))
                node.Add(nameof(OnGameControllerScript), OnGameControllerScript.Script);

            if (OnProcessStateScript!= null && !string.IsNullOrEmpty(OnProcessStateScript.Script))
                node.Add(nameof(OnProcessStateScript), OnProcessStateScript.Script);

            node["Perf"] = Performance?.SaveNode();

            return node;
        }
        #endregion

        #region Process
        [Category("Process")]
        [DefaultValue(false)]
        public virtual bool IsRunning
        {
            get
            {
                if (this.IsDisposed || this.Disposing)
                    return false;

                return ProcessState == ThreadState.Running || ProcessState == ThreadState.Suspended;
            }
        }
        [Category("Process")]
        [DefaultValue(false)]
        public virtual bool IsPaused
        {
            get
            {
                if (this.IsDisposed || this.Disposing)
                    return false;

                return ProcessState == ThreadState.Suspended;
            }
        }


        [Browsable(false)]
        public IProcess.ProcessStateChangedDelegate? ProcessStateChanged { get; set; }

        public virtual void OnProcessStateChanged(IProcess sender, System.Threading.ThreadState state) {
            ProcessStateChanged?.Invoke(this, state);
            OnProcessStateScript?.Eval(this, ProcessState);
        }
        
        [Browsable(true)]
        [Category("Process")]
        [Editor(typeof(EventScriptEditor), typeof(UITypeEditor))]
        [TypeConverter(typeof(EventScriptConvertor))]
        public ProcessStateScript? OnProcessStateScript { get; set; }

        public virtual void Stop()
        {

            if (Performance != null && Performance.IsRunning)
                Performance.Stop(true);

            if (!IsRunning)
                return;

            ProcessState = ThreadState.StopRequested;

            ////Kills delegate links to object
            //OnImageChanged = null;
            ////if (OnImageChanged != null)
            ////    foreach (var del in OnImageChanged.GetInvocationList())
            ////        OnImageChanged -= (ImageChangedDelegate)del;

            ProcessState = ThreadState.Stopped;

            if (Disposing)
                IsDisposed = true;
        }

        /**
         * Start
         * 
         * Inherits to set ProcessState = ThreadState.Started;
         */
        public virtual void Start()
        {
            if (IsRunning)
            {
                if (ProcessState == ThreadState.Suspended)
                    Resume();
                return;
            }

            ProcessState = ThreadState.Unstarted;

            Performance?.Start($"Start {this}", true);

            UndoClear();

            ProcessStatic.InvokingPropertyChangedReset(this);

            //Override next :
            /*
            ProcessState = ThreadState.Running;
            IsRunning = true;
            */
        }


        [Browsable(false)]
        public bool Disposing { get; private set; }
        [Browsable(false)]
        public bool IsDisposed { get; private set; }

        private ThreadState _ProcessState = ThreadState.Unstarted;
        [ReadOnly(true)]
        [Category("Process")]
        public virtual ThreadState ProcessState
        {
            get => _ProcessState;
            set
            {
                if (_ProcessState != value)
                    OnProcessStateChanged(this, _ProcessState = value);
                else
                    _ProcessState = value;
            }
        }


        public virtual void Pause()
        {
            if (IsRunning)
            {
                ProcessState = ThreadState.Suspended;
                Performance?.Suspend("Process.Pause");
            }
        }

        public virtual void Resume()
        {
            if (IsRunning)
            {
                Performance?.Resume("Process.Resume");
                ProcessState = ThreadState.Running;
            }
        }
        #endregion

        #region IUndo
        private readonly int _UndoStackCountMax = 64;
        private Stack<Dictionary<string, object>> _UndoStack = new();
        public virtual void UndoClear() => _UndoStack.Clear();
        public virtual Dictionary<string, object> UndoModeSaveProperties()
        {
            _UndoStack ??= new();
            if (_UndoStack.Count > _UndoStackCountMax + 8)
                _UndoStack = new(_UndoStack.SkipLast(_UndoStack.Count - _UndoStackCountMax).Reverse());

            var dic = new Dictionary<string, object>();
            _UndoStack.Push(dic);

            return dic;
        }
        public virtual Dictionary<string, object>? Undo(int length = 1)
        {
            if (_UndoStack == null || _UndoStack.Count == 0)
                return null;

            Dictionary<string, object>? dic = [];
            for (int i = 0; i < length; i++)
                if (!_UndoStack.TryPop(out dic))
                    break;

            if (dic == null || dic.Count == 0)
                return dic;

            var type = this.GetType();

            foreach (var (propertyName, propertyValue) in dic)
            {
                var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (property == null || !property.CanWrite)
                    continue;
                property.SetValue(this, propertyValue);
            }

            return dic;
        }
        #endregion

    }
}
