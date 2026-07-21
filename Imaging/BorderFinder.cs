using DirectShowLib.DMO;
using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

/**
 * 
 * https://stackoverflow.com/questions/9752410/creating-a-graphicspath-from-a-semi-transparent-bitmap
 * 
 */

namespace MED.Imaging
{
    public class BorderFinder
    {
        public BorderFinder()
        {
            BackgroundColor = Color.Transparent;
        }
        public BorderFinder(Color backgroundColor):base()
        {
            if( ! backgroundColor.IsEmpty)
                BackgroundColor = backgroundColor;
        }
        int stride = 0;
        int[] visited = null;
        PointFData borderdata = null;
        Size size = Size.Empty;
        int ColorBytes = 4;
        bool outside = false;
        PointF zeropoint = new PointF(-1, -1);

        Color _BackgroundColor;
        Color BackgroundColor
        {
            get => _BackgroundColor;
            set
            {
                _BackgroundColor = value;
                BackgroundColor_R = value.R;
                BackgroundColor_G = value.G;
                BackgroundColor_B = value.B;
            }
        }
        int BackgroundColor_R;
        int BackgroundColor_G;
        int BackgroundColor_B;

        public GraphicsPath GetPath(Emgu.CV.Mat frame)
        {

            return GetPath(Find(frame));
        }
        public GraphicsPath GetPath(Bitmap bmp)
        {
            return GetPath(Find(bmp));
        }
        public GraphicsPath GetPath(List<PointF[]> _outlinePoints)
        {
            GraphicsPath outlinePath = new GraphicsPath();
            foreach (var points in _outlinePoints)
            {
                outlinePath.AddLines(points);
                outlinePath.CloseFigure();
            }
            return outlinePath;
        }

        public List<PointF[]> Find(Emgu.CV.Mat frame, bool outside = true)
        {
            size = frame.Size;
            //byte[,,] bytes = new byte[size.Height, size.Width, 3];

            byte[] bytes = new byte[size.Width * size.Height * 3];
            ColorBytes = 3;
            stride = size.Width* ColorBytes;
            Marshal.Copy(frame.GetDataPointer(), bytes, 0, bytes.Length); 
            //byte[,,] bytes = (byte[,,])frame.GetData();
            return Find(bytes, size, outside);
        }

        public List<PointF[]> Find(Bitmap bmp, bool outside = true)
        {
            BitmapData bmpdata = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            stride = bmpdata.Stride;

            byte[] bytes = new byte[bmp.Width * bmp.Height * ColorBytes ];

            size = bmp.Size;


            Marshal.Copy(bmpdata.Scan0, bytes, 0, bytes.Length);

            return Find(bytes, size, outside, bmp, bmpdata);
        }

