using Emgu.CV;
using Emgu.CV.BgSegm;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System;
using System.Drawing;
using System.Collections.Generic;

namespace libMotionDetection
{
    public class MotionDetectionWithBackgroundSubtraction : IDisposable
    {
        private readonly object _lock = new object();
        private IBackgroundSubtractor _subtractor = null;
        private SubtractorType _subtractorType = SubtractorType.MOG2;
        private Mat _foregroundMask = new Mat();
        private Rectangle[] _motionComponents = Array.Empty<Rectangle>();

        public enum SubtractorType
        {
            MOG2,
            KNN,
            CNT,
            GMG
        }

        public MotionDetectionWithBackgroundSubtraction()
        {
            InitializeSubtractor(SubtractorType.MOG2);
        }

        public SubtractorType ActiveSubtractor
        {
            get
            {
                lock (_lock)
                {
                    return _subtractorType;
                }
            }
            set
            {
                lock (_lock)
                {
                    if (_subtractorType != value || _subtractor == null)
                    {
                        _subtractorType = value;
                        InitializeSubtractor(_subtractorType);
                    }
                }
            }
        }

        public Mat MotionForgroundMask
        {
            get
            {
                lock (_lock)
                {
                    return _foregroundMask;
                }
            }
        }

        public Rectangle[] MotionComponents
        {
            get
            {
                lock (_lock)
                {
                    if (_motionComponents == null) return Array.Empty<Rectangle>();
                    Rectangle[] copy = new Rectangle[_motionComponents.Length];
                    Array.Copy(_motionComponents, copy, _motionComponents.Length);
                    return copy;
                }
            }
        }

        private void InitializeSubtractor(SubtractorType type)
        {
            if (_subtractor != null)
            {
                (_subtractor as IDisposable)?.Dispose();
                _subtractor = null;
            }

            switch (type)
            {
                case SubtractorType.MOG2:
                    _subtractor = new BackgroundSubtractorMOG2(500, 16, true);
                    break;
                case SubtractorType.KNN:
                    _subtractor = new BackgroundSubtractorKNN(500, 400.0, true);
                    break;
                case SubtractorType.CNT:
                    _subtractor = new BackgroundSubtractorCNT(15, true, 15 * 60, true);
                    break;
                case SubtractorType.GMG:
                    _subtractor = new BackgroundSubtractorGMG(120, 0.8);
                    break;
            }

            _foregroundMask?.Dispose();
            _foregroundMask = new Mat();
            _motionComponents = Array.Empty<Rectangle>();
        }

        public void ProcessFrame(Mat frame)
        {
            lock (_lock)
            {
                if (_subtractor == null) return;

                // Apply background subtraction to get foreground mask
                _subtractor.Apply(frame, _foregroundMask);

                // Morphological opening/closing to filter noise
                using (Mat element = CvInvoke.GetStructuringElement(MorphShapes.Rectangle, new Size(3, 3), new Point(-1, -1)))
                {
                    CvInvoke.MorphologyEx(_foregroundMask, _foregroundMask, MorphOp.Open, element, new Point(-1, -1), 1, BorderType.Constant, default);
                    CvInvoke.MorphologyEx(_foregroundMask, _foregroundMask, MorphOp.Close, element, new Point(-1, -1), 1, BorderType.Constant, default);
                }

                // Extract contours to identify components
                List<Rectangle> detectedComponents = new List<Rectangle>();
                double minArea = (frame.Size.Width * frame.Size.Height) * 0.005; // 0.5% of frame area

                using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
                using (Mat hierarchy = new Mat())
                {
                    CvInvoke.FindContours(_foregroundMask, contours, hierarchy, RetrType.External, ChainApproxMethod.ChainApproxSimple);

                    for (int i = 0; i < contours.Size; i++)
                    {
                        double area = CvInvoke.ContourArea(contours[i]);
                        if (area >= minArea)
                        {
                            Rectangle rect = CvInvoke.BoundingRectangle(contours[i]);
                            detectedComponents.Add(rect);
                        }
                    }
                }

                _motionComponents = detectedComponents.ToArray();
            }
        }

        public void DrawMotionGraphics(Mat frame)
        {
            lock (_lock)
            {
                foreach (Rectangle rect in _motionComponents)
                {
                    CvInvoke.Rectangle(frame, rect, new MCvScalar(0, 0, 255), 2);
                }
            }
        }

        public void Reset()
        {
            lock (_lock)
            {
                InitializeSubtractor(_subtractorType);
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                if (_subtractor != null)
                {
                    (_subtractor as IDisposable)?.Dispose();
                    _subtractor = null;
                }

                _foregroundMask?.Dispose();
                _foregroundMask = null;
            }
        }
    }
}
