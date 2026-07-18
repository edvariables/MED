using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MED
{
    public interface IImageProvider: IProvider
    {

        [Browsable(false)]
        Size ImageSizeMax { get; set; }

        Bitmap Image { get; set; }

        delegate void ImageChangedDelegate(IImageProvider sender);

        void InvokeImageChanged(IImageProvider sender = null);
    }
}
