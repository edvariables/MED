using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MED.Imaging
{
    public interface IImageMove
    {
        float SpeedMax { get; }
        float Density { get; }
        SizeF Speed { get; set; }
        float RotationSpeed { get; }
    }
}
