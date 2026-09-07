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
using static MED.Imaging.Images;
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
        List<IImageCollider>? _Colliders;
        public List<IImageCollider>? Colliders
        {
            get
            {
                if (_Colliders != null)
                    return _Colliders;

                if (Process.Items == null)
                    return null;

                var colliders = new List<IImageCollider>();

                foreach (var prov in Process.Items)
                {
                    if (prov is not IImageCollider collider
                        || !prov.Enabled
                        )
                        continue;

                    Region? clipRegion;

                    if (collider is IImageMover mover)
                        clipRegion = mover.ClipRegionTranslated;
                    else
                        clipRegion = collider.ClipRegion;

                    //if (clipRegion == null)
                    //    continue;

                    if (collider.Mass == 0F)
                        continue;

                    colliders.Add(collider);
                }
                if (colliders.Count == 0)
                    return null;

                return _Colliders = colliders;
            }
            set => _Colliders = value;
        }

        /**
         * Collide with image borders
         * 
         * */
        public bool CollideItemWithImageBorders(BorderBehaviors borderBehavior, Bitmap image, Graphics gr, IImageMover item, PointF offset)
        {
            if (borderBehavior == BorderBehaviors.None)
                return false;
            if (item.SpeedMax == 0F)
                return false;
            var location = item.Location;
            if (!offset.IsEmpty)
            {
                location.X += offset.X;
                location.Y += offset.Y;
            }
            else if (location.IsEmpty && item.Speed == 0F)
                return false;

            var direction = item.Direction;

            if (item.ClipRegion == null)
                return false;

            bool changed = false;
            var itemBounds = item.GetClipRegionTranslatedBounds(gr, offset);
            Vector2 overlap = Vector2.Zero;

            switch (borderBehavior)
            {
                case BorderBehaviors.Bump:
                    if (itemBounds.Top < 0)
                    {
                        overlap.Y = -itemBounds.Top + 1;
                        if (direction.Y < 0)
                            direction.Y *= -1;
                        changed = true;
                    }
                    if (itemBounds.Left < 0)
                    {
                        overlap.X = -itemBounds.Left + 1;
                        //location.X = 1;
                        if (direction.X < 0)
                            direction.X *= -1;
                        changed = true;
                    }
                    if (itemBounds.Bottom > image.Height)
                    {
                        overlap.Y = image.Height - itemBounds.Bottom;
                        if (direction.Y > 0)
                            direction.Y *= -1;
                        changed = true;
                    }
                    if (itemBounds.Right > image.Width)
                    {
                        overlap.X = image.Width - itemBounds.Right;
                        //location.X = image.Width - bounds.Width;
                        if (direction.X > 0)
                            direction.X *= -1;
                        changed = true;
                    }
                    break;

                case BorderBehaviors.Circular:
                default:
                    var itemCenter = new PointF(itemBounds.X + itemBounds.Width / 2, itemBounds.Y + itemBounds.Height / 2);
                    if (itemCenter.Y < 0)
                    {
                        overlap.Y = location.Y - itemBounds.Y;
                        location.Y = image.Height + itemCenter.Y - itemBounds.Height / 2;
                        changed = true;
                    }
                    else if (itemCenter.Y > image.Height)
                    {
                        overlap.Y = location.Y - itemBounds.Y;
                        location.Y = itemCenter.Y - image.Height - itemBounds.Height / 2;
                        changed = true;
                    }
                    if (itemCenter.X < 0)
                    {
                        overlap.X = location.X - itemBounds.X;
                        location.X = image.Width + itemBounds.X;
                        changed = true;
                    }
                    else if (itemCenter.X > image.Width)
                    {
                        overlap.X = location.X - itemBounds.X;
                        location.X = itemBounds.X - image.Width;
                        changed = true;
                    }
                    break;
            }
            if (changed)
            {
                if (!Vector2.Zero.Equals(overlap))
                {
                    location.X += overlap.X;
                    location.Y += overlap.Y;
                }
                item.Location = location;

                item.Direction = direction;
            }
            return changed;
        }
        /**
         * Collide an item moving to an offset with all others
         * 
         * <param name="image">Not current drawing image. May be previous one.</param>
         * */
        public Dictionary<IImageCollider, Region> Collide(Bitmap image, Graphics gr, IImageCollider item1, PointF offset)
        {
            Dictionary<IImageCollider, Region> someChanges = new();

            var colliders = Colliders;// ManageBorders(image, gr);
            if (colliders == null || colliders.Count < 2) return someChanges;

            IImageMover? mover1 = null;
            Region? region1;
            if (item1 is IImageMover)
                region1 = (mover1 = (IImageMover)item1).ClipRegionTranslated;
            else
                region1 = item1.ClipRegion;

            if (region1 == null)
                return someChanges;
            if (!offset.IsEmpty)
                (region1 = region1.Clone()).Translate(offset.X, offset.Y);

            RectangleF bounds1;
            if (mover1 != null)
                bounds1 = mover1.GetClipRegionTranslatedBounds(gr, offset);
            else
                bounds1 = item1.GetClipRegionBounds(gr);

            foreach (var item2 in colliders)
            {
                if (item2 == item1)
                    continue;

                IImageMover? mover2 = null;
                Region? region2;
                if (item2 is IImageMover)
                    region2 = (mover2 = (IImageMover)item2).ClipRegionTranslated;
                else
                    region2 = item2.ClipRegion;
                if (region2 == null)
                {
                    item2.CollideItem(item1, offset);
                    continue;
                }
                RectangleF bounds2;
                if (mover2 != null)
                    bounds2 = mover2.GetClipRegionTranslatedBounds(gr, offset);
                else
                    bounds2 = item2.GetClipRegionBounds(gr);
                if (bounds2.IsEmpty)
                    continue;

                var intersect = region1.Clone();
                intersect.Intersect(region2);

                if (!intersect.IsEmpty(gr))
                {
                    var intersectBounds = intersect.GetBounds(gr);
                    if (intersectBounds.Width == 0 || intersectBounds.Height == 0)
                        continue;

                    PointF intersectBoundsCenter = new((intersectBounds.Right + intersectBounds.Left) / 2F, (intersectBounds.Bottom + intersectBounds.Top) / 2F);

                    if (mover1 != null && CollideItemPair(gr, intersectBounds, intersectBoundsCenter, intersect, mover1, offset, region1, item2, PointF.Empty))
                        if (someChanges.ContainsKey(item2)) someChanges[item2] = region2;
                        else someChanges.Add(item2, region2);

                    else if (mover2 != null
                        && CollideItemPair(gr, intersectBounds, intersectBoundsCenter, intersect, mover2, PointF.Empty, region2, item1, offset))
                        if (someChanges.ContainsKey(mover2)) someChanges[mover2] = region2;
                        else someChanges.Add(mover2, region2);

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
            , IImageMover item, PointF offset, Region region
            , IImageCollider item2, PointF offset2)
        {
            item.CollideItem(item2, offset2);
            item2.CollideItem(item, offset);

            if (item.Location.IsEmpty)
                return false;
            if (item2 is IImageMover mover2)
                return CollideMovingItems(gr, intersectBounds, intersectBoundsCenter, intersectRegion, item, offset, region, mover2, PointF.Empty);
            return CollideMoverAndWall(gr, intersectBounds, intersectBoundsCenter, intersectRegion, item, offset, region, item2, PointF.Empty);
        }

        /**
         * CollideMoverAndWall
         * A wall is an image process with Speed == 0
         * 
         * */
        private bool CollideMoverAndWall(Graphics gr, RectangleF intersectBounds, PointF intersectBoundsCenter, Region intersectRegion
            , IImageMover item, PointF offset, Region regionTranslated
            , IImageCollider item2, PointF offset2)
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
            , IImageMover item, PointF offset, Region regionTranslated
            , IImageCollider item2, PointF offset2)
        {
            PointF borderPoint = intersectBoundsCenter;

            var itemBounds = regionTranslated.GetBounds(gr);
            var itemBoundsCenter = new PointF((itemBounds.Right + itemBounds.Left) / 2, (itemBounds.Bottom + itemBounds.Top) / 2);

            //var item1partialRegion = item.ClipEdgesRegionTranslated?.Clone();
            //item1partialRegion.Intersect(item2.ClipRegionTranslated);
            //var scans = item1partialRegion.GetRegionScans(new());

            //var item2partialRegion = item2.ClipRegionTranslated?.Clone();
            var intersectPartialRegion = intersectRegion.Clone();

            Region? region2 = null;
            if (item2 is IImageMover mover2)
                region2 = mover2.ClipEdgesRegionTranslated;
            else
                region2 = item2.ClipEdgesRegion;
            if (region2 != null)
            {
                var item2partialRegion = region2.Clone();
                if (!offset2.IsEmpty)
                    item2partialRegion.Translate(offset2.X, offset2.Y);
                intersectPartialRegion.Intersect(item2partialRegion);
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

        /**
         * see  IsLineIntersectingLine https://github.com/dotnet/maui/blob/43db9d77f2ff59999dea36ab8befb8541e919013/src/Graphics/src/Graphics/GeometryUtil.cs#L214
         * */

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

        private Vector2 IntersectPath(IImageCollider item, PointF offset, RectangleF intersectBounds, out PointF borderPoint)
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
                        if (points != null)
                        {
                            borderPoint.X += (points[0].X + points[1].X) / 2F;
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
            , IImageMover item, PointF offset
            , Region regionTranslated, IImageCollider item2, PointF offset2
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
            Region? item2partialRegion = null;
            if (item2 is IImageMover mover2)
                item2partialRegion = mover2.ClipEdgesRegionTranslated;
            else
                item2partialRegion = item2.ClipEdgesRegion;
            if (item2partialRegion == null)
                return Vector2.Zero;
            item2partialRegion = item2partialRegion.Clone();
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
            , IImageMover item1, PointF offset, Region region1Translated
            , IImageMover item2, PointF offset2)
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

            var item1Bounds = region1Translated.GetBounds(gr);
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

        private Vector2 GetRegionBorderVectorQuarters(Graphics gr, RectangleF intersectBounds, PointF intersectBoundsCenter, Region intersectRegion
            , IImageMover item, PointF offset
            , Region regionTranslated, IImageMover item2, PointF offset2
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
