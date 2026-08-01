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
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MED.Imaging
{
    public class Mover : Background, IImageProvider, IImageMove, IImageCollidable
    {
        public Mover(string name = "Mover", Performance performance = null, Control invokeHandler = null, IImageConsumer imageConsumer = null, bool isAsynchrone = true)
        : base(name, performance, invokeHandler, imageConsumer, isAsynchrone)
        {
            FPSMax = 0;
            if( DateTime.Now.Ticks%2==0)
                Direction = new(1, 0);
            else
                Direction = new(0, 1);
        }

        #region Properties

        public virtual float Speed { get; set; } = 0F;
        public virtual float SpeedMax { get; set; }
        public virtual float Density { get; set; } = 1F;
        public virtual PointF Direction { get; set; }
        public virtual float RotationSpeed { get; set; }

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

                location.X += Direction.X * Speed * duration;
                location.Y += Direction.Y * Speed * duration;

                if (RotationSpeed != 0F)
                    Rotation += RotationSpeed * duration;

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

            base.Start();
        }
        #endregion

        #region Settings
        public override void LoadSettings(ProcessSettings settings = null, string fileName = "")
        {
            base.LoadSettings(settings, fileName);

            Speed = (float)settings.GetValue("Speed", Speed);
            SpeedMax = (float)settings.GetValue("SpeedMax", SpeedMax);
            Density = (float)settings.GetValue("Density", Density);
            RotationSpeed = (float)settings.GetValue("RotationSpeed", RotationSpeed);
        }
        public override JsonObject SaveProcess(JsonObject node = null)
        {
            node = base.SaveProcess(node);
            node.Add("Speed", Speed);
            node.Add("SpeedMax", SpeedMax);
            node.Add("Density", Density);
            node.Add("RotationSpeed", RotationSpeed);
            return node;
        }
        #endregion
    }
}