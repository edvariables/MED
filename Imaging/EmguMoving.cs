using Emgu.CV;
using Emgu.CV.Structure;

using libMotionDetection;
using libVideoCapture;


using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using static Emgu.CV.ML.KNearest;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace MED
{
    public class EmguMoving : ImageProcess, IMatFrameConsumer, IMatFrameProvider
    {
        //isAynchrone = true
        public EmguMoving(string paramSection = "EmguMoving", Performance performance = null, Form formHandler = null, IImageConsumer imageConsumer = null, bool isAynchrone = true)
            : base(paramSection, performance, formHandler, imageConsumer, isAynchrone)
        {
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
            MoveDetectInit = false;

            PreviousFrame?.Dispose();
            PreviousFrame = null;

            base.Start();

            ProcessState = System.Threading.ThreadState.Running;
        }

        public bool MoveDetectInit = false;
        //public DISOpticalFlow DISOpticalFlow { get; private set; }

        #region Image
        public override void ImageChanged(IImageProvider sender)
        {
            ImageProvider = sender;
            var image = Image;//Init in same thread
            base.ImageChanged(sender);
        }
        [Browsable(false)]

        public override Bitmap Image
        {
            get
            {
                if (ImageProvider == null)
                    return null;

                if (!HasImageChanged || IsInvokingImageChanged)
                    return base.Image;

                Mat frame = (ImageProvider as EDVideoCapture).Frame;//TODO as EDVideoCapture
                if (frame == null)
                    return null;

                if (!MoveDetectInit)
                {
                    PreviousFrame = frame; //TODO

                    InitMoveDetector(frame);

                    HasImageChanged = false;
                    return ImageProvider.Image;
                }

                Performance.Resume($"Process MoveDetectorAction Algorithm #{Algorithm}", true);

                Performance.Step(Environment.StackTrace.ReplaceLineEndings("\n\t\t\t"));

                Bitmap image = MoveDetectorAction(ImageProvider, frame);

                Performance.Pause("done Process MoveDetectorAction");

                HasImageChanged = false;
                return base.Image = image;

            }
            set { base.Image = value; }
        }

        #endregion


        /**
         * 
         */
        private void InitMoveDetector(Mat currentFrame)
        {
            //if (DISOpticalFlow == null)
            //    DISOpticalFlow = new DISOpticalFlow(DISOpticalFlow.Preset.Fast);
            InitializeCapture();

            MoveDetectInit = true;
        }


        #region Frame

        private Mat PreviousFrame;
        public bool HasFrameChanged { get; set; }

        private Mat _Frame = null;
        public Mat Frame
        {
            get
            {
                if (_Frame == null || HasFrameChanged)
                    if (ImageProvider != null && ImageProvider is IMatFrameProvider)
                    {
                        _Frame = (ImageProvider as IMatFrameProvider).Frame;

                        HasFrameChanged = false;
                    }
                return _Frame;
            }
            set
            {
                bool changed = _Frame == value;
                _Frame = value;
                Image = null;
                if (changed)
                {
                    FrameChanged(this);
                }
            }
        }
        public void FrameChanged(IMatFrameProvider sender)
        {
            ImageProvider = (IImageProvider)sender;

            HasFrameChanged = true;
            HasImageChanged = true;

            InvokeFrameChanged(sender);

            //Do the job in same thread
            Image = MoveDetectorAction((IImageProvider)sender, Frame);
            HasImageChanged = false;

            InvokeImageChanged((IImageProvider)sender);
        }

        public void InvokeFrameChanged(IMatFrameProvider sender = null)
        {
            InvokePropertyChanged(sender, OnFrameChanged);
        }
        public IMatFrameProvider.FrameChangedDelegate OnFrameChanged;
        #endregion

        public int Algorithm { get; set; }

        private MotionDetectionWithDenseOpticalFlow motionDetectionWithDenseOpticalFlow;
        private MotionDetectionWithMotionHistory motionDetectionWithMotionHistory;
        private MotionDetectionWithSparseOpticalFlow motionDetectionWithSparseOpticalFlow;
        private MotionDetectionWithFrameDifferencing motionDetectionWithFrameDifferencing;
        private MotionDetectionWithBackgroundSubtraction motionDetectionWithBackgroundSubtraction;
        private void InitializeCapture()
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

            //VideoCapture _capture = (ImageProvider as WebCam).Capture;
            //_capture.ImageGrabbed += ProcessFrame;

        }
        //private void ProcessFrame(object sender, EventArgs e)
        //{
        //    VideoCapture _capture = (ImageProvider as WebCam).Capture;
        //    Mat currentFrame = new();
        //    if (_capture.Retrieve(currentFrame))
        //        Image = MoveDetectorAction(ImageProvider, currentFrame);
        //}

        public Bitmap MoveDetectorAction(IImageProvider sender, Mat currentFrame)
        {
            if (this.Disposing || this.IsDisposed || ImageProvider == null)
                return null;

            ImageProvider = sender;

            if (Algorithm <= 0)
            {
                // Dense Optical Flow
                VideoCapture _capture = (ImageProvider as EDVideoCapture).Capture;
                if (currentFrame != null)
                {
                    Mat frameDiff = null;
                    if (PreviousFrame != null
                        && !currentFrame.Size.IsEmpty && !PreviousFrame.Size.IsEmpty &&
                        currentFrame.Size == PreviousFrame.Size)
                    {
                        if (motionDetectionWithDenseOpticalFlow == null)
                            motionDetectionWithDenseOpticalFlow = new();

                        //Mat flow = motionDetectionWithDenseOpticalFlow.CalculateDenseOpticalFlow( PreviousFrame, currentFrame);

                        //frameDiff = MotionDetection.OpticalFlowVisualizationWithHSV(flow);
                        //////SafeSetImageBoxImage(motionImageBox, motionDetectionWithDenseOpticalFlow.OpticalFlowVisualizationWithHSV(flow));
                        //flow.Dispose();

                        ////SafeSetImageBoxImage(capturedImageBox, currentFrame.Clone());

                        ////frameDiff = currentFrame.Clone();
                        ////CvInvoke.AbsDiff(PreviousFrame, currentFrame, frameDiff);

                        ////frameDiff = currentFrame;
                        ///

                        //Mat flow = motionDetectionWithDenseOpticalFlow.CalculateDenseOpticalFlow(PreviousFrame, currentFrame);

                        //frameDiff = motionDetectionWithDenseOpticalFlow.OpticalFlowVisualizationWithHSV(flow);
                        //SafeSetImageBoxImage(motionImageBox, frameDiff);
                        //flow.Dispose();

                        //SafeSetImageBoxImage(motionImageBox, currentFrame.Clone());

                        frameDiff = currentFrame.Clone();
                        CvInvoke.AbsDiff(PreviousFrame, currentFrame, frameDiff);
                        //SafeSetImageBoxImage(motionImageBox, frameDiff);
                    }

                    var oldPrev = PreviousFrame;
                    PreviousFrame = currentFrame.Clone();
                    oldPrev?.Dispose();

                    if (frameDiff != null)
                        return frameDiff.ToBitmap();
                }
            }
            //else if (Algorithm == 1)
            //{
            //    // Motion History
            //    Mat image = new Mat();

            //    if (_capture.Retrieve(image))
            //    {
            //        motionDetectionWithMotionHistory.GetFrameMotionComponents(image);

            //        // If the check box for motion info is checked, then calculate the motion image
            //        if (checkBoxCalculateMotionInfo.Checked)
            //        {
            //            Mat motionImage = motionDetectionWithMotionHistory.GetMotionImage();
            //            SafeSetImageBoxImage(motionImageBox, motionImage);
            //        }
            //        else
            //        {
            //            SafeSetImageBoxImage(motionImageBox, null);
            //        }

            //        motionDetectionWithMotionHistory.MotionDetectionDrawGraphics(image);

            //        if (this.Disposing || this.IsDisposed)
            //        {
            //            image.Dispose();
            //            return;
            //        }

            //        SafeSetImageBoxImage(capturedImageBox, image);
            //        SafeSetImageBoxImage(forgroundImageBox, motionDetectionWithMotionHistory.MotionForgroundMask.Clone());

            //        // Display the amount of motions found on the current image
            //        var components = motionDetectionWithMotionHistory.MotionComponents;
            //        UpdateText($"Total Motions found: {components.Length}");
            //        logger.Information($"Total Motions found: {components.Length}");

            //        int idx = 0;
            //        foreach (MotionDetectionWithMotionHistory.MotionComponent comp in components)
            //        {
            //            UpdateText($"Motion Component {idx}: {comp}");
            //            logger.Information($"Motion Component {idx}: {comp}");
            //            idx++;
            //        }
            //    }
            //    else
            //    {
            //        image.Dispose();
            //    }
            //}
            //else if (Algorithm == 2)
            //{
            //    // Sparse Optical Flow (Lucas-Kanade)
            //    Mat image = new Mat();

            //    if (_capture.Retrieve(image))
            //    {
            //        motionDetectionWithSparseOpticalFlow.ProcessFrame(image);

            //        if (this.Disposing || this.IsDisposed)
            //        {
            //            image.Dispose();
            //            return;
            //        }

            //        // 1. Main visualizer: Frame with overlay
            //        Mat displayImage = image.Clone();
            //        motionDetectionWithSparseOpticalFlow.DrawMotionVectors(displayImage);
            //        SafeSetImageBoxImage(capturedImageBox, displayImage);

            //        // 2. Motion visualizer: Isolated vectors on black background
            //        Mat motionMat = new Mat(image.Size, image.Depth, image.NumberOfChannels);
            //        motionMat.SetTo(new MCvScalar(0, 0, 0));
            //        motionDetectionWithSparseOpticalFlow.DrawMotionVectors(motionMat);
            //        SafeSetImageBoxImage(motionImageBox, motionMat);

            //        // 3. Foreground visualizer: Temporal frame differencing
            //        if (!PreviousFrame.Size.IsEmpty && PreviousFrame.Size == image.Size)
            //        {
            //            Mat frameDiff = image.Clone();
            //            CvInvoke.AbsDiff(PreviousFrame, image, frameDiff);
            //            SafeSetImageBoxImage(forgroundImageBox, frameDiff);
            //        }
            //        else
            //        {
            //            SafeSetImageBoxImage(forgroundImageBox, null);
            //        }

            //        // Update previous frame for differencing
            //        var oldPrev = PreviousFrame;
            //        PreviousFrame = image.Clone();
            //        oldPrev?.Dispose();

            //        image.Dispose();

            //        var vectors = motionDetectionWithSparseOpticalFlow.MotionVectors;
            //        UpdateText($"Active Tracked Points: {vectors.Count}");
            //        logger.Information($"Active Tracked Points: {vectors.Count}");
            //    }
            //    else
            //    {
            //        image.Dispose();
            //    }
            //}
            //else if (Algorithm == 3)
            //{
            //    // Temporal Frame Differencing (AbsDiff)
            //    Mat image = new Mat();

            //    if (_capture.Retrieve(image))
            //    {
            //        motionDetectionWithFrameDifferencing.ProcessFrame(image);

            //        if (this.Disposing || this.IsDisposed)
            //        {
            //            image.Dispose();
            //            return;
            //        }

            //        // 1. Captured visualizer: Raw frame with bounding boxes overlaid
            //        Mat displayImage = image.Clone();
            //        motionDetectionWithFrameDifferencing.DrawMotionGraphics(displayImage);
            //        SafeSetImageBoxImage(capturedImageBox, displayImage);

            //        // 2. Motion visualizer: Grayscale raw diff
            //        SafeSetImageBoxImage(motionImageBox, motionDetectionWithFrameDifferencing.MotionRawDiff.Clone());

            //        // 3. Foreground visualizer: Binary thresholded mask
            //        SafeSetImageBoxImage(forgroundImageBox, motionDetectionWithFrameDifferencing.MotionForgroundMask.Clone());

            //        image.Dispose();

            //        var components = motionDetectionWithFrameDifferencing.MotionComponents;
            //        UpdateText($"Total Motions found: {components.Length}");
            //        logger.Information($"Total Motions found: {components.Length}");

            //        int idx = 0;
            //        foreach (Rectangle comp in components)
            //        {
            //            UpdateText($"Motion Box {idx}: {comp}");
            //            logger.Information($"Motion Box {idx}: {comp}");
            //            idx++;
            //        }
            //    }
            //    else
            //    {
            //        image.Dispose();
            //    }
            //}
            //else
            //{
            //    // Background Subtraction (MOG2 / KNN / CNT / GMG)
            //    int index = Algorithm;
            //    MotionDetectionWithBackgroundSubtraction.SubtractorType subtractorType =
            //        index == 4 ? MotionDetectionWithBackgroundSubtraction.SubtractorType.MOG2 :
            //        index == 5 ? MotionDetectionWithBackgroundSubtraction.SubtractorType.KNN :
            //        index == 6 ? MotionDetectionWithBackgroundSubtraction.SubtractorType.CNT :
            //                     MotionDetectionWithBackgroundSubtraction.SubtractorType.GMG;

            //    motionDetectionWithBackgroundSubtraction.ActiveSubtractor = subtractorType;

            //    Mat image = new Mat();

            //    if (_capture.Retrieve(image))
            //    {
            //        motionDetectionWithBackgroundSubtraction.ProcessFrame(image);

            //        if (this.Disposing || this.IsDisposed)
            //        {
            //            image.Dispose();
            //            return;
            //        }

            //        // 1. Captured visualizer: Raw frame with bounding boxes overlaid
            //        Mat displayImage = image.Clone();
            //        motionDetectionWithBackgroundSubtraction.DrawMotionGraphics(displayImage);
            //        SafeSetImageBoxImage(capturedImageBox, displayImage);

            //        // 2. Motion visualizer: Raw camera frame
            //        SafeSetImageBoxImage(motionImageBox, image.Clone());

            //        // 3. Foreground visualizer: Binary mask
            //        SafeSetImageBoxImage(forgroundImageBox, motionDetectionWithBackgroundSubtraction.MotionForgroundMask.Clone());

            //        image.Dispose();

            //        var components = motionDetectionWithBackgroundSubtraction.MotionComponents;
            //        UpdateText($"Total Motions found: {components.Length}");
            //        logger.Information($"Total Motions found: {components.Length}");

            //        int idx = 0;
            //        foreach (Rectangle comp in components)
            //        {
            //            UpdateText($"Motion Box {idx}: {comp}");
            //            logger.Information($"Motion Box {idx}: {comp}");
            //            idx++;
            //        }
            //    }
            //    else
            //    {
            //        image.Dispose();
            //    }
            // }

            return null;
        }
        private void SafeSetImageBoxImage(Emgu.CV.UI.ImageBox imageBox, Mat newImage)
        {
            var oldImage = imageBox.Image;
            imageBox.Image = newImage;
            if (oldImage != null && oldImage != newImage)
            {
                oldImage.Dispose();
            }
        }

    }
}
