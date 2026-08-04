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
    public class Mover : ImageSourced, IImageProvider, IImageCollidable
    {
        public Mover(string name = "Mover", Performance? performance = null, Control? invokeHandler = null, IImageConsumer? imageConsumer = null, bool isAsynchrone = true)
        : base(name, performance, invokeHandler, imageConsumer, isAsynchrone)
        {
            FPSMax = 0;
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

        long _LocationTime = 0;
        [Browsable(true)]
        public override System.Drawing.PointF Location
        {
            get
            {
                var location = base.Location;
                if (ProcessState != ThreadState.Running
                    || Speed == 0)
                    return location;
                long now = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                long duration = now - _LocationTime;
                _LocationTime = now;
                if (duration > 1000 || duration == 0)
                    return location;

                location.X += Velocity.X * duration;
                location.Y += Velocity.Y * duration;

                if (RotationSpeed != 0F)
                    Rotation = (float)((Rotation + RotationSpeed * duration) % 360F);

                return Location = location;
            }
            set
            {
                _ClipRegionTranslated = null;
                base.Location = value;
            }
        }

        Vector2 _LocationVector;
        [Browsable(false)]
        public virtual Vector2 LocationVector
        {
            get
            {
                if (_LocationVector.Equals(Vector2.Zero))
                    return _LocationVector = new Vector2(base.Location.X, base.Location.Y);
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
                    return _DirectionVector = new Vector2(Direction.X, Direction.Y);
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
                    return _Velocity = new(Direction.X * Speed, Direction.Y * Speed);
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
                    return _VelocityVector = new Vector2(Velocity.X, Velocity.Y);
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
                if (_ClipRegionTranslated != null || Image == null || ClipRegion==null)
                    return _ClipRegionTranslated;
                return _ClipRegionTranslated = ImagesCollider.ClipRegionTranslated(ClipRegion, Location, Rotation, Image.Size);
            }
        }

        #endregion

        #region Process
        public override void Start()
        {
            Location = PointF.Empty;
            Rotation = 0F;

            RandomizeDirection();

            Image = null;

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

            Speed = (float)settings.GetValue("Speed", Speed);
            SpeedMax = (float)settings.GetValue("SpeedMax", SpeedMax);
            Mass = (float)settings.GetValue("Mass", Mass);
            RotationSpeedMax = (float)settings.GetValue("RotationSpeedMax", RotationSpeedMax);
        }
        public override JsonObject SaveProcess(JsonObject node = null)
        {
            node = base.SaveProcess(node);
            node.Add("Speed", Speed);
            node.Add("SpeedMax", SpeedMax);
            node.Add("Mass", Mass);
            node.Add("RotationSpeedMax", RotationSpeedMax);
            return node;
        }
        #endregion
    }
}