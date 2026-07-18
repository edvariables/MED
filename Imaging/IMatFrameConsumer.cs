using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MED
{
    public interface IMatFrameConsumer: IConsumer
    {
        void FrameChanged(IMatFrameProvider sender, EventArgs e);
    }
}
