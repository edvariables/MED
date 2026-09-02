using DirectShowLib;
using Emgu.CV;
using MED.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Design;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms.Design;

namespace MED.Imaging
{
    //isAsynchrone = true
    public abstract class VideoCaptureEmgu : ImageProcess, IImageProvider, IMatFrameProvider
    {
        public VideoCaptureEmgu(string name = "VideoCaptureEmgu", Performance? performance = null, Control? invokeHandler = null, IImageConsumer? imageConsumer = null, bool isAsynchrone = true)
        : base(name, performance, invokeHandler, imageConsumer, isAsynchrone)
        {
            ProcessIcon = ProcessIconDefault = "Object";
        }
        public override void Dispose()
        {
            base.Dispose();

            Dispose_Capture();
        }


        #region Properties

        #endregion

        #region Frame

        //public bool HasFrameChanged { get; set; }

        private Mat? _Frame = null;
        [Browsable(false)]
        public Mat? Frame
        {
            get
            {
                if (_Frame == null)
                {
                    if (ImageProvider != null && ImageProvider is IMatFrameProvider matFrameProvider && ImageProvider != this)
                    {
                        _Frame = matFrameProvider.Frame;

                    }
                }
                return _Frame;
            }
            set
            {
                bool changed = _Frame != value;

                _Frame = value;
                Image = null;
                if (changed)
                {
                    FrameChanged(this, EventArgs.Empty);
                }
            }
        }
        public void FrameChanged(IMatFrameProvider sender, EventArgs e)
        {
            ImageProvider = (IImageProvider)sender;

            InvokeFrameChanged(sender, e);

            if (OnImageChanged != null)
                foreach (var del in OnImageChanged.GetInvocationList())
                    if (del.Target is IMatFrameConsumer)
                        continue;
                    else if (del.Target is IImageConsumer)
                    {
                        //Need Image instance creation in the same thread
                        var _ = Image;
                        break;
                    }

            InvokeImageChanged((IImageProvider)sender, e);
        }

        public void InvokeFrameChanged(IMatFrameProvider sender, EventArgs e) => InvokePropertyChanged(sender, OnFrameChanged, e);

        public IMatFrameProvider.FrameChangedDelegate? OnFrameChanged;

        #endregion

        #region Image

        /**
         * GetImage
         * 
         * */
        public override Bitmap? GetImage(IImageProvider? provider = null)
        {
            if (Frame == null || IsDisposed || Disposing)
                return null;

            Performance?.Step("LastFrame.ToBitmap()");
            try
            {
                return FrameToImage((IMatFrameProvider?)provider, Frame);
            }
            catch (System.AccessViolationException ex)
            {
                Performance?.Error("AccessViolationException in FrameToImage()", ex);
            }
            catch (Exception ex)
            {
                Performance?.Error("in LastFrame.ToBitmap() : ", ex);
            }
            return null;
        }

        public Bitmap? FrameToImage(IMatFrameProvider? sender, Mat? currentFrame = null)
        {
            if (ImageSizeMin.IsEmpty || currentFrame == null || currentFrame.Size == ImageSizeMin)
                return currentFrame?.ToBitmap();

            Mat resized = new();
            CvInvoke.Resize(currentFrame, resized, ImageSizeMin);
            return resized.ToBitmap();
        }

        #endregion

        /**
         * Capture
         * 
         */
        [Browsable(false)]
        public Emgu.CV.VideoCapture? Capture { get; protected set; }

        public abstract bool Initialize_Capture();
        public abstract void Dispose_Capture();

        #region Process
        /**
         * Start
         * 
         */
        public override void Start()
        {
            if (!IsAsynchrone)
                throw new ArgumentException("WebCam may be IsAsynchrone = true (constructor)");

            if (!Initialize_Capture())
                throw new ArgumentException($"Capture NOT initialized");

            base.Start();

            Capture?.Start();

            Performance?.Step($"Connected fps={Capture?.Get(Emgu.CV.CvEnum.CapProp.Fps)}");

            ProcessState = System.Threading.ThreadState.Running;
        }

        public override void Stop()
        {
            Capture?.Stop();

            base.Stop();

            Capture?.Dispose();
            Capture = null;
        }
        public override void Pause()
        {
            Capture?.Pause();

            base.Pause();
        }
        public override void Resume()
        {
            if (Capture != null && Capture.IsOpened)
                Capture?.Start();

            base.Resume();
        }
        #endregion

    }
}
