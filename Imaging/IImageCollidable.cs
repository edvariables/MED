using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MED.Imaging
{
    /**
     * interface IImageCollidable : IImageMover
     * <summary>Image as a physic object</summary>
     * */
    public interface IImageCollidable : IImageMover
    {
        float Mass { get; }
    }
}
