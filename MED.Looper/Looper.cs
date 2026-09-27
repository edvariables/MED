using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MED.Looper
{
    public class Looper: MED.Imaging.Images
    {
        public Looper(string name = "Looper", Performance? performance = null, Control? invokeHandler = null, IConsumer? consumer = null, bool isAsynchrone = true)
            : base(name, performance, invokeHandler, consumer, isAsynchrone)
        {
            ProcessIcon = ProcessIconDefault = "refresh";
        }
    }
}
