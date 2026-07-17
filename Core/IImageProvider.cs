using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MED
{
    public interface IImageProvider
    {
        bool IsAsynchrone { get; set; }

        [Browsable(false)]
        Size ImageSizeMax { get; set; }

        bool HasImageChanged { get; set; }
        Bitmap Image { get; set; }

        void InvokeImageChanged(IImageProvider sender = null);
    }
}
