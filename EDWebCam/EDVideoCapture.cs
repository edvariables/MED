using DirectShowLib;
using Emgu.CV;
using MED.EDWebCam;
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
        : ImageProcess(paramSection, performance, formHandler, imageConsumer, isAynchrone), IImageProvider
    {
        public override void Dispose()
        {
            base.Dispose();

            Dispose_Capture();
        }


        private Mat _LastFrame = null;
        public Mat LastFrame
        {
            get { return _LastFrame; }
            set
            {
                _LastFrame = value;
                _LastImage = null;

                if (value != null)
                {
                    //Image = (ImageConsumer as EmguMoving).MoveDetectorAction(this, value);
                    HasImageChanged = true;
                    InvokeImageChanged();
                }
            }
        }
        private Bitmap _LastImage = null;

        public override Bitmap Image
        {
            get
            {
                if (_LastImage == null)
                {
                    if (LastFrame == null)
                        return null;
                    //Generated at first query
                    Performance.Step("LastFrame.ToBitmap()");
                    try
                    {
                        _LastImage = LastFrame.ToBitmap();
                    }
                    catch (System.AccessViolationException ex)
                    {
                        Performance.Step("ERROR AccessViolationException in LastFrame.ToBitmap()");
                    }
                    catch (Exception ex)
                    {
                        Performance.Step("ERROR in LastFrame.ToBitmap() : " + ex.ToString());
                    }
                    HasImageChanged = false;
                }
                return _LastImage;
            }
            set
            {
                _LastImage = value;
            }
        }



        [Browsable(true)]
        [ReadOnly(true)]
        public override Size ImageSizeMax { get; set; }

        [ReadOnly(true)]
        public int CameraIndex { get; set; }

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
            Performance.Resume($"Capture_ImageGrabbed. Sleep : {sleep}", true);
            Mat frame = new();
            if (Capture.Retrieve(frame))
            {
                //Performance.Step("Retrieved");
                LastFrame = frame;
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
