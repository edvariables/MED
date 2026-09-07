using DirectShowLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace MED.Imaging
{
    public class Propulsor : ImageMover
    {
        //isAsynchrone = true
        public Propulsor(string name = "Propulsor", Performance? performance = null, Control? invokeHandler = null, IConsumer? consumer = null, bool isAsynchrone = true)
            : base(name, performance, invokeHandler, consumer, isAsynchrone)
        {
            ProcessIcon = ProcessIconDefault = "Object";
            ResetOnImageChanged = false;//self managed
        }


        [Category("Propulsor")]
        [Description("Power factor in propultion")]
        [DefaultValue(1F)]
        public float PropulsorStrenght { get; set; } = 1F;

        /**
         * Interaction angle added to RotationAngle in degrees
         * */
        [Browsable(true)]
        [Category("Propulsor")]
        [Description("Angle added to RotationAngle in degrees")]
        [DefaultValue(0F)]
        public virtual float RotationAngleOffset { get; set; }

        [Category("Propulsor")]
        [DefaultValue(0F)]
        public float Propulsion { get; set; } = 0F;

        public override void Move(long elapsedTime)
        {
            if (Propulsion != 0F)
            {
                double radians = (float)(Math.PI * (RotationAngle + RotationAngleOffset) / 180F);
                var propulsionVector = new Vector2((float)Math.Cos(radians), (float)Math.Sin(radians));
                var propulsionFactor = Propulsion * PropulsorStrenght;
                var direction = VelocityVector * elapsedTime - propulsionVector * Math.Abs(propulsionFactor);
                Speed += propulsionFactor;
                //if (Speed < 0F)
                //    Speed *= -1F;

                Direction = new(Vector2.Normalize(direction));
            }
            base.Move(elapsedTime);
        }

        #region Settings
        public override void LoadSettings(ProcessSettings? settings = null, string fileName = "")
        {
            base.LoadSettings(settings, fileName);
            if (settings == null && (settings = ProcessSettings) == null)
                return;
            PropulsorStrenght = (float)(settings.GetValue("PropulsorStrenght", PropulsorStrenght) ?? PropulsorStrenght);
            RotationAngleOffset = (float)(settings.GetValue("RotationAngleOffset", RotationAngleOffset) ?? RotationAngleOffset);
        }
        public override JsonObject SaveProcess(JsonObject? node = null)
        {
            node = base.SaveProcess(node);
            if (!Location.IsEmpty)
                node.Add("Location", Location.ToString());
            node.Add("PropulsorStrenght", PropulsorStrenght);
            node.Add("RotationAngleOffset", RotationAngleOffset);
            return node;
        }
        #endregion
    }
}
