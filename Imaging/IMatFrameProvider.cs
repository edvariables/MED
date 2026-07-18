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

        Mat Frame { get; }

        delegate void FrameChangedDelegate(IMatFrameProvider sender, EventArgs e);

        void InvokeFrameChanged(IMatFrameProvider sender, EventArgs e);
    }
}
