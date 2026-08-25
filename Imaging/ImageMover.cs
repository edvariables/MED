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
     * class Mover : ImageCollidable, IImageMover
     * <summary>Image as a physical object that can move, rotate and collide</summary>
     * */
    public class ImageMover(string name = "Mover", Performance? performance = null, Control? invokeHandler = null, IImageConsumer? imageConsumer = null, bool isAsynchrone = true) 
                : ImageCollider(name, performance, invokeHandler, imageConsumer, isAsynchrone), IImageMover
    {
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
            if (offset.IsEmpty)
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

        #region Properties

        /**
         * Speed
         * <summary>Vitesse mesurée en pixel par seconde</summary>
         * */
        [Category("Mover")]
        public virtual float Speed
        {
            get => _Speed_msec * 1000;
            set => Speed_msec = value / 1000;
        }

        [Category("Mover")]
        public virtual float SpeedMax { get; set; }

        float _RotationSpeed = 0F;
        [Category("Mover")]
        public virtual float RotationSpeed
        {
            get => _RotationSpeed;
            set
            {
                if (value > RotationSpeedMax)
                    _RotationSpeed = RotationSpeedMax;
                else if (value < -RotationSpeedMax)
                    _RotationSpeed = -RotationSpeedMax;
                else
                    _RotationSpeed = value;
            }
        }
        [Category("Mover")]
        public virtual float RotationSpeedMax { get; set; } = 0.5F;

        float _Speed_msec = 0F;
        /**
         * Speed
         * <summary>Vitesse mesurée en pixel par milliseconde</summary>
         * */
        [Browsable(false)]
        public float Speed_msec
        {
            get => _Speed_msec;
            set
            {
                if (SpeedMax == 0)
                    _Speed_msec = value;
                else
                    _Speed_msec = Math.Min(value, SpeedMax / 1000);
                Direction = _Direction;//Reset Velocity and Vector
            }
        }

        [Browsable(true)]
        [ReadOnly(false)]
        [Category("Image")]
        [TypeConverter(typeof(MED.Core.PointFTypeConverter))]

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

        public virtual void Move(long elapsedTime)
        {
            if (ProcessState != ThreadState.Running
                || Speed == 0)
                return;

            var location = Collide(new PointF(Velocity.X * elapsedTime, Velocity.Y * elapsedTime));

            if (RotationSpeed != 0F)
                RotationAngle += (float)((RotationSpeed * elapsedTime) % 360F);

            Location = location;
            //Performance?.Debug($"Move sets Location = {Location}");
        }

        [Browsable(false)]
        public override float RotationAngle
        {
            get => base.RotationAngle;
            set
            {
                _RotationVector = Vector2.Zero;
                _ClipRegionTranslated = null;
                base.RotationAngle = value;
            }
        }

        Vector2 _RotationVector;
        /**
         * Rotation angle as a vector (cos, -sin) * RotationSpeed
         * <summary>Rotation angle as a vector (cos, -sin)</summary>
         * */
        [Browsable(false)]
        public virtual Vector2 RotationVector
        {
            get
            {
                if (_RotationVector.Equals(Vector2.Zero))
                    return _RotationVector = new(RotationSpeed * (float)Math.Cos(base.RotationAngle), -RotationSpeed * (float)Math.Sin(base.RotationAngle));
                return _RotationVector;
            }
            private set { _RotationVector = value; }
        }

        PointF _Direction;
        [Category("Mover")]
        public virtual PointF Direction
        {
            get => _Direction;
            set
            {
                if (float.IsInfinity(value.X) || float.IsInfinity(value.Y) || float.IsNaN(value.X))
                    RandomizeDirection();
                else
                    _Direction = value;
                DirectionVector = Vector2.Zero;
            }
        }

        Vector2 _DirectionVector;
        [Browsable(false)]
        public virtual Vector2 DirectionVector
        {
            get
            {
                if (_DirectionVector.Equals(Vector2.Zero))
                    return _DirectionVector = Direction.ToVector2();
                return _DirectionVector;
            }
            private set
            {
                Velocity = PointF.Empty;
                _DirectionVector = value;
            }
        }
        public void RandomizeDirection()
        {
            if (SpeedMax != 0)
            {
                Random rnd = new Random((int)(DateTime.Now.Ticks % int.MaxValue));
                Vector2 vector = new Vector2((float)rnd.NextDouble(), (float)rnd.NextDouble());
                vector = Vector2.Normalize(vector);
                Direction = new(vector.X, vector.Y);
            }
        }

        PointF _Velocity;
        [Category("Mover")]
        public virtual PointF Velocity
        {
            get
            {
                if (_Velocity.IsEmpty)
                    return _Velocity = new(Direction.X * Speed_msec, Direction.Y * Speed_msec);
                return _Velocity;
            }
            set
            {
                _Velocity = value;
                _VelocityVector = Vector2.Zero;
            }
        }

        Vector2 _VelocityVector;
        [Browsable(false)]
        public virtual Vector2 VelocityVector
        {
            get
            {
                if (_VelocityVector.Equals(Vector2.Zero))
                    return _VelocityVector = Velocity.ToVector2();
                return _VelocityVector;
            }
            private set { _VelocityVector = value; }
        }
        #endregion

        #region Process
        public override void Start()
        {
            Location = PointF.Empty;
            RotationAngle = 0F;

            RandomizeDirection();

            base.Start();
        }
        #endregion

        #region Settings
        public override void LoadSettings(ProcessSettings? settings = null, string fileName = "")
        {
            base.LoadSettings(settings, fileName);
            if (settings == null && (settings = ProcessSettings) == null)
                return;
            Speed = (float)(settings.GetValue("Speed", Speed) ?? Speed);
            SpeedMax = (float)(settings.GetValue("SpeedMax", SpeedMax) ?? SpeedMax);
            RotationSpeedMax = (float)(settings.GetValue("RotationSpeedMax", RotationSpeedMax) ?? RotationSpeedMax);
        }
        public override JsonObject SaveProcess(JsonObject? node = null)
        {
            node = base.SaveProcess(node);
            node.Add("Speed", Speed);
            node.Add("SpeedMax", SpeedMax);
            node.Add("RotationSpeedMax", RotationSpeedMax);
            return node;
        }
        #endregion


        public override Dictionary<string, object> UndoModeSaveProperties()
        {
            Dictionary<string, object> dic = base.UndoModeSaveProperties();
            if (!(Speed == 0F && Location.IsEmpty))//debug
            {
                dic.Add("Speed", Speed);
                dic.Add("RotationAngle", RotationAngle);
                dic.Add("RotationSpeed", RotationSpeed);
                dic.Add("Direction", Direction);
                dic.Add("Location", Location);

                //Performance?.Sub("Stack").Debug($"Stack Location = {Location}");
            }
            return dic;
        }
        public override Dictionary<string, object>? Undo(int length = 1)
        {
            var dic = base.Undo(length);
            if (dic != null && dic.Count > 0 && dic.ContainsKey("Location"))
            {
                Performance?.Sub("Stack").Debug($"Restored Location = {dic["Location"]}");
            }
            return dic;
        }
    }
}