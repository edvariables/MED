using DirectShowLib;
using Emgu.CV;
using MED.EDWebCam;
using MED.Imaging;
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
    public class WebCam(string paramSection = "WebCam", Performance performance = null, Form formHandler = null, IImageConsumer imageConsumer = null, bool isAynchrone = true)
        : ImageProcess(paramSection, performance, formHandler, imageConsumer, isAynchrone), IImageProvider
    {

        public override void Dispose()
        {
            base.Dispose();

            Capture?.Stop();
            Capture?.Dispose();
            Capture = null;
        }
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


        private Mat _LastFrame = null;
        [Browsable(false)]
        public Mat LastFrame
        {
            get { return _LastFrame; }
            set
            {   
                _LastFrame?.Dispose();
                _LastFrame = value;
                _LastImage = null;

                if (value != null)
                {
                    Image = (ImageConsumer as EmguMoving).MoveDetectorAction(this, value);
                    HasImageChanged = false;
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
                    catch(System.AccessViolationException ex)
                    {
                        Performance.Step("ERROR AccessViolationException in LastFrame.ToBitmap()");
                    }
                    catch(Exception ex)
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
        public int CameraIndex{get;set;}

        [Browsable(true)]
        public VideoCapture Capture { get; set; }
        /**
         * Run
         * 
         */
        public override void Start()
        {
            if (!isAynchrone && ! (ImageConsumer != null && ImageConsumer is ImageProcess && (ImageConsumer as ImageProcess).IsAsynchrone))
                throw new ArgumentException("WebCam may be isAynchrone = true (constructor)");

            base.Start();

            var t = new Thread(() =>
            {
                //String win1 = "Test Window (Press any key to close)"; //The name of the window
                //                                                      //CvInvoke.NamedWindow(win1); //Create the window using the specific name
                //                                                      //Emgu.CV.VideoCapture.API.
                //string fileName = $"@device:pnp:\\\\?\\usb#vid_0471&pid_0329&mi_00#7&a021c5e&0&0000#{{65e8773d-8f56-11d0-a3b9-00a0c9223196}}\\global";
                //string fileName = "C:\\Users\\Manu\\Desktop\\TheEndOfSuburbia.avi";
                //string fileName = "https://www.youtube.com/watch?v=Q62SJ--JNiY";
                using (Mat frame = new Mat())
                using (VideoCapture capture = Capture = new VideoCapture(CameraIndex))
                {
                    if (!ImageSizeMax.IsEmpty)
                    {
                        Performance.Step($"ImageSizeMax From {capture.Get(Emgu.CV.CvEnum.CapProp.FrameWidth)} x {capture.Get(Emgu.CV.CvEnum.CapProp.FrameHeight)}");
                        capture.Set(Emgu.CV.CvEnum.CapProp.FrameWidth, ImageSizeMax.Width);
                        capture.Set(Emgu.CV.CvEnum.CapProp.FrameHeight, ImageSizeMax.Height);
                        Performance.Step($"To {capture.Get(Emgu.CV.CvEnum.CapProp.FrameWidth)} x {capture.Get(Emgu.CV.CvEnum.CapProp.FrameHeight)}");
                    }
                    //capture.Start();

                    Performance.Step($"Connected {capture.Get(Emgu.CV.CvEnum.CapProp.Fps)}");
                    int counter_max = -500;
                    int sleep = 0;
                    while (IsRunning
                    && (counter_max <= 0 || counter_max > Performance.Counter)
                    /*&& CvInvoke.WaitKey(1) == -1*/)
                    {
                        if(ProcessState == System.Threading.ThreadState.Suspended)
                        {
                            Thread.Sleep(200);
                            continue;
                        }

                        Performance.Increment($"ReadFrame {capture.Get(Emgu.CV.CvEnum.CapProp.Fps)}");

                        capture.Read(frame);

                        if (sleep > 0)
                            Performance.Step($"done. Sleep = {sleep}, avg = {Performance.Average_msec.ToString("# ##0")} msec");
                        else
                            Performance.Step($"capture.Read done");

                        //CvInvoke.Imshow(win1, frame);
                        //PerfVideoCapture.Step("CvInvoke.Imshow(win1, frame) done");

                        LastFrame = frame.Clone();

                        if (Performance.Average_msec < 40)
                            sleep += 5;
                        else if (sleep > 0)
                            sleep -= 10;
                        if (sleep > 0)
                            Thread.Sleep(sleep);
                    }

                    Stop();
                }
            });
            t.Start();

            Capture?.Dispose();
            Capture = null;

            ProcessState = System.Threading.ThreadState.Running;
        }

    }
}