        public List<PointF[]> Find(Array bytes, Size size, bool outside = true, Bitmap bmp = null, BitmapData bmpdata = null)
        {
            this.outside = outside;
            List<PointF> border = new List<PointF>();


            // Get all Borderpoint
            if(bytes is Byte[,,])
                borderdata = getBorderData((Byte[,,])bytes);
            else
                borderdata = getBorderData((Byte[])bytes);

            if (bmp != null)
                bmp.UnlockBits(bmpdata);

            List<List<PointF>> regions = new List<List<PointF>>();

            //Loop until no more borderpoints are available
            while (borderdata.PointFCount > 0)
            {
                List<PointF> region = new List<PointF>();

                //if valid is false the region doesn't close
                bool valid = true;

                //Find the first borderpoint from where whe start crawling
                PointF startpos = getFirstPointF(borderdata);

                //we need this to know if and how often we already visted the point.
                //we somtime have to visit a point a second time because we have to go backward until a unvisted point is found again
                //for example if we go int a narrow 1px hole
                visited = new int[size.Width * size.Height];

                region.Add(startpos);

                //Find the next possible point
                PointF current = getNextPointF(startpos);

                if (current != zeropoint)
                {
                    visited[(int)(current.Y * size.Width + current.X)]++;
                    region.Add(current);
                }

                //May occure with just one transparent pixel without neighbors
                if (current == zeropoint)
                    valid = false;

                //Loop until the area closed or colsing the area wasn't poosible
                while (!current.Equals(startpos) && valid)
                {
                    var pos = current;
                    //Check if the area was aready visited
                    if (visited[(int)(current.Y * size.Width + current.X)] < 2)
                    {
                        current = getNextPointF(pos);
                        visited[(int)(pos.Y * size.Width + pos.X)]++;
                        //If no possible point was found, search in reversed direction
                        if (current == zeropoint)
                            current = getNextPointFBackwards(pos);
                    }
                    else
                    { //If point was already visited, search in reversed direction
                        current = getNextPointFBackwards(pos);
                    }

                    //No possible point was found. Closing isn't possible
                    if (current == zeropoint)
                    {
                        valid = false;
                        break;
                    }

                    visited[(int)(current.Y * size.Width + current.X)]++;

                    region.Add(current);
                }
                //Remove point from source borderdata
                foreach (var p in region)
                {
                    borderdata.SetPointF((int)(p.Y * size.Width + p.X), false);
                }
                //Add region if closing was possible
                if (valid)
                    regions.Add(region);
            }

            //Checks if Region goes the same way back and trims it in this case
            foreach (var region in regions)
            {
                int duplicatedpos = -1;

                bool[] duplicatecheck = new bool[size.Width * size.Height];
                int length = region.Count;
                for (int i = 0; i < length; i++)
                {
                    var p = region[i];
                    if (duplicatecheck[(int)(p.Y * size.Width + p.X)])
                    {
                        duplicatedpos = i - 1;
                        break;
                    }
                    duplicatecheck[(int)(p.Y * size.Width + p.X)] = true;
                }

                if (duplicatedpos == -1)
                    continue;

                if (duplicatedpos != ((region.Count - 1) / 2))
                    continue;

                bool reversed = true;

                for (int i = 0; i < duplicatedpos; i++)
                {
                    if (region[duplicatedpos - i - 1] != region[duplicatedpos + i + 1])
                    {
                        reversed = false;
                        break;
                    }
                }

                if (!reversed)
                    continue;

                region.RemoveRange(duplicatedpos + 1, region.Count - duplicatedpos - 1);
            }

            List<List<PointF>> tempregions = new List<List<PointF>>(regions);
            regions.Clear();

            bool connected = true;
            //Connects region if possible
            while (connected)
            {
                connected = false;
                foreach (var region in tempregions)
                {
                    int connectionpos = -1;
                    int connectionregion = -1;
                    PointF pointstart = region.First();
                    PointF pointend = region.Last();
                    for (int ir = 0; ir < regions.Count; ir++)
                    {
                        var otherregion = regions[ir];
                        if (region == otherregion)
                            continue;

                        for (int ip = 0; ip < otherregion.Count; ip++)
                        {
                            var p = otherregion[ip];
                            if ((isConnected(pointstart, p) && isConnected(pointend, p)) ||
                                (isConnected(pointstart, p) && isConnected(pointstart, p)))
                            {
                                connectionregion = ir;
                                connectionpos = ip;
                            }

                            if ((isConnected(pointend, p) && isConnected(pointend, p)))
                            {
                                region.Reverse();
                                connectionregion = ir;
                                connectionpos = ip;
                            }
                        }

                    }

                    if (connectionpos == -1)
                    {
                        regions.Add(region);
                    }
                    else
                    {
                        regions[connectionregion].InsertRange(connectionpos, region);
                    }

                }

                tempregions = new List<List<PointF>>(regions);
                regions.Clear();
            }

            List<PointF[]> returnregions = new List<PointF[]>();

            foreach (var region in tempregions)
                returnregions.Add(region.ToArray());

            return returnregions;
        }

