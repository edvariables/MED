using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MED
{
    public interface IImageConsumer: IConsumer
    {

        [Browsable(false)]
        Size ImageSizeMin { get; }
        void ImageChanged(IImageProvider sender, EventArgs e);
    }
}
