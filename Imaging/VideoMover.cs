using Emgu.CV;
using Emgu.CV.Structure;
using MED.Core;
using MED.Imaging;
using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.IO;
using System.Numerics;
using System.Text.Json.Nodes;
using static MED.Imaging.VideoMover;

namespace MED.Imaging
{
    public class VideoMover : ImageMover, IMatFrameConsumer, IMatFrameProvider
    {
        //isAsynchrone = true
        public VideoMover(string name = "VideoMover", Performance? performance = null, Control? invokeHandler = null, IConsumer? consumer = null, bool isAsynchrone = true)
            : base(name, performance, invokeHandler, consumer, isAsynchrone)
        {
            ProcessIcon = ProcessIconDefault = "Client";
            ResetOnImageChanged = false;//self managed
        }


        [Browsable(false)]
        public override Dictionary<string, object> ObjectsProperties
        {
            get
            {
                var dict = base.ObjectsProperties;
                //if (Capture != null)
                //    dict.Add("Capture", Capture);//Forbidden

                return dict;
            }
        }

        public override void Start()
        {
            PreviousFrame?.Dispose();
            PreviousFrame = null;

            base.Start();

            ProcessState = System.Threading.ThreadState.Running;
        }

        #region Image

        public override Bitmap? GetImage(IImageProvider? provider = null)
        {
            Performance?.Resume($"GetImage Transformer #{MovingDetector}", true);
            var image = FrameToImage((IMatFrameProvider?)provider, Frame);
            Performance?.Pause();
            return image;
        }
        #endregion



        #region Frame

        private Mat? PreviousFrame;

        [Browsable(false)]
        [Category("Video capture")]
        public Mat? Frame { get; protected set; }
        public void FrameChanged(IMatFrameProvider? sender, EventArgs e)
        {
            ImageProvider = (IImageProvider?)sender;

            Frame = ((IMatFrameProvider?)ImageProvider)?.Frame;

            ////Do the job in same thread
            //Performance.Resume($"Process MoveDetectorAction Algorithm #{Transformer}", true);

            //Image = GetImage((IImageProvider)sender);

            //Performance.Pause($"done Process MoveDetectorAction");
            //Performance.Step(Performance.ToString());

            InvokeFrameChanged(sender, e);

            if (sender != null)
                ImageChanged((IImageProvider)sender, e);
        }

        public void InvokeFrameChanged(IMatFrameProvider? sender, EventArgs e) => InvokePropertyChanged(sender, OnFrameChanged, e);

        public IMatFrameProvider.FrameChangedDelegate? OnFrameChanged;
        #endregion

        #region Settings

        [Browsable(false)]
        public override string ImageFile => base.ImageFile;

        public enum MovingDetectors
        {
            GrayDiffBlur4px = 1,
            GrayDiffBlur2px,
            GrayDiff,

            None = 0,
            _DevTest = -1,
        }

        [Browsable(true)]
        [Category("Motion")]
        public MovingDetectors MovingDetector { get; set; }

        [Category("Motion")]
        public bool FixedBackground { get; set; }
        [Category("Motion")]
        public bool FixedBackgroundNow { get; set; }

        private Mat? _ThresholdMat = null;

        /**
         * DetectionLimit = 1 to 255
         * */
        [Browsable(true)]
        [Category("Motion")]
        public int DetectionLimit { get; set; } = 1;


        public override void LoadSettings(ProcessSettings? settings = null, string fileName = "")
        {
            base.LoadSettings(settings, fileName);
            if (settings == null && (settings = ProcessSettings) == null)
                return;
            FixedBackground = (bool)(settings.GetValue("FixedBackground", FixedBackground) ?? FixedBackground);
            DetectionLimit = (int)(settings.GetValue("DetectionLimit", DetectionLimit) ?? DetectionLimit);
            if (Enum.TryParse((settings.GetValue("MovingDetector", MovingDetector) ?? MovingDetector).ToString(), out MovingDetectors a))
                MovingDetector = a;
        }
        public override JsonObject SaveProcess(JsonObject? node = null)
        {
            node = base.SaveProcess(node);
            node.Add("MovingDetector", MovingDetector.ToString());
            node.Add("DetectionLimit", DetectionLimit);
            node.Add("FixedBackground", FixedBackground);
            return node;
        }

        #endregion

