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
        public virtual float RotationSpeed { get; set; }
        public virtual float RotationSpeedMax { get; set; } = 0.5F;

        float _Speed = 0F;
        public virtual float Speed
        {
            get => _Speed;
            set
            {
                if (SpeedMax == 0)
                    _Speed = value;
                else
                    _Speed = Math.Min(value, SpeedMax);
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
                if (float.IsNaN(value.X))
                    return;
                _ClipRegionTranslated = null;
                if (base.Location != value)
                    Performance?.Debug($"Location _setter {value}");
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
                Rotation = (float)((Rotation + RotationSpeed * elapsedTime) % 360F);

            Location = location;
            Performance?.Debug($"Move sets Location = {Location}");
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
        public override float Rotation
        {
            get => base.Rotation;
            set
            {
                _ClipRegionTranslated = null;
                base.Rotation = value;
            }
        }

        PointF _Direction;
        public virtual PointF Direction
        {
            get => _Direction;
            set
            {
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
                    return _Velocity = new(Direction.X * Speed / 1000, Direction.Y * Speed / 1000);
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
                return _ClipRegionTranslated = TranslateRegion(ClipRegion, Location, Rotation, Image.Size);
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
                return _ClipEdgesRegionTranslated = TranslateRegion(ClipEdgesRegion, Location, Rotation, Image.Size);
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
            Rotation = 0F;

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
        }
        public override JsonObject SaveProcess(JsonObject? node = null)
        {
            node = base.SaveProcess(node);
            node.Add("Speed", Speed);
            node.Add("SpeedMax", SpeedMax);
            node.Add("Mass", Mass);
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
                dic.Add("Rotation", Rotation);
                dic.Add("RotationSpeed", RotationSpeed);
                dic.Add("Direction", Direction);
                dic.Add("Location", Location);

                Performance?.Sub("Stack").Debug($"Stack Location = {Location}");
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