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
            FormHandler = formHandler;
            IsAsynchrone = isAynchrone;
            ImageConsumer = imageConsumer;

            Name = paramSection.Trim();
            Performance = performance == null ? MED.Performance.Empty() : performance;

            LoadSettings();
        }

        private IImageConsumer _ImageConsumer;
        public virtual IImageConsumer ImageConsumer
        {
            get => _ImageConsumer;
            set
            {
                if (value == null)
                {
                    if (FormHandler is IImageConsumer)
                    {
                        OnImageChanged -= ((IImageConsumer)FormHandler).ImageChanged;
                        OnImageChanged += ((IImageConsumer)FormHandler).ImageChanged;
                    }
                }
                else
                {
                    OnImageChanged -= value.ImageChanged;
                    OnImageChanged += value.ImageChanged;
                } 
                _ImageConsumer = value;
            }
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

        public delegate void ProcessStateChangedDelegate(IProcess sender, System.Threading.ThreadState state);
        public ProcessStateChangedDelegate ProcessStateChanged;

        protected IImageProvider ImageProvider;

        [Browsable(false)]
        public virtual Size ImageSizeMax { get; set; }


        [Browsable(false)]
        public virtual bool HasImageChanged { get; set; }

        [Browsable(false)]
        public virtual Bitmap Image { get; set; }

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
        protected bool IsInvokingImageChanged;
        public virtual void InvokeImageChanged(IImageProvider sender = null)
        {
            if (FormHandler == null || FormHandler.Disposing || FormHandler.IsDisposed)
                return;
            if (OnImageChanged != null && IsRunning)
            {
                //IsAsynchrone but if next Consumer is also asynchrone
                bool invoke = IsAsynchrone && !(ImageConsumer != null && ImageConsumer is IImageProvider && (ImageConsumer as IImageProvider).IsAsynchrone);
                string invoke_str = invoke ? "Invoke" : "Call";
                try
                {
                    IsInvokingImageChanged = true;
                    var invocationList = OnImageChanged.GetInvocationList();
                    if (invocationList.Count() > 1)
                    {
                        Performance.Step($"{this.Name} : {invoke_str} for {invocationList.Count()}");
                        foreach (var del in invocationList)
                        {
                            Performance.Step($"-> {del.Target.ToString()}.{del.Method.Name}");
                        }

                    }

                    if (invoke)
                        FormHandler.Invoke(OnImageChanged, this is IImageProvider ? (IImageProvider)this : sender);
                    else
                        OnImageChanged(this is IImageProvider ? (IImageProvider)this : sender);
                }
                catch (Exception ex)
                {
                    Performance.Step("ERROR : " + ex.Message);
                }
                finally
                {
                    IsInvokingImageChanged = false;
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

            IsRunning = false;

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

            IsInvokingImageChanged = false;

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

            Stop();

            ProcessState = ThreadState.Aborted;

            ImageProvider = null;
            Performance = null;
            _ImageConsumer = null;
            FormHandler = null;

            if (Disposing)
                IsDisposed = true;
        }
    }
}
