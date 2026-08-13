using DirectShowLib;
using DirectShowLib.DES;
using Emgu.CV;
using Emgu.CV.CvEnum;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.Marshalling;
using System.Runtime.Intrinsics;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using static Emgu.Util.Platform;
using static System.Windows.Forms.LinkLabel;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ToolTip;

namespace MED.Imaging
{
    public class ImagesCollider(IProcesses process)
    {

        public IProcesses Process { get; } = process;

        /**
         * Collider
         * 
         * */
        List<IImageCollidable>? _Colliders;
        public List<IImageCollidable>? Colliders
        {
            get
            {
                if (_Colliders != null)
                    return _Colliders;

                if (Process.Items == null)
                    return null;

                var colliders = new List<IImageCollidable>();

                foreach (var prov in Process.Items)
                {
                    if (prov is not IImageCollidable
                        || !prov.Enabled
                        )
                        continue;

                    var clipRegion = ((IImageCollidable)prov).ClipRegionTranslated;

                    //if (clipRegion == null)
                    //    continue;

                    if (((IImageCollidable)prov).Mass == 0F)
                        continue;

                    colliders.Add((IImageCollidable)prov);
                }
                if (colliders.Count == 0)
                    return null;

                return _Colliders = colliders;
            }
            set => _Colliders = value;
        }

        /**
         * ColliderBorders
         * 
         * */
        public List<IImageCollidable>? ManageBorders(Bitmap image, Graphics gr)
        {
            var colliders = Colliders;
            if (colliders == null || colliders.Count == 0) return colliders;

            //Process.Performance?.Sub(".Collider.Borders").Resume($"{colliders.Count} colliders", true);

            foreach (var item in colliders)
                CollideItemWithImageBorders(image, gr, item, PointF.Empty);
            //Process.Performance?.Sub(".Collider.Borders").Pause($"Collider done {colliders.Count}");

            return colliders;
        }

        public bool CollideItemWithImageBorders(Bitmap image, Graphics gr, IImageCollidable item, PointF offset)
        {
            if (item.Speed == 0F)
                return false;
            var location = item.Location;
            if (!offset.IsEmpty)
            {
                location.X += offset.X;
                location.Y += offset.Y;
            }
            else if (location.IsEmpty)
                return false;
            var direction = item.Direction;
            var region = item.ClipRegionTranslated;
            if (region == null)
                return false;
            if (!offset.IsEmpty)
                (region = region.Clone()).Translate(offset.X, offset.Y);
            //TODO Rotation

            bool changed = false;
            var bounds = region.GetBounds(gr);

            Vector2 overlap = Vector2.Zero;

            //Process.Performance?.Sub(".Collider.Borders").Step($"{item} {bounds}");
            if (bounds.Top < 0)
            {
                overlap.Y = -bounds.Top + 1;
                if (direction.Y < 0)
                    direction.Y *= -1;
                changed = true;
            }
            if (bounds.Left < 0)
            {
                overlap.X = -bounds.Left + 1;
                //location.X = 1;
                if (direction.X < 0)
                    direction.X *= -1;
                changed = true;
            }
            if (bounds.Bottom > image.Height)
            {
                overlap.Y = image.Height - bounds.Bottom;
                if (direction.Y > 0)
                    direction.Y *= -1;
                changed = true;
            }
            if (bounds.Right > image.Width)
            {
                overlap.X = image.Width - bounds.Right;
                //location.X = image.Width - bounds.Width;
                if (direction.X > 0)
                    direction.X *= -1;
                changed = true;
            }
            if (changed)
            {
                if (!Vector2.Zero.Equals(overlap))
                {
                    location.X += overlap.X;
                    location.Y += overlap.Y;
                }
                (item as IImageCollidable).Location = location;

                (item as IImageCollidable).Direction = direction;
            }

            return changed;
        }
        /**
         * Collide an item moving to an offset with all others
         * 
         * <param name="image">Not current drawing image. May be previous one.</param>
         * */
        public Dictionary<IImageCollidable, Region> Collide(Bitmap image, IImageCollidable item1, PointF offset)
        {
            Dictionary<IImageCollidable, Region> someChanges = new();

            var colliders = Colliders;// ManageBorders(image, gr);
            if (colliders == null || colliders.Count < 2) return someChanges;

            var region1 = item1.ClipRegionTranslated;
            if (region1 == null)
                return someChanges;
            if (!offset.IsEmpty)
                (region1 = region1.Clone()).Translate(offset.X, offset.Y);

            Graphics gr = Graphics.FromImage(image);

            if (CollideItemWithImageBorders(image, gr, item1, offset))
            {
                someChanges.Add(item1, region1);
                return someChanges;//Do not both image borders and collides
            }
            foreach (var item2 in colliders)
            {
                if (item2 == item1)
                    continue;

                var region2 = item2.ClipRegionTranslated;
                if (region2 == null)
                    continue;
                var bounds = region2.GetBounds(gr);
                if (bounds.IsEmpty)
                    continue;
                var intersect = region1.Clone();
                intersect.Intersect(region2);

                if (!intersect.IsEmpty(gr))
                {
                    var intersectBounds = intersect.GetBounds(gr);
                    if (intersectBounds.Width == 0 || intersectBounds.Height == 0)
                        continue;

                    PointF intersectBoundsCenter = new((intersectBounds.Right + intersectBounds.Left) / 2F, (intersectBounds.Bottom + intersectBounds.Top) / 2F);

                    if (CollideItemPair(gr, intersectBounds, intersectBoundsCenter, intersect, item1, offset, region1, item2, PointF.Empty))
                        if (someChanges.ContainsKey(item1)) someChanges[item1] = region1;
                        else someChanges.Add(item1, region1);

                    else if (CollideItemPair(gr, intersectBounds, intersectBoundsCenter, intersect, item2, PointF.Empty, region2, item1, offset))
                        if (someChanges.ContainsKey(item2)) someChanges[item2] = region2;
                        else someChanges.Add(item2, region2);

                }
            }
            //Process.Performance?.Sub(".Collider").Pause($"Collider done {colliders.Count}");

            return someChanges;
        }

        /**
         * * Collide
         * */
        //public Dictionary<IImageCollidable, Region> Collide(Bitmap image, Graphics gr)
        //{
        //    var colliders = Colliders;// ManageBorders(image, gr);
        //    if (colliders == null || colliders.Count < 2) return new();

        //    //Process.Performance?.Sub(".Collider").Resume($"{colliders.Count} colliders", true);

        //    Dictionary<IImageCollidable, Region> someChanges = new();
        //    for (var i1 = 0; i1 < colliders.Count; i1++)
        //    {
        //        var item1 = colliders.ElementAt(i1);

        //        var region1 = item1.ClipRegionTranslated;
        //        if (region1 == null)
        //            continue;

        //        if (CollideItemWithImageBorders(image, gr, item1, PointF.Empty))
        //            continue;//Do not both image borders and collides

        //        //var location = item1.Location;
        //        //region1.Translate(location.X, location.Y);

        //        for (var i2 = i1 + 1; i2 < colliders.Count; i2++)
        //        {
        //            var item2 = colliders.ElementAt(i2);
        //            var region2 = item2.ClipRegionTranslated;
        //            if (region2 == null)
        //                continue;
        //            //location = item2.Location;

