using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.VisualStyles;

namespace MED.Imaging
{
    internal static class Physics
    {

        public static void PositionalCorrection(IImageCollidable itemA, IImageCollidable itemB, RectangleF intersectBounds, PointF intersectBoundsCenter)
        {
            if (itemA.SpeedMax == 0f)
                return;

            Vector2 normal = Vector2.Normalize(new Vector2(intersectBoundsCenter.X, intersectBoundsCenter.Y));
            //Vector2 normal = Vector2.Normalize(new Vector2(-intersectBoundsCenter.Y, intersectBoundsCenter.X));
            var percent = 0.6f; // usually 20% to 80%
            var slop = 0.05f;    // usually 0.01 to 0.1
            float penetrationAB = 1F;
            // Only correct penetration beyond the slop.
            float penetration = Math.Max(penetrationAB - slop, 0.0f);
            float correctionMagnitude = penetration / (1 / itemA.Mass + 1 / itemB.Mass) * percent;
            Vector2 correction = normal * correctionMagnitude;

            
                PointF location = itemA.Location;
                Vector2 vector = (-correction / itemA.Mass);
                location.X += vector.X;
                location.Y += vector.Y;
                itemA.Location = location;
            itemA.Direction = new PointF(normal);

            //if (itemB.Speed != 0f) { 
            //itemB.Move(+correction / itemB.Mass);
            //location = itemB.Location;
            //vector = (-correction / itemB.Mass);
            //location.X += vector.X;
            //location.Y += vector.Y;
            //itemB.Location = location;
            //}
        }

        public static void ResolveCollisionRotational(IImageMover itemA, IImageMover itemB)
        {
            //// Retrieve the two physics objects.
            //IImageMove A = itemA;
            //IImageMove B = itemB;

            //// For each object, if it's rotational, get its angular velocity and inverse inertia; otherwise, treat as zero.
            //float angularVelA = A.CanRotate ? A.AngularVelocity : 0F;
            //float iInertiaA = A.CanRotate ? A.IInertia : 0F;
            //float angularVelB = B.CanRotate ? B.AngularVelocity : 0F;
            //float iInertiaB = B.CanRotate ? B.IInertia : 0F;

            //// Compute vectors from centers to contact point.
            //Vector2 rA = m.ContactPoint - A.Center;
            //Vector2 rB = m.ContactPoint - B.Center;

            //// Compute the relative velocity at the contact point (including any rotational contribution).
            //Vector2 vA_contact = A.Velocity + PhysMath.Perpendicular(rA) * angularVelA;
            //Vector2 vB_contact = B.Velocity + PhysMath.Perpendicular(rB) * angularVelB;
            //Vector2 relativeVelocity = vB_contact - vA_contact;

            //float velAlongNormal = Vector2.Dot(relativeVelocity, m.Normal);
            //if (velAlongNormal > 0)
            //    return;

            //float e = Math.Min(A.Restitution, B.Restitution);

            //// Compute cross products for the normal.
            //float rA_cross_N = PhysMath.Cross(rA, m.Normal);
            //float rB_cross_N = PhysMath.Cross(rB, m.Normal);

            //// Denominator includes linear inertia plus rotational contributions.
            //float invMassSum = A.IMass + B.IMass +
            //                   (rA_cross_N * rA_cross_N) * iInertiaA +
            //                   (rB_cross_N * rB_cross_N) * iInertiaB;

            //float j = -(1 + e) * velAlongNormal;
            //j /= invMassSum;

            //Vector2 impulse = m.Normal * j;

            //if (!A.Locked && !A.Sleeping)
            //{
            //    A.Velocity -= impulse * A.IMass;
            //    if (A.CanRotate)
            //    {
            //        A.AngularVelocity -= PhysMath.Cross(rA, impulse) * iInertiaA;
            //    }
            //}
            //if (!B.Locked && !B.Sleeping)
            //{
            //    B.Velocity += impulse * B.IMass;
            //    if (B.CanRotate)
            //    {
            //        B.AngularVelocity += PhysMath.Cross(rB, impulse) * iInertiaB;
            //    }
            //}

            //// --- Friction impulse ---
            //Vector2 tangent = relativeVelocity - m.Normal * Vector2.Dot(relativeVelocity, m.Normal);
            //if (tangent.LengthSquared() > 0.0001f)
            //    tangent = Vector2.Normalize(tangent);
            //else
            //    tangent = new Vector2(0, 0);

            //float jt = -Vector2.Dot(relativeVelocity, tangent);

            //float rA_cross_t = PhysMath.Cross(rA, tangent);
            //float rB_cross_t = PhysMath.Cross(rB, tangent);
            //float invMassSumFriction = A.IMass + B.IMass +
            //                           (rA_cross_t * rA_cross_t) * iInertiaA +
            //                           (rB_cross_t * rB_cross_t) * iInertiaB;
            //jt /= invMassSumFriction;

            //// Clamp friction impulse (Coulomb friction).
            //float mu = Math.Max(A.Friction, B.Friction);
            //jt = Math.Min(Math.Abs(jt), mu * Math.Abs(j));
            //jt = jt * (jt < 0 ? -1 : 1); // restore sign

            //Vector2 frictionImpulse = tangent * jt;

            //if (!A.Locked && !A.Sleeping)
            //{
            //    A.Velocity += frictionImpulse * A.IMass;
            //    if (A.CanRotate)
            //    {
            //        A.AngularVelocity += PhysMath.Cross(rA, frictionImpulse) * iInertiaA;
            //    }
            //}
            //if (!B.Locked && !B.Sleeping)
            //{
            //    B.Velocity -= frictionImpulse * B.IMass;
            //    if (B.CanRotate)
            //    {
            //        B.AngularVelocity -= PhysMath.Cross(rB, frictionImpulse) * iInertiaB;
            //    }
            //}
        }
    }
}
