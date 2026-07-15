using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MED
{
    public interface IImageProvider
    {
        public Size ImageSizeMax { get; set; }

        public bool HasImageChanged { get; set; }
        public Bitmap Image { get; set; }

        public void InvokeImageChanged(IImageProvider sender = null);
    }
}