        //            //region2.Translate(location.X, location.Y);
        //            var bounds = region2.GetBounds(gr);
        //            if (bounds.IsEmpty)
        //                continue;
        //            var intersect = region1.Clone();
        //            intersect.Intersect(region2);

        //            if (!intersect.IsEmpty(gr))
        //            {
        //                var intersectBounds = intersect.GetBounds(gr);
        //                if (intersectBounds.Width == 0 || intersectBounds.Height == 0)
        //                    continue; ;

        //                var intersectBoundsCenter = new PointF((intersectBounds.Right + intersectBounds.Left) / 2, (intersectBounds.Bottom + intersectBounds.Top) / 2);

        //                if (CollideItemPair(gr, intersectBounds, intersectBoundsCenter, intersect, item1, PointF.Empty, region1, item2, PointF.Empty))
        //                    if (someChanges.ContainsKey(item1)) someChanges[item1] = region1;
        //                    else someChanges.Add(item1, region1);

        //                if (CollideItemPair(gr, intersectBounds, intersectBoundsCenter, intersect, item2, PointF.Empty, region2, item1, PointF.Empty))
        //                    if (someChanges.ContainsKey(item2)) someChanges[item2] = region2;
        //                    else someChanges.Add(item2, region2);

        //            }
        //        }
        //    }
        //    //Process.Performance?.Sub(".Collider").Pause($"Collider done {colliders.Count}");

        //    return someChanges;
        //}

        private bool CollideItemPair(Graphics gr, RectangleF intersectBounds, PointF intersectBoundsCenter, Region intersectRegion
            , IImageCollidable item, PointF offset, Region region
            , IImageCollidable item2, PointF offset2)
        {
            if (item.Location.IsEmpty && item.Speed == 0)
                return false;
            if (item2.Speed == 0F)
                return CollideMoverAndWall(gr, intersectBounds, intersectBoundsCenter, intersectRegion, item, offset, region, item2, PointF.Empty);
            return CollideMovingItems(gr, intersectBounds, intersectBoundsCenter, intersectRegion, item, offset, region, item2, PointF.Empty);
        }

        /**
         * CollideMoverAndWall
         * A wall is an image process with Speed == 0
         * 
         * */
        private bool CollideMoverAndWall(Graphics gr, RectangleF intersectBounds, PointF intersectBoundsCenter, Region intersectRegion
            , IImageCollidable item, PointF offset, Region regionTranslated
            , IImageCollidable item2, PointF offset2)
        {
            var location = item.Location;
            if (location.IsEmpty)//TODO abuse
                return false;
            if (!offset.IsEmpty)
            {
                location.X += offset.X;
                location.Y += offset.Y;
            }
            var itemBounds = regionTranslated.GetBounds(gr);
            var itemBoundsCenter = new PointF(itemBounds.Left + itemBounds.Width / 2F, itemBounds.Top + itemBounds.Height / 2);
            PointF move = new(itemBoundsCenter.X - intersectBoundsCenter.X, itemBoundsCenter.Y - intersectBoundsCenter.Y);
            if (move.IsEmpty)
                return false;

            #region Closest Point
            PointF closest;
            var wallBorder_vector = GetRegionBorderVector(gr, intersectBounds, intersectBoundsCenter, intersectRegion, item, offset, regionTranslated, item2, offset2, out closest);
            if (Vector2.Zero.Equals(wallBorder_vector) || float.IsNaN(wallBorder_vector.X))
                return false;

            //Debug
            gr.FillRegion(Brushes.Red, intersectRegion);
            gr.FillEllipse(Brushes.Green, new RectangleF(closest.X - 2.5F, closest.Y - 2.5F, 5F, 5F));

            move = new(itemBoundsCenter.X - closest.X, itemBoundsCenter.Y - closest.Y);
            #endregion

            #region Detecting Ball and Wall Overlap

            float dx = itemBoundsCenter.X - closest.X;
            float dy = itemBoundsCenter.Y - closest.Y;
            float distance_squared = dx * dx + dy * dy;  // Using squared distance to avoid unnecessary square root calculations
            float radius_sum = itemBounds.Width / 2 /*+ w.radius*/;  // The combined radius of the ball and the wall's thickness
            var overlapping = distance_squared <= radius_sum * radius_sum;   // True if overlapping


            //Determine the Penetration Depth
            float distance = (float)Math.Sqrt(distance_squared) - 1;   // The actual distance between the ball's center and the closest point
            float overlap = radius_sum - distance;

            #endregion

            #region Resolving Ball and Wall Collision

            //Normal
            Vector2 wallNormal = new(-wallBorder_vector.Y, wallBorder_vector.X);

            Vector2 collision_normal = wallNormal;
            collision_normal = Vector2.Normalize(collision_normal);


            var overlapLocation = Vector2.Zero;
            //Push the Ball Out of the Wall
            if (overlap > 0)
            {
                item.Performance?.Step($"Penetration {overlap}");
                var outter = collision_normal * radius_sum;
                overlapLocation.X = closest.X + outter.X - location.X;
                overlapLocation.Y = closest.Y + outter.Y - location.Y;
                //overlap = overlapLocation.Length();
                //location.X = closest.X + outter.X;
                //location.Y = closest.Y + outter.Y;
                location.X += (overlapLocation.X = collision_normal.X * overlap);
                location.Y += (overlapLocation.Y = collision_normal.Y * overlap);
            }
            else
                overlap = 0F;
            #endregion

            #region Reflect the Velocity
            float velocity_dot_normal = Vector2.Dot(item.VelocityVector, collision_normal);
            Vector2 velocity_normal = collision_normal * velocity_dot_normal;
            Vector2 velocity_tangent = item.VelocityVector - velocity_normal;
            var itemVelocity = Vector2.Normalize(velocity_tangent - velocity_normal * 1F);

            #endregion
            if (float.IsNaN(itemVelocity.X) || float.IsInfinity(itemVelocity.X))
            {
                item.Performance?.Error($"location.X IsNaN == {float.IsNaN(itemVelocity.X)} or IsInfinity == {float.IsInfinity(itemVelocity.X)}");
                return false;
            }

            item.Direction = new PointF(itemVelocity.X, itemVelocity.Y);

            if (!offset.IsEmpty)
            {
                overlapLocation.X -= offset.X;
                overlapLocation.X -= offset.Y;
                float remainLength = (float)Math.Sqrt(offset.X * offset.X + offset.Y * offset.Y) - overlap;
                if (remainLength > 0)
                {
                    location.X += remainLength * item.Direction.X;
                    location.Y += remainLength * item.Direction.Y;
                }
                item.Location = location;

                PointF nextBorderPoint;
                itemBounds.X = location.X;
                itemBounds.Y = location.Y;
                itemBoundsCenter.X = itemBounds.X + itemBounds.Width / 2F;
                itemBoundsCenter.Y = itemBounds.Y + itemBounds.Height / 2F;
                var nextBorder = IntersectPath(item2, PointF.Empty, itemBounds, out nextBorderPoint);
                if (!nextBorder.Equals(Vector2.Zero))
                {
                    Console.WriteLine("Still intersect");
                    collision_normal = Vector2.Normalize(new Vector2(-nextBorder.Y, nextBorder.X));
                    location.X += radius_sum * collision_normal.X;
                    location.Y += radius_sum * collision_normal.Y;
                    item.Location = location;
                }
            }

            return true;
        }


