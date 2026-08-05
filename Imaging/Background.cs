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
    /**
     * class Background : ImageSourced, IImageProvider
     * <summary>Image as a physic object that can move, rotate and collide</summary>
     * */
    public class Background(string name = "BackgroundImage", Performance? performance = null, Control? invokeHandler = null, IImageConsumer? imageConsumer = null, bool isAsynchrone = true)
        : ImageSourced(name, performance, invokeHandler, imageConsumer, isAsynchrone), IImageProvider
    {

        #region Properties

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

        public override void LoadSettings(ProcessSettings? settings = null, string fileName = "")
        {
            base.LoadSettings(settings, fileName);

            BackgroundColor = (Color)settings.GetValue("BackgroundColor", BackgroundColor);
        }
        public override JsonObject SaveProcess(JsonObject node = null)
        {
            node = base.SaveProcess(node);
            node.Add("BackgroundColor", BackgroundColor.ToString());
            return node;
        }
        #endregion

        #region Image

        /**
         * GetImageFromSource
         * 
         * */
        public override Bitmap GetImageFromSource(IImageProvider? provider = null)
        {
            Size size = ImageSizeMin;
            if (size.IsEmpty)
                if (Consumer is ImageProcess)
                    size = (Consumer as ImageProcess).ImageSizeMin;
            if (size.IsEmpty)
                size = EmptyImage.Size;

            Bitmap image;
            ClipRegion = null;

            image = new Bitmap(size.Width, size.Height);
            Graphics graphics = Graphics.FromImage(image);

            Color color = BackgroundColor;
            //Color color = Color.FromArgb((int)Performance.Average_msec, (int)Performance.Counter % 255, (int)Performance.Counter % 255);
            SolidBrush brush = new SolidBrush(color);

            GraphicsUnit units = GraphicsUnit.Point;
            graphics.FillRectangle(brush, image.GetBounds(ref units));
            graphics.Dispose();
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
            Thread.Sleep(100);

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
