using Emgu.CV;
using Emgu.CV.Structure;

using libMotionDetection;
using libVideoCapture;
using MED.Core;
using MED.Imaging;
using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.IO;
using System.Numerics;
using System.Text.Json.Nodes;
using static libMotionDetection.MotionDetectionWithMotionHistory;

namespace MED.Imaging
{
    public class EmguMoving : Mover, IMatFrameConsumer, IMatFrameProvider
    {
        //isAsynchrone = true
        public EmguMoving(string name = "EmguMoving", Performance? performance = null, Control? invokeHandler = null, IImageConsumer? imageConsumer = null, bool isAsynchrone = true)
            : base(name, performance, invokeHandler, imageConsumer, isAsynchrone)
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
            MotionDetectionInitialized = false;

            PreviousFrame?.Dispose();
            PreviousFrame = null;

            base.Start();

            ProcessState = System.Threading.ThreadState.Running;
        }

        private bool MotionDetectionInitialized = false;

        #region Image

        public override Bitmap? GetImage(IImageProvider? provider = null)
        {
            Performance?.Resume($"GetImage Transformer #{Transformer}", true);
            var image = FrameToImage((IMatFrameProvider)provider, Frame);
            Performance?.Pause();
            return image;
        }
        #endregion



        #region Frame

        private Mat? PreviousFrame;

        [Browsable(false)]
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

