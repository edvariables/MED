using Emgu.CV;
using Emgu.CV.Structure;

using libMotionDetection;
using libVideoCapture;

using System.ComponentModel;

namespace MED
{
    public class EmguMoving : ImageProcess, IMatFrameConsumer, IMatFrameProvider
    {
        //isAsynchrone = true
        public EmguMoving(string name = "EmguMoving", Performance performance = null, Control invokeHandler = null, IImageConsumer imageConsumer = null, bool isAsynchrone = true)
            : base(name, performance, invokeHandler, imageConsumer, isAsynchrone)
        {
            ResetOnImageChanged = false;//self managed
        }


        [Browsable(false)]
        public override Dictionary<string, object> ObjectsProperties
        {
            get
            {
                var dict = base.ObjectsProperties;
                //if (DISOpticalFlow != null)
                //{
                //    dict.Add(this.Name + ".DISOpticalFlow", DISOpticalFlow);
                //    //for (int i = 0; i < MoveDetector.IdxLimites; i++)
                //    //    dict.Add($"{this.Name}.MoveDetector.Limites{i}", MoveDetector.get_Limites(i));
                //}

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
        //public override void ImageChanged(IImageProvider sender, EventArgs e)
        //{
        //    ImageProvider = sender;
        //    base.ImageChanged(sender, e);
        //}


        //private Bitmap _Image;
        //[Browsable(false)]
        //public override Bitmap Image
        //{
        //    get => _Image;
        //    set
        //    {
        //        //Performance.Debug("Set_Image (override) : " + (_Image == null ? "<null>" : "Bitmap") + " => " + (value == null ? "<null>" : "Bitmap"));
        //        _Image = value;
        //    }
        //}

        public override Bitmap GetImage(IImageProvider provider = null)
        {
            return FrameToImage((IMatFrameProvider)provider, Frame);
        }
        #endregion



        #region Frame

        private Mat PreviousFrame;

        [Browsable(false)]
        public Mat Frame { get; protected set; }
        public void FrameChanged(IMatFrameProvider sender, EventArgs e)
        {
            ImageProvider = (IImageProvider)sender;

            Frame = (ImageProvider as IMatFrameProvider).Frame;

            //Do the job in same thread
            Performance.Resume($"Process MoveDetectorAction Algorithm #{Algorithm}", true);

            Image = GetImage((IImageProvider)sender);

            Performance.Pause($"done Process MoveDetectorAction");
            //Performance.Step(Performance.ToString());

            InvokeFrameChanged(sender, e);

            ImageChanged((IImageProvider)sender, e);
        }

        public void InvokeFrameChanged(IMatFrameProvider sender, EventArgs e)
        {
            InvokePropertyChanged(sender, OnFrameChanged, e);
        }

        public IMatFrameProvider.FrameChangedDelegate OnFrameChanged;
        #endregion

        #region Settings
        public enum CvInvokeAlgorithms
        {
            CvInvoke_AbsDiff,
            Test,
            DenseOpticalFlow_WithHSV,
            Motion_History,
            SparseOpticalFlow,
            TemporalFrameDifferencing,
            BackgroundSubtraction_MOG2,
            BackgroundSubtraction_KNN,
            BackgroundSubtraction_CNT,
            BackgroundSubtraction_GMG,
        }
        [Browsable(true)]
        public CvInvokeAlgorithms Algorithm { get; set; }

        [Browsable(true)]
        public List<CvInvokeAlgorithms> Algorithms { get; set; }

        public override void LoadSettings(bool loadChildren = true)
        {
            base.LoadSettings(loadChildren);

            CvInvokeAlgorithms a;
            if (Enum.TryParse<CvInvokeAlgorithms>(Core.Settings.GetValue("Algorithm", Name, Algorithm).ToString(), out a))
                Algorithm = a;
        }
        public override void SaveSettings(bool saveChildren = true)
        {
            Core.Settings.SetValue("Algorithm", Name, Algorithm);

            base.SaveSettings(saveChildren);
        }
        #endregion

        #region CreateImage
        public Bitmap FrameToImage(IMatFrameProvider sender, Mat currentFrame)
        {
            if (this.Disposing || this.IsDisposed || ImageProvider == null)
                return null;

            if (PreviousFrame == null)
            {
                InitializeMotionDetection(Frame);
                if (ImageSizeMax.IsEmpty)
                    return null;
                else
                    return new Bitmap(ImageSizeMax.Width, ImageSizeMax.Height);
            }

            if (currentFrame == null)
                return null;

            VideoCapture _capture = (ImageProvider as IMatFrameProvider).Capture;
            Mat frameDiff = null;
            Mat oldPrev = null;

            switch (Algorithm)
            {
                case CvInvokeAlgorithms.Test:

                    if (PreviousFrame != null
                        && !currentFrame.Size.IsEmpty && !PreviousFrame.Size.IsEmpty &&
                        currentFrame.Size == PreviousFrame.Size)
                    {
                        frameDiff = new();
                        CvInvoke.AbsDiff(PreviousFrame, currentFrame, frameDiff);

                        Object v = CvInvoke.ContourArea(frameDiff, false);
                        Performance.Log($"ContourArea {v}");

                        v = CvInvoke.Moments(frameDiff, false);
                        Performance.Log($"Moments {v}");

                    }
                    break;

                case CvInvokeAlgorithms.CvInvoke_AbsDiff:
                    // Dense Optical Flow
                    if (PreviousFrame != null
                        && !currentFrame.Size.IsEmpty && !PreviousFrame.Size.IsEmpty &&
                        currentFrame.Size == PreviousFrame.Size)
                    {
                        frameDiff = new();
                        CvInvoke.AbsDiff(PreviousFrame, currentFrame, frameDiff);
                    }
                    break;

                case CvInvokeAlgorithms.DenseOpticalFlow_WithHSV:
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

                case CvInvokeAlgorithms.Motion_History:
                    frameDiff = currentFrame.Clone();

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

                case CvInvokeAlgorithms.SparseOpticalFlow:

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
                        Performance.Log($"{Algorithm} : PreviousFrame.Size.IsEmpty");
                        frameDiff = displayImage;
                    }

                    //frameDiff = displayImage;

                    image.Dispose();

                    var vectors = motionDetectionWithSparseOpticalFlow.MotionVectors;
                    Performance.Log($"Active Tracked Points: {vectors.Count}");
                    break;

                case CvInvokeAlgorithms.TemporalFrameDifferencing:
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
                case CvInvokeAlgorithms.BackgroundSubtraction_MOG2:
                case CvInvokeAlgorithms.BackgroundSubtraction_KNN:
                case CvInvokeAlgorithms.BackgroundSubtraction_CNT:
                case CvInvokeAlgorithms.BackgroundSubtraction_GMG:

                    // Background Subtraction (MOG2 / KNN / CNT / GMG)
                    MotionDetectionWithBackgroundSubtraction.SubtractorType subtractorType =
                        Algorithm == CvInvokeAlgorithms.BackgroundSubtraction_MOG2 ? MotionDetectionWithBackgroundSubtraction.SubtractorType.MOG2 :
                        Algorithm == CvInvokeAlgorithms.BackgroundSubtraction_KNN ? MotionDetectionWithBackgroundSubtraction.SubtractorType.KNN :
                        Algorithm == CvInvokeAlgorithms.BackgroundSubtraction_CNT ? MotionDetectionWithBackgroundSubtraction.SubtractorType.CNT :
                                     MotionDetectionWithBackgroundSubtraction.SubtractorType.GMG;

                    motionDetectionWithBackgroundSubtraction.ActiveSubtractor = subtractorType;

                    image = new Mat();

                    motionDetectionWithBackgroundSubtraction.ProcessFrame(image);

                    // 1. Captured visualizer: Raw frame with bounding boxes overlaid
                    displayImage = image.Clone();
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
                default:
                    Performance.Error($"Algorithme {Algorithm} inconnu");
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
        private void InitializeMotionDetectionExt()
        {
            motionDetectionWithDenseOpticalFlow?.Dispose();
            motionDetectionWithDenseOpticalFlow = new MotionDetectionWithDenseOpticalFlow();

            motionDetectionWithMotionHistory?.Dispose();
            motionDetectionWithMotionHistory = new MotionDetectionWithMotionHistory();

            motionDetectionWithSparseOpticalFlow?.Dispose();
            motionDetectionWithSparseOpticalFlow = new MotionDetectionWithSparseOpticalFlow();

            motionDetectionWithFrameDifferencing?.Dispose();
            motionDetectionWithFrameDifferencing = new MotionDetectionWithFrameDifferencing();

            motionDetectionWithBackgroundSubtraction?.Dispose();
            motionDetectionWithBackgroundSubtraction = new MotionDetectionWithBackgroundSubtraction();

        }

    }
}
