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
        public ImageProcess(string name, Performance performance = null, Control invokeHandler = null, IImageConsumer imageConsumer = null, bool isAsynchrone = false)
            : base(name, performance, invokeHandler, imageConsumer, isAsynchrone)
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
                    if (InvokeHandler is IImageConsumer)
                        consumer = (IImageConsumer)InvokeHandler;
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
            Performance.Log($"isAsynchrone = {IsAsynchrone}");
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
            Performance.Debug($"ImageChanged from {sender.ToString()}");
            ImageProvider = sender; //Add

            if (ImageProviders.Count <= 1 || ImageProviders.Last() == sender)
            {
                if (ResetOnImageChanged)
                {
                    Performance.Debug($"ResetOnImageChanged {sender} " + (_Image == null ? "<null>" : "Bitmap") + " => <null>");
                    Image = null;
                }
                if (IsAsynchrone)
                {
                    //Generate in same thread
                    var image = GetImage(sender);
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
                if (_Image != null)
                    return _Image;
                if (!ImageIsProvided)
                    return _Image = GetImage();
                var firstProvider = ImageProvider;
                if (firstProvider == null)
                    return _Image;
                return _Image = GetImage(firstProvider);
            }
            set
            {
                //Performance.Debug($"Set_Image " + (_Image == null ? "<null>" : "Bitmap") + " => " + (value == null ? "<null>" : "Bitmap"));
                _Image = value;
            }
        }

        /**
         * GetImage abstract
         */
        public virtual Bitmap GetImage(IImageProvider provider = null)
        {
            Performance.Debug($"ImageProcess.GetImage ImageIsProvided={ImageIsProvided}, " + (provider == null ? "<null>" : "provider") + " / " + (ImageProvider == null ? "<null>" : "ImageProvider"));

            if (ImageIsProvided)
                if (provider != null)
                    return provider.Image;
                else
                    return ImageProvider?.Image;
            return null;
        }


        #region ImageProviders
        [Browsable(true)]
        public List<IImageProvider> ImageProviders { get; set; }

        [Browsable(false)]
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
        #endregion

        /**
         * InvokeImageChanged
         * 
         */
        public virtual void InvokeImageChanged(IImageProvider sender, EventArgs e) => InvokePropertyChanged(sender, OnImageChanged, e);

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
        [Browsable(false)]
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

                return _EmptyImage = new Bitmap(size.Width, size.Height);
            }
            set => _EmptyImage = value;
        }

        Bitmap _WaitingImage;
        [Browsable(false)]
        public Bitmap WaitingImage
        {
            get
            {
                if (_WaitingImage != null)
                    return _WaitingImage;

                _WaitingImage = ImageProvider?.Image;

                Size size = ImageSizeMax;
                if (size.IsEmpty)
                {
                    if (_WaitingImage != null)
                        size = _WaitingImage.Size;
                    if (size.IsEmpty)
                        size = new Size(256, 128);
                }

                string msg = "En attente";

                _WaitingImage = new Bitmap(size.Width, size.Height);
                Graphics graphics = Graphics.FromImage(_WaitingImage);

                SolidBrush brush = new(Color.LightSlateGray);
                graphics.FillRectangle(brush, 0F, 0F, size.Width, size.Height);

                Font font = new(FontFamily.GenericMonospace, 14F);
                brush = new(Color.DarkOliveGreen);
                var msgSize = graphics.MeasureString(msg, font, int.MaxValue, StringFormat.GenericDefault);

                graphics.DrawString(msg, font, brush, (size.Width - msgSize.Width) / 2, (size.Height - msgSize.Height) / 2);

                graphics.Dispose();
                return _WaitingImage;
            }
            set => _WaitingImage = value;
        }
    }
}
