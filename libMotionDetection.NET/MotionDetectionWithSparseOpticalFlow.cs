using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Emgu.CV.Features2D;
using System;
using System.Drawing;
using System.Collections.Generic;

namespace libMotionDetection
{
    public class MotionDetectionWithSparseOpticalFlow : IDisposable
    {
        private readonly object _lock = new object();
        private Mat _prevGray = new Mat();
        private VectorOfPointF _prevPoints = new VectorOfPointF();

        public struct SparseMotionVector
        {
            public PointF Start { get; set; }
            public PointF End { get; set; }
            public float Magnitude { get; set; }
            public float Angle { get; set; } // in degrees
        }

        private List<SparseMotionVector> _motionVectors = new List<SparseMotionVector>();

        public IReadOnlyList<SparseMotionVector> MotionVectors
        {
            get
            {
                lock (_lock)
                {
                    return _motionVectors.ToArray();
                }
            }
        }

        public void ProcessFrame(Mat frame)
        {
            lock (_lock)
            {
                _motionVectors.Clear();

                using (Mat gray = new Mat())
                {
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
                        // First frame, populate features
                        FindFeatures(gray);
                        gray.CopyTo(_prevGray);
                        return;
                    }

                    if (_prevPoints.Size > 0)
                    {
                        using (VectorOfPointF nextPoints = new VectorOfPointF())
                        using (VectorOfByte status = new VectorOfByte())
                        using (VectorOfFloat err = new VectorOfFloat())
                        {
                            CvInvoke.CalcOpticalFlowPyrLK(
                                _prevGray,
                                gray,
                                _prevPoints,
                                nextPoints,
                                status,
                                err,
                                new Size(21, 21),
                                3,
                                new MCvTermCriteria(30, 0.01));

                            PointF[] prevPtsArray = _prevPoints.ToArray();
                            PointF[] nextPtsArray = nextPoints.ToArray();
                            byte[] statusArray = status.ToArray();

                            List<PointF> validNextPoints = new List<PointF>();

                            for (int i = 0; i < statusArray.Length; i++)
                            {
                                if (statusArray[i] == 1)
                                {
                                    PointF p1 = prevPtsArray[i];
                                    PointF p2 = nextPtsArray[i];

                                    float dx = p2.X - p1.X;
                                    float dy = p2.Y - p1.Y;
                                    float magnitude = (float)Math.Sqrt(dx * dx + dy * dy);

                                    // Filter out sub-pixel noise
                                    if (magnitude > 1.0f)
                                    {
                                        float angle = (float)(Math.Atan2(dy, dx) * 180.0 / Math.PI);
                                        if (angle < 0) angle += 360f;

                                        _motionVectors.Add(new SparseMotionVector
                                        {
                                            Start = p1,
                                            End = p2,
                                            Magnitude = magnitude,
                                            Angle = angle
                                        });
                                    }

                                    validNextPoints.Add(p2);
                                }
                            }

                            // Keep successfully tracked points for next frame
                            _prevPoints.Clear();
                            if (validNextPoints.Count > 0)
                            {
                                _prevPoints.Push(validNextPoints.ToArray());
                            }
                        }
                    }

                    // If tracked points are getting low, detect new features
                    if (_prevPoints.Size < 20)
                    {
                        FindFeatures(gray);
                    }

                    gray.CopyTo(_prevGray);
                }
            }
        }

        private void FindFeatures(Mat gray)
        {
            using (var detector = new GFTTDetector(100, 0.03, 10, 3, false, 0.04))
            {
                MKeyPoint[] keypoints = detector.Detect(gray, null);
                if (keypoints != null && keypoints.Length > 0)
                {
                    // Merge new corners with existing tracked points to keep active tracking
                    PointF[] existing = _prevPoints.ToArray();
                    List<PointF> merged = new List<PointF>(existing);

                    foreach (var kp in keypoints)
                    {
                        PointF p = kp.Point;
                        // Avoid adding new features too close to existing ones
                        bool tooClose = false;
                        foreach (var ext in existing)
                        {
                            float dx = ext.X - p.X;
                            float dy = ext.Y - p.Y;
                            if (dx * dx + dy * dy < 100f) // 10px threshold
                            {
                                tooClose = true;
                                break;
                            }
                        }

                        if (!tooClose)
                        {
                            merged.Add(p);
                        }
                    }

                    _prevPoints.Clear();
                    _prevPoints.Push(merged.ToArray());
                }
            }
        }

        public void DrawMotionVectors(Mat frame)
        {
            lock (_lock)
            {
                foreach (var vec in _motionVectors)
                {
                    Point p1 = Point.Round(vec.Start);
                    Point p2 = Point.Round(vec.End);

                    // Draw velocity line (Green)
                    CvInvoke.Line(frame, p1, p2, new MCvScalar(0, 255, 0), 2);

                    // Draw tracker node (Red)
                    CvInvoke.Circle(frame, p2, 3, new MCvScalar(0, 0, 255), -1);
                }
            }
        }

        public void Reset()
        {
            lock (_lock)
            {
                _prevGray?.Dispose();
                _prevGray = new Mat();
                _prevPoints.Clear();
                _motionVectors.Clear();
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                _prevGray?.Dispose();
                _prevGray = null;
                _prevPoints?.Dispose();
                _prevPoints = null;
            }
        }
    }
}
