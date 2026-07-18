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
        public Process(string paramSection, Performance performance = null, Form formHandler = null, IConsumer consumer = null, bool isAynchrone = false)
        {
            FormHandler = formHandler;
            IsAsynchrone = isAynchrone;
            Consumer = consumer;

            Name = paramSection.Trim();
            Performance = performance == null ? MED.Performance.Empty() : performance;

            LoadSettings();
        }


        public virtual void Dispose()
        {
            Disposing = true;

            Stop();

            ProcessState = ThreadState.Aborted;

            Performance = null;
            _Consumer = null;
            FormHandler = null;

            if (Disposing)
                IsDisposed = true;
        }

        public IConsumer AddConsumer(IConsumer consumer)
        {
            throw new NotImplementedException();
        }

        private IConsumer _Consumer;
        [Browsable(false)]
        public virtual IConsumer Consumer { get; set; }

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
        public Form FormHandler;

        [Browsable(true)]//SIC unvisible
        public Performance Performance;

        protected virtual void LoadSettings()
        {
            Core.Settings.ClearCache(true, true, Name);

            Performance.LoadSettings(Name);
        }
        public virtual void SaveSettings()
        {
            Performance.SaveSettings(Name);
        }

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
                Performance.Stop();

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

            Performance.Start();

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
