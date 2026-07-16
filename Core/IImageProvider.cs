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
        [Browsable(false)]
        public Size ImageSizeMax { get; set; }

        public bool HasImageChanged { get; set; }
        public Bitmap Image { get; set; }

        public void InvokeImageChanged(IImageProvider sender = null);
    }
}