        private PointF GetInterceptionPoint(Graphics gr, RectangleF intersectBounds, PointF intersectBoundsCenter, Region intersectRegion
            , IImageCollidable item, PointF offset, Region regionTranslated
            , IImageCollidable item2, PointF offset2)
        {
            PointF borderPoint = intersectBoundsCenter;

            var itemBounds = regionTranslated.GetBounds(gr);
            var itemBoundsCenter = new PointF((itemBounds.Right + itemBounds.Left) / 2, (itemBounds.Bottom + itemBounds.Top) / 2);

            //var item1partialRegion = item.ClipEdgesRegionTranslated?.Clone();
            //item1partialRegion.Intersect(item2.ClipRegionTranslated);
            //var scans = item1partialRegion.GetRegionScans(new());

            //var item2partialRegion = item2.ClipRegionTranslated?.Clone();
            var intersectPartialRegion = intersectRegion.Clone();
            if (item2.ClipEdgesRegionTranslated != null)
            {
                var item2partialRegion = item2.ClipRegionTranslated?.Clone();
                if (item2partialRegion != null)
                {
                    if (!offset2.IsEmpty)
                        item2partialRegion.Translate(offset2.X, offset2.Y);
                    intersectPartialRegion.Intersect(item2partialRegion);
                }
            }

            //int duration = 40;
            //PointF previousLocation = new(itemBoundsCenter.X - item.Velocity.X * duration, itemBoundsCenter.Y - item.Velocity.Y * duration);
            PointF itemCenterOffset = new(itemBoundsCenter.X - item.Direction.Y, itemBoundsCenter.Y + item.Direction.X);
            PointF intersectBoundsCenterOffset = new(intersectBoundsCenter.X + item.Direction.Y, itemBoundsCenter.Y - item.Direction.X);

            GraphicsPath directionPath = new();
            directionPath.StartFigure();
            directionPath.AddPolygon(itemCenterOffset, itemBoundsCenter, intersectBoundsCenterOffset, intersectBoundsCenter);
            directionPath.CloseFigure();
            intersectPartialRegion.Intersect(directionPath);
            var scans = intersectPartialRegion.GetRegionScans(new());
            var b = intersectPartialRegion.GetBounds(gr);

            return new PointF(b.X, b.Y/*(b.Right + b.Left) / 2, (b.Bottom + b.Top) / 2*/); ;
        }

        private PointF[]? RectangleIntersectLine(RectangleF intersectBounds, PointF point1, PointF point2)
        {
            Vector2 line = new(point1.X - point2.X, point1.Y - point2.Y);
            var lineRect = new RectangleF(Math.Min(point1.X, point2.X) - 1F, Math.Min(point1.Y, point2.Y) - 1F, Math.Abs(point1.X - point2.X) + 2F, Math.Abs(point1.Y - point2.Y) + 2F);
            if (!intersectBounds.IntersectsWith(lineRect))
                return null;

            lineRect.Intersect(intersectBounds);

            if (point1.X < lineRect.X)
                point1.X = lineRect.X;
            else if (point1.X > lineRect.Right)
                point1.X = lineRect.Right;
            if (point1.Y < lineRect.Y)
                point1.Y = lineRect.Y;
            else if (point1.Y > lineRect.Bottom)
                point1.Y = lineRect.Bottom;

            if (point2.X < lineRect.X)
                point2.X = lineRect.X;
            else if (point2.X > lineRect.Right)
                point2.X = lineRect.Right;
            if (point2.Y < lineRect.Y)
                point2.Y = lineRect.Y;
            else if (point2.Y > lineRect.Bottom)
                point2.Y = lineRect.Bottom;
            return [point1, point2];
        }

        private Vector2 IntersectPath(IImageMover item, PointF offset, RectangleF intersectBounds, out PointF borderPoint)
        {
            Vector2 vector = Vector2.Zero;
            borderPoint = PointF.Empty;

            if (intersectBounds.Width == 1F || intersectBounds.Height == 1F)
                return new(intersectBounds.Width, intersectBounds.Height);

            //var grPath = item.ClipPath;
            if (item.ClipPathsBounds == null)
                return new(intersectBounds.Width, intersectBounds.Height);

            if (!item.Location.IsEmpty)//TODO Rotation
                intersectBounds.Offset(-item.Location.X, -item.Location.Y);
            else if (!offset.IsEmpty)
                intersectBounds.Offset(offset);

            byte typeIsLast = (byte)0x80;
            List<PointF[]> lines = new();
            PointF undefinedPoint = new(-1F, -1F);
            int foundRegions = 0;
            foreach (var (grPath, pathBounds) in item.ClipPathsBounds)
            {

                if (!pathBounds.IntersectsWith(intersectBounds))
                    continue;

                //var intersectBoundsTest = intersectBounds;
                //intersectBoundsTest.Intersect(pathBounds);
                //if (intersectBoundsTest.Equals(intersectBounds))
                //    continue;

                //var grPath = item.ClipPath;

                int nPoint = 0;
                int nAddedLines = 0;
                PointF previousPoint = undefinedPoint;
                PointF firstOfFigure = undefinedPoint;

                foreach (var point in grPath.PathPoints)
                {
                    byte pointType = grPath.PathTypes[nPoint];
                    //if (nPoint == 189)
                    //    Console.WriteLine("CIIC DEBUG");
                    if (pointType == 0)
                        firstOfFigure = point;
                    else if (!previousPoint.Equals(undefinedPoint))
                    {
                        var points = RectangleIntersectLine(intersectBounds, previousPoint, point);
                        if(points!= null)
                        {
                            borderPoint.X += (points[0].X + points[1].X)/ 2F;
                            borderPoint.Y += (points[0].Y + points[1].Y) / 2F;

                            lines.Add(points);
                            nAddedLines++;
                        }
                    }
                    if (((int)pointType & typeIsLast) == typeIsLast)
                    {
                        if (!firstOfFigure.Equals(undefinedPoint))
                        {
                            var points = RectangleIntersectLine(intersectBounds, point, firstOfFigure);
                            if (points != null)
                            {
                                borderPoint.X += (points[0].X + points[1].X) / 2F;
                                borderPoint.Y += (points[0].Y + points[1].Y) / 2F;

                                lines.Add(points);
                                nAddedLines++;
                            }
                            firstOfFigure = undefinedPoint;
                        }
                        previousPoint = undefinedPoint;
                    }
                    else
                        previousPoint = point;
                    nPoint++;
                }
                if (nAddedLines > 0)
                    foundRegions++;
                //else //Totaly included

            }
            if (lines.Count == 0)
                Console.WriteLine("LALALALACIIC DEBUG");
            else
            {
                //if (foundRegions > 1)
                //    Console.WriteLine($"LALALALACIIC {foundRegions} DEBUG");
                int nLine = 0;
                foreach (PointF[] line in lines)
                {
                    vector.X += line[1].X - line[0].X;
                    vector.Y += line[1].Y - line[0].Y;
                    nLine++;
                }
                borderPoint.X /= nLine;
                borderPoint.Y /= nLine;
            }
            if (!item.Location.IsEmpty)
            {//TODO Rotation ?
                borderPoint.X += item.Location.X;
                borderPoint.Y += item.Location.Y;
            }

            return vector;
        }


