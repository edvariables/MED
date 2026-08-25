using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MED.Imaging
{
    /**
     * interface IImageInteractor : IImageMover
     * <summary>Image that interacts with others</summary>
     * */
    public interface IImageInteractor : IImageProvider
    {
        float Mass { get; }

        void InteractWithItems(List<IProcess> items);

        bool InteractWithItem(IImageInteractor item2);
    }
}
