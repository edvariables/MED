using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MED
{
    public interface IMatFrameConsumer: IConsumer
    {
        void OnFrameChanged(IMatFrameProvider sender, EventArgs e);
    }
}
