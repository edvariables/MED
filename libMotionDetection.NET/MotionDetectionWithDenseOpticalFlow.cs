using Emgu.CV;
using Emgu.CV.Util;
using Serilog;
using System.Collections.Generic;
using System.Drawing;

namespace libMotionDetection
{
    /// <summary>
    /// A class for motion detection using dense optical flow
    /// </summary>
    public class MotionDetectionWithDenseOpticalFlow : System.IDisposable
    {
        private static readonly ILogger logger = Log.Logger.ForContext (typeof (MotionDetectionWithDenseOpticalFlow));
        private bool disposedValue = false;

        // A list of motion zones.
        public List<Rectangle> MotionZones { get; set; } = new List<Rectangle> ();

        public struct MotionSetting
        {
            public int MotionThreshold { get; set; }

            public override string ToString () =>
                $"MotionThreshold: {MotionThreshold}";
        }
        public MotionSetting Setting { get; set; } = new MotionSetting {
            MotionThreshold = 1000,
        };

        public struct OpticalFlowSetting
        {
            public DISOpticalFlow.Preset OpticalFlowPreset { get; set; }

            public override string ToString () =>
                $"OpticalFlowPreset: {OpticalFlowPreset}";
        }
        public OpticalFlowSetting FlowSetting { get; set; } = new OpticalFlowSetting {
            OpticalFlowPreset = DISOpticalFlow.Preset.Fast,
        };

        private readonly DISOpticalFlow denseOpticalFlow;
        public MotionDetectionWithDenseOpticalFlow ()
        {
            denseOpticalFlow = new DISOpticalFlow ();
        }

        public MotionDetectionWithDenseOpticalFlow (OpticalFlowSetting setting)
        {
            denseOpticalFlow = new DISOpticalFlow (setting.OpticalFlowPreset);
        }

        /// <summary>
        /// Calculate dense optical flow based on the example here, https://docs.opencv.org/4.5.4/d4/dee/tutorial_optical_flow.html
        /// </summary>
        /// <param name="prevFrame">The previous frame</param>
        /// <param name="currFrame">The current frame</param>
        /// <returns>a 2D matrix with the same size as the video frames</returns>
        public Mat CalculateDenseOpticalFlow (Mat prevFrame, Mat currFrame)
        {
            Mat currFrameGreyscale = null;
            bool disposeCurr = false;
            // The currFrame needs to have 1 channel
            if (currFrame.Depth != Emgu.CV.CvEnum.DepthType.Cv8U || currFrame.NumberOfChannels != 1) {
                currFrameGreyscale = new Mat ();
                CvInvoke.CvtColor (currFrame, currFrameGreyscale, Emgu.CV.CvEnum.ColorConversion.Bgr2Gray);
                disposeCurr = true;
            } else {
                currFrameGreyscale = currFrame;
            }

            Mat prevFrameGreyscale = null;
            bool disposePrev = false;
            // The prevFrame needs to have 1 channel
            if (prevFrame.Depth != Emgu.CV.CvEnum.DepthType.Cv8U || prevFrame.NumberOfChannels != 1) {
                prevFrameGreyscale = new Mat ();
                CvInvoke.CvtColor (prevFrame, prevFrameGreyscale, Emgu.CV.CvEnum.ColorConversion.Bgr2Gray);
                disposePrev = true;
            } else {
                prevFrameGreyscale = prevFrame;
            }

            Mat flow = new Mat (prevFrame.Size, Emgu.CV.CvEnum.DepthType.Cv32F, 2);
            // Calculate the flow as a 2D vector which has magnitude and angle
            denseOpticalFlow.Calc (prevFrameGreyscale, currFrameGreyscale, flow);

            if (disposeCurr && currFrameGreyscale != null) {
                currFrameGreyscale.Dispose ();
            }
            if (disposePrev && prevFrameGreyscale != null) {
                prevFrameGreyscale.Dispose ();
            }

            return flow;
        }

        /// <summary>
        /// Visualize the optical flow in HSV color space
        /// </summary>
        /// <param name="opticalFlow">The 2D matrix of optical flow</param>
        /// <returns></returns>
        public Mat OpticalFlowVisualizationWithHSV (Mat opticalFlow)
        {
            VectorOfMat flowParts = new VectorOfMat ();
            // Split the flow in two parts, the magnitude and the angle
            CvInvoke.Split (opticalFlow, flowParts);

            // Calculate magnitude and angle from a 2D vector.
            Mat angle = new Mat ();
            Mat magnitude = new Mat ();
            CvInvoke.CartToPolar (flowParts[0], flowParts[1], magnitude, angle, true);
            flowParts.Dispose ();

            Mat angleNormalized = new Mat ();
            Mat magnitudeNormalized = new Mat ();
            // Normalize the magnitude and angle
            CvInvoke.Normalize (magnitude, magnitudeNormalized, 0.0f, 1.0f, Emgu.CV.CvEnum.NormType.MinMax);
            angleNormalized = angle * (1.0f / 360.0f) * (180.0f / 255.0f);
            magnitude.Dispose ();
            angle.Dispose ();

            Mat flowHSV8 = new Mat ();
            Mat flowHSV32 = new Mat ();
            VectorOfMat vectorOfFlowHSV = new VectorOfMat (angleNormalized,
                                                           Mat.Ones (angleNormalized.Height, angleNormalized.Width, Emgu.CV.CvEnum.DepthType.Cv32F, 1),
                                                           magnitudeNormalized);
            angleNormalized.Dispose ();
            magnitudeNormalized.Dispose ();

            CvInvoke.Merge (vectorOfFlowHSV, flowHSV32);
            vectorOfFlowHSV.Dispose ();
            flowHSV32.ConvertTo (flowHSV8, Emgu.CV.CvEnum.DepthType.Cv8U, 255.0f);
            flowHSV32.Dispose ();

            // Convert HSV color space to BGR
            Mat flowBGR = new Mat ();
            CvInvoke.CvtColor (flowHSV8, flowBGR, Emgu.CV.CvEnum.ColorConversion.Hsv2Bgr);
            flowHSV8.Dispose ();

            return flowBGR;
        }

        protected virtual void Dispose (bool disposing)
        {
            if (!disposedValue) {
                if (disposing) {
                    // Dispose managed state (managed objects)
                }

                // Free unmanaged resources (unmanaged objects)
                if (denseOpticalFlow != null) {
                    denseOpticalFlow.Dispose ();
                }
                disposedValue = true;
            }
        }

        ~MotionDetectionWithDenseOpticalFlow ()
        {
            Dispose (disposing: false);
        }

        public void Dispose ()
        {
            Dispose (disposing: true);
            System.GC.SuppressFinalize (this);
        }
    }
}