        private bool isConnected(PointF p0, PointF p1)
        {

            if (p0.X == p1.X && p0.Y - 1 == p1.Y)
                return true;

            if (p0.X + 1 == p1.X && p0.Y - 1 == p1.Y)
                return true;

            if (p0.X + 1 == p1.X && p0.Y == p1.Y)
                return true;

            if (p0.X + 1 == p1.X && p0.Y + 1 == p1.Y)
                return true;

            if (p0.X == p1.X && p0.Y + 1 == p1.Y)
                return true;

            if (p0.X - 1 == p1.X && p0.Y + 1 == p1.Y)
                return true;

            if (p0.X - 1 == p1.X && p0.Y == p1.Y)
                return true;

            if (p0.X - 1 == p1.X && p0.Y - 1 == p1.Y)
                return true;

            return false;
        }

        private PointF getNextPointF(PointF pos)
        {
            if (pos.Y > 0)
            {
                float x = pos.X;
                float y = pos.Y - 1;
                if ((ValidPointF(x, y)) && HasNeighbor(x, y))
                {
                    if (visited[(int)(y * size.Width + x)] == 0)
                    {
                        return new PointF(x, y);
                    }
                }
            }

            if (pos.Y > 0 && pos.X < size.Width - 1)
            {
                float x = pos.X + 1;
                float y = pos.Y - 1;
                if ((ValidPointF(x, y)) && HasNeighbor(x, y))
                {
                    if (visited[(int)(y * size.Width + x)] == 0)
                    {
                        return new PointF(x, y);
                    }
                }
            }

            if (pos.X < size.Width - 1)
            {
                float x = pos.X + 1;
                float y = pos.Y;
                if ((ValidPointF(x, y)) && HasNeighbor(x, y))
                {
                    if (visited[(int)(y * size.Width + x)] == 0)
                    {
                        return new PointF(x, y);
                    }
                }
            }

            if (pos.X < size.Width - 1 && pos.Y < size.Height - 1)
            {
                float x = pos.X + 1;
                float y = pos.Y + 1;
                if ((ValidPointF(x, y)) && HasNeighbor(x, y))
                {
                    if (visited[(int)(y * size.Width + x)] == 0)
                    {
                        return new PointF(x, y);
                    }
                }
            }

            if (pos.Y < size.Height - 1)
            {
                float x = pos.X;
                float y = pos.Y + 1;
                if ((ValidPointF(x, y)) && HasNeighbor(x, y))
                {
                    if (visited[(int)(y * size.Width + x)] == 0)
                    {
                        return new PointF(x, y);
                    }
                }
            }


            if (pos.Y < size.Height - 1 && pos.X > 0)
            {
                float x = pos.X - 1;
                float y = pos.Y + 1;
                if ((ValidPointF(x, y)) && HasNeighbor(x, y))
                {
                    if (visited[(int)(y * size.Width + x)] == 0)
                    {
                        return new PointF(x, y);
                    }
                }
            }

            if (pos.X > 0)
            {
                float x = pos.X - 1;
                float y = pos.Y;
                if ((ValidPointF(x, y)) && HasNeighbor(x, y))
                {
                    if (visited[(int)(y * size.Width + x)] == 0)
                    {
                        return new PointF(x, y);
                    }
                }
            }

            if (pos.X > 0 && pos.Y > 0)
            {
                float x = pos.X - 1;
                float y = pos.Y - 1;
                if ((ValidPointF(x, y)) && HasNeighbor(x, y))
                {
                    if (visited[(int)(y * size.Width + x)] == 0)
                    {
                        return new PointF(x, y);
                    }
                }
            }


            return zeropoint;
        }

