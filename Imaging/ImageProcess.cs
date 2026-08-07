using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;
using MED.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace MED.Imaging
{
    /**
     * abstract class ImageProcess : Process, IImageConsumer, IImageProvider
     * <summary>Image from an other provider process</summary>
     * */
    public abstract class ImageProcess : Process, IImageConsumer, IImageProvider
    {
        public ImageProcess(string name, Performance? performance = null, Control? invokeHandler = null, IImageConsumer? imageConsumer = null, bool isAsynchrone = false)
            : base(name, performance, invokeHandler, imageConsumer, isAsynchrone)
        {
            ProcessIcon = ProcessIconDefault = "Image";

            ImageProviders = new();
            ImageConsumer = imageConsumer;
        }


        public override void Dispose()
        {
            base.Dispose();
            _ImageConsumer = null;
        }

        private IImageConsumer? _ImageConsumer;
        [Browsable(false)]
        /**
         * Unique ImageConsumer
         */
        public virtual IImageConsumer? ImageConsumer
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

        [Browsable(true)]
        public List<IProcess> ImageConsumers { get => GetConsumers("Image"); }

        [Browsable(true)]
        public List<IProcess> FrameConsumers { get => GetConsumers("Frame"); }


        public override bool AddConsumer(IConsumer consumer, string property = "ProcessState") => base.AddConsumer(consumer, property);

        public override Dictionary<string, object> ObjectsProperties
        {
            get
            {
                var dict = base.ObjectsProperties;

                var consumers = new List<IProcess>();
                foreach (var proc in ImageConsumers)
                    consumers.Add(proc);
                if (consumers.Count > 0)
                    dict.Add("Images vers", consumers);

                consumers = new List<IProcess>();
                foreach (var proc in FrameConsumers)
                    consumers.Add(proc);
                if (consumers.Count > 0)
                    dict.Add("Frames vers", consumers);

                return dict;
            }
        }

        /**
         * Process
         * 
         * */
        public override void Start()
        {

            base.Start();
            Performance?.Log($"isAsynchrone = {IsAsynchrone}");
            Performance?.Log($"ResetOnImageChanged = {ResetOnImageChanged}");
            Performance?.Log($"ImageIsProvided = {ImageIsProvided}");

        }

        /**
         * Image
         * 
         */
        #region Image

        [Browsable(false)]
        public virtual Size ImageSizeMax { get; set; }

        [Browsable(true)]
        public virtual Size ImageSizeMin { get; set; }

        [Browsable(false)]
        public virtual System.Drawing.Region? ClipRegion { get; set; } = null;

        [Browsable(false)]
        public virtual System.Drawing.PointF Location { get; set; } = System.Drawing.PointF.Empty;

        [Browsable(false)]
        public virtual float Rotation { get; set; }

        [Browsable(true)]
        public virtual int FPSMax { get; set; } = 25;
        protected int FPSMaxDuration
        {
            get
            {
                if (FPSMax <= 0)
                    return 0;
                return 1000 / FPSMax;
            }
        }

        public IImageProvider.ImageChangedDelegate? OnImageChanged;

        [Browsable(true)]
        [ReadOnly(true)]
        /**
         * ResetOnImageChanged
         * <summary>in ImageChanged(){ ... if (ResetOnImageChanged)  Image = null;</summary>
         * */
        public bool ResetOnImageChanged { get; protected set; }

        [Browsable(true)]
        [ReadOnly(true)]
        /**
         * ImageIsProvided
         * <summary>ImageIsProvided means the image is provided by a IImageProvider process.
         * If false, image is sourced, from a file for example.</summary>
         * */
        public bool ImageIsProvided { get; protected set; }

        /***
         * ImageChanged
         */
        [Browsable(false)]
        public virtual void ImageChanged(IImageProvider sender, EventArgs e)
        {
            if (ProcessState != ThreadState.Running)
                return;
            //string? from = sender == this ? "myself" : sender.ToString();
            //Performance.Debug($"ImageChanged from {from}");
            ImageProvider = sender; //Add

            if (ImageProviders.Count <= 1 || ImageProviders.Last() == sender)
            {
                if (ResetOnImageChanged)
                {
                    //Performance.Debug($"ResetOnImageChanged {sender} " + (_Image == null ? "<null>" : "Bitmap") + " => <null>");
                    Image = null;
                }
                if (IsAsynchrone)
                {
                    //Generate in same thread
                    Image = GetImage(sender);
                }
                InvokeImageChanged(sender, e);
            }
            else
                Performance.Debug($"Waiting for last provider {sender} => {ImageProviders.Last()}");
        }

        protected Bitmap? _Image;
        [Browsable(false)]
        public virtual Bitmap? Image
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
        public virtual Bitmap? GetImage(IImageProvider? provider = null)
        {
            Performance?.Debug($"ImageProcess.GetImage ImageIsProvided={ImageIsProvided}, " + (provider == null ? "<null>" : "provider") + " / " + (ImageProvider == null ? "<null>" : "ImageProvider"));

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
        public IImageProvider? ImageProvider
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

        public override void LoadSettings(ProcessSettings? settings = null, string fileName = "")
        {
            base.LoadSettings(settings, fileName);

            if (settings == null)
                return;

            FPSMax = (int)settings.GetValue("FPSMax", FPSMax);

            var value = ProcessSettings.GetValue("ImageSizeMax", ImageSizeMax);
            if (value is Size)
                ImageSizeMax = (Size)value;
            else
                ImageSizeMax = Size.Empty;
            value = ProcessSettings.GetValue("ImageSizeMin", ImageSizeMin);
            if (value is Size)
                ImageSizeMin = (Size)value;
            else
                ImageSizeMin = Size.Empty;
        }
        public override JsonObject SaveProcess(JsonObject? node = null)
        {
            node = base.SaveProcess(node);

            node["ImageSizeMax"] = Parser.ObjectToString(ImageSizeMax);
            node["ImageSizeMin"] = Parser.ObjectToString(ImageSizeMin);
            if (FPSMax != 0)
                node["FPSMax"] = FPSMax;

            var consumers = new JsonObject();

            string[] properties = ["Image", "Frame"];
            foreach (var propertyName in properties)
            {
                JsonArray jsonCons = new JsonArray();
                foreach (var consumer in GetConsumers(propertyName))
                {
                    JsonObject item = new();

                    //item["ProcessClass"] = consumer.GetType().FullName;
                    item["Name"] = ProcessStatic.GetRelativePath(this, consumer);

                    jsonCons.Add(item);
                }
                if (jsonCons.Count > 0)
                    consumers[propertyName] = jsonCons;
            }
            if (consumers.Count > 0)
                node["Consumers"] = consumers;
            return node;
        }

        #endregion

        #region Contours Region

        public Region GetContourRegion(Bitmap image)
        {
            //Mat mat = Emgu.CV.BitmapExtension.ToMat(image);
            //Mat grayCurrent = new();
            //CvInvoke.CvtColor(mat, grayCurrent, Emgu.CV.CvEnum.ColorConversion.Bgra2Gray);
            Mat grayCurrent = ConvertRgbA2AlphaGray(image, Color.Transparent);
            var clipRegion = GetContourRegion(grayCurrent);
            if (clipRegion == null)
            {
                Mat white = Mat.Ones(grayCurrent.Rows, grayCurrent.Cols, grayCurrent.Depth, grayCurrent.NumberOfChannels);
                Mat dst = white - grayCurrent;
                clipRegion = GetContourRegion(dst);
                if (clipRegion != null)
                {
                    Region regionNot = new(new RectangleF(0, 0, image.Width, image.Height));
                    regionNot.Exclude(clipRegion);
                    Graphics gr = Graphics.FromImage(image);
                    var bounds = regionNot.GetBounds(gr);
                    gr.Dispose();

                    return regionNot;
                }
            }
            return clipRegion;
        }

        public Region GetContourRegion(Mat grayCurrent)
        {
            try
            {
                using (GraphicsPath grPath = new GraphicsPath())
                using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
                using (Mat hierarchy = new Mat())
                {
                    CvInvoke.FindContours(grayCurrent, contours, hierarchy, RetrType.List, ChainApproxMethod.ChainApproxSimple);

                    for (int i = 0; i < contours.Size; i++)
                    {
                        var contour = contours[i].ToArray();
                        if (contour.Length < 3)
                            continue;
                        grPath.AddPolygon(contour);
                    }

                    grPath.CloseFigure();
                    var bounds = grPath.GetBounds();
                    //Region region;
                    if (bounds.Width >= grayCurrent.Width - 1 && bounds.Height >= grayCurrent.Height - 1)
                    {
                        if (grPath.PathPoints.Length <= 8)
                            //region = new Region(grPath);
                            //if (region.GetRegionData().Data.Length <= 4)
                            return null;
                    }
                    //else
                    //    region = new Region(grPath);
                    return new Region(grPath);
                }
            }
            catch (Exception ex)
            {
                Performance?.Error("GetContourRegion", ex);
                return null;
            }
        }

        public Mat ConvertRgbA2AlphaGray(Bitmap image) => ConvertRgbA2AlphaGray(image, Color.Transparent);

        public Mat ConvertRgbA2AlphaGray(Bitmap image, Color transparentColor)
        {
            Mat matSrc = Emgu.CV.BitmapExtension.ToMat(image);
            byte[,,] bytesSrc = (byte[,,])matSrc.GetData();

            Mat grayCurrent = new(image.Size, DepthType.Cv8U, 1);
            //CvInvoke.CvtColor(matSrc, grayCurrent, Emgu.CV.CvEnum.ColorConversion.Bgra2Gray);
            byte[] bytes = new byte[image.Size.Width * image.Size.Height];
            int rows = image.Height;
            int cols = image.Width;
            int pixel = 0;
            for (int y = 0; y < rows; ++y)
                for (int x = 0; x < cols; ++x)
                {
                    bytes[pixel] = bytesSrc[y, x, 3];

                    ++pixel;
                }
            Marshal.Copy(bytes, 0, grayCurrent.DataPointer, bytes.Length);

            return grayCurrent;
        }
        #endregion

        Bitmap? _EmptyImage;
        [Browsable(false)]
        public Bitmap EmptyImage
        {
            get
            {
                if (_EmptyImage != null)
                    return _EmptyImage;

                _EmptyImage = ImageProvider?.Image;

                Size size = ImageSizeMin;
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

        Bitmap? _WaitingImage;
        [Browsable(false)]
        public Bitmap WaitingImage
        {
            get
            {
                if (_WaitingImage != null)
                    return _WaitingImage;

                _WaitingImage = ImageProvider?.Image;

                Size size = ImageSizeMin;
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
