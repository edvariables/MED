using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace MED.Imaging
{
    public interface IImageMove
    {

        System.Drawing.Region? ClipRegion { get; }
        System.Drawing.Region? ClipRegionTranslated { get; }
        System.Drawing.PointF Location { get; set; }
        float Rotation { get; }

        float SpeedMax { get; }
        float Speed { get; set; }
        PointF Direction { get; set; }
        PointF Velocity { get; set; }
        float RotationSpeed { get; set; }
        float RotationSpeedMax { get; set; }
    }
}
