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
    public interface IImageCollider : IImageProvider
    {
        float Mass { get; }

        [Browsable(false)]
        System.Drawing.Region? ClipEdgesRegion { get; }
        RectangleF GetClipRegionBounds(Graphics gr);

        [Browsable(true)]
        float SurfaceFriction { get; }

        PointF Collide(PointF offset);

        bool CollideItem(IImageCollider item2, PointF offset2);
    }
}
