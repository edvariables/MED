using Emgu.CV;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MED
{
    /**
     * interface IImageProvider : IProvider
     * <summary>A process that is able to provide an image</summary>
     * */
    public interface IImageProvider : IProvider
    {

        [Browsable(false)]
        Size ImageSizeMax { get; }

        [Browsable(false)]
        Size ImageSizeMin { get; }

        Bitmap? Image { get; }

        [Browsable(false)]
        System.Drawing.Region? ClipRegion { get; }

        [Browsable(false)]
        GraphicsPath? ClipPath { get; }

        System.Drawing.PointF Location { get; set; }
        
        float Rotation { get; }


        List<IImageProvider> ImageProviders { get; set; }

        delegate void ImageChangedDelegate(IImageProvider sender, EventArgs e);

        void InvokeImageChanged(IImageProvider? sender = null, EventArgs? e = null);
    }
}
