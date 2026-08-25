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
    public class Gravity : ImageInteractor
    {
        //isAsynchrone = true
        public Gravity(string name = "Gravity", Performance? performance = null, Control? invokeHandler = null, IImageConsumer? imageConsumer = null, bool isAsynchrone = true)
            : base(name, performance, invokeHandler, imageConsumer, isAsynchrone)
        {
            ProcessIcon = ProcessIconDefault = "Object";
            ResetOnImageChanged = false;//self managed
        }

        [Category("Attractor")]
        [DefaultValue(6.67430F)]
        public float GravityConstant { get; set; } = 6.67430F;

        [Category("Attractor")]
        [DefaultValue(false)]
        public bool Horizontal { get; set; } = false;

        [Category("Attractor")]
        [DefaultValue(0f)]
        public override float Mass { get; set; } = 0f;

        /**
         * CollideItem
         * 
         * <returns>Offseted item2.Location</returns>
         */
        public override bool InteractWithItem(IImageInteractor item2)
        {
            if (ProcessState != ThreadState.Running || GravityConstant == 0F)
                return false;
            if (item2 == this || item2 is not ImageMover mover)
                return false;
            if (mover.SpeedMax == 0F || mover.Location.IsEmpty)
                return false;

            var direction = mover.DirectionVector;
            Vector2 gravityVector = new();
            if (Horizontal)
                gravityVector.X = 1F;
            else
                gravityVector.Y = 1F;
            gravityVector *= GravityConstant * mover.Mass;
            direction += gravityVector;
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
            GravityConstant = (float)(settings.GetValue("GravityConstant", GravityConstant) ?? GravityConstant);
            Horizontal = (bool)(settings.GetValue("Horizontal", Horizontal) ?? Horizontal);
        }
        public override JsonObject SaveProcess(JsonObject? node = null)
        {
            node = base.SaveProcess(node);
            node.Add("GravityConstant", GravityConstant);
            node.Add("Horizontal", Horizontal);
            return node;
        }
        #endregion
    }
}
