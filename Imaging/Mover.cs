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
    public class Mover : Background, IImageProvider, IImageCollidable
    {
        public Mover(string name = "Mover", Performance performance = null, Control invokeHandler = null, IImageConsumer imageConsumer = null, bool isAsynchrone = true)
        : base(name, performance, invokeHandler, imageConsumer, isAsynchrone)
        {
            FPSMax = 0;

            Random rnd = new Random((int)(DateTime.Now.Ticks % int.MaxValue));

            Vector2 vector = new Vector2((float)rnd.NextDouble(), (float)rnd.NextDouble());
            vector = Vector2.Normalize(vector);
            Direction = new(vector.X, vector.Y);
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
                Direction = _Direction;//Reset Velocity
            }
        }
        PointF _Direction;
        public virtual PointF Direction
        {
            get => _Direction;
            set
            {
                _Direction = value;
                Velocity = new(value.X * Speed, value.Y * Speed);
            }
        }
        public virtual PointF Velocity { get; set; }

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

                return base.Location = location;
            }
            set
            {
                base.Location = value;
                _ClipRegionTranslated = null;
            }
        }

        Region? _ClipRegionTranslated;
        /**
         * 
         * Returns ClipRegion.Clone().Translate(Location.X, Location.Y);
        */
        public virtual Region? ClipRegionTranslated
        {
            get
            {
                //That does not work...
                //if (_ClipRegionTranslated != null)
                //    return _ClipRegionTranslated;
                return _ClipRegionTranslated = ImagesCollider.ClipRegionTranslated(ClipRegion, Location);
            }
        }

        #endregion

        #region Process
        public override void Start()
        {
            Location = PointF.Empty;
            Rotation = 0F;
            Image = null;

            base.Start();
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