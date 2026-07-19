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
    //isAynchrone = true
    public class EDVideoCapture(string paramSection = "VideoCapture", Performance performance = null, Form formHandler = null, IImageConsumer imageConsumer = null, bool isAynchrone = true)
        : ImageProcess(paramSection, performance, formHandler, imageConsumer, isAynchrone), IImageProvider, IMatFrameProvider
    {
        public override void Dispose()
        {
            base.Dispose();

            Dispose_Capture();
        }


        #region Properties

        [Browsable(true)]
        [ReadOnly(true)]
        public override Size ImageSizeMax { get; set; }

        [ReadOnly(true)]
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
                _Image = null;
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
                        //Need Image
                        var image = Image;
                        break;
                    }

            InvokeImageChanged((IImageProvider)sender, e);
        }

        public void InvokeFrameChanged(IMatFrameProvider sender, EventArgs e)
        {
            InvokePropertyChanged(sender, OnFrameChanged, e);
        }
        public IMatFrameProvider.FrameChangedDelegate OnFrameChanged;
        #endregion

        #region Image

        private Bitmap _Image;
        public override Bitmap Image
        {
            get
            {
                if (_Image == null)
                {
                    if (Frame == null)
                        return null;
                    //if (false)
                    //{
                    //    if (IsInvokingPropertyChanged(OnFrameChanged))
                    //    {
                    //        Performance.StackTrace($"IsInvokingPropertyChanged {OnFrameChanged.Method.Name}");
                    //        return _Image;
                    //    }
                    //    if (IsInvokingPropertyChanged(OnImageChanged))
                    //    {
                    //        Performance.StackTrace($"IsInvokingPropertyChanged {OnImageChanged.Method.Name}");
                    //        return _Image;
                    //    }
                    //}
                    //Generated at first query
                    Performance.Step("LastFrame.ToBitmap()");
                    try
                    {
                        _Image = Frame.ToBitmap();
                    }
                    catch (System.AccessViolationException ex)
                    {
                        Performance.Error("AccessViolationException in LastFrame.ToBitmap()");
                    }
                    catch (Exception ex)
                    {
                        Performance.Error("in LastFrame.ToBitmap() : " , ex);
                    }
                }
                return _Image;
            }
            set
            {
                _Image = value;
            }
        }
        #endregion

        /**
         * Capture
         * 
         */
        [Browsable(true)]
        public VideoCapture Capture { get; set; }

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
            if (!isAynchrone)
                throw new ArgumentException("WebCam may be isAynchrone = true (constructor)");

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
         * delegate Capture_ImageGrabbed
         * 
         */
        private void Capture_ImageGrabbed(object? sender, EventArgs e)
        {
            Performance.Step("------------------");
            Performance.Resume($"Capture_ImageGrabbed. Sleep : {sleep}", true);
            Mat frame = new();
            if (Capture.Retrieve(frame))
            {
                //Performance.Step("Retrieved");
                Frame = frame;
                //Performance.Pause("Invoked");
            }
            //else
            //    Performance.Pause("None");

            if (Performance.Average_msec < 40)
                sleep += 5;
            else if (sleep > 0)
                sleep -= 5;
            if (sleep > 0)
                Thread.Sleep(sleep);
        }
        int sleep = 0;

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
