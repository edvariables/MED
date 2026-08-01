using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MED.Imaging
{
    public interface IImageMove
    {

        System.Drawing.Region? ClipRegion { get; }
        System.Drawing.PointF Location { get; set; }
        float Rotation { get; }

        float SpeedMax { get; }
        SizeF Speed { get; set; }
        float RotationSpeed { get; }
    }
}
