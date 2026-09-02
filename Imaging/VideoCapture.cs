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
    public class VideoCapture : VideoCaptureEmgu
    {
        public VideoCapture(string name = "VideoCapture", Performance? performance = null, Control? invokeHandler = null, IImageConsumer? imageConsumer = null, bool isAsynchrone = true)
        : base(name, performance, invokeHandler, imageConsumer, isAsynchrone)
        {
            ProcessIcon = ProcessIconDefault = "Object";
        }


        #region Properties

        [Browsable(true)]
        [ReadOnly(false)]
        [Category("Video capture")]
        [DefaultValue(0)]
        public int CameraIndex { get; set; }

        public override void LoadSettings(ProcessSettings? settings = null, string fileName = "")
        {
            base.LoadSettings(settings, fileName);
            if (settings == null && (settings = ProcessSettings) == null)
                return;

            CameraIndex = (int)(settings.GetValue("CameraIndex", CameraIndex) ?? CameraIndex);
        }

        public override JsonObject SaveProcess(JsonObject? node = null)
        {
            node = base.SaveProcess(node);
            node.Add("CameraIndex", CameraIndex);
            return node;
        }

        #endregion
       
        /**
         * delegate Capture_ImageGrabbed
         * 
         */
        private void Capture_ImageGrabbed(object? sender, EventArgs e)
        {
            if (IsDisposed || Disposing || Capture == null)
            {
                Stop();
                return;
            }
            Performance?.Step("------------------");
            Performance?.Resume($"Capture_ImageGrabbed. Sleep : {sleep}", true);//increment

            Mat frame = new();
            if (Capture.Retrieve(frame))
                Frame = frame;

            if (IsDisposed || Disposing)
            {
                Stop();
                return;
            }

            if (Performance?.Average_msec < FPSMaxDuration)
                sleep += 5;
            else if (sleep > 0)
                sleep -= 5;
            if (sleep > 0)
                Thread.Sleep(sleep);
        }
        int sleep = 0;

        /**
         * Capture
         * 
         */

        public override bool Initialize_Capture()
        {
            Dispose_Capture();

            Capture = new(CameraIndex);

            Capture.ImageGrabbed += Capture_ImageGrabbed;

            if (!ImageSizeMin.IsEmpty)
            {
                Capture.Set(Emgu.CV.CvEnum.CapProp.FrameWidth, ImageSizeMin.Width);
                Capture.Set(Emgu.CV.CvEnum.CapProp.FrameHeight, ImageSizeMin.Height);
                Performance?.Step($"ImageSizeMin {Capture.Get(Emgu.CV.CvEnum.CapProp.FrameWidth)} x {Capture.Get(Emgu.CV.CvEnum.CapProp.FrameHeight)}");
            }

            return true;
        }
        public override void Dispose_Capture()
        {
            if (Capture != null)
            {
                Capture.ImageGrabbed -= Capture_ImageGrabbed;
                Capture.Stop();
                Capture?.Dispose();
                Capture = null;
            }
        }

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
