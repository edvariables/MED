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
        public ProcessForm():base()
        {
            this.FormClosed += Form_FormClosed;
        }
        #region Settings


        protected virtual void Form_FormClosed(object sender, FormClosedEventArgs e)
        {
            Stop();
        }

        [ReadOnly(true)]
        public bool IsAsynchrone { get; set; }

        protected virtual void LoadSettings()
        {
            Core.Settings.ClearCache(true, true, this.Name);
        }
        public virtual void SaveSettings()
        {
            //if (Processes != null)
            //    foreach (var proc in Processes)
            //        proc.SaveSettings();
        }
        #endregion


        [Browsable(true)]
        public virtual List<IProcess> Processes { get; set; }


        protected virtual void DisposeProcesses()
        {

            if (Processes != null)
            {
                foreach (var handler in Processes)
                    if( handler is IDisposable)
                        (handler as IDisposable).Dispose();
                Processes = null;
            }
        }

        protected virtual void InitializeProcesses(bool resetAll = false)
        {

            if (resetAll)
                DisposeProcesses();

            if (Processes != null && !resetAll)
            {
                foreach (var handler in Processes)
                    handler.Stop();

                ////Restaure delegates
                //ImageProcess prevHandler = null;
                //foreach (var handler in ImageProcesses)
                //{
                //    if (prevHandler != null)
                //        handler.OnImageChanged += prevHandler.ImageChanged;
                //    else
                //        handler.OnImageChanged += this.ImageChanged;

                //    prevHandler = handler;
                //}

                return;
            }

            if (Processes == null)
                Processes = new();
            else
                Processes.Clear();
        }

        public Performance Performance;

        public bool IsRunning { get => ProcessState == ThreadState.Running || ProcessState == ThreadState.Suspended; }


        public IProcess.ProcessStateChangedDelegate ProcessStateChanged;

        private ThreadState _ProcessState = ThreadState.Unstarted;
        public System.Threading.ThreadState ProcessState
        {
            get
            {
                return _ProcessState;
            }
            set
            {
                var changed = value != _ProcessState;
                _ProcessState = value;
                if (changed && ProcessStateChanged != null)
                    ProcessStateChanged(this, value);
            }
        }


        #region Process

        /**
         * 
         * 
         */
        public virtual void Start()
        {
            if (IsRunning)
            {
                if (ProcessState == System.Threading.ThreadState.Suspended)
                    ProcessState = ThreadState.Running;
                return;
            }

            ProcessState = ThreadState.Unstarted;

            InitializeProcesses();

            //Inheritor must set ProcessState = ThreadState.Start;
        }

        /**
         * 
         * 
         */
        public virtual void Stop()
        {

            if (!IsRunning)
                return;

            ProcessState = ThreadState.StopRequested;

            if (Processes == null)
                return;

            foreach (var item in Processes.Reverse<IProcess>())
            {
                item.Stop();
            }

            //Inheritor must set ProcessState = ThreadState.Stopped;
        }

        public virtual void Resume()
        {
            if (IsRunning)
                foreach (var item in Processes)
                    item.Resume();

            ProcessState = ThreadState.Running;
        }

        public virtual void Pause()
        {
            if (IsRunning)
                foreach (var item in Processes)
                    item.Pause();

            ProcessState = ThreadState.Suspended;
        }

        #endregion

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
    }
}
