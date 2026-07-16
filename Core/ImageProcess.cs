using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MED
{
    public abstract class ImageProcess : IDisposable, IImageConsumer, IImageProvider
    {
        public ImageProcess(string paramSection, Performance performance = null, Form formHandler = null, IImageConsumer imageConsumer = null, bool isAynchrone = false)
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
            Performance = performance == null ? performance.Empty() : performance;

            LoadSettings();
        }

        [Browsable(false)]
        public virtual Dictionary<string, object> ObjectsProperties
        {
            get
            {
                var dict = new Dictionary<string, object>();
                dict.Add(this.ParamSection, this);
                dict.Add(this.ParamSection + ".Performance", Performance);

                return dict;
            }
        }

        [ReadOnly(true)]
        public bool IsAsynchrone { get; set; }

        [ReadOnly(true)]
        public string ParamSection { get; set; }

        [Browsable(false)]
        public Form FormHandler;

        [Browsable(true)]//SIC unvisible
        public Performance Performance;

        public delegate void ImageChangedDelegate(IImageProvider sender);
        public ImageChangedDelegate OnImageChanged;

        public delegate void IsRunningChangedDelegate(ImageProcess sender);
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
            Core.Settings.ClearCache(true, true, ParamSection);

            Performance.LoadSettings(ParamSection);

            var value = Core.Settings.GetValue("ImageSizeMax", ParamSection, ImageSizeMax);
            if (value is Size)
                ImageSizeMax = (Size)value;
            else
                ImageSizeMax = Size.Empty;
        }
        public virtual void SaveSettings()
        {
            Performance.SaveSettings(ParamSection);

            Core.Settings.SetValue("ImageSizeMax", ParamSection, ImageSizeMax.IsEmpty ? "" : ImageSizeMax);
            Core.Settings.Save();
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
            protected set
            {
                if (_IsRunning != value && IsRunningChanged != null)
                    IsRunningChanged(this);
                _IsRunning = value;
            }
        }

        public virtual void Stop()
        {

            if (Performance != null && Performance.IsRunning)
                Performance.Stop();

            IsRunning = false;

            //Kills delegate links to object
            OnImageChanged = null;
            //if (OnImageChanged != null)
            //    foreach (var del in OnImageChanged.GetInvocationList())
            //        OnImageChanged -= (ImageChangedDelegate)del;

            if (Disposing)
                IsDisposed = true;
        }

        /**
         * Run
         * 
         */
        public virtual void Run()
        {
            IsRunning = true;

            Performance.Start();
        }


        [Browsable(false)]
        public bool Disposing { get; private set; }
        [Browsable(false)]
        public bool IsDisposed { get; private set; }

        [Browsable(false)]
        public virtual Bitmap Image { get; set; }

        [Browsable(false)]
        public virtual bool HasImageChanged { get; set; }

        public virtual void Dispose()
        {
            Disposing = true;
            ImageProvider = null;
            Performance = null;
            Stop();
        }

    }
}
