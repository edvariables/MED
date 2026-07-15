using System;
using System.Collections.Generic;
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

        public bool IsAsynchrone { get; set; }
        public string ParamSection { get; set; }

        public Form FormHandler;

        public Performance Performance;

        public delegate void ImageChangedDelegate(IImageProvider sender);
        public ImageChangedDelegate OnImageChanged;

        protected IImageProvider ImageProvider;
        public Size ImageSizeMax { get; set; }

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
                    OnImageChanged(this is IImageProvider ? (IImageProvider)this : sender);
                }
            }
        }

        protected virtual void LoadSettings()
        {
            Performance.LoadSettings(ParamSection);

            var value = Core.Settings.GetValue("ImageSizeMax", ParamSection, Size.Empty);
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

            Performance.Start();
        }

        public bool Disposing { get; private set; }
        public bool IsDisposed { get; private set; }
        public virtual Bitmap Image { get; set; }
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
