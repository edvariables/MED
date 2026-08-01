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
    public class Mover : Background, IImageProvider, IImageMove
    {
        public Mover(string name = "Mover", Performance performance = null, Control invokeHandler = null, IImageConsumer imageConsumer = null, bool isAsynchrone = true)
        : base(name, performance, invokeHandler, imageConsumer, isAsynchrone)
        {
            FPSMax = 0;
        }

        #region Properties

        public virtual float SpeedMax { get; set; }
        public virtual float Density { get; set; }
        public virtual SizeF Speed { get; set; }
        public virtual float RotationSpeed { get; set; }

        long _LocationTime = 0;
        System.Drawing.PointF _Location = System.Drawing.Point.Empty;
        public override System.Drawing.PointF Location
        {
            get
            {
                if (ProcessState != ThreadState.Running
                    || Speed.IsEmpty)
                    return _Location;
                long now = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                long duration = now - _LocationTime;
                _LocationTime = now;
                if (duration > 1000 || duration == 0)
                    return _Location;

                _Location.X += Speed.Width * duration;
                _Location.Y += Speed.Height * duration;

                if (RotationSpeed != 0F)
                    Rotation += RotationSpeed * duration;

                return _Location;
            }
            set => _Location = value;
        }

        public override void Start()
        {
            Location = PointF.Empty;
            Rotation = 0F;

            base.Start();
        }

        public override void LoadSettings(ProcessSettings settings = null, string fileName = "")
        {
            base.LoadSettings(settings, fileName);

            Speed = (SizeF)settings.GetValue("Speed", Speed);
            SpeedMax = (float)settings.GetValue("SpeedMax", SpeedMax);
            Density = (float)settings.GetValue("Density", Density);
            RotationSpeed = (float)settings.GetValue("RotationSpeed", RotationSpeed);
        }
        public override JsonObject SaveProcess(JsonObject node = null)
        {
            node = base.SaveProcess(node);
            node.Add("Speed", Parser.ObjectToString(Speed));
            node.Add("SpeedMax", SpeedMax);
            node.Add("Density", Density);
            node.Add("RotationSpeed", RotationSpeed);
            return node;
        }
        #endregion
    }
}