        #region CreateImage
        public virtual Bitmap? FrameToImage(IMatFrameProvider? sender, Mat? currentFrame)
        {
            if (this.Disposing || this.IsDisposed || ImageProvider == null)
                return null;

            if (PreviousFrame == null)
            {
                if (currentFrame == null)
                    return null;
                InitializeMotionDetection(currentFrame);
                if (ImageSizeMin.IsEmpty)
                    return null;
                else
                    return EmptyImage;
            }

            if (currentFrame == null)
                return null;

            Mat? frameDiff;

            GraphicsPath? grPath = null;
            Dictionary<GraphicsPath, RectangleF>? grPathsBounds = null;
            Region? region = null;

            switch (MovingDetector)
            {
                case MovingDetectors._DevTest:

                    if (PreviousFrame != null
                        && !currentFrame.Size.IsEmpty && !PreviousFrame.Size.IsEmpty &&
                        currentFrame.Size == PreviousFrame.Size)
                    {
                        frameDiff = new();
                        CvInvoke.AbsDiff(PreviousFrame, currentFrame, frameDiff);

                        Object v = CvInvoke.ContourArea(frameDiff, false);
                        Performance?.Log($"ContourArea {v}");

                        v = CvInvoke.Moments(frameDiff, false);
                        Performance?.Log($"Moments {v}");

                    }
                    break;

                case MovingDetectors.None:
                    frameDiff = currentFrame;
                    break;

                case MovingDetectors.GrayDiff:
                case MovingDetectors.GrayDiffBlur2px:
                case MovingDetectors.GrayDiffBlur4px:
                    if (_ThresholdMat == null
                        || !_ThresholdMat.Size.Equals(currentFrame.Size)
                        || FixedBackgroundNow)
                    {
                        _ThresholdMat = new();
                        CvInvoke.CvtColor(currentFrame, _ThresholdMat, Emgu.CV.CvEnum.ColorConversion.Bgr2Gray);
                        double threshold = (double)DetectionLimit;
                        _ThresholdMat.SetTo(new MCvScalar(threshold, threshold, threshold));
                    }
                    if (FixedBackgroundNow)
                    {
                        FixedBackgroundNow = false;
                        PreviousFrame = new();
                        CvInvoke.CvtColor(currentFrame, PreviousFrame, Emgu.CV.CvEnum.ColorConversion.Bgr2Gray);
                        //PreviousFrame = currentFrame.Clone();
                    }
                    if (PreviousFrame == null || PreviousFrame.ElementSize != 1)
                    {
                        PreviousFrame = new();
                        CvInvoke.CvtColor(currentFrame, PreviousFrame, Emgu.CV.CvEnum.ColorConversion.Bgr2Gray);
                        return null;
                    }

                    if (PreviousFrame != null
                        && !currentFrame.Size.IsEmpty && !PreviousFrame.Size.IsEmpty &&
                        currentFrame.Size.Equals(PreviousFrame.Size))
                    {
                        frameDiff = new();

                        Mat grayCurrent = new();
                        CvInvoke.CvtColor(currentFrame, grayCurrent, Emgu.CV.CvEnum.ColorConversion.Bgr2Gray);

                        CvInvoke.AbsDiff(PreviousFrame, grayCurrent, frameDiff);

                        if (!FixedBackground)
                        {
                            PreviousFrame.Dispose();
                            PreviousFrame = grayCurrent;
                        }

                        CvInvoke.Subtract(frameDiff, _ThresholdMat, frameDiff);

                        switch (MovingDetector)
                        {
                            case MovingDetectors.GrayDiffBlur2px:
                                CvInvoke.Blur(frameDiff, frameDiff, new Size(2, 2), new Point(-1, -1));
                                break;
                            case MovingDetectors.GrayDiffBlur4px:
                            default:
                                CvInvoke.Blur(frameDiff, frameDiff, new Size(4, 4), new Point(-1, -1));
                                break;
                        }

                        region = GetContourRegion(frameDiff, out grPath, out grPathsBounds);
                    }
                    break;
                default:
                    Performance?.Error($"Transformer {MovingDetector} inconnu");
                    break;
            }

            if (currentFrame == null)
                return null;

            if (!ImageSizeMin.IsEmpty && currentFrame.Size != ImageSizeMin)
            {
                Mat resized = new();
                CvInvoke.Resize(currentFrame, resized, ImageSizeMin);

                if (region != null && grPath != null && grPathsBounds != null)
                {
                    Matrix transformMatrix = new Matrix();
                    transformMatrix.Scale((float)resized.Width / currentFrame.Width, (float)resized.Height / currentFrame.Height);

                    grPath.Transform(transformMatrix);

                    foreach (var (grPathU, grPathBounds) in grPathsBounds)
                    {
                        grPathU.Transform(transformMatrix);
                        grPathsBounds[grPathU] = grPathU.GetBounds();
                    }
                    region.Transform(transformMatrix);
                }
                currentFrame = resized;
            }
            ClipRegion = region;
            //Performance?.Debug($"Set ClipRegion {region}");
            //Performance?.Debug($"Set ClipRegionTranslated {ClipRegionTranslated}");
            ClipPath = grPath;
            ClipPathsBounds = grPathsBounds;

            return currentFrame.ToBitmap();
        }

        #endregion

        /**
         * InitializeMotionDetection
         * 
         */
        private void InitializeMotionDetection(Mat currentFrame)
        {
            PreviousFrame = currentFrame.Clone();
        }

        [Browsable(false)]
        public Emgu.CV.VideoCapture? Capture
        {
            get
            {
                if (ImageProvider == null || !(ImageProvider is IMatFrameProvider))
                    return null;
                return ((IMatFrameProvider)ImageProvider).Capture;
            }
        }

    }
}
