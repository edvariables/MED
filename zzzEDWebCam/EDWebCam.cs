using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DirectShowLib;
using Emgu.CV;

namespace MED
{
    public class EDWebCam : IDisposable
    {
        private EDWebCam()
        {
            LoadSettings();
        }

        public System.Windows.Forms.Form FormHandler;
        public bool RenderLoggerEnabled;
        public bool VideoCaptureLoggerEnabled;
        private void LoadSettings()
        {
            RenderLoggerEnabled = (bool)MED.Core.Settings.GetValue("RenderLogger", "EDWebCam", true);
            VideoCaptureLoggerEnabled = (bool)MED.Core.Settings.GetValue("VideoCaptureLogger", "EDWebCam", true);
        }
        public void SaveSettings()
        {
            MED.Core.Settings.SetValue("RenderLogger", "EDWebCam", RenderLoggerEnabled);
            MED.Core.Settings.SetValue("VideoCaptureLogger", "EDWebCam", VideoCaptureLoggerEnabled);
            MED.Core.Settings.Save();
        }

        public List<string> AvailableCameras()
        {
            List<string> cams = [];
            foreach (var cam in DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice))
                if (cam.Name != null)
                    cams.Add(cam.Name);
            return cams;
        }

        StringBuilder ProgressMessage = new();
        Mat LastFrame;

        Performance PerfVideoCapture = new();
        Performance PerfImageRender = new();

        private void PerformanceStart()
        {
            PerfVideoCapture = new("VideoCapture", ProgressMessage);
            PerfImageRender = new("ImageRender", ProgressMessage);

            PerfVideoCapture.LoggerDisable = !VideoCaptureLoggerEnabled;
            PerfImageRender.LoggerDisable = !RenderLoggerEnabled;

            // Create the counters.
            PerfVideoCapture.Start();
            PerfImageRender.Start();
        }

        private bool _IsRunning;
        public bool IsRunning
        {
            get
            {
                if (this.IsDisposed || this.Disposing)
                    return _IsRunning = false;
                return _IsRunning;
            }
            protected set { _IsRunning = value; }
        }

        public void Stop()
        {
            IsRunning = false;
        }
        /**
         * 
         * 
         */
        private void Run(int nCamera = 0)
        {
            IsRunning = true;

            PerformanceStart();

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
                    PerfVideoCapture.Step("Connected");
                    int counter_max = -500;
                    int sleep = 0;
                    while (IsRunning
                    && (counter_max <= 0 || counter_max > PerfVideoCapture.Counter)
                    && CvInvoke.WaitKey(1) == -1)
                    {
                        PerfVideoCapture.Increment("ReadFrame");

                        capture.Read(frame);

                        if (sleep > 0)
                            PerfVideoCapture.Step($"done. Sleep = {sleep}, avg = {PerfVideoCapture.Average_msec.ToString("# ##0")} msec");
                        else
                            PerfVideoCapture.Step($"capture.Read done");

                        //CvInvoke.Imshow(win1, frame);

                        LastFrame = frame;

                        InvokeRefreshRender();

                        if (PerfVideoCapture.Average_msec < 30)
                            sleep += 5;
                        else if (sleep > 0)
                            sleep -= 10;
                        if (sleep > 0)
                            Thread.Sleep(sleep);

                        PerfVideoCapture.Step("CvInvoke.Imshow(win1, frame) done");

                    }

                    PerfVideoCapture.Stop();
                    PerfImageRender.Stop();
                    IsRunning = false;
                    if (Disposing)
                        IsDisposed = true;
                    else
                        InvokeRefreshRender();
                }
            });
            t.Start();
        }

        private void InvokeRefreshRender()
        {
            PerfImageRender.Step("InvokeRefresh", false);
            if (this.Disposing || this.IsDisposed)
                return;
            try
            {
                this.Invoke(RefreshRender);
            }
            catch { }
        }

        public bool Disposing { get; private set; }
        public bool IsDisposed { get; private set; }
        public void Dispose()
        {
            Disposing = true;
            Stop();
        }
    }
}
