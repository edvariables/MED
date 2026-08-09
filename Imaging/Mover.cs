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
     * <summary>Image as a physic object that can move, rotate and collide</summary>
     * */
    public class Mover : ImageSourced, IImageProvider, IImageCollidable
    {
        public Mover(string name = "Mover", Performance? performance = null, Control? invokeHandler = null, IImageConsumer? imageConsumer = null, bool isAsynchrone = true)
        : base(name, performance, invokeHandler, imageConsumer, isAsynchrone)
        {
        }

        #region Properties

        public virtual float SpeedMax { get; set; }
        public virtual float Mass { get; set; } = 1F;
        float _RotationSpeed = 0F;
        public virtual float RotationSpeed { 
            get=> _RotationSpeed;
            set
            {
                if (value > RotationSpeedMax)
                    _RotationSpeed = RotationSpeedMax;
                else
                    _RotationSpeed = value;
            }
        }
        public virtual float RotationSpeedMax { get; set; } = 0.5F;

        /**
         * Speed
         * <summary>Vitesse mesurée en pixel par seconde</summary>
         * */
        public virtual float Speed
        {
            get => _Speed_msec * 1000;
            set => Speed_msec = value / 1000;
        }

        float _Speed_msec = 0F;
        /**
         * Speed
         * <summary>Vitesse mesurée en pixel par milliseconde</summary>
         * */
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

        public virtual void Move(long elapsedTime)
        {
            var location = Location;
            if (ProcessState != ThreadState.Running
                || Speed == 0)
                return;

            if (this.Consumer is Images)
                location = ((Images)this.Consumer).CollideItem(this, new PointF(Velocity.X * elapsedTime, Velocity.Y * elapsedTime));
            else
            {
                location.X += Velocity.X * elapsedTime;
                location.Y += Velocity.Y * elapsedTime;
            }
            if (RotationSpeed != 0F)
                RotationAngle = (float)((RotationAngle + RotationSpeed * elapsedTime) % 360F);

            Location = location;
            //Performance?.Debug($"Move sets Location = {Location}");
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

        [Browsable(false)]
        public override float RotationAngle
        {
            get => base.RotationAngle;
            set
            {
                _ClipRegionTranslated = null;
                _RotationVector = Vector2.Zero;
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

        /**
         * Surface friction in collision
         * 
         * */
        public virtual float SurfaceFriction { get; set; } = 0F;

        PointF _Direction;
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

        PointF _Velocity;
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

        Region? _ClipRegionTranslated;
        /**
         * 
         * Returns ClipRegion.Clone().Translate(Location.X, Location.Y);
        */
        [Browsable(false)]
        public virtual Region? ClipRegionTranslated
        {
            get
            {
                if (_ClipRegionTranslated != null || Image == null || ClipRegion == null)
                    return _ClipRegionTranslated;
                return _ClipRegionTranslated = TranslateRegion(ClipRegion, Location, RotationAngle, Image.Size);
            }
        }

        Region? _ClipEdgesRegionTranslated;
        /**
         * 
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

        #region Process
        public override void Start()
        {
            Location = PointF.Empty;
            RotationAngle = 0F;

            RandomizeDirection();

            base.Start();
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
        #endregion

        #region Settings
        public override void LoadSettings(ProcessSettings settings = null, string fileName = "")
        {
            base.LoadSettings(settings, fileName);

            Speed = (float)(settings.GetValue("Speed", Speed) ?? Speed);
            SpeedMax = (float)(settings.GetValue("SpeedMax", SpeedMax) ?? SpeedMax);
            Mass = (float)(settings.GetValue("Mass", Mass) ?? Mass);
            RotationSpeedMax = (float)(settings.GetValue("RotationSpeedMax", RotationSpeedMax) ?? RotationSpeedMax);
            SurfaceFriction = (float)(settings.GetValue("SurfaceFriction", SurfaceFriction) ?? SurfaceFriction);
        }
        public override JsonObject SaveProcess(JsonObject? node = null)
        {
            node = base.SaveProcess(node);
            node.Add("Speed", Speed);
            node.Add("SpeedMax", SpeedMax);
            node.Add("Mass", Mass);
            node.Add("RotationSpeedMax", RotationSpeedMax);
            node.Add("SurfaceFriction", SurfaceFriction);
            return node;
        }
        #endregion


        public override Dictionary<string, object> UndoModeSaveProperties()
        {
            Dictionary<string, object> dic = base.UndoModeSaveProperties();
            if (!(Speed == 0F && Location.IsEmpty))//debug
            {
                dic.Add("Speed", Speed);
                dic.Add("Rotation", RotationAngle);
                dic.Add("RotationSpeed", RotationSpeed);
                dic.Add("SurfaceFriction", SurfaceFriction);
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