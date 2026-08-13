using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace MED.Imaging
{
    /**
     * interface IImageMover: IImageProvider
     * <summary>Image as a physic object</summary>
     * */
    public interface IImageMover: IImageCollider
    {
        void Move(long elapsedTime);

        float SpeedMax { get; }
        float Speed { get; }
        float Speed_msec{ get; set; }

        PointF Direction { get; set; }
        Vector2 DirectionVector { get; }
        PointF Velocity { get; set; }
        Vector2 VelocityVector { get; }

        float RotationSpeed { get; set; }
        float RotationSpeedMax { get; set; }
    }
}
