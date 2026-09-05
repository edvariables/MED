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
    public class Attractor : ImageInteractor
    {
        //isAsynchrone = true
        public Attractor(string name = "Attractor", Performance? performance = null, Control? invokeHandler = null, IImageConsumer? imageConsumer = null, bool isAsynchrone = true)
            : base(name, performance, invokeHandler, imageConsumer, isAsynchrone)
        {
            ProcessIcon = ProcessIconDefault = "Object";
            ResetOnImageChanged = false;//self managed
        }


        [Category("Attractor")]
        [DefaultValue(1F)]
        public float InteractionStrenght { get; set; } = 1F;

        [Category("Attractor")]
        [DefaultValue(false)]
        public bool InteractionAsSquaredDistance { get; set; } = false;

        [Category("Attractor")]
        [DefaultValue(1F)]
        public override float Mass { get; set; } = 1F;

        [Category("Attractor")]
        [DefaultValue(360F)]
        public virtual float InteractionCone { get; set; } = 360F;

        /**
         * Rotation angle (angular direction) in degrees
         * */
        [Browsable(true)]
        [Category("Attractor")]
        public override float RotationAngle { get; set; }

        /**
         * Interaction angle added to RotationAngle in degrees
         * */
        [Browsable(true)]
        [Category("Attractor")]
        [Description("Interaction angle added to RotationAngle in degrees")]
        [DefaultValue(0F)]
        public virtual float InteractionConeAngleOffset { get; set; }

        [Browsable(true)]
        [TypeConverter(typeof(MED.Core.PointFTypeConverter))]
        public override System.Drawing.PointF Location { get => base.Location; set => base.Location = value; }

        /**
         * InteractWithItem
         */
        public override bool InteractWithItem(IImageInteractor item2)
        {
            if (ProcessState != ThreadState.Running || InteractionStrenght == 0F)
                return false;
            if (item2 == this || item2 is not ImageMover mover)
                return false;
            if (mover.SpeedMax == 0F || mover.Location.IsEmpty)
                return false;

            Vector2 interactVector = LocationCenter.ToVector2();
            var location2Center = ((ImageProcess)item2).LocationCenter;
            interactVector.X -= location2Center.X;
            interactVector.Y -= location2Center.Y;

            var interactionStrenght = InteractionStrenght;

            if (InteractionCone != 360F && InteractionCone != 0F)
            {
                var interactionCone = (float)(Math.PI * InteractionCone / 180F);
                var rotationAngle = (float)(Math.PI * RotationAngle / 180F);
                var interactionAngleOffset = (float)(Math.PI * InteractionConeAngleOffset / 180F);
                var toItemAngle = (float)Math.Atan2(interactVector.Y, interactVector.X);
                var angleDiff = (float)((toItemAngle - (rotationAngle + interactionAngleOffset) + Math.PI * 2) % (Math.PI * 2));
                //var item2LocationAngle = (float)((toItemAngle
                //                                - (rotationAngle + interactionAngleOffset)
                //                                + Math.PI * 2) % (Math.PI * 2))
                //                                + interactionCone / 2 ;
                //var interactionDirection = new Vector2((float)Math.Cos(RotationAngle), (float)Math.Sin(RotationAngle));
                if (angleDiff < -interactionCone / 2 || angleDiff > interactionCone / 2)
                //if (item2LocationAngle < 0 || item2LocationAngle > interactionCone)
                {

                    Performance?.Debug($"angleDiff : {angleDiff} vs {interactionCone}");
                    return false;
                }

                interactionStrenght *= 1F - Math.Abs(angleDiff) / (interactionCone / 2);

                Performance?.Debug($"interactionStrenght : {interactionStrenght},  angles : {angleDiff} < {interactionCone}");
            }

            var distance = interactVector.Length();
            interactVector = Vector2.Normalize(interactVector);
            if (InteractionAsSquaredDistance)
                interactVector *= interactionStrenght * (Mass + mover.Mass) / (distance * distance);
            else
                interactVector *= interactionStrenght * (Mass + mover.Mass) / (distance);

            var direction = mover.DirectionVector;
            direction += interactVector;
            mover.Speed *= direction.Length();

            direction = Vector2.Normalize(direction);
            mover.Direction = new(direction.X, direction.Y);

            return true;
        }

        #region Settings
        public override void LoadSettings(ProcessSettings? settings = null, string fileName = "")
        {
            base.LoadSettings(settings, fileName);
            if (settings == null && (settings = ProcessSettings) == null)
                return;
            Location = (PointF)(settings.GetValue("Location", Location) ?? Location);
            InteractionStrenght = (float)(settings.GetValue("InteractionStrenght", InteractionStrenght) ?? InteractionStrenght);
            InteractionCone = (float)(settings.GetValue("InteractionCone", InteractionCone) ?? InteractionCone);
            InteractionConeAngleOffset = (float)(settings.GetValue("InteractionConeAngleOffset", InteractionConeAngleOffset) ?? InteractionConeAngleOffset);
            InteractionAsSquaredDistance = (bool)(settings.GetValue("InteractionAsSquaredDistance", InteractionAsSquaredDistance) ?? InteractionAsSquaredDistance);
        }
        public override JsonObject SaveProcess(JsonObject? node = null)
        {
            node = base.SaveProcess(node);
            if (!Location.IsEmpty)
                node.Add("Location", Location.ToString());
            node.Add("InteractionStrenght", InteractionStrenght);
            node.Add("InteractionCone", InteractionCone);
            node.Add("InteractionConeAngleOffset", InteractionConeAngleOffset);
            node.Add("InteractionAsSquaredDistance", InteractionAsSquaredDistance);
            return node;
        }
        #endregion
    }
}