            ImageChanged((IImageProvider)sender, e);
        }

        public void InvokeFrameChanged(IMatFrameProvider? sender, EventArgs e) => InvokePropertyChanged(sender, OnFrameChanged, e);

        public IMatFrameProvider.FrameChangedDelegate OnFrameChanged;
        #endregion

        #region Settings
        public enum CvInvokeTransformers
        {
            CvInvoke_AbsDiff,
            BorderFinder,
            PyrDown,
            Test,
            DenseOpticalFlow_WithHSV,
            Motion_History,
            SparseOpticalFlow,
            TemporalFrameDifferencing,
            BackgroundSubtraction_MOG2,
            BackgroundSubtraction_KNN,
            BackgroundSubtraction_CNT,
            BackgroundSubtraction_GMG,
            FixedBackground_MOG2,
        }

        [Browsable(true)]
        public CvInvokeTransformers Transformer { get; set; }

        public int MotionThreshold { get; set; } = 1000;
        public bool MotionCalculateInfo { get; set; } = true;
        public double MotionPixelCountThresholdPerCentArea { get; set; } = 0.05;

        public bool MhiReset { get; set; }
        public double MhiDuration { get; set; } = 2;
        public double MhiMinTimeDelta { get; set; } = 0.05;
        public double MhiMaxTimeDelta { get; set; } = 0.5;

        public bool FixedBackground { get; set; }
        public bool FixedBackgroundNow { get; set; }

        private Mat _ThresholdMat = null;

        /**
         * DetectionLimit = 1 to 255
         * */
        [Browsable(true)]
        public int DetectionLimit { get; set; } = 1;


        public override void LoadSettings(ProcessSettings settings = null, string fileName = "")
        {
            base.LoadSettings(settings, fileName);

            FixedBackground = (bool)ProcessSettings.GetValue("FixedBackground", FixedBackground);
            DetectionLimit = (int)ProcessSettings.GetValue("DetectionLimit", DetectionLimit);
            CvInvokeTransformers a;
            if (Enum.TryParse<CvInvokeTransformers>(ProcessSettings.GetValue("Transformer", Transformer).ToString(), out a))
                Transformer = a;
        }
        public override JsonObject SaveProcess(JsonObject node = null)
        {
            node = base.SaveProcess(node);
            node.Add("Transformer", Transformer.ToString());
            node.Add("DetectionLimit", DetectionLimit);
            node.Add("FixedBackground", FixedBackground);
            return node;
        }
        //public override void SaveSettings(bool saveChildren = true)
        //{
        //    Core.Settings.SetValue("Transformer", Name, Transformer);

        //    base.SaveSettings(saveChildren);
        //}
        #endregion

        #region CreateImage
        public virtual Bitmap? FrameToImage(IMatFrameProvider sender, Mat currentFrame)
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

            VideoCapture? _capture = ((IMatFrameProvider)ImageProvider).Capture;
            Mat? frameDiff = null;
            Mat? oldPrev = null;

            switch (Transformer)
            {
                case CvInvokeTransformers.Test:

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

                case CvInvokeTransformers.PyrDown:

                    if (PreviousFrame != null
                        && !currentFrame.Size.IsEmpty && !PreviousFrame.Size.IsEmpty &&
                        currentFrame.Size == PreviousFrame.Size)
                    {
                        frameDiff = new();
                        CvInvoke.PyrDown(currentFrame, frameDiff, Emgu.CV.CvEnum.BorderType.Reflect);


                    }
                    break;

                case CvInvokeTransformers.BorderFinder:

                    if (PreviousFrame != null
                        && !currentFrame.Size.IsEmpty && !PreviousFrame.Size.IsEmpty &&
                        currentFrame.Size == PreviousFrame.Size)
                    {
                        frameDiff = new();
                        CvInvoke.AbsDiff(PreviousFrame, currentFrame, frameDiff);

                        bool useBmp = true;
                        Bitmap bmp;
                        if (useBmp)
                            bmp = frameDiff.ToBitmap();
                        else
                            bmp = new Bitmap(currentFrame.Width, currentFrame.Height);



                        Performance.Step($"borderFinder, useBmp={useBmp}");

                        BorderFinder borderFinder = new(Color.FromArgb(DetectionLimit, DetectionLimit, DetectionLimit));

                        List<PointF[]> points;
                        if (useBmp)
                            points = borderFinder.GetOutlinePointsNEW(bmp);
                        //points = borderFinder.Find(bmp);
                        else
                            points = borderFinder.Find(frameDiff);

                        if (points.Count > 0 && points[0].Count() > 0)
                        {
                            Performance.Step($"points {points.Count}");

                            GraphicsPath path = borderFinder.GetPath(points);

                            Performance.Step($"path {path.PointCount}");

                            bmp = new Bitmap(currentFrame.Width, currentFrame.Height);

                            Graphics gr = Graphics.FromImage(bmp);

                            SolidBrush brush = new(Color.Red);
                            gr.FillPath(brush, path);

                            gr.Dispose();


                            Performance.Step($"FillPath done");
                        }
                        oldPrev = PreviousFrame;
                        PreviousFrame = currentFrame.Clone();
                        oldPrev?.Dispose();

                        return bmp;

                    }
                    break;

                case CvInvokeTransformers.CvInvoke_AbsDiff:
                    if (_ThresholdMat == null || FixedBackgroundNow)
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
                        currentFrame.Size == PreviousFrame.Size)
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

                        //Blur TODO
                        CvInvoke.Blur(frameDiff, frameDiff, new Size(8, 8), new Point(-1, -1));

                        GraphicsPath grPath;
                        Region? region = GetContourRegion(frameDiff, out grPath);
                        if (region != null
                            && !ImageSizeMin.IsEmpty && currentFrame.Size != ImageSizeMin)
                        {
                            Mat resized = new();
                            CvInvoke.Resize(currentFrame, resized, ImageSizeMin);

                            Matrix transformMatrix = new Matrix();
                            transformMatrix.Scale((float)resized.Width / currentFrame.Width, (float)resized.Height / currentFrame.Height);
                            grPath.Transform(transformMatrix);
                            region.Transform(transformMatrix);
                            currentFrame = resized;
                        }
                        ClipRegion = region;
                        ClipPath = grPath;

                        return currentFrame.ToBitmap();
                    }
                    break;

                case CvInvokeTransformers.DenseOpticalFlow_WithHSV:
                    // Dense Optical Flow
                    if (PreviousFrame != null
                        && !currentFrame.Size.IsEmpty && !PreviousFrame.Size.IsEmpty &&
                        currentFrame.Size == PreviousFrame.Size)
                    {

                        Mat flow = motionDetectionWithDenseOpticalFlow.CalculateDenseOpticalFlow(PreviousFrame, currentFrame);

                        frameDiff = motionDetectionWithDenseOpticalFlow.OpticalFlowVisualizationWithHSV(flow);
                        flow.Dispose();
                    }
                    break;

                case CvInvokeTransformers.Motion_History:
                    frameDiff = currentFrame.Clone();

                    motionDetectionWithMotionHistory.Setting = new MotionDetectionWithMotionHistory.MotionSetting
                    {
                        MotionThreshold = MotionThreshold,
                        CalculateMotionInfo = MotionCalculateInfo,
                        MotionPixelCountThresholdPerCentArea = MotionPixelCountThresholdPerCentArea
                    };
                    if (MhiReset)
                    {
                        motionDetectionWithMotionHistory.Reset(MhiDuration, MhiMaxTimeDelta, MhiMinTimeDelta);
                        MhiReset = false;
                    }

                    motionDetectionWithMotionHistory.GetFrameMotionComponents(frameDiff);

                    // If the check box for motion info is checked, then calculate the motion image
                    bool checkBoxCalculateMotionInfo = false;
                    if (checkBoxCalculateMotionInfo)
                    {
                        Mat motionImage = motionDetectionWithMotionHistory.GetMotionImage();
                        //SafeSetImageBoxImage(motionImageBox, motionImage);
                    }
                    else
                    {
                        //SafeSetImageBoxImage(motionImageBox, null);
                    }

                    motionDetectionWithMotionHistory.MotionDetectionDrawGraphics(frameDiff);

                    //Foreground
                    frameDiff = motionDetectionWithMotionHistory.MotionForgroundMask.Clone();

                    // Display the amount of motions found on the current image
                    var components = motionDetectionWithMotionHistory.MotionComponents;
                    Performance.Log($"Total Motions found: {components.Length}");

                    int idx = 0;
                    foreach (MotionDetectionWithMotionHistory.MotionComponent comp in components)
                    {
                        Performance.Log($"Motion Component {idx}: {comp}");
                        idx++;
                    }
                    if (frameDiff != null)
                        return frameDiff.ToBitmap();
                    break;

                case CvInvokeTransformers.SparseOpticalFlow:

                    // Sparse Optical Flow (Lucas-Kanade)
                    Mat image = new Mat();
                    motionDetectionWithSparseOpticalFlow.ProcessFrame(image);

                    // 1. Main visualizer: Frame with overlay
                    Mat displayImage = image.Clone();
                    motionDetectionWithSparseOpticalFlow.DrawMotionVectors(displayImage);
                    //SafeSetImageBoxImage(capturedImageBox, displayImage);

                    // 2. Motion visualizer: Isolated vectors on black background
                    Mat motionMat = new Mat(image.Size, image.Depth, image.NumberOfChannels);
                    motionMat.SetTo(new MCvScalar(0, 0, 0));
                    motionDetectionWithSparseOpticalFlow.DrawMotionVectors(motionMat);
                    //SafeSetImageBoxImage(motionImageBox, motionMat);

                    // 3. Foreground visualizer: Temporal frame differencing
                    if (!PreviousFrame.Size.IsEmpty && PreviousFrame.Size == image.Size)
                    {
                        frameDiff = image.Clone();
                        CvInvoke.AbsDiff(PreviousFrame, image, frameDiff);
                        //SafeSetImageBoxImage(forgroundImageBox, frameDiff);
                    }
                    else
                    {
                        Performance.Log($"{Transformer} : PreviousFrame.Size.IsEmpty");
                        frameDiff = displayImage;
                    }

                    //frameDiff = displayImage;

                    image.Dispose();

                    var vectors = motionDetectionWithSparseOpticalFlow.MotionVectors;
                    Performance.Log($"Active Tracked Points: {vectors.Count}");
                    break;

                case CvInvokeTransformers.TemporalFrameDifferencing:
                    // Temporal Frame Differencing (AbsDiff)
                    image = new Mat();
                    motionDetectionWithFrameDifferencing.ProcessFrame(image);

                    // 1. Captured visualizer: Raw frame with bounding boxes overlaid
                    displayImage = image.Clone();
                    motionDetectionWithFrameDifferencing.DrawMotionGraphics(displayImage);

                    // 2. Motion visualizer: Grayscale raw diff
                    frameDiff = motionDetectionWithFrameDifferencing.MotionRawDiff.Clone();

                    // 3. Foreground visualizer: Binary thresholded mask
                    frameDiff = motionDetectionWithFrameDifferencing.MotionForgroundMask.Clone();

                    //frameDiff = displayImage;

                    image.Dispose();

                    var componentsR = motionDetectionWithFrameDifferencing.MotionComponents;
                    Performance.Log($"Total Motions found: {componentsR.Length}");

                    idx = 0;
                    foreach (Rectangle comp in componentsR)
                    {
                        Performance.Log($"Motion Box {idx}: {comp}");
                        idx++;
                    }

                    break;
                case CvInvokeTransformers.BackgroundSubtraction_MOG2:
                case CvInvokeTransformers.BackgroundSubtraction_KNN:
                case CvInvokeTransformers.BackgroundSubtraction_CNT:
                case CvInvokeTransformers.BackgroundSubtraction_GMG:

                    // Background Subtraction (MOG2 / KNN / CNT / GMG)
                    MotionDetectionWithBackgroundSubtraction.SubtractorType subtractorType =
                        Transformer == CvInvokeTransformers.BackgroundSubtraction_MOG2 ? MotionDetectionWithBackgroundSubtraction.SubtractorType.MOG2 :
                        Transformer == CvInvokeTransformers.BackgroundSubtraction_KNN ? MotionDetectionWithBackgroundSubtraction.SubtractorType.KNN :
                        Transformer == CvInvokeTransformers.BackgroundSubtraction_CNT ? MotionDetectionWithBackgroundSubtraction.SubtractorType.CNT :
                                     MotionDetectionWithBackgroundSubtraction.SubtractorType.GMG;

                    motionDetectionWithBackgroundSubtraction.ActiveSubtractor = subtractorType;

                    image = new Mat();
                    frameDiff = currentFrame.Clone();

                    motionDetectionWithBackgroundSubtraction.ProcessFrame(frameDiff);

                    // 1. Captured visualizer: Raw frame with bounding boxes overlaid
                    displayImage = frameDiff.Clone();
                    motionDetectionWithBackgroundSubtraction.DrawMotionGraphics(displayImage);
                    //SafeSetImageBoxImage(capturedImageBox, displayImage);

                    // 2. Motion visualizer: Raw camera frame
                    //SafeSetImageBoxImage(motionImageBox, image.Clone());

                    // 3. Foreground visualizer: Binary mask
                    frameDiff = motionDetectionWithBackgroundSubtraction.MotionForgroundMask.Clone();

                    //frameDiff = displayImage;

                    image.Dispose();

                    componentsR = motionDetectionWithBackgroundSubtraction.MotionComponents;
                    Performance.Log($"Total Motions found: {componentsR.Length}");

                    idx = 0;
                    foreach (Rectangle comp in componentsR)
                    {
                        Performance.Log($"Motion Box {idx}: {comp}");
                        idx++;
                    }
                    break;
                case CvInvokeTransformers.FixedBackground_MOG2:

                    // Background Subtraction (MOG2 / KNN / CNT / GMG)
                    MotionDetectionWithFixedBackgroundSubtraction.SubtractorType fixedSubtractorType =
                        Transformer == CvInvokeTransformers.FixedBackground_MOG2 ? MotionDetectionWithFixedBackgroundSubtraction.SubtractorType.MOG2 :
                        Transformer == CvInvokeTransformers.BackgroundSubtraction_MOG2 ? MotionDetectionWithFixedBackgroundSubtraction.SubtractorType.MOG2 :
                        Transformer == CvInvokeTransformers.BackgroundSubtraction_KNN ? MotionDetectionWithFixedBackgroundSubtraction.SubtractorType.KNN :
                        Transformer == CvInvokeTransformers.BackgroundSubtraction_CNT ? MotionDetectionWithFixedBackgroundSubtraction.SubtractorType.CNT :
                                     MotionDetectionWithFixedBackgroundSubtraction.SubtractorType.GMG;

                    motionDetectionWithFixedBackgroundSubtraction.ActiveSubtractor = fixedSubtractorType;
                    if (FixedBackgroundNow)
                    {
                        FixedBackgroundNow = false;
                        motionDetectionWithFixedBackgroundSubtraction.MotionBackgroundMask = currentFrame.Clone();
                    }
                    image = new Mat();
                    frameDiff = currentFrame.Clone();

                    motionDetectionWithFixedBackgroundSubtraction.ProcessFrame(frameDiff);
                    if (motionDetectionWithFixedBackgroundSubtraction.MotionComponents.Length == 0)
                        return null;
                    //GraphicsPath grPath = new GraphicsPath();
                    //foreach (var pts in motionDetectionWithFixedBackgroundSubtraction.GetContours(0.01))
                    //{
                    //    grPath.AddPolygon(pts);
                    //    grPath.CloseFigure();
                    //}
                    //Region grRegion = new Region(grPath);

                    //var bmpSource = frameDiff.ToBitmap();

                    //Bitmap newBmp = new Bitmap(frameDiff.Width, frameDiff.Height); 

                    //Graphics grBmp = Graphics.FromImage(newBmp);
                    //grBmp.SetClip(grRegion, CombineMode.Replace);
                    //grBmp.DrawImage(bmpSource, 0, 0);
                    //grBmp.Dispose();
                    //return newBmp;


                    // 1. Captured visualizer: Raw frame with bounding boxes overlaid
                    displayImage = frameDiff.Clone();
                    motionDetectionWithFixedBackgroundSubtraction.DrawMotionGraphics(displayImage);
                    //SafeSetImageBoxImage(capturedImageBox, displayImage);

                    // 2. Motion visualizer: Raw camera frame
                    //SafeSetImageBoxImage(motionImageBox, image.Clone());

                    // 3. Foreground visualizer: Binary mask
                    frameDiff = motionDetectionWithFixedBackgroundSubtraction.MotionForgroundMask.Clone();

                    //frameDiff = displayImage;

                    image.Dispose();

                    componentsR = motionDetectionWithFixedBackgroundSubtraction.MotionComponents;
                    Performance.Log($"Total Motions found: {componentsR.Length}");

                    idx = 0;
                    foreach (Rectangle comp in componentsR)
                    {
                        Performance.Log($"Motion Box {idx}: {comp}");
                        idx++;
                    }
                    break;
                default:
                    Performance.Error($"Algorithme {Transformer} inconnu");
                    break;
            }

            oldPrev = PreviousFrame;
            PreviousFrame = currentFrame.Clone();
            oldPrev?.Dispose();

            if (frameDiff != null)
                return frameDiff.ToBitmap();

            return null;
        }

        #endregion

        /**
         * InitializeMotionDetection
         * 
         */
        private void InitializeMotionDetection(Mat currentFrame)
        {
            //if (DISOpticalFlow == null)
            //    DISOpticalFlow = new DISOpticalFlow(DISOpticalFlow.Preset.Fast);
            InitializeMotionDetectionExt();

            PreviousFrame = currentFrame.Clone();

            MotionDetectionInitialized = true;
        }

        [Browsable(true)]
        public VideoCapture Capture
        {
            get
            {
                if (ImageProvider == null || !(ImageProvider is IMatFrameProvider))
                    return null;
                return (ImageProvider as IMatFrameProvider).Capture;
            }
        }


        private MotionDetectionWithDenseOpticalFlow motionDetectionWithDenseOpticalFlow;
        private MotionDetectionWithMotionHistory motionDetectionWithMotionHistory;
        private MotionDetectionWithSparseOpticalFlow motionDetectionWithSparseOpticalFlow;
        private MotionDetectionWithFrameDifferencing motionDetectionWithFrameDifferencing;
        private MotionDetectionWithBackgroundSubtraction motionDetectionWithBackgroundSubtraction;
        private MotionDetectionWithFixedBackgroundSubtraction motionDetectionWithFixedBackgroundSubtraction;
        private void InitializeMotionDetectionExt()
        {
            motionDetectionWithDenseOpticalFlow?.Dispose();
            motionDetectionWithDenseOpticalFlow = new MotionDetectionWithDenseOpticalFlow();

            motionDetectionWithMotionHistory?.Dispose();
            motionDetectionWithMotionHistory = new MotionDetectionWithMotionHistory();
            MhiDuration = motionDetectionWithMotionHistory.HistorySetting.MhiDuration;
            MhiMinTimeDelta = motionDetectionWithMotionHistory.HistorySetting.MinTimeDelta;
            MhiMaxTimeDelta = motionDetectionWithMotionHistory.HistorySetting.MaxTimeDelta;

            motionDetectionWithSparseOpticalFlow?.Dispose();
            motionDetectionWithSparseOpticalFlow = new MotionDetectionWithSparseOpticalFlow();

            motionDetectionWithFrameDifferencing?.Dispose();
            motionDetectionWithFrameDifferencing = new MotionDetectionWithFrameDifferencing();

            motionDetectionWithBackgroundSubtraction?.Dispose();
            motionDetectionWithBackgroundSubtraction = new MotionDetectionWithBackgroundSubtraction();

            motionDetectionWithFixedBackgroundSubtraction?.Dispose();
            motionDetectionWithFixedBackgroundSubtraction = new();

        }
    }
}