        private PointF getNextPointFBackwards(PointF pos)
        {
            PointF backpoint = zeropoint;

            int trys = 0;

            if (pos.X > 0 && pos.Y > 0)
            {
                float x = pos.X - 1;
                float y = pos.Y - 1;
                if (ValidPointF(x, y) && HasNeighbor(x, y))
                {
                    if (visited[(int)(y * size.Width + x)] == 0)
                    {
                        return new PointF(x, y);
                    }
                    if (backpoint == zeropoint || trys > visited[(int)(y * size.Width + x)])
                    {
                        backpoint = new PointF(x, y);
                        trys = visited[(int)(y * size.Width + x)];
                    }
                }
            }

            if (pos.X > 0)
            {
                float x = pos.X - 1;
                float y = pos.Y;
                if ((ValidPointF(x, y)) && HasNeighbor(x, y))
                {
                    if (visited[(int)(y * size.Width + x)] == 0)
                    {
                        return new PointF(x, y);
                    }
                    if (backpoint == zeropoint || trys > visited[(int)(y * size.Width + x)])
                    {
                        backpoint = new PointF(x, y);
                        trys = visited[(int)(y * size.Width + x)];
                    }
                }
            }

            if (pos.Y < size.Height - 1 && pos.X > 0)
            {
                float x = pos.X - 1;
                float y = pos.Y + 1;
                if ((ValidPointF(x, y)) && HasNeighbor(x, y))
                {
                    if (visited[(int)(y * size.Width + x)] == 0)
                    {
                        return new PointF(x, y);
                    }
                    if (backpoint == zeropoint || trys > visited[(int)(y * size.Width + x)])
                    {
                        backpoint = new PointF(x, y);
                        trys = visited[(int)(y * size.Width + x)];
                    }
                }
            }

            if (pos.Y < size.Height - 1)
            {
                float x = pos.X;
                float y = pos.Y + 1;
                if ((ValidPointF(x, y)) && HasNeighbor(x, y))
                {
                    if (visited[(int)(y * size.Width + x)] == 0)
                    {
                        return new PointF(x, y);
                    }
                    if (backpoint == zeropoint || trys > visited[(int)(y * size.Width + x)])
                    {
                        backpoint = new PointF(x, y);
                        trys = visited[(int)(y * size.Width + x)];
                    }
                }
            }


            if (pos.X < size.Width - 1 && pos.Y < size.Height - 1)
            {
                float x = pos.X + 1;
                float y = pos.Y + 1;
                if ((ValidPointF(x, y)) && HasNeighbor(x, y))
                {
                    if (visited[(int)(y * size.Width + x)] == 0)
                    {
                        return new PointF(x, y);
                    }
                    if (backpoint == zeropoint || trys > visited[(int)(y * size.Width + x)])
                    {
                        backpoint = new PointF(x, y);
                        trys = visited[(int)(y * size.Width + x)];
                    }
                }
            }


            if (pos.X < size.Width - 1)
            {
                float x = pos.X + 1;
                float y = pos.Y;
                if ((ValidPointF(x, y)) && HasNeighbor(x, y))
                {
                    if (visited[(int)(y * size.Width + x)] == 0)
                    {
                        return new PointF(x, y);
                    }
                    if (backpoint == zeropoint || trys > visited[(int)(y * size.Width + x)])
                    {
                        backpoint = new PointF(x, y);
                        trys = visited[(int)(y * size.Width + x)];
                    }
                }
            }

            if (pos.Y > 0 && pos.X < size.Width - 1)
            {
                float x = pos.X + 1;
                float y = pos.Y - 1;
                if ((ValidPointF(x, y)) && HasNeighbor(x, y))
                {
                    if (visited[(int)(y * size.Width + x)] == 0)
                    {
                        return new PointF(x, y);
                    }
                    if (backpoint == zeropoint || trys > visited[(int)(y * size.Width + x)])
                    {
                        backpoint = new PointF(x, y);
                        trys = visited[(int)(y * size.Width + x)];
                    }
                }
            }


            if (pos.Y > 0)
            {
                float x = pos.X;
                float y = pos.Y - 1;
                if ((ValidPointF(x, y)) && HasNeighbor(x, y))
                {
                    if (visited[(int)(y * size.Width + x)] == 0)
                    {
                        return new PointF(x, y);
                    }
                    if (backpoint == zeropoint || trys > visited[(int)(y * size.Width + x)])
                    {
                        backpoint = new PointF(x, y);
                        trys = visited[(int)(y * size.Width + x)];
                    }
                }
            }

            return backpoint;
        }

