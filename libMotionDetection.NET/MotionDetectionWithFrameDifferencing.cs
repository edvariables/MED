using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System;
using System.Drawing;
using System.Collections.Generic;

namespace libMotionDetection
{
    public class MotionDetectionWithFrameDifferencing : IDisposable
    {
        private readonly object _lock = new object();
        private Mat _prevGray = new Mat();
        private Mat _rawDiff = new Mat();
        private Mat _foregroundMask = new Mat();
        private Rectangle[] _motionComponents = Array.Empty<Rectangle>();
        private int _motionPixelCount = 0;

        public struct DifferenceSetting
        {
            public int Threshold { get; set; }           // Intensity change threshold (0-255)
            public double MinAreaPercent { get; set; }   // Minimum contour area threshold (% of frame)
        }

        private DifferenceSetting _setting = new DifferenceSetting
        {
            Threshold = 30,
            MinAreaPercent = 0.5
        };

        public DifferenceSetting Setting
        {
            get
            {
                lock (_lock)
                {
                    return _setting;
                }
            }
            set
            {
                lock (_lock)
                {
                    _setting = value;
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

        public Mat MotionRawDiff
        {
            get
            {
                lock (_lock)
                {
                    return _rawDiff;
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

        public int MotionPixelCount
        {
            get
            {
                lock (_lock)
                {
                    return _motionPixelCount;
                }
            }
        }

        public void ProcessFrame(Mat frame)
        {
            lock (_lock)
            {
                using (Mat gray = new Mat())
                {
                    // 1. Grayscale Conversion
                    if (frame.NumberOfChannels > 1)
                    {
                        CvInvoke.CvtColor(frame, gray, ColorConversion.Bgr2Gray);
                    }
                    else
                    {
                        frame.CopyTo(gray);
                    }

                    if (_prevGray.IsEmpty)
                    {
                        gray.CopyTo(_prevGray);
                        _foregroundMask.SetTo(new MCvScalar(0));
                        _rawDiff.SetTo(new MCvScalar(0));
                        _motionComponents = Array.Empty<Rectangle>();
                        _motionPixelCount = 0;
                        return;
                    }

                    // 2. Grayscale size safety
                    if (gray.Size != _prevGray.Size)
                    {
                        _prevGray.Dispose();
                        _prevGray = new Mat();
                        gray.CopyTo(_prevGray);
                        return;
                    }

                    // 3. Absolute Difference
                    CvInvoke.AbsDiff(_prevGray, gray, _rawDiff);

                    // 4. Binary Thresholding
                    CvInvoke.Threshold(_rawDiff, _foregroundMask, _setting.Threshold, 255, ThresholdType.Binary);

                    // 5. Morphological Dilate to bridge gaps
                    using (Mat element = CvInvoke.GetStructuringElement(MorphShapes.Rectangle, new Size(3, 3), new Point(-1, -1)))
                    {
                        CvInvoke.Dilate(_foregroundMask, _foregroundMask, element, new Point(-1, -1), 1, BorderType.Constant, default);
                    }

                    // 6. Count active moving pixels
                    _motionPixelCount = CvInvoke.CountNonZero(_foregroundMask);

                    // 7. Find Contours and filter by MinAreaPercent
                    List<Rectangle> detectedComponents = new List<Rectangle>();
                    double minArea = (gray.Size.Width * gray.Size.Height) * (_setting.MinAreaPercent / 100.0);

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

                    // 8. Update history frame
                    gray.CopyTo(_prevGray);
                }
            }
        }

        public void DrawMotionGraphics(Mat frame)
        {
            lock (_lock)
            {
                foreach (Rectangle rect in _motionComponents)
                {
                    // Draw red bounding box
                    CvInvoke.Rectangle(frame, rect, new MCvScalar(0, 0, 255), 2);
                }
            }
        }

        public void Reset()
        {
            lock (_lock)
            {
                _prevGray?.Dispose();
                _prevGray = new Mat();

                _rawDiff?.Dispose();
                _rawDiff = new Mat();

                _foregroundMask?.Dispose();
                _foregroundMask = new Mat();

                _motionComponents = Array.Empty<Rectangle>();
                _motionPixelCount = 0;
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                _prevGray?.Dispose();
                _prevGray = null;

                _rawDiff?.Dispose();
                _rawDiff = null;

                _foregroundMask?.Dispose();
                _foregroundMask = null;
            }
        }
    }
}
