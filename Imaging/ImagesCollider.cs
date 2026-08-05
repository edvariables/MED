using DirectShowLib.DES;
using Emgu.CV;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Numerics;
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

            Process.Performance?.Sub(".Collider.Borders").Resume($"{colliders.Count} colliders", true);

            int nCollider = 0;
            foreach (var item in colliders)
            {
                if (item.SpeedMax == 0F)
                    continue;
                var location = item.Location;
                var direction = item.Direction;
                var region = item.ClipRegionTranslated;
                if (region == null)
                    continue;
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
                            (item as IImageCollidable).Location = location;

                            (item as IImageCollidable).Direction = direction;
                        }
                    }
                }
                nCollider++;
            }
            Process.Performance?.Sub(".Collider.Borders").Pause($"Collider done {colliders.Count}");

            return colliders;
        }
        /**
         * Collide
         * 
         * */
        public Dictionary<IImageCollidable, Region> Collide(Bitmap image, Graphics gr)
        {
            var colliders = ManageBorders(image, gr);
            if (colliders == null || colliders.Count < 2) return new();

            Process.Performance?.Sub(".Collider").Resume($"{colliders.Count} colliders", true);

            Dictionary<IImageCollidable, Region> someChanges = new();
            for (var i1 = 0; i1 < colliders.Count - 1; i1++)
            {
                var item1 = colliders.ElementAt(i1);

                var region1 = item1.ClipRegionTranslated;
                if (region1 == null)
                    continue;
                //var location = item1.Location;
                //region1.Translate(location.X, location.Y);

                for (var i2 = i1 + 1; i2 < colliders.Count; i2++)
                {
                    var item2 = colliders.ElementAt(i2);
                    var region2 = item2.ClipRegionTranslated;
                    if (region2 == null)
                        continue;
                    //location = item2.Location;

                    //region2.Translate(location.X, location.Y);
                    var bounds = region2.GetBounds(gr);
                    if (bounds.IsEmpty)
                        continue;
                    var intersect = region1.Clone();
                    intersect.Intersect(region2);

                    if (!intersect.IsEmpty(gr))
                    {
                        var intersectBounds = intersect.GetBounds(gr);
                        if (intersectBounds.Width == 0 || intersectBounds.Height == 0)
                            continue; ;

                        var intersectBoundsCenter = new PointF((intersectBounds.Right + intersectBounds.Left) / 2, (intersectBounds.Bottom + intersectBounds.Top) / 2);

                        if (CollideItem(gr, intersectBounds, intersectBoundsCenter, item1, region1, item2))
                            if (someChanges.ContainsKey(item1)) someChanges[item1] = region1;
                            else someChanges.Add(item1, region1);

                        if (CollideItem(gr, intersectBounds, intersectBoundsCenter, item2, region2, item1))
                            if (someChanges.ContainsKey(item2)) someChanges[item2] = region2;
                            else someChanges.Add(item2, region2);

                    }
                }
            }
            Process.Performance?.Sub(".Collider").Pause($"Collider done {colliders.Count}");

            return someChanges;
        }

        private PointF[] GetRegionStartAndEnd(Graphics gr, RectangleF intersectBounds, PointF intersectBoundsCenter, IImageCollidable item, Region region, IImageCollidable item2, bool exploreItem2Source = true)
        {
            if (intersectBounds.Width > intersectBounds.Height * 2)
            {
                return new PointF[] { new PointF(intersectBounds.X, intersectBounds.Y+ intersectBounds.Height/2)
                                    , new PointF(intersectBounds.Right, intersectBounds.Y+ intersectBounds.Height/2) };
            }
            if (intersectBounds.Height > intersectBounds.Width * 2)
            {
                return new PointF[] { new PointF(intersectBounds.X+intersectBounds.Width/2, intersectBounds.Y)
                                    , new PointF(intersectBounds.X+intersectBounds.Height/2, intersectBounds.Bottom) };
            }
            //Rotate 45
            var rotateRegion = region.Clone();
            var matrix = new Matrix();
            matrix.RotateAt(45, intersectBoundsCenter);
            rotateRegion.Transform(matrix);
            var bounds = rotateRegion.GetBounds(gr);

            if (bounds.Width > bounds.Height * 2)
            {
                float delta = (1F - (float)Math.Cos(Math.PI / 4)) * bounds.Width;

                return new PointF[] { new PointF(bounds.X-delta, bounds.Y+ bounds.Height/2-delta)
                                    , new PointF(bounds.Right - delta, bounds.Y + bounds.Height / 2 - delta) };
            }
            if (bounds.Height > bounds.Width * 2)
            {
                float delta = (1F - (float)Math.Cos(Math.PI / 4)) * bounds.Width;

                return new PointF[] { new PointF(bounds.X+bounds.Width/2-delta, bounds.Y-delta)
                                    , new PointF(bounds.X+bounds.Height/2-delta, bounds.Bottom-delta) };
            }

            //Still rectangular
            var item2partialRegion = item2.ClipRegionTranslated?.Clone();
            if (item2partialRegion == null
                || !exploreItem2Source)
                if (intersectBounds.Width > intersectBounds.Height)
                    return new PointF[] { new PointF(intersectBounds.X, intersectBounds.Y+ intersectBounds.Height/2)
                                    , new PointF(intersectBounds.Right, intersectBounds.Y+ intersectBounds.Height/2) };
                else if (intersectBounds.Height > intersectBounds.Width)
                    return new PointF[] { new PointF(intersectBounds.X+intersectBounds.Width/2, intersectBounds.Y)
                                    , new PointF(intersectBounds.X+intersectBounds.Height/2, intersectBounds.Bottom) };
                else
                    return new PointF[] { new PointF(intersectBounds.X, intersectBounds.Y)
                                    , new PointF(intersectBounds.Right, intersectBounds.Bottom) };

            //Part of item2 in intersectBounds
            item2partialRegion.Intersect(intersectBounds);
            bounds = item2partialRegion.GetBounds(gr);
            var boundsCenter = new PointF((bounds.Right + bounds.Left) / 2, (bounds.Bottom + bounds.Top) / 2);

            return GetRegionStartAndEnd(gr, bounds, boundsCenter, item, region, item2, false);
        }

        private bool CollideItem(Graphics gr, RectangleF intersectBounds, PointF intersectBoundsCenter, IImageCollidable item, Region region, IImageCollidable item2)
        {
            var location = item.Location;
            if (location.IsEmpty)
                return false;
            if (item2.Speed == 0F)
                return CollideMoverAndWall(gr, intersectBounds, intersectBoundsCenter, item, region, item2);
            return CollideMovingItems(gr, intersectBounds, intersectBoundsCenter, item, region, item2);
        }
        private bool CollideMoverAndWall(Graphics gr, RectangleF intersectBounds, PointF intersectBoundsCenter, IImageCollidable item, Region region, IImageCollidable item2)
        {
            var location = item.Location;
            if (location.IsEmpty)
                return false;

            var itemBounds = region.GetBounds(gr);
            var itemBoundsCenter = new PointF((itemBounds.Right + itemBounds.Left) / 2, (itemBounds.Bottom + itemBounds.Top) / 2);
            PointF move = new(itemBoundsCenter.X - intersectBoundsCenter.X, itemBoundsCenter.Y - intersectBoundsCenter.Y);
            if (move.IsEmpty)
                return false;

            var wallPoints = GetRegionStartAndEnd(gr, intersectBounds, intersectBoundsCenter, item, region, item2);
            PointF wallStart = wallPoints[0];
            PointF wallEnd = wallPoints[1];

            #region Closest Point

            // Vector representing the direction and length of the wall

            //**Calculating the Dot Product for the Closest Point
            // Vector from the start of the wall to the ball's centre
            Vector2 vector_to_point = new(-move.X, -move.Y);

            // Vector representing the direction and length of the wall
            Vector2 line_vector = new(wallStart.X - wallEnd.X, wallEnd.Y - wallEnd.Y);

            // Calculate the dot product between the two vectors
            double dot_product_result = Vector2.Dot(vector_to_point, line_vector);

            //**Normalising the Wall and Calculating the Parameter t
            // Square of the wall's length for normalisation
            double line_length_squared = line_vector.X * line_vector.X + line_vector.Y * line_vector.Y;
            // Calculate the normalised parameter 't' for the closest point along the wall
            float t = (float)(dot_product_result / line_length_squared);

            //**Clamping t to Constrain the Closest Point Within Wall Bounds
            // Clamp 't' to ensure the closest point remains within the wall's bounds
            t = Math.Max(0, Math.Min(1, t));

            // Return the coordinates of the closest point on the wall
            PointF closest = new(wallStart.X + line_vector.X * t, wallStart.Y + line_vector.Y * t);
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
            Vector2 collision_normal = new(closest.X - itemBoundsCenter.X, closest.Y - itemBoundsCenter.Y);
            collision_normal = Vector2.Normalize(collision_normal);

            //Determine the Penetration Depth
            float distance = (float)Math.Sqrt(dx * dx + dy * dy);   // The actual distance between the ball's center and the closest point
            float penetration = radius_sum - distance;

            //Push the Ball Out of the Wall
            if (penetration > 0)
            {
                location.X += collision_normal.X * penetration;
                location.Y += collision_normal.Y * penetration;
            }

            #endregion

            #region Reflect and Dampen the Velocity

            float velocity_dot_normal = Vector2.Dot(item.VelocityVector, collision_normal);
            Vector2 velocity_normal = collision_normal * velocity_dot_normal;
            Vector2 velocity_tangent = item.VelocityVector - velocity_normal;

            // Reverse and dampen the normal component of the velocity
            // Damping factor is arbitrarily chosen as 0.6
            var itemVelocity = velocity_tangent - velocity_normal * 1F;

            #endregion

            location.X += itemVelocity.X;
            location.Y += itemVelocity.Y;

            //location.X += item.Velocity.X;
            //location.Y += item.Velocity.Y;
            if (float.IsNaN(location.X))
            {
                item.Performance?.Error("location.X IsNaN !");
                return false;
            }

            item.Location = location;

            return true;
        }

        private bool CollideMovingItems(Graphics gr, RectangleF intersectBounds, PointF intersectBoundsCenter, IImageCollidable item, Region region, IImageCollidable item2)
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