        private Vector2 GetRegionBorderVector(Graphics gr, RectangleF intersectBounds, PointF intersectBoundsCenter, Region intersectRegion
            , IImageCollidable item, PointF offset
            , Region regionTranslated, IImageCollidable item2, PointF offset2
            , out PointF borderPoint)
        {
            borderPoint = PointF.Empty;
            Vector2 borderVector = Vector2.Zero;

            var itemBounds = regionTranslated.GetBounds(gr);
            var itemBoundsCenter = new PointF((itemBounds.Right + itemBounds.Left) / 2, (itemBounds.Bottom + itemBounds.Top) / 2);

            //var item2partialEdgesRegion = item2.ClipEdgesRegionTranslated?.Clone();
            //if (!offset2.IsEmpty)
            //    item2partialEdgesRegion.Translate(offset2.X, offset2.Y);
            //item2partialEdgesRegion.Intersect(intersectBounds);
            //var edgesBounds = item2partialEdgesRegion.GetBounds(gr);
            //var scans = item2partialEdgesRegion.GetRegionScans(new());
            //if (scans.Length > 0)
            //{
            //    var covariance = new CovarianceMatrix(scans);
            //    var covarVector = covariance.Variance();
            //    borderVector = covarVector;// new(-covarVector.Y, covarVector.X);
            //    borderPoint = new(covariance.Mean());

            //    //foreach (RectangleF scan in scans)
            //    //{
            //    //    borderVector += new Vector2((intersectBoundsCenter.X - scan.X) * scan.Width, (intersectBoundsCenter.Y - scan.Y) * scan.Height);
            //    //}
            //    //borderPoint = intersectBoundsCenter;
            //    //return borderVector;
            //}
            //else
            gr.DrawRectangle(Pens.Green, intersectBounds);

            if (item2.ClipPath != null)
            {
                borderVector = IntersectPath(item2, offset2, intersectBounds, out borderPoint);
                if (!borderVector.Equals(Vector2.Zero))
                {
                    if (borderPoint.IsEmpty)
                        borderPoint = intersectBoundsCenter;
                    return borderVector;
                }
            }

            // Inflate scan of the wall
            var item2partialRegion = item2.ClipRegionTranslated?.Clone();
            if (item2partialRegion == null)
            {
                return Vector2.Zero;
            }
            if (!offset2.IsEmpty)
                item2partialRegion.Translate(offset2.X, offset2.Y);

            var inflateBounds = intersectBounds;
            int nInflate = 2;
            //inflateBounds.Offset(-nInflate / 2, -nInflate / 2);
            inflateBounds.Inflate(nInflate, nInflate);

            //Part of item2 in intersectBounds
            item2partialRegion.Intersect(inflateBounds);

            RectangleF expBounds = item2partialRegion.GetBounds(gr);
            RectangleF deltaBounds = new(expBounds.Left - intersectBounds.Left, expBounds.Top - intersectBounds.Top, expBounds.Right - intersectBounds.Right, expBounds.Bottom - intersectBounds.Bottom);
            var boundsCenter = new PointF(expBounds.Left + expBounds.Width / 2, expBounds.Top + expBounds.Height / 2);
            if (Vector2.Zero.Equals(borderVector))
            {
                Vector2 moveVector = new(boundsCenter.X - intersectBoundsCenter.X, boundsCenter.Y - intersectBoundsCenter.Y);
                borderVector = new(-moveVector.Y * expBounds.Height, moveVector.X * expBounds.Width);
            }
            //Vector2 borderVector = new(intersectBounds.X- bounds.X, intersectBounds.Y- bounds.Y);//Approx
            if (borderPoint.IsEmpty)
                borderPoint = intersectBoundsCenter;
            //borderPoint = GetInterceptionPoint(gr, intersectBounds, intersectBoundsCenter, intersectRegion, item, offset, regionTranslated, item2, offset2);

            var overBottom = expBounds.Bottom >= gr.VisibleClipBounds.Bottom - nInflate;
            var overTop = expBounds.Top <= nInflate;
            var overLeft = expBounds.Left < nInflate;
            var overRight = expBounds.Right >= gr.VisibleClipBounds.Right - nInflate;
            var overNO = overLeft && overTop;
            var overNE = overRight && overTop;
            var overSE = overRight && overBottom;
            var overSO = overLeft && overBottom;
            var overAny = overLeft || overRight || overTop || overBottom;
            var borderAtTop = overTop || expBounds.Bottom == intersectBounds.Bottom;   // X = +1
            var borderAtBottom = overBottom || expBounds.Top == intersectBounds.Top;      // X = -1
            var borderAtRight = overRight || expBounds.Left == intersectBounds.Left;     // Y = +1
            var borderAtLeft = overLeft || expBounds.Right == intersectBounds.Right;    // Y = -1
            if (!borderAtBottom && !borderAtTop && !borderAtLeft && !borderAtRight)
            {
                borderAtTop = expBounds.Bottom - 1 == intersectBounds.Bottom;// X = +1
                borderAtBottom = expBounds.Top + 1 == intersectBounds.Top;      // X = -1
                borderAtRight = expBounds.Left + 1 == intersectBounds.Left;     // Y = +1
                borderAtLeft = expBounds.Right - 1 == intersectBounds.Right;    // Y = -1
            }

            //Closest point
            if (overTop)
            {
                if (overNO)
                    borderPoint = itemBoundsCenter; // new(intersectBounds.Right, intersectBounds.Bottom);
                else if (overNE)
                    borderPoint = itemBoundsCenter;// new(intersectBounds.Left, intersectBounds.Bottom);
                else
                    borderPoint.Y = itemBoundsCenter.Y;
            }
            else if (overBottom)
            {
                if (overSO)
                    borderPoint = itemBoundsCenter;// new(intersectBounds.Right, intersectBounds.Top);
                else if (overSE)
                    borderPoint = itemBoundsCenter;// new(intersectBounds.Left, intersectBounds.Top);
                else
                    borderPoint.Y = itemBoundsCenter.Y;
            }
            //else if (borderAtTop)
            //{
            //    if (borderAtBottom)
            //        borderPoint.Y = (intersectBounds.Top + intersectBounds.Bottom) / 2;
            //    else
            //        borderPoint.Y = intersectBounds.Bottom;
            //}
            //else if (borderAtBottom)
            //    borderPoint.Y = intersectBounds.Top;

            if (overLeft)
            {
                if (!(overTop || overBottom))
                    borderPoint.X = itemBoundsCenter.X;
            }
            else if (overRight)
            {
                if (!(overTop || overBottom))
                    borderPoint.X = itemBoundsCenter.X;
            }
            //else if (borderAtLeft)
            //{
            //    if (borderAtRight)
            //        borderPoint.X = (intersectBounds.Left + intersectBounds.Right) / 2;
            //    else
            //        borderPoint.X = intersectBounds.Right;
            //}
            //else if (borderAtRight)
            //    borderPoint.X = intersectBounds.Left;

            /**
             * Vector
             * */
            //borderAtBottom
            if (borderAtBottom)//(X = -1)
            {
                //Vector
                if (overBottom)
                {
                    if (overLeft)
                        borderVector.X = +1F;
                    else
                        borderVector.X = -1F;
                    if (borderVector.Y == 0F)
                        if (overLeft)
                            borderVector.Y = -1F;
                        else if (overRight)
                            borderVector.Y = +1F;
                }
                else if (borderAtTop)
                {
                    if (overTop)
                        borderVector.X = +1F;
                    else if (overBottom)
                        borderVector.X = -1F;
                    else if (borderVector.Y == 0F && !borderAtLeft && !borderAtRight)
                    {
                        borderVector.X = -item.Direction.Y;
                    }
                    else
                        borderVector.X = 0F;
                }
                else if (borderVector.X == 0F)
                    borderVector.X = -1F;
                else if (borderVector.X > 0F)
                    borderVector.X *= -1;
            }
            //borderAtTop
            else if (borderAtTop)//(X = +1)
            {
                //Vector
                if (overTop)
                {
                    borderVector.X = +1F;
                }
                else if (borderVector.X == 0F)
                    borderVector.X = +1F;
                else if (borderVector.X < 0F)
                    borderVector.X *= -1F;
            }

            //borderAtLeft
            if (borderAtLeft) // (Y = -1)
            {
                //Vector
                if (overLeft)
                {
                    borderVector.Y = -1F;

                    if (borderVector.X == 0F)
                        if (overBottom)
                            borderVector.X = -1F;
                        else if (overTop)
                            borderVector.X = 1F;
                }
                else if (borderAtRight)
                {
                    if (overRight)
                    {
                        if (overBottom)
                            borderVector.Y = -1F;
                        else
                            borderVector.Y = +1F;
                    }
                    else if (overLeft)
                        borderVector.Y = -1F;
                    else if (borderVector.X == 0F && !borderAtBottom && !borderAtTop)
                    {
                        borderVector.Y = item.Direction.X;
                    }
                    else
                        borderVector.Y = 0F;
                }
                else if (borderVector.Y == 0F)
                    borderVector.Y = -1F;
                else if (borderVector.Y > 0F)
                    borderVector.Y *= -1F;
            }
            //borderAtRight
            else if (borderAtRight) // (Y = +1)
            {
                //Vector
                if (overRight)
                    borderVector.Y = +1F;
                else if (borderVector.Y == 0F)
                    borderVector.Y = +1F;
                else if (borderVector.Y < 0F)
                    borderVector.Y *= -1F;
            }
            else if (Vector2.Zero.Equals(borderVector) && !borderAtBottom && !borderAtTop)
            {
                borderVector.X = -item.Direction.Y;
                borderVector.Y = item.Direction.X;
                borderVector = Vector2.Normalize(borderVector);
                if (float.IsInfinity(borderVector.X) || float.IsNaN(borderVector.X))
                {
                    borderVector = Vector2.Zero;
                }
            }
            //Analyse des arêtes d'angles
            if (!overAny && !(borderVector.X == 0F || borderVector.Y == 0F))
            {
                if (borderAtTop || borderAtBottom)
                    if (borderAtRight && itemBoundsCenter.X > expBounds.X)
                    {
                        //Arrive par la droite
                        borderVector.Y = 0F;
                    }
                    else if (borderAtLeft && itemBoundsCenter.X < expBounds.X)
                    {
                        //Arrive par la gauche
                        borderVector.Y = 0F;
                    }
                if (borderAtLeft || borderAtRight)
                    if (borderAtBottom && itemBoundsCenter.Y > expBounds.Y)
                    {
                        //Arrive par dessous 
                        borderVector.X = 0F;
                    }
                    else if (borderAtTop && itemBoundsCenter.Y < expBounds.Y)
                    {
                        //Arrive par dessus
                        borderVector.X = 0F;
                    }
            }


            if (Vector2.Zero.Equals(borderVector))
            {
                if (expBounds.Left <= nInflate)
                    borderVector.X = 1F;
                else if (overRight)
                    borderVector.X = -1F;
                else if (!borderAtLeft && !borderAtRight)
                    borderVector.X = 1F;

                if (overTop)
                    borderVector.Y = 1F;
                else if (overBottom)
                    borderVector.Y = -1F;

                else if (borderVector.X == 0F)
                    borderVector = new((gr.VisibleClipBounds.X + intersectBoundsCenter.X) / 2, (gr.VisibleClipBounds.Y + intersectBoundsCenter.Y) / 2);
            }


            borderVector = Vector2.Normalize(borderVector);
            return borderVector;
        }

