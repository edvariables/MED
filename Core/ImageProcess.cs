using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MED
{
    public abstract class ImageProcess : Process, IImageConsumer, IImageProvider
    {
        public ImageProcess(string paramSection, Performance performance = null, Form formHandler = null, IImageConsumer imageConsumer = null, bool isAynchrone = false)
            : base(paramSection, performance , formHandler , imageConsumer , isAynchrone )
        {
            ImageConsumer = imageConsumer;
        }


        public virtual void Dispose()
        {
            base.Dispose();
            _ImageConsumer = null;
        }

        public IConsumer AddConsumer(IConsumer consumer)
        {
            throw new NotImplementedException();
        }

        private IImageConsumer _ImageConsumer;
        [Browsable(false)]
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

        public IImageProvider.ImageChangedDelegate OnImageChanged;

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
                bool invoke = IsAsynchrone && !(ImageConsumer != null && ImageConsumer.IsAsynchrone);
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
            base.LoadSettings();

            var value = Core.Settings.GetValue("ImageSizeMax", Name, ImageSizeMax);
            if (value is Size)
                ImageSizeMax = (Size)value;
            else
                ImageSizeMax = Size.Empty;
        }
        public virtual void SaveSettings()
        {

            Core.Settings.SetValue("ImageSizeMax", Name, ImageSizeMax.IsEmpty ? "" : ImageSizeMax);

            base.SaveSettings();
        }

    }
}
