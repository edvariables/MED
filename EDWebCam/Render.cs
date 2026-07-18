using DirectShowLib;
using Emgu.CV;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MED.EDWebCam
{
    public class Render(string paramSection = "Render", Performance performance = null, Form formHandler = null, IImageConsumer imageConsumer = null)
        : ImageProcess(paramSection, performance, formHandler , imageConsumer)
    {
        public override void Start()
        {
            base.Start();

            ProcessState = System.Threading.ThreadState.Running;
        }

    }
}
