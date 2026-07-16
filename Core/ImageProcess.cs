using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MED
{
    public abstract class ImageProcess : IProcess, IDisposable, IImageConsumer, IImageProvider
    {
        public ImageProcess(string paramSection, Performance performance = null, Form formHandler = null, IImageConsumer imageConsumer = null, bool isAynchrone = false)
        {
            ProcessState = ThreadState.Unstarted;

            FormHandler = formHandler;
            IsAsynchrone = isAynchrone;
            if (imageConsumer == null)
            {
                if (formHandler is IImageConsumer)
                    OnImageChanged = ((IImageConsumer)formHandler).ImageChanged;
            }
            else
                OnImageChanged = imageConsumer.ImageChanged;
            Name = paramSection.Trim();
            Performance = performance == null ? MED.Performance.Empty() : performance;

            LoadSettings();
        }

        [Browsable(false)]
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

        public delegate void ImageChangedDelegate(IImageProvider sender);
        public ImageChangedDelegate OnImageChanged;

        public delegate void IsRunningChangedDelegate(ImageProcess sender, bool isRunning);
        public IsRunningChangedDelegate IsRunningChanged;

        protected IImageProvider ImageProvider;

        [Browsable(false)]
        public virtual Size ImageSizeMax { get; set; }

        /***
         * ImageChanged
         */
        [Browsable(false)]
        public virtual void ImageChanged(IImageProvider sender)
        {
            HasImageChanged = true;
            ImageProvider = sender;
            InvokeImageChanged(sender);
        }

        /**
         * InvokeImageChanged
         * 
         */
        public virtual void InvokeImageChanged(IImageProvider sender = null)
        {
            if (FormHandler == null || FormHandler.Disposing || FormHandler.IsDisposed)
                return;
            if (OnImageChanged != null && IsRunning)
            {
                if (IsAsynchrone)
                {
                    try
                    {
                        FormHandler.Invoke(OnImageChanged, this is IImageProvider ? (IImageProvider)this : sender);
                    }
                    catch { }
                }
                else
                {
                    OnImageChanged(this is IImageProvider ? (IImageProvider)this : sender);
                }
            }
        }

        protected virtual void LoadSettings()
        {
            Core.Settings.ClearCache(true, true, Name);

            Performance.LoadSettings(Name);

            var value = Core.Settings.GetValue("ImageSizeMax", Name, ImageSizeMax);
            if (value is Size)
                ImageSizeMax = (Size)value;
            else
                ImageSizeMax = Size.Empty;
        }
        public virtual void SaveSettings()
        {
            Performance.SaveSettings(Name);

            Core.Settings.SetValue("ImageSizeMax", Name, ImageSizeMax.IsEmpty ? "" : ImageSizeMax);
            Core.Settings.Save();
        }

        private bool _IsRunning;
        public bool IsRunning
        {
            get
            {
                if (this.IsDisposed || this.Disposing)
                    return _IsRunning = false;

                bool changed = _IsRunning != (ProcessState == ThreadState.Running || ProcessState == ThreadState.Suspended);
                _IsRunning = (ProcessState == ThreadState.Running || ProcessState == ThreadState.Suspended);
                if (changed && IsRunningChanged != null)
                    IsRunningChanged(this, _IsRunning);
                return _IsRunning;
            }
            protected set
            {
                bool changed = _IsRunning != value;
                _IsRunning = value;
                if (changed && IsRunningChanged != null)
                    IsRunningChanged(this, value);
            }
        }

        public virtual void Stop()
        {
            if (!IsRunning)
                return;

            ProcessState = ThreadState.StopRequested;

            if (Performance != null && Performance.IsRunning)
                Performance.Stop();

            //Kills delegate links to object
            OnImageChanged = null;
            //if (OnImageChanged != null)
            //    foreach (var del in OnImageChanged.GetInvocationList())
            //        OnImageChanged -= (ImageChangedDelegate)del;

            IsRunning = false;

            ProcessState = ThreadState.Stopped;

            if (Disposing)
                IsDisposed = true;
        }

        /**
         * Run
         * 
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

        [Browsable(false)]
        public virtual Bitmap Image { get; set; }

        [Browsable(false)]
        public virtual bool HasImageChanged { get; set; }

        public virtual ThreadState ProcessState { get; set; }

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

        public virtual void Dispose()
        {
            Disposing = true;
            ImageProvider = null;
            Performance = null;
            Stop();
        }
    }
}
