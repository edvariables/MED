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

        [Category("Propulsor")]
        [Description("Propulsion acceleration")]
        [DefaultValue(0.1F)]
        public float PropulsionAcceleration { get; set; } = 0.1F;

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
                var propulsionFactor = Propulsion * PropulsorStrenght/1000F;
                var direction = VelocityVector - propulsionVector * propulsionFactor;
                Speed = direction.Length() *1000;
                //if (Speed < 0F)
                //    Speed *= -1F;

                Direction = new(Vector2.Normalize(direction));

                Propulsion *= (1F + PropulsionAcceleration);
                if (Propulsion == float.NegativeInfinity || Propulsion == float.PositiveInfinity)
                    Propulsion = 0F;
            }
            base.Move(elapsedTime);
        }

        #region Settings
        public override void LoadSettings(ProcessSettings? settings = null, string fileName = "")
        {
            base.LoadSettings(settings, fileName);
            if (settings == null && (settings = ProcessSettings) == null)
                return;
            PropulsionAcceleration = (float)(settings.GetValue(nameof(PropulsionAcceleration), PropulsionAcceleration) ?? PropulsionAcceleration);
            PropulsorStrenght = (float)(settings.GetValue(nameof(PropulsorStrenght), PropulsorStrenght) ?? PropulsorStrenght);
            RotationAngleOffset = (float)(settings.GetValue(nameof(RotationAngleOffset), RotationAngleOffset) ?? RotationAngleOffset);
        }
        public override JsonObject SaveProcess(JsonObject? node = null)
        {
            node = base.SaveProcess(node);
            node.Add(nameof(PropulsionAcceleration), PropulsionAcceleration);
            node.Add(nameof(PropulsorStrenght), PropulsorStrenght);
            node.Add(nameof(RotationAngleOffset), RotationAngleOffset);
            return node;
        }
        #endregion
    }
}
