using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MED
{
    public class Processes : Process
    {
        public Processes(string name = "MED.Project", Performance performance = null, Control invokeHandler = null, IConsumer consumer = null, bool isAsynchrone = false)
            : base(name == null || name == "" ? "MED.Project" : name, performance, invokeHandler, consumer, isAsynchrone)
        {
            Items = new();
        }

        private Logger _Logger { get; set; }

        [Browsable(false)]
        public Logger Logger
        {
            get => _Logger;
            set
            {
                if (value != null && (Performance == null || Performance.IsEmpty))
                    Performance = new(Name, value/*, false TODO*/);
                else
                    Performance.Logger = value;
                _Logger = value;
            }
        }

        #region Settings

        [Browsable(true)]
        public override string SettingsPath { get; set; }

        public override void LoadSettings(bool loadChildren = true)
        {
            base.LoadSettings(loadChildren);

            //Horizontal = (bool)Core.Settings.GetValue("Horizontal", Name, Horizontal);

            InitializeProcesses(false);
        }
        public override void SaveSettings(bool saveChildren = true)
        {
            //Core.Settings.SetValue("Horizontal", Name, Horizontal);

            base.SaveSettings(saveChildren);
        }
        #endregion

        #region Processes

        [Browsable(true)]
        public virtual List<IProcess> Items { get; protected set; }


        public virtual void DisposeProcesses()
        {

            if (Items != null)
            {
                foreach (var handler in Items)
                    if (handler is IDisposable)
                        (handler as IDisposable).Dispose();
                Items = null;
            }
        }

        public virtual void InitializeProcesses(bool resetAll = false)
        {

            if (resetAll)
                DisposeProcesses();

            if (Items != null && !resetAll)
            {
                foreach (var handler in Items)
                    handler.Stop();

                return;
            }

            if (Logger == null)
            {
                Logger = new();
            }
            else
                Logger.Clear();

            if (Performance == null || resetAll)
            {
                Performance = new(this.Name, Logger);
            }

            if (Items == null)
                Items = new();
            else
                Items.Clear();
        }

        #endregion

        #region Process

        /**
         * 
         * 
         */
        public override void Start()
        {
            base.Start();

            InitializeProcesses();

            foreach (var item in Items)
            {
                item.Start();
            }

            ProcessState = ThreadState.Running;
        }

        /**
         * 
         * 
         */
        public override void Stop()
        {

            if (!IsRunning)
                return;

            base.Stop();

            if (Items == null)
                return;

            foreach (var item in Items.Reverse<IProcess>())
            {
                item.Stop();
            }
        }

        public override void Resume()
        {
            base.Resume();

            if (IsRunning)
                foreach (var item in Items)
                    item.Resume();

            ProcessState = ThreadState.Running;
        }

        public override void Pause()
        {
            ProcessState = ThreadState.Suspended;

            if (IsRunning)
                foreach (var item in Items)
                    item.Pause();
        }

        #endregion

        /**
         * ObjectsProperties
         */
        public override Dictionary<string, object> ObjectsProperties
        {
            get
            {
                var dict = new Dictionary<string, object>();
                dict.Add(this.Name, this);
                if( ! Performance.IsEmpty)
                    dict.Add(this.Name + ".Performance", Performance);

                return dict;
            }
        }
    }
}