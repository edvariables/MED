using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using static Emgu.Util.Platform;

namespace MED.Imaging
{
    public class ImagesCollider(IProcesses process)
    {

        public IProcesses Process { get; } = process;

        /**
         * Collider
         * 
         * */
        Dictionary<IImageCollidable, Region>? _CollidersRegions;
        public Dictionary<IImageCollidable, Region> CollidersRegions
        {
            get
            {
                if (_CollidersRegions != null)
                    return _CollidersRegions;

                if (Process.Items == null)
                    return null;

                var colliders = new Dictionary<IImageCollidable, Region>();

                foreach (var prov in Process.Items)
                {
                    if (prov is not IImageCollidable)
                        continue;

                    var clipRegion = (prov as IImageCollidable).ClipRegionTranslated;

                    if (clipRegion == null)
                        continue;

                    if ((prov as IImageCollidable).Density == 0F)
                        continue;

                    colliders.Add((IImageCollidable)prov, clipRegion);
                }
                if (colliders.Count == 0)
                    return null;

                return _CollidersRegions = colliders;
            }
            set => _CollidersRegions = value;
        }

        /**
         * ColliderBorders
         * 
         * */
        public Dictionary<IImageCollidable, Region> ManageBorders(Bitmap image, Graphics gr)
        {
            var colliders = CollidersRegions;
            if (colliders == null || colliders.Count == 0) return colliders;

            Process.Performance?.Sub(".Collider.Borders").Resume($"{colliders.Count} colliders", true);

            int nCollider = 0;
            foreach (var (item, _region) in colliders.ToArray())
            {
                var location = item.Location;
                var direction = item.Direction;
                var region = item.ClipRegionTranslated;
                var bounds0 = region.GetBounds(gr);

                if (region != null)
                {
                    if (!location.IsEmpty)
                    {
                        //region = region.Clone();
                        //region.Translate(location.X, location.Y);
                        var bounds = region.GetBounds(gr);

                        //Process.Performance?.Sub(".Collider.Borders").Step($"{item} {bounds}");
                        bool changed = false;
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
                            location.Y = image.Height - (item as IImageProvider).ImageSizeMax.Height;
                            if (direction.Y > 0)
                                direction.Y *= -1;
                            changed = true;
                        }
                        if (bounds.Right > image.Width)
                        {
                            location.X = image.Width - (item as IImageProvider).ImageSizeMax.Width;
                            if (direction.X > 0)
                                direction.X *= -1;
                            changed = true;
                        }
                        if (changed)
                        {
                            (item as IImageCollidable).Location = location;

                            (item as IImageCollidable).Direction = direction;

                            UpdateColliderRegion(gr, item);
                        }
                    }
                }
                nCollider++;
            }
            Process.Performance?.Sub(".Collider.Borders").Pause($"Collider done {colliders.Count}");

            return colliders;
        }
        /**
         * Collider
         * 
         * */
        public Dictionary<IImageCollidable, Region> Collide(Bitmap image, Graphics gr)
        {
            var colliders = ManageBorders(image, gr);
            if (colliders == null || colliders.Count < 2) return new();

            Process.Performance?.Sub(".Collider").Resume($"{colliders.Count} colliders", true);

            Dictionary<IImageCollidable, Region> someChanges = new();
            var i1 = 0;
            foreach ((IImageCollidable item1, Region _region1) in colliders.ToArray())
            {
                var region1 = item1.ClipRegionTranslated;
                var location = item1.Location;
                //region1.Translate(location.X, location.Y);

                for (var i2 = i1 + 1; i2 < colliders.Count; i2++)
                {
                    var item2 = colliders.Keys.ElementAt(i2);
                    var region2 = item2.ClipRegionTranslated;// colliders[item2];
                    location = item2.Location;

                    //region2.Translate(location.X, location.Y);

                    var intersect = region1.Clone();
                    intersect.Intersect(region2);

                    if (!intersect.IsEmpty(gr))
                    {
                        var intersectBounds = intersect.GetBounds(gr);
                        var intersectBoundsCenter = new PointF((intersectBounds.Right + intersectBounds.Left) / 2, (intersectBounds.Bottom + intersectBounds.Top) / 2);

                        if (CollideItem(gr, intersectBounds, intersectBoundsCenter, item1, region1))
                            someChanges.Add(item1, region1);

                        if (CollideItem(gr, intersectBounds, intersectBoundsCenter, item2, region2))
                            someChanges.Add(item2, region2);

                    }
                }

                i1++;
            }
            Process.Performance?.Sub(".Collider").Pause($"Collider done {colliders.Count}");

            return someChanges;
        }

        private bool CollideItem(Graphics gr, RectangleF intersectBounds, PointF intersectBoundsCenter, IImageCollidable item, Region region)
        {
            var location = item.Location;
            if (location.IsEmpty)
                return false;
            var itemBounds = region.GetBounds(gr);
            var itemBoundsCenter = new PointF((itemBounds.Right + itemBounds.Left) / 2, (itemBounds.Bottom + itemBounds.Top) / 2);
            if (intersectBounds.Width == 0 || intersectBounds.Height == 0)
                return false;
            var overRatio = Math.Abs((intersectBounds.Width * intersectBounds.Height) / (itemBounds.Width * itemBounds.Height));
            var changed = false;
            PointF move = new(itemBoundsCenter.X - intersectBoundsCenter.X, itemBoundsCenter.Y - intersectBoundsCenter.Y);
            PointF moveRatio = new(Math.Abs(move.X / itemBounds.Width), Math.Abs(move.Y / itemBounds.Height));
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

                var direction = item.Direction = new PointF(vector.X, vector.Y);

                location.X += direction.X * speed;
                location.Y += direction.Y * speed;

                item.Location = location;

                UpdateColliderRegion(gr, item);

            }
            return changed;
        }

        private void UpdateColliderRegion(Graphics gr, IImageCollidable item)
        {
            var location = item.Location;

            var clipRegion = item.ClipRegionTranslated;
            var itemBounds = clipRegion.GetBounds(gr);
            //clipRegion.Translate(location.X, location.Y);
            //itemBounds = clipRegion.GetBounds(gr);
            itemBounds = CollidersRegions[item].GetBounds(gr);

            CollidersRegions[item] = clipRegion;//Update
            itemBounds = CollidersRegions[item].GetBounds(gr);
        }


        public static Region? ClipRegionTranslated(Region? clipRegion, PointF location)
        {
            if (clipRegion == null)
                return null;
            if (location.IsEmpty)
                return clipRegion;
            clipRegion = clipRegion.Clone();
            clipRegion.Translate(location.X, location.Y);
            return clipRegion;
        }
    }
}