        private bool ValidPointF(float x, float y)
        {
            return (borderdata[(int)(y * size.Width + x)]);
        }

        private bool HasNeighbor(float x, float y)
        {
            if (y > 0)
            {
                if (!borderdata[(int)((y - 1) * size.Width + x)])
                {
                    return true;
                }
            }
            else if (ValidPointF(x, y))
            {
                return true;
            }

            if (x < size.Width - 1)
            {
                if (!borderdata[(int)(y * size.Width + (x + 1))])
                {
                    return true;
                }
            }
            else if (ValidPointF(x, y))
            {
                return true;
            }

            if (y < size.Height - 1)
            {
                if (!borderdata[(int)((y + 1) * size.Width + x)])
                {
                    return true;
                }
            }
            else if (ValidPointF(x, y))
            {
                return true;
            }

            if (x > 0)
            {
                if (!borderdata[(int)(y * size.Width + (x - 1))])
                {
                    return true;
                }
            }
            else if (ValidPointF(x, y))
            {
                return true;
            }

            return false;
        }

        private PointF getFirstPointF(PointFData data)
        {
            PointF startpos = zeropoint;
            for (int y = 0; y < size.Height; y++)
            {
                for (int x = 0; x < size.Width; x++)
                {
                    if (data[y * size.Width + x])
                    {
                        startpos = new PointF(x, y);
                        return startpos;
                    }
                }
            }
            return startpos;
        }

        /**
         * Bitmap byte[] bytes
         * 
         * */
        private PointFData getBorderData(byte[] bytes)
        {

            PointFData isborderpoint = new PointFData(size.Height * size.Width);
            bool prevtrans = false;
            bool currenttrans = false;
            for (int y = 0; y < size.Height; y++)
            {
                prevtrans = false;
                for (int x = 0; x <= size.Width; x++)
                {
                    if (x == size.Width)
                    {
                        if (!prevtrans)
                        {
                            isborderpoint.SetPointF(y * size.Width + x - 1, true);
                        }
                        continue;
                    }
                    if (BackgroundColor != Color.Transparent || ColorBytes == 3)
                        currenttrans = bytes[y * stride + x * ColorBytes + 0] <= BackgroundColor_R
                            && bytes[y * stride + x * ColorBytes + 1] <= BackgroundColor_G
                            && bytes[y * stride + x * ColorBytes + 2] <= BackgroundColor_B;
                    else
                        currenttrans = bytes[y * stride + x * ColorBytes + 3] == 0;
                    if (x == 0 && !currenttrans)
                        isborderpoint.SetPointF(y * size.Width + x, true);
                    if (prevtrans && !currenttrans)
                        isborderpoint.SetPointF(y * size.Width + x - 1, true);
                    if (!prevtrans && currenttrans && x != 0)
                        isborderpoint.SetPointF(y * size.Width + x, true);
                    prevtrans = currenttrans;
                }
            }
            for (int x = 0; x < size.Width; x++)
            {
                prevtrans = false;
                for (int y = 0; y <= size.Height; y++)
                {
                    if (y == size.Height)
                    {
                        if (!prevtrans)
                        {
                            isborderpoint.SetPointF((y - 1) * size.Width + x, true);
                        }
                        continue;
                    }
                    if (BackgroundColor != Color.Transparent || ColorBytes==3)
                        currenttrans = bytes[y * stride + x * ColorBytes + 0] <= BackgroundColor_R
                            && bytes[y * stride + x * ColorBytes + 1] <= BackgroundColor_G
                            && bytes[y * stride + x * ColorBytes + 2] <= BackgroundColor_B;
                    else
                        currenttrans = bytes[y * stride + x * ColorBytes + 3] == 0;
                    if (y == 0 && !currenttrans)
                        isborderpoint.SetPointF(y * size.Width + x, true);
                    if (prevtrans && !currenttrans)
                        isborderpoint.SetPointF((y - 1) * size.Width + x, true);
                    if (!prevtrans && currenttrans && y != 0)
                        isborderpoint.SetPointF(y * size.Width + x, true);
                    prevtrans = currenttrans;
                }
            }
            return isborderpoint;
        }


