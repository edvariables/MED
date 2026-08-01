using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms.Design;

namespace MED.Imaging
{
    public class Background(string name = "BackgroundImage", Performance performance = null, Control invokeHandler = null, IImageConsumer imageConsumer = null, bool isAsynchrone = true)
        : ImageProcess(name, performance, invokeHandler, imageConsumer, isAsynchrone), IImageProvider
    {

        #region Properties
        //ImageIsProvided = false;


        Size _ImageSizeMax = new Size(320, 240);
        [Browsable(true)]
        public override Size ImageSizeMax
        {
            get => _ImageSizeMax;
            set
            {
                _ImageSizeMax = value;
                Image = null;
            }
        }

        private Color _BackgroundColor = Color.Black;
        [Browsable(true)]
        [ReadOnly(false)]
        public Color BackgroundColor
        {
            get => _BackgroundColor;
            set
            {
                _BackgroundColor = value;
                Image = null;
            }
        }

        private string _ImageFile = "";
        [Browsable(true)]
        [EditorAttribute(typeof(FileNameEditor), typeof(UITypeEditor))]
        [ReadOnly(false)]
        public string ImageFile
        {
            get => _ImageFile;
            set
            {
                _ImageFile = value;
                Image = null;
            }
        }

        public override void LoadSettings(ProcessSettings settings = null, string fileName = "")
        {
            base.LoadSettings(settings, fileName);

            BackgroundColor = (Color)settings.GetValue("BackgroundColor", BackgroundColor);
            ImageFile = (String)settings.GetValue("ImageFile", ImageFile);
        }
        public override JsonObject SaveProcess(JsonObject node = null)
        {
            node = base.SaveProcess(node);
            node.Add("BackgroundColor", BackgroundColor.ToString());
            node.Add("ImageFile", ImageFile);
            return node;
        }
        #endregion

        #region Image

        /**
         * GetImage
         * 
         * */
        public override Bitmap GetImage(IImageProvider provider = null)
        {
            if (_Image != null)
                return _Image;
            Bitmap image;
            if (string.IsNullOrEmpty(_ImageFile))
            {
                ClipRegion = null;

                image = new Bitmap(ImageSizeMax.Width, ImageSizeMax.Height);
                Graphics graphics = Graphics.FromImage(image);

                Color color = BackgroundColor;
                //Color color = Color.FromArgb((int)Performance.Average_msec, (int)Performance.Counter % 255, (int)Performance.Counter % 255);
                SolidBrush brush = new SolidBrush(color);

                GraphicsUnit units = GraphicsUnit.Point;
                graphics.FillRectangle(brush, image.GetBounds(ref units));
                graphics.Dispose();
            }
            else
            {
                ClipRegion = null;
                if (File.Exists(ImageFile))
                {
                    if(ImageSizeMax.IsEmpty)
                        image = (Bitmap)Bitmap.FromFile(ImageFile);
                    else
                    {
                        var imageSrc = (Bitmap)Bitmap.FromFile(ImageFile);
                        image = new Bitmap(ImageSizeMax.Width, ImageSizeMax.Height);
                        Graphics graphics = Graphics.FromImage(image);

                        graphics.DrawImage(imageSrc, 0,0,image.Width, image.Height);
                        graphics.Dispose();
                        imageSrc.Dispose();
                    }
                    ClipRegion = GetContourRegion(image);
                }
                else
                    throw new FileNotFoundException("Fichier introuvable", ImageFile);
            }
            return image;

        }

        public Region GetContourRegion(Bitmap image)
        {
            Mat mat = Emgu.CV.BitmapExtension.ToMat(image);
            Mat grayCurrent = new();
            CvInvoke.CvtColor(mat, grayCurrent, Emgu.CV.CvEnum.ColorConversion.Bgr2Gray);

            using (GraphicsPath grPath = new GraphicsPath())
            using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
            using (Mat hierarchy = new Mat())
            {
                CvInvoke.FindContours(grayCurrent, contours, hierarchy, RetrType.External, ChainApproxMethod.ChainApproxSimple);

                for (int i = 0; i < contours.Size; i++)
                {
                    grPath.AddPolygon(contours[i].ToArray());
                    grPath.CloseFigure();
                }
                if (grPath.PointCount == 4)
                {
                    var bounds = grPath.GetBounds();
                    if (bounds.Width >= image.Width-1 && bounds.Height >= image.Height-1)
                        return null;
                }
                return new Region(grPath);
            }
        }
        #endregion

        #region Process
        /**
         * Start
         * 
         */
        public override void Start()
        {
            base.Start();

            ProcessState = System.Threading.ThreadState.Running;
            if (FPSMax > 0)
            {
                Thread thread = new(Ticker);
                thread.Start();
            }
        }
        private void Ticker()
        {
            int sleep = 0;
            while (IsRunning)
            {
                if (IsDisposed || Disposing)
                {
                    Stop();
                    return;
                }
                if (ProcessState == ThreadState.Suspended)
                {
                    Thread.Sleep(100);
                    continue;
                }

                Performance?.Resume($"------------------Tick. Sleep : {sleep}", true);//increment

                ImageChanged(this, EventArgs.Empty);

                if (IsDisposed || Disposing)
                {
                    Stop();
                    return;
                }

                if (Performance.Average_msec < FPSMaxDuration)
                    sleep += 5;
                else if (sleep > 0)
                    sleep -= 5;
                if (sleep > 0)
                    Thread.Sleep(sleep);
            }
        }
        #endregion
    }
}