        /**
         * Collide 2 items
         * */
        private bool CollideMovingItems(Graphics gr, RectangleF intersectBounds, PointF intersectBoundsCenter, Region intersectRegion
            , IImageCollidable item1, PointF offset, Region region
            , IImageCollidable item2, PointF offset2)
        {
            var location = item1.Location;
            if (location.IsEmpty)
                return false;

            if (!offset.IsEmpty)
            {
                location.X += offset.X;
                location.Y += offset.Y;
            }
            var location2 = item2.Location;
            if (!offset2.IsEmpty)
            {
                location2.X += offset2.X;
                location2.Y += offset2.Y;
            }

            //Debug
            gr.FillRegion(Brushes.Red, intersectRegion);
            gr.FillClosedCurve(Brushes.Green, new PointF(intersectBoundsCenter.X - 2, intersectBoundsCenter.Y - 2), new PointF(intersectBoundsCenter.X - 2, intersectBoundsCenter.Y + 2), new PointF(intersectBoundsCenter.X + 2, intersectBoundsCenter.Y + 2), new PointF(intersectBoundsCenter.X + 2, intersectBoundsCenter.Y - 2));

            var item1Bounds = region.GetBounds(gr);
            var item1BoundsCenter = new PointF((item1Bounds.Right + item1Bounds.Left) / 2, (item1Bounds.Bottom + item1Bounds.Top) / 2);
            var item2BoundsCenter = new PointF(location2.X + item1Bounds.Width / 2, location2.Y + item1Bounds.Height / 2);

            //Vector2 item1ToContact = new Vector2(item1BoundsCenter.X - intersectBoundsCenter.X, item1BoundsCenter.Y - intersectBoundsCenter.Y);

            float dx = (item1BoundsCenter.X - intersectBoundsCenter.X);
            float dy = (item1BoundsCenter.Y - intersectBoundsCenter.Y);
            float distance_squared = dx * dx + dy * dy; // Square of the distance between the centers
            float distance = (float)Math.Sqrt(distance_squared);    // Actual distance between the centers
            float radius_sum = (item1Bounds.Width + item1Bounds.Height) / 2F;
            float overlap = (radius_sum - distance) / 2F; // Amount of overlap between the balls

            //Collision Direction
            Vector2 normalised_collision = new(dx / distance, dy / distance);

            //Resolve Overlap
            location.X += overlap * normalised_collision.X;
            location.Y += overlap * normalised_collision.Y;
            if (!item2.Location.IsEmpty && item2.Speed != 0F)
            {
                location2.X -= overlap * normalised_collision.X;
                location2.Y -= overlap * normalised_collision.Y;

                location2.X -= overlap * item2.Direction.X;
                location2.Y -= overlap * item2.Direction.Y;

                item2.Location = location2;//TODO if( !offset2.IsEmpty) ?

                //item2.Speed = item1EnergyRatio * item2.Speed + item2EnergyRatio * item.Speed;
            }
            //item.Speed = item1EnergyRatio * item.Speed + item2EnergyRatio * item2.Speed;

            //Calculate Vector Normal
            Vector2 collision_normal = new(-normalised_collision.Y, normalised_collision.X);

            //Project Responses
            float ball_1_normal_dot_product = Vector2.Dot(item1.VelocityVector, collision_normal);
            float ball_2_normal_dot_product = Vector2.Dot(item2.VelocityVector, collision_normal);

            float ball_1_collision_dot_product = Vector2.Dot(item1.VelocityVector, normalised_collision);
            float ball_2_collision_dot_product = Vector2.Dot(item2.VelocityVector, normalised_collision);

            //Caclulate Resulting Velocities
            float ball_1_momentum = (ball_1_collision_dot_product * (item1.Mass - item2.Mass) + 2.0f * item2.Mass * ball_2_collision_dot_product) / (item1.Mass + item2.Mass);
            float ball_2_momentum = (ball_2_collision_dot_product * (item2.Mass - item1.Mass) + 2.0f * item1.Mass * ball_1_collision_dot_product) / (item1.Mass + item2.Mass);

            // Set the new velocities after collision
            //Apply Velocities
            var item1Velocity = (collision_normal * ball_1_normal_dot_product) + (normalised_collision * ball_1_momentum);
            var item1Direction = Vector2.Normalize(item1Velocity);
            item1.Direction = new PointF(item1Direction);

            var friction = Math.Max(0, item1.SurfaceFriction);
            if (!item2.Location.IsEmpty && item2.SpeedMax != 0F)
            {
                var item2Velocity = (collision_normal * ball_2_normal_dot_product) + (normalised_collision * ball_2_momentum);

                var item2Direction = Vector2.Normalize(item2Velocity);
                item2.Direction = new PointF(item2Direction);
                item1.Speed_msec = item1Velocity.Length();
                item2.Speed_msec = item2Velocity.Length();

                friction = Math.Max(friction, item2.SurfaceFriction);
                if (friction > 0)
                {
                    if (item2.RotationSpeedMax > 0F && item1.RotationSpeedMax > 0F && (item1.RotationSpeed != 0F || item2.RotationSpeed != 0F))
                    {
                        //var radiusSquared = (item1Bounds.Width * item1Bounds.Height) / 4;
                        var speedDelta = item2.RotationSpeed * item2.Mass - item1.RotationSpeed * item1.Mass;
                        //var item1RotMomentum = speedDelta * ball_1_momentum;// * radiusSquared;
                        //var item2RotMomentum = speedDelta * ball_2_momentum;// * radiusSquared;

                        //var item2RotationSpeed = item2.RotationSpeed;
                        item2.RotationSpeed -= speedDelta * ball_1_momentum * friction;
                        item1.RotationSpeed += speedDelta * ball_2_momentum * friction;

                        //var angle = Math.Atan2(item2Direction.Y, item2Direction.X);
                        //item2.RotationSpeed += (float)(angle - item2PreviousAngle);
                    }

                    //Tangential collision to rotation
                    if (item2.RotationSpeedMax > 0F)
                    {
                        item2.RotationSpeed += ball_1_normal_dot_product * friction;
                    }
                    if (item1.RotationSpeedMax > 0F)
                    {
                        item1.RotationSpeed += ball_2_normal_dot_product * friction;
                    }
                }
            }

            //Rotation
            //if (item1.RotationSpeedMax > 0F)
            //{
            //    item1.RotationAngle += item1.RotationAngle * ball_1_collision_dot_product * friction;

            //    //var angle = Math.Atan2(item1Direction.Y, item1Direction.X);
            //    //item1.RotationSpeed += (float)(angle - item1PreviousAngle);
            //}

            //collisionReaction = Vector2.Normalize(collisionReaction);

            //if (item.RotationSpeedMax != 0)
            //{
            //    //var oldAngle = Math.Atan2(item.Direction.Y, item.Direction.X);
            //    //var angle = Math.Atan2(vector.Y, vector.X);
            //    //item.RotationSpeed += (float)(angle - oldAngle);
            //    //if (item.RotationSpeed > item.RotationSpeedMax)
            //    //    item.RotationSpeed = item.RotationSpeedMax;
            //    //else if (item.RotationSpeed < -item.RotationSpeedMax)
            //    //    item.RotationSpeed = -item.RotationSpeedMax;
            //}

            //item.Direction = new(collisionReaction.X, collisionReaction.Y);

            if (!offset.IsEmpty)
            {
                int duration = 20;//TODO Part of rebound
                location.X += item1.Velocity.X * duration;
                location.Y += item1.Velocity.Y * duration;
                item1.Location = location;
            }

            return true;
        }

