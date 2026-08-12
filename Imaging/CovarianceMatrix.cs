using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace MED.Imaging
{
    // Source - https://stackoverflow.com/a/37811640
    // Posted by Terje Kolderup
    // Retrieved 2026-08-12, License - CC BY-SA 3.0

    public class CovarianceMatrix
    {
        private int _n;
        private Vector2 _oldMean, _newMean,
                        _oldVarianceSum, _newVarianceSum,
                        _oldCovarianceSum, _newCovarianceSum;

        public CovarianceMatrix(PointF[] points)
        {
            foreach (var pt in points)
            {
                Push(pt.ToVector2());
            }
        }
        public CovarianceMatrix(RectangleF[] rects)
        {
            //Vector2 mean = Vector2.Zero;
            //System.Drawing.Drawing2D.Matrix covariance = new();
            //for (int i = 0; i < rects.Length; ++i)
            //{
            //    var rect = rects[i];
            //    Vector2 ptV = new(rect.X + rect.Width / 2, rect.Y + rect.Height / 2);
            //    Vector2 diff = ptV - mean;
            //    mean += diff / (i + 1);
            //    covariance += diff * (new Vector2(diff.Y, diff.X)) * i / (i + 1);
            //}

            //covariance = covariance / (rects.Length - 1);

            //foreach (var rect in rects)
            //{
            //    Push(new(rect.X + rect.Width / 2, rect.Y + rect.Height / 2));
            //    //Push(new(rect.X, rect.Y ));
            //    //Push(new(rect.X, rect.Bottom));
            //    //Push(new(rect.Right, rect.Bottom));
            //    //Push(new(rect.Right, rect.Y));
            //}
            _n = rects.Length;
            float sum = 0F;
            Vector2 meanNO = new();
            Vector2 meanSE = new();
            foreach (var rect in rects)
            {
                float surface = rect.Width * rect.Height;
                Vector2 ptV = new(rect.X + rect.Width / 2, rect.Y + rect.Height / 2);
                _newMean += ptV * surface;
                
                ptV = new(rect.X , rect.Y );
                meanNO += ptV * surface;

                ptV = new(rect.Right, rect.Bottom);
                meanSE += ptV * surface;
                
                sum += surface;

            }
            if (sum != 0)
            {
                _newMean /= sum;
                meanNO /= sum;
                meanSE /= sum;
                _newMean = (meanNO + meanSE) / 2F;
            }
            if (_n == 1)
            {
                _newVarianceSum = new(rects[0].Width - 1, rects[0].Height - 1);
                _n = 2;
            }
            else{

                Vector2 cornerNO = new(float.MaxValue, float.MaxValue);
                Vector2 cornerNE = new(0, float.MaxValue);
                Vector2 cornerSE = new(0, 0);
                Vector2 cornerSO = new(float.MaxValue, 0);

                sum = 0F;
                foreach (var rect in rects)
                {
                    if (rect.Left <= cornerNO.X && rect.Top <= cornerNO.Y)
                    {
                        cornerNO.X = rect.Left;
                        cornerNO.Y = rect.Top;
                    }
                    if (rect.Right >= cornerNE.X && rect.Top <= cornerNO.Y)
                    {
                        cornerNE.X = rect.Right;
                        cornerNE.Y = rect.Top;
                    }
                    if (rect.Right >= cornerSE.X && rect.Bottom >= cornerSE.Y)
                    {
                        cornerSE.X = rect.Right;
                        cornerSE.Y = rect.Bottom;
                    }
                    if (rect.Left <= cornerSO.X && rect.Bottom >= cornerSO.Y)
                    {
                        cornerSO.X = rect.Left;
                        cornerSO.Y = rect.Bottom;
                    }

                    float surface = rect.Width * rect.Height;
                    Vector2 ptV = new Vector2(rect.X + rect.Width / 2 - _newMean.X, rect.Y + rect.Height / 2 - _newMean.Y);
                    _newVarianceSum += new Vector2(ptV.X * ptV.X, ptV.Y * ptV.Y) * surface;

                    sum += surface;
                }
                _newVarianceSum /= sum;
                _newVarianceSum.X = (float)Math.Sqrt(_newVarianceSum.X);
                _newVarianceSum.Y = (float)Math.Sqrt((double)_newVarianceSum.Y);
            }
        }
        public void Push(Vector2 x)
        {
            _n++;
            if (_n == 1)
            {
                _oldMean = _newMean = x;
                _oldVarianceSum = new Vector2(0, 0);
                _oldCovarianceSum = new Vector2(0, 0);
            }
            else
            {
                //_newM = _oldM + (x - _oldM) / _n;
                _newMean = new Vector2(
                    _oldMean.X + (x.X - _oldMean.X) / _n,
                    _oldMean.Y + (x.Y - _oldMean.Y) / _n);

                //_newS = _oldS + (x - _oldM) * (x - _newM);
                _newVarianceSum = new Vector2(
                    _oldVarianceSum.X + (x.X - _oldMean.X) * (x.X - _newMean.X),
                    _oldVarianceSum.Y + (x.Y - _oldMean.Y) * (x.Y - _newMean.Y));

                /* .X is X vs Y
                 * .Y is Y vs Z
                 * .Z is Z vs X
                 */
                _newCovarianceSum = new Vector2(
                    _oldCovarianceSum.X + (x.X - _oldMean.X) * (x.Y - _newMean.Y),
                    0);

                // set up for next iteration
                _oldMean = _newMean;
                _oldVarianceSum = _newVarianceSum;
            }
        }
        public int NumDataValues()
        {
            return _n;
        }

        public Vector2 Mean()
        {
            return (_n > 0) ? _newMean : new Vector2(0, 0);
        }

        public Vector2 Variance()
        {
            return _n <= 1 ? new Vector2(0, 0) : _newVarianceSum / (float)(_n - 1);
        }
    }

}
