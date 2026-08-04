using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MED.Imaging
{
    public interface IImageCollidable : IImageMover
    {
        float Mass { get; }
    }
}
