using MED.Core;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;

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

        public static bool AddConsumer(IProvider provider, IConsumer consumer, string property = "ProcessState")
        {

            //switch (property)
            //{
            //    case "Image":
            //        var type = consumer.GetType();
            //        break;
            //    default:
            RemoveHandler(provider, $"On{property}Changed", consumer, consumer.GetType(), $"{property}Changed");
            AddHandler(provider, $"On{property}Changed", consumer, consumer.GetType(), $"{property}Changed");
            //throw new ArgumentOutOfRangeException($"AddConsumer( IConsumer, string Property = \"{AddConsumer}\" UNKNOWN ");
            //        break;
            //}

            return true;
        }
        public virtual bool AddConsumer(IConsumer consumer, string property = "ProcessState")
        {
            return Process.AddConsumer(this, consumer, property);
        }

        /**
         * 
         * */
        protected List<IProcess> GetConsumers(string propertyName = "")
        {
            List<IProcess> consumers = new();
            foreach (var member in this.GetType().GetFields())
            {
                if (!member.FieldType.BaseType.Equals(typeof(MulticastDelegate))) continue;
                if (member.Name == $"On{propertyName}Changed" || member.Name == $"{propertyName}Changed")
                {
                    MulticastDelegate del = (MulticastDelegate)(member.GetValue(this));
                    if (del == null)
                        continue;
                    foreach (var invocation in del.GetInvocationList())
                    {
                        if (invocation.Target is IProcess)
                            consumers.Add((IProcess)invocation.Target);
                    }
                }
            }
            return consumers;

        }
        private List<Delegate> _IsInvokingPropertyChanged = new();

        protected bool IsInvokingPropertyChanged(Delegate delegateMethod)
        {
            return _IsInvokingPropertyChanged.Contains(delegateMethod);
        }

        public virtual void InvokePropertyChanged(IProvider sender, Delegate delegateMethod, EventArgs e)
        {
            if (InvokeHandler == null || InvokeHandler.Disposing || InvokeHandler.IsDisposed)
                return;
            if (delegateMethod != null && IsRunning)
            {
                if (IsInvokingPropertyChanged(delegateMethod))
                {
                    Performance.Alert($"IsInvokingPropertyChanged {delegateMethod.Method.Name}");
                    return;
                }

                //IsAsynchrone but if next Consumer is also asynchrone
                try
                {

                    _IsInvokingPropertyChanged.Add(delegateMethod);

                    foreach (var consumerDelegate in delegateMethod.GetInvocationList())
                    {
                        var consumer = consumerDelegate.Target as IConsumer;
                        bool invoke = IsAsynchrone && !consumer.IsAsynchrone;
                        string invoke_str = invoke ? "Invoke" : "Call";
                        //Performance.Step($"-> {invoke_str}({consumer.ToString()} . {consumerDelegate.Method.Name})");
                        //if (invocationList.Count() > 0)
                        //{
                        //Performance.Step($"{invoke_str} {delegateMethod.Method.Name} for {invocationList.Count()}");
                        //foreach (var del in invocationList)
                        //{
                        // Performance.Step($"-> {del.Target.ToString()} . {del.Method.Name}");
                        //}

                        if (invoke)
                        {
                            Performance.Debug($"-> PInvoke({consumer.GetType().Name}.{consumerDelegate.Method.Name}, {this is IProvider})");

                            InvokeHandler.Invoke(consumerDelegate, this is IProvider ? (IProvider)this : sender, e);

                            Performance.Debug($"{invoke_str} done");
                        }
                        else
                        {
                            //delegateMethod.Method.Invoke(delegateMethod.Target, [this is IProvider ? (IProvider)this : sender]);
                            //Performance.Step($"-> {invoke_str}({consumer.GetType().Name}.{consumerDelegate.Method.Name})");
                            consumerDelegate.DynamicInvoke(/*delegateMethod.Target,*/ this is IProvider ? (IProvider)this : sender, e);
                        }

                    }

                    //if (invoke)
                    //{
                    //    Performance.Debug($"invokeHandler.Invoke({delegateMethod.Method.Name}, {this is IProvider});");
                    //    invokeHandler.Invoke(delegateMethod, this is IProvider ? (IProvider)this : sender, e);
                    //    Performance.Debug($"done");
                    //}
                    //else
                    //{
                    //    //delegateMethod.Method.Invoke(delegateMethod.Target, [this is IProvider ? (IProvider)this : sender]);
                    //    delegateMethod.DynamicInvoke(/*delegateMethod.Target,*/ this is IProvider ? (IProvider)this : sender, e);
                    //}
                }
                catch (Exception ex)
                {
                    Performance.Error("InvokePropertyChanged", ex);
                }
                finally
                {
                    _IsInvokingPropertyChanged.Remove(delegateMethod);
                }
            }
        }

        public void AddHandler( string handler_field, IConsumer consumer, Type consumer_type, string consumer_method)
        {
            Process.AddHandler(this, handler_field, consumer, consumer_type, consumer_method);
        }
        public static void AddHandler(IProvider handler_obj ,string handler_field, IConsumer consumer, Type consumer_type, string consumer_method)
        {
            var memberInfo = handler_obj.GetType().GetMember(handler_field);
            if (memberInfo == null)
                throw new Exception($"Le type {handler_obj.GetType().FullName} n'a pas de delegate {handler_field}");
            var eventInfo = (System.Reflection.FieldInfo)memberInfo.GetValue(0);

            var miHandler = consumer_type.GetMethod(consumer_method);
            if (miHandler == null)
                throw new Exception($"Le type '{consumer_type.FullName}' n'a pas de méthode {consumer_method}");
            Delegate handler =
                 Delegate.CreateDelegate(eventInfo.FieldType,
                                         consumer,
                                         miHandler);
            //TODO  
            //eventInfo.RemoveEventHandler(this, handler);
            eventInfo.SetValue(handler_obj, handler);
        }

        public void RemoveHandler(string handler_field, IConsumer consumer, Type consumer_type, string consumer_method)
        {
            Process.RemoveHandler(this, handler_field, consumer, consumer_type, consumer_method);
        }
        public static void RemoveHandler(IProvider handler_obj, string handler_field, IConsumer consumer, Type consumer_type, string consumer_method)
        {
            //TODO
            var memberInfo = handler_obj.GetType().GetMember(handler_field);
            if (memberInfo == null)
                throw new Exception($"Le type {handler_obj.GetType().FullName} n'a pas de delegate {handler_field}");
            var eventInfo = (System.Reflection.FieldInfo)memberInfo.GetValue(0);


            var miHandler = consumer_type.GetMethod(consumer_method);
            if (miHandler == null)
                throw new Exception($"Le type '{consumer_type.FullName}' n'a pas de méthode {consumer_method}");
            Delegate handler =
                 Delegate.CreateDelegate(eventInfo.FieldType,
                                         consumer,
                                         miHandler);
            //eventInfo.RemoveEventHandler(this, handler);
            //TODO eventInfo.SetValue(handler_obj, handler);

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

        public virtual void LoadSettings(string fileName)
        {
            ProcessSettings = ProcessSettings.FromFile(fileName);

            Performance.LoadSettings(ProcessSettings.ChildSettings("Perf"));
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

        public static IProcess CreateProcess(JsonNode node, Performance performance, Control invokeHandler)
        {
            string processClass = node["ProcessClass"].GetValue<string>();
            string processLib = node["ProcessLib"].GetValue<string>();
            string name = node["Name"].GetValue<string>();
            bool isAsynchrone = (bool)Parser.ObjectFromJsonNode(node["IsAsynchrone"], false);

            if (processClass == "")
                processClass = MethodBase.GetCurrentMethod().DeclaringType.FullName;

            try
            {
                IProcess item = (IProcess)AssemblyLoader.CreateObjectInstance(processLib, processClass, [name, performance.Sub(name), invokeHandler, null, isAsynchrone]);
                //IProcess item = (IProcess)Activator.CreateInstance(processLib, processClass, [name, performance.Sub(name), invokeHandler, null, isAsynchrone]);
                //Process item = new Process(name, performance.Sub(name), invokeHandler, null, isAsynchrone);
                return item;
            }
            catch (Exception ex)
            {
                return null;
                //                throw ex;
            }
            return null;
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
