using DirectShowLib;
using DirectShowLib.DES;
using Emgu.CV;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.Marshalling;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading.Tasks;
using static Emgu.Util.Platform;
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
                    if (prov is not IImageCollidable)
                        continue;

                    var clipRegion = (prov as IImageCollidable).ClipRegionTranslated;

                    //if (clipRegion == null)
                    //    continue;

                    if ((prov as IImageCollidable).Mass == 0F)
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
            if (location.IsEmpty)
                return false;
            location.X += offset.X;
            location.Y += offset.Y;
            var direction = item.Direction;
            var region = item.ClipRegionTranslated;
            if (region == null)
                return false;
            if (!offset.IsEmpty)
                (region = region.Clone()).Translate(offset.X, offset.Y);
            //TODO Rotation

            bool changed = false;
            var bounds = region.GetBounds(gr);

            //Process.Performance?.Sub(".Collider.Borders").Step($"{item} {bounds}");
            if (bounds.Top < 0)
            {
                location.Y = 1;
                if (direction.Y < 0)
                    direction.Y *= -1;
                changed = true;
            }
            if (bounds.Left < 0)
            {
                location.X = 1;
                if (direction.X < 0)
                    direction.X *= -1;
                changed = true;
            }
            if (bounds.Bottom > image.Height)
            {
                location.Y = image.Height - bounds.Height;
                if (direction.Y > 0)
                    direction.Y *= -1;
                changed = true;
            }
            if (bounds.Right > image.Width)
            {
                location.X = image.Width - bounds.Width;
                if (direction.X > 0)
                    direction.X *= -1;
                changed = true;
            }
            if (changed)
            {
                //TODO rebound of partial offset

                (item as IImageCollidable).Location = location;

                (item as IImageCollidable).Direction = direction;
            }

            return changed;
        }
        /**
         * Collide an item moving to an offset with all others
         * 
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
                return new();//Do not both image borders and collides
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

                    if (CollideItemPair(gr, intersectBounds, intersectBoundsCenter, intersect, item2, PointF.Empty, region2, item1, offset))
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
            if (item.Location.IsEmpty)
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
            , IImageCollidable item, PointF offset, Region region
            , IImageCollidable item2, PointF offset2)
        {
            var location = item.Location;
            if (location.IsEmpty)//TODO abuse
                return false;
            var itemBounds = region.GetBounds(gr);
            var itemBoundsCenter = new PointF((itemBounds.Right + itemBounds.Left) / 2, (itemBounds.Bottom + itemBounds.Top) / 2);
            PointF move = new(itemBoundsCenter.X - intersectBoundsCenter.X, itemBoundsCenter.Y - intersectBoundsCenter.Y);
            if (move.IsEmpty)
                return false;

            #region Closest Point
            PointF closest;
            var wallBorder_vector = GetRegionBorderVector(gr, intersectBounds, intersectBoundsCenter, intersectRegion, item, offset, region, item2, offset2, out closest);
            if (Vector2.Zero.Equals(wallBorder_vector) || float.IsNaN(wallBorder_vector.X))
                return false;

            #endregion

            #region Detecting Ball and Wall Overlap

            float dx = itemBoundsCenter.X - closest.X;
            float dy = itemBoundsCenter.Y - closest.Y;
            float distance_squared = dx * dx + dy * dy;  // Using squared distance to avoid unnecessary square root calculations
            float radius_sum = itemBounds.Width / 2 /*+ w.radius*/;  // The combined radius of the ball and the wall's thickness
            var overlapping = distance_squared <= radius_sum * radius_sum;   // True if overlapping

            #endregion

            #region Resolving Ball and Wall Collision

            //Normal
            Vector2 wallNormal = new(-wallBorder_vector.Y, wallBorder_vector.X);

            Vector2 collision_normal = wallNormal;// new(closest.X - itemBoundsCenter.X, closest.Y - itemBoundsCenter.Y);
            collision_normal = Vector2.Normalize(collision_normal);

            //Determine the Penetration Depth
            float distance = (float)Math.Sqrt(distance_squared);   // The actual distance between the ball's center and the closest point
            float penetration = radius_sum - distance;

            //Push the Ball Out of the Wall
            if (penetration > 0)
            {
                item.Performance?.Step($"Penetration {penetration}");
                location.X += collision_normal.X * penetration;
                location.Y += collision_normal.Y * penetration;
            }

            #endregion

            #region Reflect and Dampen the Velocity
            float velocity_dot_normal = Vector2.Dot(item.DirectionVector, collision_normal);
            Vector2 velocity_normal = collision_normal * velocity_dot_normal;
            Vector2 velocity_tangent = item.VelocityVector - velocity_normal;

            // Reverse and dampen the normal component of the velocity
            // Damping factor is arbitrarily chosen as 0.6
            var itemVelocity = Vector2.Normalize(velocity_tangent - velocity_normal * 1F);

            #endregion

            if (float.IsNaN(itemVelocity.X))
            {
                item.Performance?.Error("location.X IsNaN !");
                return false;
            }

            item.Direction = new PointF(itemVelocity.X, itemVelocity.Y);

            if (!offset.IsEmpty)
            {
                int duration = 20;//TODO Part of rebound
                location.X += item.Velocity.X * duration;
                location.Y += item.Velocity.Y * duration;
                item.Location = location;
            }

            return true;
        }


        private PointF GetInterceptionPoint(Graphics gr, RectangleF intersectBounds, PointF intersectBoundsCenter, Region intersectRegion
            , IImageCollidable item, PointF offset, Region region
            , IImageCollidable item2, PointF offset2)
        {
            PointF borderPoint = intersectBoundsCenter;

            var itemBounds = region.GetBounds(gr);
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

        private Vector2 GetRegionBorderVector(Graphics gr, RectangleF intersectBounds, PointF intersectBoundsCenter, Region intersectRegion
            , IImageCollidable item, PointF offset
            , Region region, IImageCollidable item2, PointF offset2
            , out PointF borderPoint)
        {
            var itemBounds = region.GetBounds(gr);
            var itemBoundsCenter = new PointF((itemBounds.Right + itemBounds.Left) / 2, (itemBounds.Bottom + itemBounds.Top) / 2);

            var item2partialRegion = item2.ClipRegionTranslated?.Clone();
            if (item2partialRegion == null)
            {
                borderPoint = PointF.Empty;
                return Vector2.Zero;
            }
            if (!offset2.IsEmpty)
                item2partialRegion.Translate(offset2.X, offset2.Y);

            var inflateBounds = intersectBounds;
            int nInflate = 1;
            //inflateBounds.Offset(-nInflate / 2, -nInflate / 2);
            inflateBounds.Inflate(nInflate, nInflate);

            //Part of item2 in intersectBounds
            item2partialRegion.Intersect(inflateBounds);
            RectangleF expBounds = item2partialRegion.GetBounds(gr);
            RectangleF deltaBounds = new(expBounds.Left - intersectBounds.Left, expBounds.Top - intersectBounds.Top, expBounds.Right - intersectBounds.Right, expBounds.Bottom - intersectBounds.Bottom);
            var boundsCenter = new PointF((expBounds.Right + expBounds.Left) / 2, (expBounds.Bottom + expBounds.Top) / 2);
            Vector2 moveVector = new(boundsCenter.X - intersectBoundsCenter.X, boundsCenter.Y - intersectBoundsCenter.Y);
            Vector2 borderVector = new(-moveVector.Y, moveVector.X);
            //Vector2 borderVector = new(intersectBounds.X- bounds.X, intersectBounds.Y- bounds.Y);//Approx
            borderPoint = intersectBoundsCenter;
            borderPoint = GetInterceptionPoint(gr, intersectBounds, intersectBoundsCenter, intersectRegion, item, offset, region, item2, offset2);

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
            else if (borderAtTop)
            {
                if (borderAtBottom)
                    borderPoint.Y = (intersectBounds.Top + intersectBounds.Bottom) / 2;
                else
                    borderPoint.Y = intersectBounds.Bottom;
            }
            else if (borderAtBottom)
                borderPoint.Y = intersectBounds.Top;

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
            else if (borderAtLeft)
            {
                if (borderAtRight)
                    borderPoint.X = (intersectBounds.Left + intersectBounds.Right) / 2;
                else
                    borderPoint.X = intersectBounds.Right;
            }
            else if (borderAtRight)
                borderPoint.X = intersectBounds.Left;

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

            //else if (bounds.Bottom == intersectBounds.Bottom)
            //{
            //    //Border is top (X = +1)
            //    borderPoint.Y = bounds.Bottom;

            //    if (bounds.Left == intersectBounds.Left)
            //    {
            //        //Border is right (Y = +1)
            //        borderVector.Y = intersectBounds.Top - bounds.Top;

            //        borderVector.X = bounds.Right - intersectBounds.Right;

            //    }
            //    else if (bounds.Right == intersectBounds.Right)
            //    {
            //        //Border is left (Y = -1)

            //        borderVector.X = intersectBounds.Left - bounds.Left;
            //        if (borderVector.Y > 0)
            //            borderVector.Y *= -1;
            //    }
            //    else if (bounds.Bottom > gr.VisibleClipBounds.Bottom - 2 && borderVector.X >= 0)
            //        borderVector.X = -1;
            //    else if (borderVector.X <= 0)
            //        borderVector.X = 1;

            //    if (bounds.Top == 0)
            //        borderVector.X = 1;
            //}
            //else if (bounds.Left == intersectBounds.Left)
            //{   //Border is right  (Y = +1)

            //    borderVector = new(0, intersectBounds.Top - bounds.Top);

            //    borderPoint.X = bounds.X;
            //    if (bounds.Right > gr.VisibleClipBounds.Right - 2 && borderVector.Y >= 0)
            //        borderVector.Y = -1;
            //}
            //else if (bounds.Right == intersectBounds.Right)
            //{
            //    //Border is left  (Y = -1)
            //    borderVector = new(0, bounds.Top - intersectBounds.Top);

            //    borderPoint.X = bounds.Right;
            //    if (bounds.Left == 0)
            //        borderVector.Y = -1;
            //}
            //else
            //{
            //    Console.Error.Write("");
            //}
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

        private bool CollideMovingItems(Graphics gr, RectangleF intersectBounds, PointF intersectBoundsCenter, Region intersectRegion
            , IImageCollidable item, PointF offset, Region region
            , IImageCollidable item2, PointF offset2)
        {
            var location = item.Location;
            if (location.IsEmpty)
                return false;
            var itemBounds = region.GetBounds(gr);
            var itemBoundsCenter = new PointF((itemBounds.Right + itemBounds.Left) / 2, (itemBounds.Bottom + itemBounds.Top) / 2);
            var overRatio = Math.Abs((intersectBounds.Width * intersectBounds.Height) / (itemBounds.Width * itemBounds.Height));
            var changed = false;
            PointF move = new(itemBoundsCenter.X - intersectBoundsCenter.X, itemBoundsCenter.Y - intersectBoundsCenter.Y);
            PointF moveRatio = new(Math.Abs(move.X / itemBounds.Width), Math.Abs(move.Y / itemBounds.Height));
            Vector2 oldVector = new Vector2((float)(item.Direction.X), (float)(item.Direction.Y));
            Vector2 normal = Vector2.Normalize(new Vector2((float)(intersectBounds.X), (float)(intersectBounds.Y)));
            Vector2 dirNormal = Vector2.Normalize(new Vector2(item.Direction.X, item.Direction.Y));
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
                    item.Speed *= energyRatio;
                    //item2.Speed /= energyRatio;
                }

                //Physics.PositionalCorrection((IImageCollidable)item, (IImageCollidable)item2, intersectBounds, intersectBoundsCenter);
                dirNormal = Vector2.Normalize(new Vector2(item.Direction.X, item.Direction.Y));
                normal += (normal - dirNormal);
                vector = Vector2.Normalize(-normal);

                //float dx = itemBounds.Width;//2* ( itemBoundsCenter.X - intersectBoundsCenter.X);
                //float dy = itemBounds.Height;// 2*(itemBoundsCenter.Y - intersectBoundsCenter.Y);
                //float distance_squared = dx * dx + dy * dy; // Square of the distance between the centers
                //float distance = (float)Math.Sqrt(distance_squared);    // Actual distance between the centers
                //float radius_sum = distance;
                //float overlap = (float)Math.Sqrt(intersectBounds.Width * intersectBounds.Width + intersectBounds.Height * intersectBounds.Height);// 0.5f * (radius_sum - distance); // Amount of overlap between the balls
                //Vector2 normalised_collision = new Vector2(dx / distance, dy / distance);

                //location.X -= overlap * normalised_collision.X;
                //location.Y -= overlap * normalised_collision.Y;

                //Vector2 collision_normal = Vector2.Normalize(normalised_collision);
                //Vector2 ball_1_velocity = new Vector2(item.Velocity.X, item.Velocity.Y);
                //Vector2 ball_2_velocity = new Vector2(item2.Velocity.X, item2.Velocity.Y);
                //float ball_1_normal_dot_product = Vector2.Dot(ball_1_velocity, collision_normal);
                //float ball_1_collision_dot_product = Vector2.Dot(ball_1_velocity, normalised_collision);
                //float ball_2_normal_dot_product = Vector2.Dot(ball_2_velocity, collision_normal);
                //float ball_2_collision_dot_product = Vector2.Dot(ball_2_velocity, normalised_collision);
                //float ball_1_momentum = (float)(ball_1_collision_dot_product * (item.Mass - item2.Mass) + 2.0f * item2.Mass * ball_2_collision_dot_product) / (item.Mass + item2.Mass);
                //ball_1_velocity = collision_normal * ball_1_normal_dot_product + normalised_collision * ball_1_momentum;

                //// Vector from the start of the wall to the ball's centre
                //Vector2 vector_to_point = new(-move.X, -move.Y);

                //// Vector representing the direction and length of the wall
                //Vector2 line_vector = new(intersectBounds.Width, intersectBounds.Height);

                //// Calculate the dot product between the two vectors
                //double dot_product_result = Vector2.Dot(vector_to_point, line_vector);

                //var direction = item.Direction = new PointF(collision_normal.X, collision_normal.Y);

                //// Square of the wall's length for normalisation
                //double line_length_squared = line_vector.Xx * line_vector.X + line_vector.Y * line_vector.Y;

                //// Calculate the normalised parameter 't' for the closest point along the wall
                //double t = dot_product_result / line_length_squared;
                //// Clamp 't' to ensure the closest point remains within the wall's bounds
                //t = Math.Max (0, Math.Min(1, t));

                //// Return the coordinates of the closest point on the wall
                //PointF contactPoint = intersectBoundsCenter;// start.x + line_vector.x * t, start.y + line_vector.y * t;

                PointF closest = intersectBoundsCenter;
                double dx = itemBoundsCenter.X - closest.X;
                double dy = itemBoundsCenter.Y - closest.Y;
                //double distance_squared = dx * dx + dy * dy;  // Using squared distance to avoid unnecessary square root calculations
                //double radius_sum = itemBounds.Width + w.radius;  // The combined radius of the ball and the wall's thickness
                Vector2 collision_normal = new(-move.X, -move.Y);
                collision_normal = Vector2.Normalize(collision_normal);
                double distance = Math.Sqrt(dx * dx + dy * dy);   // The actual distance between the ball's center and the closest point
                float penetration = (float)Math.Sqrt(intersectBounds.Width * intersectBounds.Width + intersectBounds.Height * intersectBounds.Height);


                Vector2 ball_1_velocity = new Vector2(item.Velocity.X, item.Velocity.Y);

                location.X -= penetration * ball_1_velocity.X;
                location.Y -= penetration * ball_1_velocity.Y;

                float velocity_dot_normal = (float)Vector2.Dot(ball_1_velocity, collision_normal);

                Vector2 velocity_normal = collision_normal * velocity_dot_normal;
                Vector2 velocity_tangent = ball_1_velocity - velocity_normal;

                // Reverse and dampen the normal component of the velocity
                // Damping factor is arbitrarily chosen as 0.6
                ball_1_velocity = velocity_tangent - velocity_normal * 0.6F;

                var direction = item.Direction = new PointF(ball_1_velocity.X, ball_1_velocity.Y);

                location.X += ball_1_velocity.X;
                location.Y += ball_1_velocity.Y;

                //location.X += item.Velocity.X;
                //location.Y += item.Velocity.Y;

                item.Location = location;

            }
            return changed;
        }

    }
}
