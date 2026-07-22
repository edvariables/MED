using MED.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace MED
{
    public class Process : IProcess, IConsumer, IProvider
    {
        public Process(string name, Performance performance = null, Control invokeHandler = null, IConsumer consumer = null, bool isAsynchrone = false)
        {
            InvokeHandler = invokeHandler;
            IsAsynchrone = isAsynchrone;
            //Consumer = consumer;

            if (name == "")
                name = this.GetType().Name;
            ProcessIcon = "Process";

            Name = name;

            Performance = performance == null ? MED.Performance.Empty() : performance;

        }


        public virtual void Dispose()
        {
            Disposing = true;

            Stop();

            ProcessState = ThreadState.Aborted;

            RemovePropertyDelegateConsumers();

            Performance = null;
            //_Consumer = null;
            InvokeHandler = null;

            if (Disposing)
                IsDisposed = true;
        }

        public override string ToString()
        {
            var typeName = GetType().Name;
            if (typeName == Name)
                return $"{Name}({ProcessState})";
            return $"{typeName}[{Name}]({ProcessState})";
        }

        /**
         * Delegates ans consumers
         * 
         * */
        public virtual bool AddConsumer(IConsumer consumer, string property = "ProcessState")
        {
            var b = ProcessStatic.AddConsumer(this, consumer, property);

            PropertiesConsumers_CacheReset();

            return b;
        }

        /**
         * 
         * */
        protected List<IProcess> GetConsumers(string propertyName = "")
        {
            return GetPropertyDelegateConsumers(propertyName).Value ?? new();

            //var consumers = new List<IProcess>();
            //foreach (var onChangedDelegate in GetOnChangedDelegates(propertyName))
            //    consumers.AddRange(GetOnChangedConsumers(onChangedDelegate));
            //return consumers;
        }
        /**
         * 
         * */
        protected List<MulticastDelegate> GetOnChangedDelegates(string propertyName = "") => ProcessStatic.GetOnChangedDelegates(this, propertyName);

        /**
         * 
         * */
        protected MulticastDelegate GetOnChangedDelegate(string propertyName)
        {
            var onChangedDelegates = GetOnChangedDelegates(propertyName);
            if (onChangedDelegates.Count == 0)
                return null;
            return onChangedDelegates.First();

        }
        /**
         * 
         * */
        protected List<IProcess> GetOnChangedConsumers(string propertyName = "") => ProcessStatic.GetOnChangedConsumers(GetOnChangedDelegate(propertyName));

        protected List<IProcess> GetOnChangedConsumers(MulticastDelegate onChangedDelegate) => ProcessStatic.GetOnChangedConsumers(onChangedDelegate);

        /**
         * 
         * */
        protected List<string> GetProperties(string propertyName = "") => GetPropertiesDelegatesConsumers(propertyName).Keys.ToList();

        //List<string> properties = new();
        //foreach (var onChangedDelegate in GetOnChangedDelegates(propertyName))
        //{
        //    string delegateName = onChangedDelegate.GetMethodInfo().Name;
        //    if (propertyName == ""
        //        || delegateName == $"On{propertyName}Changed"
        //        || delegateName == $"{propertyName}Changed")
        //    {
        //        properties.Add(delegateName);
        //        if (propertyName != "")
        //            break;
        //    }

        //}

        //return properties;
        //}
        /**
         * 
         * */
        protected bool PropertyExists(string propertyName) => GetProperties(propertyName).Count > 0;

        /**
         * _PropertiesDelegatesConsumers
         * */
        private Dictionary<string, KeyValuePair<MulticastDelegate, List<IProcess>>> _PropertiesDelegatesConsumers;
        protected void PropertiesConsumers_CacheReset(string propertyName = "")
        {
            if (_PropertiesDelegatesConsumers != null)
            {
                if (propertyName != "")
                {
                    if (_PropertiesDelegatesConsumers.ContainsKey(propertyName))
                        _PropertiesDelegatesConsumers.Remove(propertyName);
                }
                else
                    _PropertiesDelegatesConsumers = null;
            }
        }
        /**
         * 
         * */
        public KeyValuePair<MulticastDelegate, List<IProcess>> GetPropertyDelegateConsumers(string propertyName = "", bool evenEmpty = true)
        {
            var dic = GetPropertiesDelegatesConsumers(propertyName, evenEmpty);
            if (dic.Count == 0)
                return new();
            return dic.First().Value;
        }
        /**
         * 
         * */
        public void RemovePropertyDelegateConsumers(string propertyName = "")
        {
            if (_PropertiesDelegatesConsumers != null)
                if (propertyName != "")
                {
                    if (_PropertiesDelegatesConsumers.ContainsKey(propertyName))
                        _PropertiesDelegatesConsumers.Remove(propertyName);
                }
                else
                    _PropertiesDelegatesConsumers = new();
        }
        /**
         * 
         * */
        public void CleanPropertiesDelegatesConsumers(string propertyName = "")
        {
            if (_PropertiesDelegatesConsumers == null)
                return;
            foreach (var kvp in GetPropertiesDelegatesConsumers(propertyName).ToArray())
            {
                List<IProcess> processes = kvp.Value.Value;
                foreach (var process in processes.ToArray())
                {
                    if ((process is Process) && (process as Process).IsDisposed
                        || (process is Control) && (process as Control).IsDisposed)
                    {
                        processes.Remove(process);
                        if (processes.Count == 0)
                            _PropertiesDelegatesConsumers.Remove(propertyName);
                    }
                }
            }
        }

        /**
         * 
         * */
        public Dictionary<string, KeyValuePair<MulticastDelegate, List<IProcess>>> GetPropertiesDelegatesConsumers(string propertyName = "", bool evenEmpty = true)
        {
            if (_PropertiesDelegatesConsumers != null)
            {
                if (propertyName != "")
                {
                    if (_PropertiesDelegatesConsumers.ContainsKey(propertyName))
                    {
                        Dictionary<string, KeyValuePair<MulticastDelegate, List<IProcess>>> dic = new();
                        dic.Add(propertyName, _PropertiesDelegatesConsumers[propertyName]);
                        return dic;
                    }
                }
                else
                    return _PropertiesDelegatesConsumers;
            }
            Dictionary<string, KeyValuePair<MulticastDelegate, List<IProcess>>> propertiesDelegatesConsumers = new();
            foreach (var onChangedDelegate in GetOnChangedDelegates(propertyName))
            {
                string prop = onChangedDelegate.GetMethodInfo().Name;
                if (prop.StartsWith("On"))
                    prop = prop.Substring(2);
                if (prop.EndsWith("Changed"))
                    prop = prop.Substring(0,prop.Length - "Changed".Length);

                List<IProcess> consumers;
                if ((consumers = GetOnChangedConsumers(onChangedDelegate)) != null || evenEmpty)
                {
                    KeyValuePair<MulticastDelegate, List<IProcess>> delegatesConsumers = new(onChangedDelegate, consumers);
                    propertiesDelegatesConsumers.Add(prop, delegatesConsumers);
                }
            }
            if (propertyName != "")
            {

                Dictionary<string, KeyValuePair<MulticastDelegate, List<IProcess>>> dic = new();

                if (!propertiesDelegatesConsumers.ContainsKey(propertyName))
                    return dic;
                dic.Add(propertyName, propertiesDelegatesConsumers[propertyName]);

                if (_PropertiesDelegatesConsumers == null)
                    _PropertiesDelegatesConsumers = new();
                _PropertiesDelegatesConsumers[propertyName] = dic.First().Value;
                return dic;
            }

            return _PropertiesDelegatesConsumers = propertiesDelegatesConsumers;
        }

        /***
         * Invoke
         * 
         * */

        public bool IsInvokingPropertyChanged(Delegate delegateMethod) => ProcessStatic.IsInvokingPropertyChanged(this, delegateMethod);

        public virtual void InvokePropertyChanged(IProvider sender, Delegate delegateMethod, EventArgs e) => ProcessStatic.InvokePropertyChanged(this, sender, delegateMethod, e);
        //    {
        //        if (InvokeHandler == null || InvokeHandler.Disposing || InvokeHandler.IsDisposed)
        //            return;
        //        if (delegateMethod != null && IsRunning)
        //        {
        //            if (IsInvokingPropertyChanged(delegateMethod))
        //            {
        //                Performance.Alert($"IsInvokingPropertyChanged {delegateMethod.Method.Name}");
        //                return;
        //            }

        //            //IsAsynchrone but if next Consumer is also asynchrone
        //            try
        //            {

        //                _IsInvokingPropertyChanged.Add(delegateMethod);

        //                foreach (var consumerDelegate in delegateMethod.GetInvocationList())
        //                {
        //                    var consumer = consumerDelegate.Target as IConsumer;
        //bool invoke = IsAsynchrone && !consumer.IsAsynchrone;
        //string invoke_str = invoke ? "Invoke" : "Call";

        //                    if (invoke)
        //                    {
        //                        Performance.Debug($"-> PInvoke({consumer.GetType().Name}.{consumerDelegate.Method.Name}, {this is IProvider})");

        //                        InvokeHandler.Invoke(consumerDelegate, this is IProvider? (IProvider)this : sender, e);

        //Performance.Debug($"{invoke_str} done");
        //}
        //                    else
        //                    {
        //                        //delegateMethod.Method.Invoke(delegateMethod.Target, [this is IProvider ? (IProvider)this : sender]);
        //                        //Performance.Step($"-> {invoke_str}({consumer.GetType().Name}.{consumerDelegate.Method.Name})");
        //                        consumerDelegate.DynamicInvoke(/*delegateMethod.Target,*/ this is IProvider? (IProvider)this : sender, e);
        //}

        //}

        //                    //if (invoke)
        //                    //{
        //                    //    Performance.Debug($"invokeHandler.Invoke({delegateMethod.Method.Name}, {this is IProvider});");
        //                    //    invokeHandler.Invoke(delegateMethod, this is IProvider ? (IProvider)this : sender, e);
        //                    //    Performance.Debug($"done");
        //                    //}
        //                    //else
        //                    //{
        //                    //    //delegateMethod.Method.Invoke(delegateMethod.Target, [this is IProvider ? (IProvider)this : sender]);
        //                    //    delegateMethod.DynamicInvoke(/*delegateMethod.Target,*/ this is IProvider ? (IProvider)this : sender, e);
        //                    //}
        //                }
        //                catch (Exception ex)
        //                {
        //                    Performance.Error("InvokePropertyChanged", ex);
        //                }
        //                finally
        //                {
        //    _IsInvokingPropertyChanged.Remove(delegateMethod);
        //}
        //            }
        //        }

        public void AddHandler(string handler_field, IConsumer consumer, Type consumer_type, string consumer_method)
        {
            ProcessStatic.AddHandler(this, handler_field, consumer, consumer_type, consumer_method);
            PropertiesConsumers_CacheReset();
        }

        public void RemoveHandler(string handler_field, IConsumer consumer, Type consumer_type, string consumer_method)
        {
            ProcessStatic.RemoveHandler(this, handler_field, consumer, consumer_type, consumer_method);
            PropertiesConsumers_CacheReset();
        }

        public virtual Dictionary<string, object> ObjectsProperties
        {
            get
            {
                var dict = new Dictionary<string, object>();
                dict.Add(this.Name, this);
                dict.Add(this.Name + ".Performance", Performance);

                return dict;
            }
        }

        [ReadOnly(true)]
        public bool IsAsynchrone { get; set; }

        [ReadOnly(true)]
        public string Name { get; set; }
        public string ProcessIcon { get; set; }

        [Browsable(false)]
        public Control InvokeHandler { get; set; }

        [Browsable(true)]
        public Performance Performance { get; set; }

        #region Settings

        [Browsable(true)]
        public ProcessSettings ProcessSettings { get; set; }

        public virtual void LoadSettings(ProcessSettings settings = null, string fileName = "")
        {
            if (settings == null)
                ProcessSettings = ProcessSettings.FromFile(fileName);
            else
                ProcessSettings = settings;

            LoadProcess(ProcessSettings.Root);

            Performance.LoadSettings(ProcessSettings.ChildSettings("Perf"));
        }

        public virtual void LoadProcess(JsonNode node)
        {

        }

        public virtual void SaveSettings(ProcessSettings settings = null, string fileName = "")
        {
            if (settings == null)
                settings = ProcessSettings;

            if (settings != null)
            {
                SaveProcess(settings.Root.AsObject());

                if (fileName != "" || settings.FileName != "")
                    settings.Save(fileName);
            }
        }


        public virtual JsonObject SaveProcess(JsonObject node = null)
        {
            if (node == null)
                node = new JsonObject();
            var type = this.GetType();
            var assembly = type.Assembly.Location;
            if (Directory.GetParent(assembly).FullName == Directory.GetParent(Assembly.GetExecutingAssembly().Location).FullName)
                assembly = type.AssemblyQualifiedName;

            node["ProcessClass"] = type.FullName;
            node["ProcessLib"] = assembly;
            node["Name"] = Name;
            node["IsAsynchrone"] = IsAsynchrone;

            node["Perf"] = Performance.SaveNode();

            return node;
        }
        #endregion


        private bool _IsRunning;
        public bool IsRunning
        {
            get
            {
                if (this.IsDisposed || this.Disposing)
                    return _IsRunning = false;

                return _IsRunning = (ProcessState == ThreadState.Running || ProcessState == ThreadState.Suspended);
            }
        }

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

            Performance.Start($"Start {this.ToString()}", true);

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
        public virtual ThreadState ProcessState
        {
            get => _ProcessState;
            set
            {
                if (_ProcessState != value && OnProcessStateChanged != null)
                    OnProcessStateChanged(this, _ProcessState = value);
                else
                    _ProcessState = value;
            }
        }

        public IProcess.ProcessStateChangedDelegate OnProcessStateChanged;



        public virtual void Pause()
        {
            if (IsRunning)
            {
                ProcessState = ThreadState.Suspended;
                Performance.Suspend("Process.Pause");
            }
        }

        public virtual void Resume()
        {
            if (IsRunning)
            {
                ProcessState = ThreadState.Running;
                Performance.Resume("Process.Resume");
            }
        }


    }
}
