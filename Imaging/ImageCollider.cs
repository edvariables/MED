using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using MED.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MED.Imaging
{
    /**
     * class Mover : ImageSourced, IImageProvider, IImageCollidable
     * <summary>Image as a physical object that can collide</summary>
     * */
    public class ImageCollider : ImageSourced, IImageProvider, IImageCollider
    {
        public ImageCollider(string name = "ImageCollidable", Performance? performance = null, Control? invokeHandler = null, IImageConsumer? imageConsumer = null, bool isAsynchrone = true)
        : base(name, performance, invokeHandler, imageConsumer, isAsynchrone)
        {
        }

        #region Properties

        [Category("Mover")]
        public virtual float Mass { get; set; } = 1F;

        Vector2 _LocationVector;
        [Browsable(false)]
        public virtual Vector2 LocationVector
        {
            get
            {
                if (_LocationVector.Equals(Vector2.Zero))
                    return _LocationVector = base.Location.ToVector2();
                return _LocationVector;
            }
            private set { _LocationVector = value; }
        }

        /**
         * Surface friction in collision
         * 
         * */
        [Category("Mover")]
        public virtual float SurfaceFriction { get; set; } = 1F;


        #endregion

        #region Collide
        public override Region? ClipRegion
        {
            get => base.ClipRegion;
            set
            {
                _ClipRegionBounds = RectangleF.Empty;
                base.ClipRegion = value;
            }
        }

        private RectangleF _ClipRegionBounds = RectangleF.Empty;
        public virtual RectangleF GetClipRegionBounds(Graphics gr)
        {
            if (ClipRegion == null)
                return RectangleF.Empty;
            if (_ClipRegionBounds.IsEmpty)
                _ClipRegionBounds = ClipRegion.GetBounds(gr);
            return _ClipRegionBounds;
        }
        /**
         * Collide
         * 
         * <returns>Offseted Location</returns>
         */
        public virtual PointF Collide(PointF offset)
        {
            if (this.Consumer is Images)
                return ((Images)this.Consumer).CollideItemWithOthers(this, offset);

            if (offset.IsEmpty)
                return Location;
            var location = Location;
            location.X += offset.X;
            location.Y += offset.Y;
            return location;
        }
        public virtual bool CollideItem(IImageCollider item2, PointF offset2) => false;

        #endregion

        #region Settings
        public override void LoadSettings(ProcessSettings? settings = null, string fileName = "")
        {
            base.LoadSettings(settings, fileName);
            if (settings == null && (settings = ProcessSettings) == null)
                return;
            Mass = (float)(settings.GetValue("Mass", Mass) ?? Mass);
            SurfaceFriction = (float)(settings.GetValue("SurfaceFriction", SurfaceFriction) ?? SurfaceFriction);
        }
        public override JsonObject SaveProcess(JsonObject? node = null)
        {
            node = base.SaveProcess(node);
            node.Add("Mass", Mass);
            node.Add("SurfaceFriction", SurfaceFriction);
            return node;
        }
        #endregion
    }
}