        /**
         * Mat Frame byte[,,] bytes
         * 
         * */
        private PointFData getBorderData(byte[,,] bytes)
        {

            PointFData isborderpoint = new PointFData(size.Height * size.Width);
            bool prevtrans = false;
            bool currenttrans = false;
            for (int y = 0; y < size.Height; y++)
            {
                prevtrans = false;
                for (int x = 0; x <= size.Width; x++)
                {
                    if (x == size.Width)
                    {
                        if (!prevtrans)
                        {
                            isborderpoint.SetPointF(y * size.Width + x - 1, true);
                        }
                        continue;
                    }
                    currenttrans = bytes[y,x,0] <= BackgroundColor.R
                        && bytes[y, x, 1] <= BackgroundColor.G
                        && bytes[y, x, 2] <= BackgroundColor.B;
                
                    if (x == 0 && !currenttrans)
                        isborderpoint.SetPointF(y * size.Width + x, true);
                    if (prevtrans && !currenttrans)
                        isborderpoint.SetPointF(y * size.Width + x - 1, true);
                    if (!prevtrans && currenttrans && x != 0)
                        isborderpoint.SetPointF(y * size.Width + x, true);
                    prevtrans = currenttrans;
                }
            }
            for (int x = 0; x < size.Width; x++)
            {
                prevtrans = false;
                for (int y = 0; y <= size.Height; y++)
                {
                    if (y == size.Height)
                    {
                        if (!prevtrans)
                        {
                            isborderpoint.SetPointF((y - 1) * size.Width + x, true);
                        }
                        continue;
                    }
                    currenttrans = bytes[y, x, 0] <= BackgroundColor.R
                        && bytes[y, x, 1] <= BackgroundColor.G
                        && bytes[y, x, 2] <= BackgroundColor.B;
                    
                    if (y == 0 && !currenttrans)
                        isborderpoint.SetPointF(y * size.Width + x, true);
                    if (prevtrans && !currenttrans)
                        isborderpoint.SetPointF((y - 1) * size.Width + x, true);
                    if (!prevtrans && currenttrans && y != 0)
                        isborderpoint.SetPointF(y * size.Width + x, true);
                    prevtrans = currenttrans;
                }
            }
            return isborderpoint;
        }



        // Source - https://stackoverflow.com/a/9776234
        // Posted by Lukasz M, modified by community. See post 'Timeline' for change history
        // Retrieved 2026-07-20, License - CC BY-SA 3.0

