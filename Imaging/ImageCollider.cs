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

        [Browsable(true)]
        [Category("Image")]
        public override System.Drawing.PointF Location
        {
            get
            {
                return base.Location;
            }
            set
            {
                if (float.IsNaN(value.X) || float.IsInfinity(value.X))
                    return;
                _ClipRegionTranslated = null;
                //if (base.Location != value)
                //    Performance?.Debug($"Location _setter {value}");
                base.Location = value;
            }
        }

        [Browsable(false)]
        public override float RotationAngle
        {
            get => base.RotationAngle;
            set
            {
                _ClipRegionTranslated = null;
                base.RotationAngle = value;
            }
        }

        public override Region? ClipRegion
        {
            get => base.ClipRegion;
            set
            {
                _ClipRegionTranslated = null;
                _ClipEdgesRegionTranslated = null;
                base.ClipRegion = value;
            }
        }

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


        Region? _ClipRegionTranslated;
        /**
         * ClipRegionTranslated
         * Returns ClipRegion.Clone().Translate(Location.X, Location.Y);
        */
        [Browsable(false)]
        public virtual Region? ClipRegionTranslated
        {
            get
            {
                if (_ClipRegionTranslated != null || Image == null || ClipRegion == null)
                    return _ClipRegionTranslated;
                _ClipRegionTranslatedBounds = RectangleF.Empty;
                return _ClipRegionTranslated = TranslateRegion(ClipRegion, Location, RotationAngle, Image.Size);
            }
        }

        private RectangleF _ClipRegionTranslatedBounds = RectangleF.Empty;
        public virtual RectangleF GetClipRegionTranslatedBounds(Graphics gr, PointF offset)
        {
            if (ClipRegionTranslated == null)
                return RectangleF.Empty;
            if (_ClipRegionTranslatedBounds.IsEmpty)
                _ClipRegionTranslatedBounds = ClipRegionTranslated.GetBounds(gr);
            if(offset.IsEmpty)
                return _ClipRegionTranslatedBounds;
            var rect = _ClipRegionTranslatedBounds;
            rect.Offset(offset);
            return rect;
        }

        Region? _ClipEdgesRegionTranslated;
        /**
         * ClipEdgesRegionTranslated
         * Returns ClipEdgesRegion.Clone().Translate(Location.X, Location.Y);
        */
        [Browsable(false)]
        public virtual Region? ClipEdgesRegionTranslated
        {
            get
            {
                if (_ClipEdgesRegionTranslated != null || Image == null || ClipEdgesRegion == null)
                    return _ClipEdgesRegionTranslated;
                return _ClipEdgesRegionTranslated = TranslateRegion(ClipEdgesRegion, Location, RotationAngle, Image.Size);
            }
        }

        public static Region? TranslateRegion(Region region, PointF location, float Rotation, Size imageSize)
        {
            if (location.IsEmpty && Rotation == 0F)
                return region;
            region = region.Clone();
            Matrix transformMatrix = new Matrix();
            transformMatrix.Translate(location.X, location.Y);
            if (Rotation != 0F)
            {
                transformMatrix.RotateAt(Rotation, new PointF(imageSize.Width / 2F, imageSize.Height / 2F));
            }
            region.Transform(transformMatrix);

            return region;
        }
        #endregion

        #region Collide
        /**
         * Collide
         * 
         * <returns>Offseted Location</returns>
         */
        public virtual PointF Collide(PointF offset)
        {
            if (this.Consumer is Images)
                return ((Images)this.Consumer).CollideItem(this, offset);

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