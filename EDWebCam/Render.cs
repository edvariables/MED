using DirectShowLib;
using Emgu.CV;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static MED.EDWebCam.Render;
using static MED.WebCam;

namespace MED.EDWebCam
{
    public class Render(string paramSection = "Render", Performance performance = null, Form formHandler = null, IImageConsumer imageConsumer = null)
        : ImageProcess(paramSection, performance, formHandler , imageConsumer)
    {
        public override void Start()
        {
            base.Start();

            ProcessState = System.Threading.ThreadState.Running;
            IsRunning = true;
        }

        public override Bitmap Image
        {
            get
            {
                if (ImageProvider == null)
                    return null;

                return ImageProvider.Image;
            }
            set
            {
                if (ImageProvider != null)
                    ImageProvider.Image = value;
            }
        }
    }
}