        public List<PointF[]> GetOutlinePointsNEW(Bitmap image)
        {
            List<PointF> outlinePoints = new List<PointF>();

            BitmapData bitmapData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            PointF currentP = new PointF(0, 0);
            PointF firstP = new PointF(0, 0);

            byte[] bytes = new byte[image.Width * image.Height * 4];
            Marshal.Copy(bitmapData.Scan0, bytes, 0, bytes.Length);
            bool empty;
            var stride = bitmapData.Stride;
            //find non-transparent pixels visible from the top of the image
            for (int x = 0; x < bitmapData.Width && outlinePoints.Count == 0; x++)
            {
                for (int y = 0; y < bitmapData.Height && outlinePoints.Count == 0; y++)
                {
                    if (BackgroundColor != Color.Transparent || ColorBytes == 3)
                        empty = bytes[y * stride + x * ColorBytes + 0] <= BackgroundColor_R
                            && bytes[y * stride + x * ColorBytes + 1] <= BackgroundColor_G
                            && bytes[y * stride + x * ColorBytes + 2] <= BackgroundColor_B;
                    else
                        empty = bytes[y * stride + x * ColorBytes + 3] == 0;
                    //byte alpha = originalBytes[y * bitmapData.Stride + 4 * x + 3];

                    //if (alpha != 0)
                    if(!empty)
                    {
                        PointF p = new PointF(x, y);

                        outlinePoints.Add(p);
                        currentP = p;
                        firstP = p;

                        break;
                    }
                }
            }

            PointF[] neighbourPoints = new PointF[] { new PointF(-1, -1), new PointF(0, -1), new PointF(1, -1),
                                                new PointF(1, 0), new PointF(1, 1), new PointF(0, 1),
                                                new PointF(-1, 1), new PointF(-1, 0) };

            bool nextPixelFound = false;
            //crawl around the object and look for the next pixel of the outline
            do
            {
                nextPixelFound = false;
                bool transparentNeighbourFound = false;
                int i;
                //searching is done in clockwise order
                for (i = 0; (i < neighbourPoints.Length * 2) && !nextPixelFound; ++i)
                {
                    int neighbourPosition = i % neighbourPoints.Length;

                    float x = currentP.X + neighbourPoints[neighbourPosition].X;
                    float y = currentP.Y + neighbourPoints[neighbourPosition].Y;

                    if (x < 0 || y < 0 || x >= image.Width-1 || y >= image.Height-1)
                    {
                        transparentNeighbourFound = false;
                        continue;
                    }
                    int n = (int)(y * stride + x * ColorBytes);
                    if (BackgroundColor != Color.Transparent || ColorBytes == 3)
                        try {
                            empty = bytes[n + 0] <= BackgroundColor_R
                                && bytes[n + 1] <= BackgroundColor_G
                                && bytes[n + 2] <= BackgroundColor_B;
                        }
                        catch(Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                            return null;
                        }
                    else
                        empty = bytes[(int)(y * stride + x * ColorBytes + 3)] == 0;

                    byte alpha= (byte)(empty ? 0 : 255);
                    //alpha = bytes[(int)(y * bitmapData.Stride + 4 * x + 3)];

                    //a transparent pixel has to be found first
                    if (!transparentNeighbourFound)
                    {
                        if (alpha == 0)
                        {
                            transparentNeighbourFound = true;
                        }
                    }
                    else //after a transparent pixel is found, a next non-transparent one is the next pixel of the outline
                    {
                        if (alpha != 0)
                        {
                            PointF p = new PointF(x, y);

                            currentP = p;
                            if (outlinePoints.Contains(p))
                                continue;
                            outlinePoints.Add(p);
                            nextPixelFound = true;
                        }
                    }
                }
            } while (currentP != firstP && nextPixelFound);

            image.UnlockBits(bitmapData);

            List<PointF[]> l = new();
            l.Add(outlinePoints.ToArray());
            return l;
            //return outlinePoints.ToArray();
        }


    }

    class PointFData
    {
        bool[] points = null;
        int validpoints = 0;
        public PointFData(int length)
        {
            points = new bool[length];
        }

        public int PointFCount
        {
            get
            {
                return validpoints;
            }
        }

        public void SetPointF(int pos, bool state)
        {
            if (points[pos] != state)
            {
                if (state)
                    validpoints++;
                else
                    validpoints--;
            }
            points[pos] = state;
        }
        public bool this[int pos]
        {
            get
            {
                return points[pos];
            }
        }


    }
}
