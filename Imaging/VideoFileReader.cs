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
    public class VideoFileReader : VideoCaptureEmgu
    {
        public VideoFileReader(string name = "VideoFileReader", Performance? performance = null, Control? invokeHandler = null, IImageConsumer? imageConsumer = null, bool isAsynchrone = true)
        : base(name, performance, invokeHandler, imageConsumer, isAsynchrone)
        {
            ProcessIcon = ProcessIconDefault = "save";
        }


        #region Properties

        [Browsable(true)]
        [Category("Video capture")]
        [DefaultValue("")]
        [Editor(typeof(FileNameEditor), typeof(UITypeEditor))]
        public string VideoFile { get; set; } = "";

        [Browsable(true)]
        [Category("Video capture")]
        [DefaultValue(true)]
        public bool PlayLoop { get; set; } = true;

        public override void LoadSettings(ProcessSettings? settings = null, string fileName = "")
        {
            base.LoadSettings(settings, fileName);
            if (settings == null && (settings = ProcessSettings) == null)
                return;

            VideoFile = (string)(settings.GetValue("VideoFile", VideoFile) ?? VideoFile);
            PlayLoop = (bool)(settings.GetValue("PlayLoop", PlayLoop) ?? PlayLoop);
        }

        public override JsonObject SaveProcess(JsonObject? node = null)
        {
            node = base.SaveProcess(node);
            node.Add("VideoFile", VideoFile);
            if (!PlayLoop)
                node.Add("PlayLoop", PlayLoop);
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
            if (!IsRunning || IsPaused)
                return;

            Performance?.Step("------------------");
            Performance?.Resume($"Capture_ImageGrabbed. Sleep : {sleep}", true);//increment

            double time_index = Capture.Get(Emgu.CV.CvEnum.CapProp.PosMsec);
            double framesCount = Capture.Get(Emgu.CV.CvEnum.CapProp.FrameCount);
            double frameIndex = Capture.Get(Emgu.CV.CvEnum.CapProp.PosFrames);
            double fps = Capture.Get(Emgu.CV.CvEnum.CapProp.Fps);
            //ImageGrabbed may stop before frameIndex == framesCount-1
            bool isLastFrame = frameIndex >= framesCount - fps;
            Performance?.Step($"{frameIndex}/{framesCount} # {time_index.ToString("0")} msec # fps = {fps}");

            Mat frame = new();
            if (Capture.Retrieve(frame))
                Frame = frame;
            //Frame = frame = Capture.QueryFrame();

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

            if (isLastFrame)
            {
                if (PlayLoop)
                {
                    Performance?.Step($"PlayLoop Timeout for last frames({framesCount - frameIndex})");
                    if (frameIndex < framesCount)
                    {//Timeout for last frames
                        if (LoopEndTask == null)
                        {
                            LoopEndTask = new Task(() =>
                            {
                                Performance?.Step($"PlayLoop Thread.Sleep({(int)((framesCount - frameIndex) / fps * 1000)})");
                                Thread.Sleep((int)((framesCount - frameIndex) / fps * 1000));
                                Performance?.Step($"PlayLoop Sleeped({(int)((framesCount - frameIndex) / fps * 1000)}) {LoopEndTask}");
                                if (LoopEndTask == null || Capture==null)
                                    return;
                                Capture.Set(Emgu.CV.CvEnum.CapProp.PosMsec, 0);
                                Capture.Set(Emgu.CV.CvEnum.CapProp.PosFrames, 0);

                                Capture.Start();

                                LoopEndTask = null;
                            });
                            LoopEndTask.Start();
                        }
                    }
                    else
                    {
                        Capture.Set(Emgu.CV.CvEnum.CapProp.PosMsec, 0);
                        Capture.Set(Emgu.CV.CvEnum.CapProp.PosFrames, 0);
                        LoopEndTask = null;
                    }
                }
                else
                {   //Timeout for last frames
                    Performance?.Step($"NOT PlayLoop Timeout for last frames({framesCount - frameIndex})");
                    if (LoopEndTask == null)
                    {
                        LoopEndTask = new Task(() =>
                        {
                            Performance?.Step($"NOT PlayLoop Thread.Sleep({(int)((framesCount - frameIndex) / fps * 1000)})");
                            Thread.Sleep((int)((framesCount - frameIndex) / fps * 1000));
                            Performance?.Step($"NOT PlayLoop Sleeped({(int)((framesCount - frameIndex) / fps * 1000)})");
                            LoopEndTask = null;
                            if (IsRunning)
                                Stop();
                        });
                        LoopEndTask.Start();
                    }
                }
            }
        }
        int sleep = 0;
        Task? LoopEndTask;

        /**
         * Capture
         * 
         */
        public override bool Initialize_Capture()
        {
            Dispose_Capture();

            Capture = new(VideoFile);

            //The four_cc returns a double so we must convert it
            double codec_double = Capture.Get(Emgu.CV.CvEnum.CapProp.FourCC);
            string s = new string(System.Text.Encoding.UTF8.GetString(BitConverter.GetBytes(Convert.ToUInt32(codec_double))).ToCharArray());
            Performance?.Step("Codec : " + s);

            Capture.ImageGrabbed += Capture_ImageGrabbed;

            return true;
        }
        public override void Dispose_Capture()
        {
            if (Capture != null)
            {
                Capture.ImageGrabbed -= Capture_ImageGrabbed;
                //Timer?.Dispose();
                Capture?.Stop();
                Capture?.Dispose();
                Capture = null;
            }
        }
    }
}
