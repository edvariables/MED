using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MED
{
    public class ImageProcessForm : ProcessForm, IImageConsumer
    {
        public ImageProcessForm()
        {

        }


        #region Image
        public PictureBox RenderPictureBox { get; set; }

        /**
         * Image
         * */
        public virtual void ImageChanged(IImageProvider sender)
        {
            if (this.Disposing || this.IsDisposed)
                return;

            if (RenderPictureBox != null)
                Render.RefreshImage(sender, RenderPictureBox, Performance);
        }
        #endregion
    }
}
