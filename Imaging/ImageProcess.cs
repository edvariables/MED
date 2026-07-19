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
            ImageProviders = new();
            ImageConsumer = imageConsumer;
        }


        public virtual void Dispose()
        {
            base.Dispose();
            _ImageConsumer = null;
        }

        private IImageConsumer _ImageConsumer;
        [Browsable(false)]
        /**
         * Unique ImageConsumer
         */
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

        public override bool AddConsumer(IConsumer consumer, string property = "ProcessState")
        {

            switch (property)
            {
                case "Image":
                    if (consumer is IImageConsumer)
                    {
                        OnImageChanged -= (consumer as IImageConsumer).ImageChanged;
                        OnImageChanged += (consumer as IImageConsumer).ImageChanged;
                        return true;
                    }
                    break;
                default:
                    return base.AddConsumer(consumer, property);
            }

            return false;
        }

        /**
         * Process
         * 
         * */
        public override void Start()
        {

            base.Start();
            Performance.Log($"IsAynchrone = {IsAsynchrone}");
            Performance.Log($"ResetOnImageChanged = {ResetOnImageChanged}");
            Performance.Log($"ImageIsProvided = {ImageIsProvided}");

        }

        /**
         * Image
         * 
         */
        #region Image

        [Browsable(false)]
        public virtual Size ImageSizeMax { get; set; }

        public IImageProvider.ImageChangedDelegate OnImageChanged;

        [Browsable(true)]
        [ReadOnly(true)]
        public bool ResetOnImageChanged { get; protected set; }

        [Browsable(true)]
        [ReadOnly(true)]
        public bool ImageIsProvided { get; protected set; }

        /***
         * ImageChanged
         */
        [Browsable(false)]
        public virtual void ImageChanged(IImageProvider sender, EventArgs e)
        {
            Performance.Step($"ImageChanged from {sender.ToString()}");
            ImageProvider = sender; //Add

            if (ImageProviders.Count <= 1 || ImageProviders.Last() == sender)
            {
                if (ResetOnImageChanged)
                {
                    Performance.Debug($"ResetOnImageChanged");
                    Image = null;
                }
                if (IsAsynchrone)
                {
                    //Generate in same thread
                    var image = Image;
                }
                InvokeImageChanged(sender, e);
            }
            else
                Performance.Debug($"Waiting for last provider {sender} => {ImageProviders.Last()}");
        }

        private Bitmap _Image;
        [Browsable(false)]
        public virtual Bitmap Image
        {
            get
            {
                if (_Image != null || !ImageIsProvided)
                    return _Image;
                var firstProvider = ImageProvider;
                if (firstProvider == null)
                    return _Image;
                return _Image = firstProvider.Image;
            }
            set
            {
                //Performance.Debug($"Set_Image " + (_Image == null ? "<null>" : "Bitmap") + " => " + (value == null ? "<null>" : "Bitmap"));
                _Image = value;
            }
        }

        [Browsable(true)]
        public List<IImageProvider> ImageProviders { get; set; }

        [Browsable(true)]
        public IImageProvider ImageProvider
        {
            get => ImageProviders.Count == 0 ? null : ImageProviders.First();
            set
            {
                if (value == this)
                    return;
                if (!ImageProviders.Contains(value))
                    ImageProviders.Add(value);
            }
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

        /**
         * Settings
         * 
         * */
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
        public override void SaveSettings(bool saveChildren = false)
        {

            Core.Settings.SetValue("ImageSizeMax", Name, ImageSizeMax.IsEmpty ? "" : ImageSizeMax);

            base.SaveSettings(saveChildren);
        }
        #endregion


        Bitmap _EmptyImage;
        public Bitmap EmptyImage
        {
            get
            {
                if (_EmptyImage != null)
                    return _EmptyImage;

                _EmptyImage = ImageProvider?.Image;

                Size size = ImageSizeMax;
                if (size.IsEmpty)
                {
                    if (_EmptyImage != null)
                        size = _EmptyImage.Size;
                    if (size.IsEmpty)
                        size = new Size(256, 128);
                }

                string msg = "En attente";

                _EmptyImage = new Bitmap(size.Width, size.Height);
                Graphics graphics = Graphics.FromImage(_EmptyImage);

                SolidBrush brush = new(Color.LightSlateGray);
                graphics.FillRectangle(brush, 0F, 0F, size.Width, size.Height);

                Font font = new(FontFamily.GenericMonospace, 14F);
                brush = new(Color.DarkOliveGreen);
                var msgSize = graphics.MeasureString(msg, font, int.MaxValue, StringFormat.GenericDefault);

                graphics.DrawString(msg, font, brush, (size.Width - msgSize.Width) / 2, (size.Height - msgSize.Height) / 2);

                graphics.Dispose();
                return _EmptyImage;
            }
            set => _EmptyImage = value;
        }
    }
}
