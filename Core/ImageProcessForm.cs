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

        #region Settings


        [Browsable(true)]

        public virtual void SaveSettings()
        {
            base.SaveSettings();

            if (Processes != null)
                foreach (var proc in Processes)
                    if(proc is ImageProcess)
                    (proc as ImageProcess).SaveSettings();
        }
        #endregion


        #region Image

        /**
         * Image
         * */
        public virtual void ImageChanged(IImageProvider sender)
        {
            if (this.Disposing || this.IsDisposed)
                return;

            if (sender is ImageProcess)
                RefreshImage((ImageProcess)sender);
        }

        protected virtual void RefreshImage(ImageProcess sender)
        {
            if (!sender.IsRunning)
                return;
            //Render.Performance.Step("RefreshImage call stack :\n" + Environment.StackTrace);
            //sender.Performance.Resume("RefreshImage", true);
            //if (sender.Image != null)
            //{
            //    try
            //    {
            //        picRender.Image = (sender as Render).Image;
            //    }
            //    catch (Exception ex)
            //    {
            //        Render.Performance.Step(ex.ToString());
            //    }
            //}
            //sender.Performance.Pause();
        }
        #endregion
    }
}
