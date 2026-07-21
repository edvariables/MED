using DirectShowLib;
using Emgu.CV;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MED
{
    //isAsynchrone = true
    public class EDVideoCapture(string name = "VideoCapture", Performance performance = null, Control invokeHandler = null, IImageConsumer imageConsumer = null, bool isAsynchrone = true)
        : ImageProcess(name, performance, invokeHandler, imageConsumer, isAsynchrone), IImageProvider, IMatFrameProvider
    {
        public override void Dispose()
        {
            base.Dispose();

            Dispose_Capture();
        }


        #region Properties

        [Browsable(true)]
        public override Size ImageSizeMax { get; set; }


        [Browsable(true)]
        [ReadOnly(false)]
        public int CameraIndex { get; set; }

        #endregion

        #region Frame

        //public bool HasFrameChanged { get; set; }

        private Mat _Frame = null;
        public Mat Frame
        {
            get
            {
                if (_Frame == null)
                {
                    if (ImageProvider != null && ImageProvider is IMatFrameProvider && ImageProvider != this)
                    {
                        _Frame = (ImageProvider as IMatFrameProvider).Frame;

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
                        //Need Image in the same thread
                        var image = Image;
                        break;
                    }

            InvokeImageChanged((IImageProvider)sender, e);
        }

        public void InvokeFrameChanged(IMatFrameProvider sender, EventArgs e)
        {
            InvokePropertyChanged(sender, OnFrameChanged, e);
        }
        public IMatFrameProvider.FrameChangedDelegate? OnFrameChanged;

        /**
         * delegate Capture_ImageGrabbed
         * 
         */
        private void Capture_ImageGrabbed(object? sender, EventArgs e)
        {
            Performance.Step("------------------");
            Performance.Resume($"Capture_ImageGrabbed. Sleep : {sleep}", true);//increment
            Mat frame = new();
            if (Capture.Retrieve(frame))
            {
                Frame = frame;
            }

            if (Performance.Average_msec < 40)
                sleep += 5;
            else if (sleep > 0)
                sleep -= 5;
            if (sleep > 0)
                Thread.Sleep(sleep);
        }
        int sleep = 0;
        #endregion

        #region Image

        /**
         * GetImage
         * 
         * */
        public override Bitmap GetImage(IImageProvider provider = null)
        {
            Bitmap image;
            if (Frame == null)
                return null;

            Performance.Step("LastFrame.ToBitmap()");
            try
            {
                return FrameToImage((IMatFrameProvider)provider, Frame);
            }
            catch (System.AccessViolationException ex)
            {
                Performance.Error("AccessViolationException in FrameToImage()");
            }
            catch (Exception ex)
            {
                Performance.Error("in LastFrame.ToBitmap() : ", ex);
            }
            return null;
        }

        public Bitmap FrameToImage(IMatFrameProvider sender, Mat currentFrame = null) => currentFrame?.ToBitmap();

        #endregion

        /**
         * Capture
         * 
         */
        [Browsable(true)]
        public VideoCapture Capture { get; protected set; }

        public bool Initialize_Capture()
        {
            Dispose_Capture();

            Capture = new(CameraIndex);
            Capture.ImageGrabbed += Capture_ImageGrabbed;

            if (!ImageSizeMax.IsEmpty)
            {
                Performance.Step($"ImageSizeMax From {Capture.Get(Emgu.CV.CvEnum.CapProp.FrameWidth)} x {Capture.Get(Emgu.CV.CvEnum.CapProp.FrameHeight)}");
                Capture.Set(Emgu.CV.CvEnum.CapProp.FrameWidth, ImageSizeMax.Width);
                Capture.Set(Emgu.CV.CvEnum.CapProp.FrameHeight, ImageSizeMax.Height);
                Performance.Step($"To {Capture.Get(Emgu.CV.CvEnum.CapProp.FrameWidth)} x {Capture.Get(Emgu.CV.CvEnum.CapProp.FrameHeight)}");
            }

            return true;
        }
        public void Dispose_Capture()
        {
            if (Capture != null)
            {
                Capture.ImageGrabbed -= Capture_ImageGrabbed;
                Capture.Stop();
                Capture?.Dispose();
                Capture = null;
            }
        }

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

            Capture.Start();

            Performance.Step($"Connected {Capture.Get(Emgu.CV.CvEnum.CapProp.Fps)}");

            ProcessState = System.Threading.ThreadState.Running;
        }

        public override void Stop()
        {
            Capture?.Stop();
            Capture?.Dispose();
            Capture = null;

            base.Stop();
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

        /**
         * AvailableCameras
         */
        public static List<string> AvailableCameras()
        {
            //GetAvailableVideoInputDevicesWithResolutions
            /*
             *DsDevice[] videoInputDevices = DsDevice.GetDevicesOfCat (FilterCategory.VideoInputDevice);

            VideoInputDevices = new DsVideoInputDevice[videoInputDevices.Length];

            int i = 0;
            foreach (DsDevice videoInputDevice in videoInputDevices) {
                VideoInputDevices[i].VideoInputDevice = videoInputDevice;
                VideoInputDevices[i].AvailableResolutions = GetVideoCapabilities (videoInputDevice);
                i++;
            }*/
            List<string> cams = [];
            foreach (var cam in DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice))
                if (cam.Name != null)
                    cams.Add(cam.Name);
            return cams;
        }

    }
}
