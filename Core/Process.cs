using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MED
{
    public abstract class Process : IProcess, IConsumer, IProvider
    {
        public Process(string name, Performance performance = null, Control invokeHandler = null, IConsumer consumer = null, bool isAsynchrone = false)
        {
            InvokeHandler = invokeHandler;
            IsAsynchrone = isAsynchrone;
            //Consumer = consumer;

            Name = name.Trim();

            Performance = performance == null ? MED.Performance.Empty() : performance;

            LoadSettings();
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
            if(typeName==Name)
                return $"{Name}({ProcessState})";
            return $"{typeName}[{Name}]({ProcessState})";
        }

        public virtual bool AddConsumer(IConsumer consumer, string property = "ProcessState")
        {

            //switch (property)
            //{
            //    case "Image":
            //        var type = consumer.GetType();
            //        break;
            //    default:
                    RemoveHandler($"On{property}Changed", consumer, consumer.GetType(), $"{property}Changed");
                    AddHandler($"On{property}Changed", consumer, consumer.GetType(), $"{property}Changed");
                    //throw new ArgumentOutOfRangeException($"AddConsumer( IConsumer, string Property = \"{AddConsumer}\" UNKNOWN ");
            //        break;
            //}

            return true;
        }

        //private IConsumer _Consumer;
        //[Browsable(false)]
        //public virtual IConsumer Consumer { get; set; }

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

                    foreach(var consumerDelegate in delegateMethod.GetInvocationList())
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
                            Performance.Step($"-> {invoke_str}({consumer.GetType().Name}.{consumerDelegate.Method.Name})");
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

        public void AddHandler(string handler_field, IConsumer consumer, Type consumer_type, string consumer_method)
        {
            IProvider handler_obj = this;
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
            //TODO
            IProvider handler_obj = this;
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

        [Browsable(false)]
        public Control InvokeHandler;

        [Browsable(true)]//SIC unvisible
        public Performance Performance;

        #region Settings
        public virtual void LoadSettings(bool loadChildren = true)
        {
            Core.Settings.ClearCache(true, true, Name);

            Performance.LoadSettings(Name);
        }
        public virtual void SaveSettings(bool saveChildren = true)
        {
            Performance.SaveSettings(Name, saveChildren);
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
                if (_ProcessState != value && ProcessStateChanged != null)
                    ProcessStateChanged(this, _ProcessState = value);
                else
                    _ProcessState = value;
            }
        }

        public IProcess.ProcessStateChangedDelegate ProcessStateChanged;



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
