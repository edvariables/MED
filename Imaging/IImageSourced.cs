using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Windows.Forms.Design;

namespace MED.Imaging
{
    /**
     * interface IImageSourced: IImageProvider
     * <summary>Image from a file or something else that is not an other process</summary>
     * */
    public interface IImageSourced: IImageProvider
        {
            [Browsable(true)]
            [EditorAttribute(typeof(FileNameEditor), typeof(UITypeEditor))]
            [ReadOnly(false)]
            string ImageFile { get; set; }

            Bitmap? GetImageFromSource(IImageProvider? provider = null);
        }
}
