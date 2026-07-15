using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MED.Imaging
{
    public class MovingRegions : ImageProcess
    {
        public MovingRegions(string paramSection= "MovingRegions", StringBuilder progressMessage = null, Form formHandler = null, IImageConsumer imageConsumer = null, bool isAynchrone = false) 
            : base(paramSection, progressMessage, formHandler, imageConsumer, isAynchrone)
        {
        }


        public override Bitmap Image { 
            get => {
                if (!HasImageChanged)
                    return base.Image;
                if( HasImageChanged)
                {
                    Bitmap image = base.Image;

                    int x, y;

                    // Loop through the images pixels to reset color.
                    for (x = 0; x < image.Width; x++)
                    {
                        for (y = 0; y < image.Height; y++)
                        {
                            Color pixelColor = image.GetPixel(x, y);
                            Color newColor = Color.FromArgb(pixelColor.R, 0, 0);
                            image.SetPixel(x, y, newColor);
                        }
                    }
                    return image;
                }
            } 
            set => { base.Image = value; }
        }
    }
}
