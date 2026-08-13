using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using static MED.Imaging.EmguMoving;

namespace MED.Imaging
{
    public class Gravity : Mover
    {
        //isAsynchrone = true
        public Gravity(string name = "Gravity", Performance? performance = null, Control? invokeHandler = null, IImageConsumer? imageConsumer = null, bool isAsynchrone = true)
            : base(name, performance, invokeHandler, imageConsumer, isAsynchrone)
        {
            ProcessIcon = ProcessIconDefault = "Object";
            ResetOnImageChanged = false;//self managed
        }

        public float GravityConstant { get; set; } = 6.67430F;
        public bool Horizontal { get; set; } = false;

        public override void Move(long elapsedTime)
        {
            if (ProcessState != ThreadState.Running || GravityConstant == 0F)
                return;

            if (this.Consumer is Images)
            {
                foreach (var item in ((Images)this.Consumer).Items)
                {
                    if (item == this || item is not IImageMover)
                        continue;
                    if (((IImageCollidable)item).SpeedMax == 0F || ((IImageCollidable)item).Location.IsEmpty)
                        continue;
                    
                    var location = ((IImageCollidable)item).Location;
                    if (Horizontal)
                        location.X += GravityConstant * ((IImageCollidable)item).Mass;
                    else
                        location.Y += GravityConstant * ((IImageCollidable)item).Mass;
                }
            }
        }
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

    }
}
