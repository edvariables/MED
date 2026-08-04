using DirectShowLib;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Reg;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms.Design;

namespace MED.Imaging
{
    public class ImageSourced
        : ImageProcess, IImageSourced
    {
        public ImageSourced(string name = "BackgroundImage", Performance? performance = null, Control? invokeHandler = null, IImageConsumer? imageConsumer = null, bool isAsynchrone = true)
            : base(name, performance, invokeHandler, imageConsumer, isAsynchrone)
        {
            ImageIsProvided = false;
        }

        #region Properties


        Size _ImageSizeMin = new Size(320, 240);
        [Browsable(true)]
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

            ImageFile = (String)settings.GetValue("ImageFile", ImageFile);
        }
        public override JsonObject SaveProcess(JsonObject node = null)
        {
            node = base.SaveProcess(node);
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

            return GetImageFromSource(provider);
        }

        /**
         * GetImageFromSource
         * 
         * */
        public virtual Bitmap? GetImageFromSource(IImageProvider? provider = null)
        {
            Size size = ImageSizeMin;
            if (size.IsEmpty)
                if (Consumer is ImageProcess)
                    size = (Consumer as ImageProcess).ImageSizeMin;
            if (size.IsEmpty)
                size = EmptyImage.Size;

            Bitmap image = null;
            ClipRegion = null;
            if (!string.IsNullOrEmpty(_ImageFile))
            {
                if (File.Exists(ImageFile))
                {
                    image = (Bitmap)Bitmap.FromFile(ImageFile);
                    if (!size.IsEmpty
                        /*&& image.Size != size*/)//Needed to normalize file format (and free file ressource)
                    {
                        var imageSrc = (Bitmap)Bitmap.FromFile(ImageFile);
                        image = new Bitmap(size.Width, size.Height);
                        Graphics graphics = Graphics.FromImage(image);

                        graphics.DrawImage(imageSrc, 0, 0, image.Width, image.Height);
                        graphics.Dispose();
                        imageSrc.Dispose();
                    }
                    ClipRegion = GetContourRegion(image);
                }
                else
                    throw new FileNotFoundException($"Fichier introuvable dans {this} : {ImageFile}", ImageFile);
            }
            return image;

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
