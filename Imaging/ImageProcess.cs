using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MED
{
    public abstract class ImageProcess : Process, IImageConsumer, IImageProvider
    {
        public ImageProcess(string name, Performance performance = null, Form formHandler = null, IImageConsumer imageConsumer = null, bool isAynchrone = false)
            : base(name, performance, formHandler, imageConsumer, isAynchrone)
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
                IImageConsumer consumer = value;
                if (consumer == null)
                {
                    if (FormHandler is IImageConsumer)
                        consumer = (IImageConsumer)FormHandler;
                }
                //Unlink previous Consumer
                if (_ImageConsumer != null)
                {
                    OnImageChanged -= _ImageConsumer.ImageChanged;
                    if (this is IMatFrameProvider
                        && _ImageConsumer is IMatFrameConsumer)
                    {
                        RemoveHandler("OnFrameChanged", consumer, typeof(IMatFrameConsumer), "FrameChanged");
                    }
                }
                //New Consumer
                _ImageConsumer = consumer;
                if (consumer != null)
                {
                    OnImageChanged -= consumer.ImageChanged;
                    OnImageChanged += consumer.ImageChanged;

                    if (this is IMatFrameProvider
                        && _ImageConsumer is IMatFrameConsumer)
                    {
                        AddHandler("OnFrameChanged", consumer, typeof(IMatFrameConsumer), "FrameChanged");
                        RemoveHandler("OnFrameChanged", consumer, typeof(IMatFrameConsumer), "FrameChanged");
                    }
                }
            }
        }

        protected IImageProvider ImageProvider;

        [Browsable(false)]
        public virtual Size ImageSizeMax { get; set; }

        #region Image
        public IImageProvider.ImageChangedDelegate OnImageChanged;

        private Bitmap _Image;
        [Browsable(false)]
        public virtual Bitmap Image
        {
            get
            {
                if (ImageProvider == null)
                    return _Image;

                return _Image = ImageProvider.Image;
            }
            set
            {
                _Image = value;
            }
        }

        /***
         * ImageChanged
         */
        [Browsable(false)]
        public virtual void ImageChanged(IImageProvider sender, EventArgs e)
        {
            ImageProvider = sender;
            InvokeImageChanged(sender, e);
        }

        /**
         * InvokeImageChanged
         * 
         */
        protected bool IsInvokingImageChanged;
        public virtual void InvokeImageChanged(IImageProvider sender, EventArgs e)
        {
            //bool invoke = IsAsynchrone && !(ImageConsumer != null && ImageConsumer.IsAsynchrone);
            //string invoke_str = invoke ? "Invoke" : "Call";
            //var invocationList = OnImageChanged.GetInvocationList();
            //if (invocationList.Count() > 1)
            //{
            //    Performance.Step($"{this.Name} : {invoke_str} for {invocationList.Count()}");
            //    foreach (var del in invocationList)
            //    {
            //        Performance.Step($"-> {del.Target.ToString()}.{del.Method.Name}");
            //    }
            //}
            InvokePropertyChanged(sender, OnImageChanged, e);
        }
        #endregion

        #region Settings
        public override void LoadSettings(bool loadChildren = true)
        {
            base.LoadSettings(loadChildren);

            var value = Core.Settings.GetValue("ImageSizeMax", Name, ImageSizeMax);
            if (value is Size)
                ImageSizeMax = (Size)value;
            else
                ImageSizeMax = Size.Empty;
        }
        public virtual void SaveSettings(bool saveChildren = false)
        {

            Core.Settings.SetValue("ImageSizeMax", Name, ImageSizeMax.IsEmpty ? "" : ImageSizeMax);

            base.SaveSettings(saveChildren);
        }
        #endregion

    }
}
