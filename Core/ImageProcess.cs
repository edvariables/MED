using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MED
{
    public abstract class ImageProcess : IDisposable, IImageConsumer, IImageProvider
    {
        public ImageProcess(string paramSection, StringBuilder progressMessage = null, Form formHandler = null, IImageConsumer imageConsumer = null, bool isAynchrone = false)
        {
            FormHandler = formHandler;
            IsAsynchrone = isAynchrone;
            if (imageConsumer == null)
            {
                if (formHandler is IImageConsumer)
                    OnImageChanged = ((IImageConsumer)formHandler).ImageChanged;
            }
            else
                OnImageChanged = imageConsumer.ImageChanged;
            ParamSection = paramSection.Trim();
            ProgressMessage = progressMessage;

            LoadSettings();
        }

        public bool IsAsynchrone { get; set; }
        public string ParamSection { get; set; }

        public Form FormHandler;
        public StringBuilder ProgressMessage;

        public Performance Performance = new();
        public KnownColor PerfColor;

        public delegate void ImageChangedDelegate(IImageProvider sender);
        public ImageChangedDelegate OnImageChanged;

        protected IImageProvider ImageProvider;

        /***
         * ImageChanged
         */
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
                    Performance.Resume("OnImageChanged", true);
                    OnImageChanged(this is IImageProvider ? (IImageProvider)this : sender);
                    Performance.Pause("Event raised");
                }
            }
        }

        /**
         * LoggerEnabled
         * */
        private bool _LoggerEnabled;
        public bool LoggerEnabled
        {
            get
            {
                return _LoggerEnabled;
            }
            set
            {
                _LoggerEnabled = value;
                if (Performance != null)
                    Performance.Enabled = _LoggerEnabled;
            }
        }

        protected virtual void LoadSettings()
        {
            LoggerEnabled = bool.Parse(Core.Settings.GetValue("Logger", ParamSection, true).ToString());
            PerfColor = (KnownColor)Enum.Parse(typeof(KnownColor), Core.Settings.GetValue("PerfColor", ParamSection, KnownColor.Green).ToString());
        }
        public virtual void SaveSettings()
        {
            Core.Settings.SetValue("Logger", ParamSection, LoggerEnabled);
            Core.Settings.SetValue("PerfColor", ParamSection, PerfColor);
            Core.Settings.Save();
        }

        protected virtual void PerformanceStart()
        {
            bool perfColored = Performance == null ? false : Performance.LoggerColored;

            Performance = new(ParamSection, ProgressMessage, LoggerEnabled, PerfColor);
            Performance.LoggerColored = perfColored;

            // Create the counters.
            Performance.Start();
        }

        private bool _IsRunning;
        public bool IsRunning
        {
            get
            {
                if (this.IsDisposed || this.Disposing)
                    return _IsRunning = false;
                return _IsRunning;
            }
            protected set { _IsRunning = value; }
        }

        public virtual void Stop()
        {

            if (Performance != null && Performance.IsRunning)
                Performance.Stop();

            IsRunning = false;

            if (Disposing)
                IsDisposed = true;
            //else if (!IsDisposed && RenderChanged != null)
            //    RenderChanged(this);
        }

        /**
         * Run
         * 
         */
        public virtual void Run()
        {
            IsRunning = true;

            PerformanceStart();
        }

        public bool Disposing { get; private set; }
        public bool IsDisposed { get; private set; }
        public abstract Bitmap Image { get; set; }
        public bool HasImageChanged { get; set; }

        public virtual void Dispose()
        {
            Disposing = true;
            ImageProvider = null;
            ProgressMessage = null;
            Stop();
        }

    }
}
