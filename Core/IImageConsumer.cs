using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MED
{
    public interface IImageConsumer
    {
        public void ImageChanged(IImageProvider sender);
    }
}