        private bool CollideMovingItemsOLD(Graphics gr, RectangleF intersectBounds, PointF intersectBoundsCenter, Region intersectRegion
            , IImageCollidable item, PointF offset, Region region
            , IImageCollidable item2, PointF offset2)
        {
            var location = item.Location;
            if (location.IsEmpty)
                return false;
            if (!offset.IsEmpty)
            {
                location.X += offset.X;
                location.Y += offset.Y;
            }
            var itemBounds = region.GetBounds(gr);
            var itemBoundsCenter = new PointF((itemBounds.Right + itemBounds.Left) / 2, (itemBounds.Bottom + itemBounds.Top) / 2);
            var overRatio = Math.Abs((intersectBounds.Width * intersectBounds.Height) / (itemBounds.Width * itemBounds.Height));
            var changed = false;
            PointF move = new(itemBoundsCenter.X - intersectBoundsCenter.X, itemBoundsCenter.Y - intersectBoundsCenter.Y);
            PointF moveRatio = new(Math.Abs(move.X / itemBounds.Width), Math.Abs(move.Y / itemBounds.Height));
            Vector2 oldVector = item.DirectionVector;
            Vector2 normal = Vector2.Normalize(new Vector2((float)(intersectBounds.X), (float)(intersectBounds.Y)));
            Vector2 dirNormal = Vector2.Normalize(item.DirectionVector);
            Vector2 vector = new Vector2((float)(move.X), (float)(move.Y));
            vector = Vector2.Normalize(vector);


            float speed = item.Speed;
            if (move.X > 0)
            {
                //speed.X = Math.Abs(speed.X /** move.X*/) * item.Density;
                //location.X += speedValue * vector.X;
                //location.Y += speedValue * vector.Y;
                //location.X += intersectBounds.Width / 2;
                changed = true;
            }
            else if (move.X < 0)
            {
                //speed.X = -1 * Math.Abs(speed.X/* * move.X*/) * item.Density;
                //location.X += speedValue * vector.X;
                //location.Y += speedValue * vector.Y;
                //location.X -= intersectBounds.Width / 2;
                changed = true;
            }
            if (move.Y > 0)
            {
                //speed.Y = Math.Abs(speed.Y/** move.Y*/) * item.Density;
                //location.X += speedValue * vector.X;
                //location.Y += speedValue * vector.Y;
                //location.Y += intersectBounds.Height / 2;
                changed = true;
            }
            else if (move.Y < 0)
            {
                //speed.Y = -1 * Math.Abs(speed.Y /** move.Y*/) * item.Density;
                //location.X += speedValue * vector.X;
                //location.Y += speedValue * vector.Y;
                //location.Y -= intersectBounds.Height / 2;
                changed = true;
            }
            if (changed)
            {
                //speed.X = Math.Min(speed.X, item.SpeedMax);
                //speed.Y = Math.Min(speed.Y, item.SpeedMax);

                if (item.RotationSpeedMax != 0)
                {
                    var oldAngle = Math.Atan2(item.Direction.Y, item.Direction.X);
                    var angle = Math.Atan2(vector.Y, vector.X);
                    item.RotationSpeed += (float)(angle - oldAngle);
                    if (item.RotationSpeed > item.RotationSpeedMax)
                        item.RotationSpeed = item.RotationSpeedMax;
                    else if (item.RotationSpeed < -item.RotationSpeedMax)
                        item.RotationSpeed = -item.RotationSpeedMax;
                }
                if (item2.Speed != 0 && item2.Mass != 0 && item.Speed != 0 && item.Mass != 0)
                {
                    var energyRatio = (item2.Speed * item2.Mass) / (item.Speed * item.Mass);
                    item.Speed_msec *= energyRatio;
                    //item2.Speed /= energyRatio;
                }

                //Physics.PositionalCorrection((IImageCollidable)item, (IImageCollidable)item2, intersectBounds, intersectBoundsCenter);
                normal += (normal - dirNormal);
                vector = Vector2.Normalize(-normal);

                PointF closest = intersectBoundsCenter;//Approx
                double dx = itemBoundsCenter.X - closest.X;
                double dy = itemBoundsCenter.Y - closest.Y;
                //double distance_squared = dx * dx + dy * dy;  // Using squared distance to avoid unnecessary square root calculations
                //double radius_sum = itemBounds.Width + w.radius;  // The combined radius of the ball and the wall's thickness
                Vector2 collision_normal = new(-move.X, -move.Y);
                collision_normal = Vector2.Normalize(collision_normal);
                float distance = (float)Math.Sqrt(dx * dx + dy * dy);   // The actual distance between the ball's center and the closest point
                var radius_sum = itemBounds.Width;
                float penetration = radius_sum - distance;

                Vector2 ball_1_velocity = item.VelocityVector;

                Vector2 collision = item.VelocityVector + item2.VelocityVector;
                var collisionReaction = new Vector2(-collision.Y, collision.X);
                collision_normal = Vector2.Normalize(collisionReaction);

                //Push the Ball Out of the Wall
                if (penetration > 0)
                {
                    item.Performance?.Step($"Penetration {penetration}");
                    location.X += collision_normal.X * penetration;
                    location.Y += collision_normal.Y * penetration;
                }

                float velocity_dot_normal = (float)Vector2.Dot(ball_1_velocity, collision_normal);

                Vector2 velocity_normal = collision_normal * velocity_dot_normal;
                Vector2 velocity_tangent = ball_1_velocity - velocity_normal;

                // Reverse and dampen the normal component of the velocity
                // Damping factor is arbitrarily chosen as 0.6
                ball_1_velocity = velocity_tangent - velocity_normal * 0.6F;

                ball_1_velocity = collision_normal;

                if (Vector2.Zero.Equals(ball_1_velocity))
                {
                    return false;
                }
                else
                    ball_1_velocity = Vector2.Normalize(ball_1_velocity);
                var direction = item.Direction = new PointF(ball_1_velocity.X, ball_1_velocity.Y);

                if (!offset.IsEmpty)
                {
                    int duration = 20;//TODO Part of rebound
                    location.X += item.Velocity.X * duration;
                    location.Y += item.Velocity.Y * duration;
                    item.Location = location;
                }

            }
            return changed;
        }

