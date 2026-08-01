using Emgu.CV;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MED
{
    public interface IImageProvider : IProvider
    {

        [Browsable(false)]
        Size ImageSizeMax { get; }

        Bitmap Image { get; }
        
        System.Drawing.Region? ClipRegion { get; }
        
        System.Drawing.PointF Location { get; }
        
        float Rotation { get; }


        List<IImageProvider> ImageProviders { get; set; }

        delegate void ImageChangedDelegate(IImageProvider sender, EventArgs e);

        void InvokeImageChanged(IImageProvider sender = null, EventArgs e = null);
    }
}
