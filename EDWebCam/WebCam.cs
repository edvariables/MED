using DirectShowLib;
using Emgu.CV;
using MED.EDWebCam;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MED
{
    public class WebCam(string paramSection = "WebCam", Performance performance = null, Form formHandler = null, IImageConsumer imageConsumer = null, bool isAynchrone = true)
        : ImageProcess(paramSection, performance, formHandler, imageConsumer, isAynchrone), IImageProvider
    {

        public static List<string> AvailableCameras()
        {
            List<string> cams = [];
            foreach (var cam in DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice))
                if (cam.Name != null)
                    cams.Add(cam.Name);
            return cams;
        }

        private Mat _LastFrame = null;
        private Mat LastFrame
        {
            get { return _LastFrame; }
            set
            {
                _LastFrame = value;
                _LastImage = null;

                if (value != null)
                    InvokeImageChanged();
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
                    _LastImage = LastFrame.ToBitmap();

                    HasImageChanged = false;
                }
                return _LastImage;
            }
            set
            {
                _LastImage = value;
            }
        }

        /**
         * Run
         * 
         */
        public void Run(int nCamera = 0)
        {
            if (!isAynchrone)
                throw new ArgumentException("WebCam may be isAynchrone = true (constructor)");

            base.Run();

            var t = new Thread(() =>
            {
                //String win1 = "Test Window (Press any key to close)"; //The name of the window
                //                                                      //CvInvoke.NamedWindow(win1); //Create the window using the specific name
                //                                                      //Emgu.CV.VideoCapture.API.
                //string fileName = $"@device:pnp:\\\\?\\usb#vid_0471&pid_0329&mi_00#7&a021c5e&0&0000#{{65e8773d-8f56-11d0-a3b9-00a0c9223196}}\\global";
                //string fileName = "C:\\Users\\Manu\\Desktop\\TheEndOfSuburbia.avi";
                //string fileName = "https://www.youtube.com/watch?v=Q62SJ--JNiY";
                using (Mat frame = new Mat())
                using (VideoCapture capture = new VideoCapture(nCamera))
                {
                    if (!ImageSizeMax.IsEmpty)
                    {
                        Performance.Step($"ImageSizeMax From {capture.Get(Emgu.CV.CvEnum.CapProp.FrameWidth)} x {capture.Get(Emgu.CV.CvEnum.CapProp.FrameHeight)}");
                        capture.Set(Emgu.CV.CvEnum.CapProp.FrameWidth, ImageSizeMax.Width);
                        capture.Set(Emgu.CV.CvEnum.CapProp.FrameHeight, ImageSizeMax.Height);
                        Performance.Step($"To {capture.Get(Emgu.CV.CvEnum.CapProp.FrameWidth)} x {capture.Get(Emgu.CV.CvEnum.CapProp.FrameHeight)}");
                    }

                    Performance.Step($"Connected {capture.Get(Emgu.CV.CvEnum.CapProp.Fps)}");
                    int counter_max = -500;
                    int sleep = 0;
                    while (IsRunning
                    && (counter_max <= 0 || counter_max > Performance.Counter)
                    /*&& CvInvoke.WaitKey(1) == -1*/)
                    {
                        Performance.Increment($"ReadFrame {capture.Get(Emgu.CV.CvEnum.CapProp.Fps)}");

                        capture.Read(frame);

                        if (sleep > 0)
                            Performance.Step($"done. Sleep = {sleep}, avg = {Performance.Average_msec.ToString("# ##0")} msec");
                        else
                            Performance.Step($"capture.Read done");

                        //CvInvoke.Imshow(win1, frame);
                        //PerfVideoCapture.Step("CvInvoke.Imshow(win1, frame) done");

                        LastFrame = frame;

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
        }

    }
}