        private Vector2 GetRegionBorderVectorQuarters(Graphics gr, RectangleF intersectBounds, PointF intersectBoundsCenter, Region intersectRegion
            , IImageCollidable item, PointF offset
            , Region regionTranslated, IImageCollidable item2, PointF offset2
            , out PointF borderPoint)
        {

            Vector2 borderVector = Vector2.Zero;

            if (intersectBounds.Width > 3F && intersectBounds.Height > 3F && (intersectBounds.Width + intersectBounds.Height > 7F))
            {
                bool isSmall = intersectBounds.Width < 6F || intersectBounds.Height < 6F;


                //Analyse per square
                Dictionary<string, RectangleF> quarterIntersectBounds = new();
                Dictionary<string, PointF> quarterIntersectCenters = new();
                foreach (var quarter in new string[] { "NO", "NE", "SE", "SO" })
                {
                    RectangleF quarterBounds = RectangleF.Empty;
                    switch (quarter)
                    {
                        case "NO":
                            if (intersectBounds.Width < 2
                                && (intersectBounds.Height > 5 ||
                                    intersectBounds.Width == 1F))
                                break;
                            quarterBounds = new(intersectBounds.X, intersectBounds.Y, intersectBounds.Width / 2F, intersectBounds.Height / 2F);
                            break;
                        case "NE":
                            if (intersectBounds.Width < 2
                                && (intersectBounds.Height > 5 ||
                                    intersectBounds.Width == 1F))
                                break;
                            quarterBounds = new(intersectBounds.X + intersectBounds.Width / 2F, intersectBounds.Y, intersectBounds.Width / 2F, intersectBounds.Height / 2F);
                            break;
                        case "SE":
                            if (intersectBounds.Height < 2
                                && (intersectBounds.Width > 5 ||
                                    intersectBounds.Height == 1F))
                                break;
                            quarterBounds = new(intersectBounds.X + intersectBounds.Width / 2F, intersectBounds.Y + intersectBounds.Height / 2F, intersectBounds.Width / 2F, intersectBounds.Height / 2F);
                            break;
                        case "SO":
                            if (intersectBounds.Height < 2
                                && (intersectBounds.Width > 5 ||
                                    intersectBounds.Height == 1F))
                                break;
                            quarterBounds = new(intersectBounds.X, intersectBounds.Y + intersectBounds.Height / 2F, intersectBounds.Width / 2F, intersectBounds.Height / 2F);
                            break;
                    }
                    if (quarterBounds.IsEmpty)
                        continue;
                    var quarterIntersectRegion = intersectRegion.Clone();
                    quarterIntersectRegion.Intersect(quarterBounds);
                    var quarterRealBounds = quarterIntersectRegion.GetBounds(gr);
                    if (isSmall || (quarterRealBounds.Width > 1F && quarterRealBounds.Height > 1F && (quarterRealBounds.Width + quarterRealBounds.Height > 3F)))
                    {
                        quarterIntersectBounds.Add(quarter, quarterRealBounds);
                        quarterIntersectCenters.Add(quarter, new(quarterRealBounds.X + quarterRealBounds.Width / 2F, quarterRealBounds.Y + quarterRealBounds.Height / 2F));
                    }
                }
                PointF quarterIntersectCenter = PointF.Empty;
                PointF quartersLocation = intersectBoundsCenter;
                if (quarterIntersectBounds.Count == 0)
                {
                    throw new NotImplementedException();
                }
                else if (quarterIntersectBounds.Count == 1)
                {
                    throw new NotImplementedException();
                }
                else if (quarterIntersectBounds.Count == 2)
                {
                    PointF previousPt = PointF.Empty;
                    PointF pt = PointF.Empty;
                    if (quarterIntersectBounds.ContainsKey("SE") && quarterIntersectBounds.ContainsKey("NO"))
                    {
                        //TODO diagonal
                        previousPt = quarterIntersectCenters["SE"];
                        pt = quarterIntersectCenters["NO"];
                    }
                    else if (quarterIntersectBounds.ContainsKey("SO") && quarterIntersectBounds.ContainsKey("NE"))
                    {
                        //TODO diagonal
                        previousPt = quarterIntersectCenters["SO"];
                        pt = quarterIntersectCenters["NE"];
                    }
                    else if (quarterIntersectBounds.ContainsKey("N0") && quarterIntersectBounds.ContainsKey("NE"))
                    {
                        previousPt = quarterIntersectCenters["NO"];
                        pt = quarterIntersectCenters["NE"];
                    }
                    else if (quarterIntersectBounds.ContainsKey("NE") && quarterIntersectBounds.ContainsKey("SE"))
                    {
                        previousPt = quarterIntersectCenters["NE"];
                        pt = quarterIntersectCenters["SE"];
                    }
                    else if (quarterIntersectBounds.ContainsKey("SE") && quarterIntersectBounds.ContainsKey("SO"))
                    {
                        previousPt = quarterIntersectCenters["SE"];
                        pt = quarterIntersectCenters["SO"];
                    }
                    else if (quarterIntersectBounds.ContainsKey("SO") && quarterIntersectBounds.ContainsKey("NO"))
                    {
                        previousPt = quarterIntersectCenters["SO"];
                        pt = quarterIntersectCenters["NO"];
                    }
                    if (!pt.IsEmpty)
                    {
                        quarterIntersectCenter.X = (previousPt.X + pt.X) / 2F;
                        quarterIntersectCenter.Y = (previousPt.Y + pt.Y) / 2F;
                        borderVector = new(pt.X - previousPt.X, pt.Y - previousPt.Y);
                    }
                }
                else if (quarterIntersectBounds.Count == 3)
                {
                    PointF previousPt = PointF.Empty;
                    PointF pt = PointF.Empty;
                    if (!quarterIntersectBounds.ContainsKey("NO")) // -X, Y
                    {
                        previousPt = quarterIntersectCenters["NE"];
                        pt = quarterIntersectCenters["SO"];
                    }
                    else if (!quarterIntersectBounds.ContainsKey("NE")) // -X, -Y
                    {
                        previousPt = quarterIntersectCenters["SE"];
                        pt = quarterIntersectCenters["NO"];
                    }
                    else if (!quarterIntersectBounds.ContainsKey("SE")) // X, -Y
                    {
                        previousPt = quarterIntersectCenters["SO"];
                        pt = quarterIntersectCenters["NE"];
                    }
                    else //if (!quarterIntersectBounds.ContainsKey("SO")) // X, Y
                    {
                        previousPt = quarterIntersectCenters["NO"];
                        pt = quarterIntersectCenters["SE"];
                    }

                    if (!pt.IsEmpty)
                    {
                        quarterIntersectCenter.X = (previousPt.X + pt.X) / 2F;
                        quarterIntersectCenter.Y = (previousPt.Y + pt.Y) / 2F;
                        borderVector = new(pt.X - previousPt.X, pt.Y - previousPt.Y);
                    }
                }
                else
                {
                    //PointF previousPt = PointF.Empty;
                    borderVector = Vector2.Zero;
                    //foreach (var (pos, bounds) in quarterIntersectBounds)
                    //{
                    //    Vector2 dirVector;
                    //    switch (pos)
                    //    {
                    //        case "NE":
                    //            dirVector = new Vector2(-bounds.Width, bounds.Height/*-1, 1*/); break;
                    //        case "SE":
                    //            dirVector = new Vector2(-bounds.Width, -bounds.Height /*- 1, -1*/); break;
                    //        case "SO":
                    //            dirVector = new Vector2(bounds.Width, -bounds.Height/*1, -1*/); break;
                    //        default:
                    //            dirVector = new Vector2(bounds.Width, bounds.Height/*1, 1*/); break;
                    //    }
                    //    borderVector += dirVector;
                    //}
                    //Vector2 dirVector;
                    //foreach (var (pos, bounds) in quarterIntersectBounds)
                    //{
                    //    //Vector2 dirVector;
                    //    switch (pos)
                    //    {
                    //        case "NO":
                    //            dirVector = new(quarterIntersectCenters["NE"].X - quarterIntersectCenters["NO"].X, quarterIntersectCenters["SO"].Y - quarterIntersectCenters["NO"].Y);
                    //            break;
                    //        case "NE":
                    //            dirVector = new(quarterIntersectCenters["NO"].X - quarterIntersectCenters["NE"].X, quarterIntersectCenters["SO"].Y - quarterIntersectCenters["NO"].Y);
                    //            break;
                    //        case "SE":
                    //            dirVector = new(quarterIntersectCenters["NE"].X - quarterIntersectCenters["NO"].X, quarterIntersectCenters["SO"].Y - quarterIntersectCenters["NO"].Y);
                    //            break;
                    //        case "SO":
                    //            dirVector = new(quarterIntersectCenters["NE"].X - quarterIntersectCenters["NO"].X, quarterIntersectCenters["SO"].Y - quarterIntersectCenters["NO"].Y);
                    //            break;
                    //        default:
                    //            dirVector = Vector2.Zero; break;
                    //    }
                    //    borderVector += dirVector;
                    //}

                    foreach (var (pos, pt) in quarterIntersectCenters)
                    {
                        if (quartersLocation.X > pt.X) quartersLocation.X = pt.X;
                        if (quartersLocation.Y > pt.Y) quartersLocation.Y = pt.Y;
                        quarterIntersectCenter.X += pt.X;
                        quarterIntersectCenter.Y += pt.Y;

                        //previousPt = pt;
                    }
                    quarterIntersectCenter.X /= quarterIntersectCenters.Count;
                    quarterIntersectCenter.Y /= quarterIntersectCenters.Count;
                }
                intersectBoundsCenter = quarterIntersectCenter;

                borderPoint = new(quarterIntersectCenter.X, quarterIntersectCenter.Y);

                if (!Vector2.Zero.Equals(borderVector))
                    return borderVector;
            }
            else
            {
                borderPoint = intersectBoundsCenter;
            }
            return borderVector;
        }
    }
}
