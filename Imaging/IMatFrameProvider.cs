using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Emgu.CV;

namespace MED
{
    public interface IMatFrameProvider: IProvider
    {

        [Browsable(false)]
        Size ImageSizeMax { get; set; }

        bool HasFrameChanged { get; set; }
        Mat Frame { get; set; }

        delegate void FrameChangedDelegate(IMatFrameProvider sender);

        void InvokeFrameChanged(IMatFrameProvider sender = null);
    }
}
