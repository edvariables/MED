using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MED.Imaging
{
    /**
     * interface IImageCollidable : IImageMover
     * <summary>Image as a physic object</summary>
     * */
    public interface IImageCollider : IImageInteractor
    {

        System.Drawing.Region? ClipEdgesRegion { get; }

        RectangleF GetClipRegionBounds(Graphics gr);

        float SurfaceFriction { get; }

        PointF Collide(PointF offset);

        bool CollideItem(IImageCollider item2, PointF offset2);

        EventScript? OnCollideItemScript { get; }
    }
}
