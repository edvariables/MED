using DirectShowLib;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Flann;
using Emgu.CV.Reg;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using static Emgu.CV.Structure.MCvMatND;

namespace MED.Imaging
{
    public class ImageSourced
        : ImageProcess, IImageSourced
    {
        public ImageSourced(string name = "BackgroundImage", Performance? performance = null, Control? invokeHandler = null, IImageConsumer? imageConsumer = null, bool isAsynchrone = true)
            : base(name, performance, invokeHandler, imageConsumer, isAsynchrone)
        {
            FPSMax = 0;
            ImageIsProvided = false;
        }

        #region Properties


        Size _ImageSizeMin = new Size(320, 240);
        [Browsable(true)]
        [Category("Image")]
        public override Size ImageSizeMin
        {
            get => _ImageSizeMin;
            set
            {
                _ImageSizeMin = value;
                Image = null;
            }
        }

        private string _ImageFile = "";
        [Browsable(true)]
        [EditorAttribute(typeof(FileNameEditor), typeof(UITypeEditor))]
        [ReadOnly(false)]
        [Category("Image")]
        public virtual string ImageFile
        {
            get => _ImageFile;
            set
            {
                _ImageFile = value;
                Image = null;
            }
        }

        [Browsable(true)]
        [ReadOnly(false)]
        [Category("Image")]
        public virtual Color TransparentColor { get; set; } = Color.Transparent;

        public override void LoadSettings(ProcessSettings? settings = null, string fileName = "")
        {
            base.LoadSettings(settings, fileName);
            if (settings == null && (settings = ProcessSettings) == null)
                return;

            ImageFile = (String)(settings.GetValue("ImageFile", ImageFile) ?? ImageFile);
            TransparentColor = (Color)(settings.GetValue("TransparentColor", TransparentColor) ?? TransparentColor);
        }
        public override JsonObject SaveProcess(JsonObject? node = null)
        {
            node = base.SaveProcess(node);
            node.Add("ImageFile", ImageFile);
            if (!TransparentColor.Equals(Color.Transparent))
                node.Add("TransparentColor", TransparentColor.ToString());
            return node;
        }
        #endregion

        #region Image

        public override Bitmap? Image
        {
            get
            {
                var image = base.Image;

                if (image != null
                    && _FrameCount > 1)
                {
                    long delay = DateTime.Now.Ticks - _FrameTime;
                    if (delay > _FrameDuration)
                    {
                        image.SelectActiveFrame(FrameDimension.Time, _FrameIndex++);//TODO GDI+ exception
                        if (_FrameIndex >= _FrameCount)
                            _FrameIndex = 0;
                    }
                    _FrameTime = DateTime.Now.Ticks;
                }
                return image;
            }

            set => base.Image = value;
        }
        /**
         * GetImage
         * 
         * */
        public override Bitmap? GetImage(IImageProvider? provider = null)
        {
            if (_Image != null)
                return _Image;

            return GetImageFromSource(provider);
        }

        int _FrameCount = 0;
        int _FrameIndex = 0;
        long _FrameTime = 0;
        long _FrameDuration = 0;

        /**
         * GetImageFromSource
         * 
         * */
        public virtual Bitmap? GetImageFromSource(IImageProvider? provider = null)
        {
            Size size = ImageSizeMin;
            if (size.IsEmpty)
                if (Consumer is ImageProcess)
                    size = ((ImageProcess)Consumer).ImageSizeMin;
            if (size.IsEmpty)
                size = EmptyImage.Size;

            Bitmap? image = null;
            ClipRegion = null;
            ClipPath = null;
            ClipPathsBounds = null;
            if (!string.IsNullOrEmpty(_ImageFile))
            {
                if (File.Exists(ImageFile))
                {
                    using (FileStream stream = new FileStream(ImageFile, FileMode.Open, FileAccess.Read))
                    {
                        image = (Bitmap)Bitmap.FromStream(stream);
                    }

                    //GIF Number of frames
                    _FrameCount = image.GetFrameCount(FrameDimension.Time);
                    if (_FrameCount > 1)
                    {
                        _FrameIndex = 0;
                        _FrameTime = 0L;
                        PropertyItem? item = image.GetPropertyItem(0x5100); // FrameDelay in libgdiplus
                                                                            // Time is in milliseconds
                        if (item == null || item.Value == null)
                            _FrameDuration = 40 * TimeSpan.TicksPerMillisecond;
                        else
                            _FrameDuration = (item.Value[0] + item.Value[1] * 256) * 10 * TimeSpan.TicksPerMillisecond;
                    }

                    var formatSrc = image.PixelFormat;
                    if (!size.IsEmpty
                        /*&& image.Size != size*/)//Needed to normalize file format
                    {
                        var imageSrc = image;

                        if (_FrameCount > 1)//GIF
                        {   //TODO GDI+ exception
                            image = GifWriter.ResizeGif((Bitmap)image.Clone(), size);
                            //image = (Bitmap)image.Clone();
                        }
                        else
                        {
                            image = new Bitmap(size.Width, size.Height);
                            Graphics graphics = Graphics.FromImage(image);

                            graphics.DrawImage(imageSrc, 0, 0, image.Width, image.Height);
                            graphics.Dispose();
                        }
                        imageSrc.Dispose();
                    }
                    if (!TransparentColor.Equals(Color.Transparent))
                    {
                        image.MakeTransparent(TransparentColor);
                    }
                    else if ((formatSrc & PixelFormat.Alpha) != PixelFormat.Alpha)
                    {
                        var transparentColor = image.GetPixel(0, 0);
                        image.MakeTransparent(transparentColor);
                    }
                    GraphicsPath grPath;
                    Dictionary<GraphicsPath, RectangleF> grPathsBounds;
                    ClipRegion = GetContourRegion(image, out grPath, out grPathsBounds);
                    ClipPath = grPath;
                    ClipPathsBounds = grPathsBounds;
                }
                else
                    throw new FileNotFoundException($"File not found from {this} : {ImageFile}", ImageFile);
            }
            return image;

        }

        public override Region? ClipRegion
        {
            get => base.ClipRegion;
            set
            {
                ClipEdgesRegion = null;
                base.ClipRegion = value;
            }
        }

        private System.Drawing.Region? _ClipRegionEdges = null;

        [Browsable(false)]
        public virtual System.Drawing.Region? ClipEdgesRegion
        {
            get
            {
                if (_ClipRegionEdges != null || ClipRegion == null || ClipPath == null)
                    return _ClipRegionEdges;

                //System.Drawing.Region clipRegionEdges= ClipRegion.Clone();
                //float offset = 1F;// 1.5F;
                //clipRegionEdges.Translate(offset, 0);
                //clipRegionEdges.Xor(ClipRegion);

                //System.Drawing.Region clipRegionEdgesV = ClipRegion.Clone();
                //clipRegionEdgesV.Translate(-offset, offset);
                //clipRegionEdgesV.Xor(ClipRegion);

                //clipRegionEdges.Union(clipRegionEdgesV);

                float penWidth = 2F;
                Pen pen = new Pen(Brushes.Black, penWidth);
                GraphicsPath grPath = (GraphicsPath)ClipPath.Clone();
                grPath.Widen(pen);
                System.Drawing.Region clipRegionEdges = new(grPath);
                clipRegionEdges.Exclude(ClipPath);
                clipRegionEdges.Translate(-penWidth / 2, -penWidth / 2);
                return _ClipRegionEdges = clipRegionEdges;

            }
            set => _ClipRegionEdges = value;
        }
        #endregion

        #region Process
        /**
         * Start
         * 
         */
        public override void Start()
        {
            _Image = null;

            base.Start();

            ProcessState = ThreadState.Running;
        }
        #endregion
    }
}
