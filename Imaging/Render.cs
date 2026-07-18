using DirectShowLib;
using Emgu.CV;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MED
{
    public class Render(string name = "Render", Performance performance = null, Form formHandler = null, IImageConsumer imageConsumer = null)
        : ImageProcess(name, performance, formHandler, imageConsumer)
    {

        #region Settings

        [Browsable(true)]
        public bool KeepRenderRatio { get; set; }
        [Browsable(true)]
        public bool ResizeToRenderSize { get; set; }

        #endregion

        public override void Start()
        {
            base.Start();

            ProcessState = System.Threading.ThreadState.Running;
        }

        public PictureBox RenderPictureBox { get; set; }

        /**
         * Image
         * */
        public override void ImageChanged(IImageProvider sender, EventArgs e)
        {
            if (this.Disposing || this.IsDisposed)
                return;
            base.ImageChanged(sender, e);

            if (RenderPictureBox != null)
                Render.RefreshImage(this, RenderPictureBox, Performance, e);
        }

        public static void RefreshImage(IImageProvider sender, PictureBox renderPictureBox, Performance performance, EventArgs e)
        {
            if (sender is IProcess && !(sender as IProcess).IsRunning)
                return;
            //performance.Step("RefreshImage call stack :\n" + Environment.StackTrace);
            if (renderPictureBox != null && sender.Image != null)
            {
                performance.Resume("RefreshImage", true);
                try
                {
                    renderPictureBox.Image = ResizeImage(sender);
                }
                catch (Exception ex)
                {
                    performance.Alert(ex.ToString());
                }
                performance.Pause();
            }
        }
        public static Bitmap ResizeImage(IImageProvider sender)
        {
            if (!(sender is Render) || sender.Image == null)
                return sender.Image;
            Render render = (Render)sender;
            var image = sender.Image;
            if (render.RenderPictureBox == null)
                return image;
            if (render.ResizeToRenderSize)
                if (render.KeepRenderRatio)
                    image = new Bitmap(image, render.RenderPictureBox.Size);
                else
                {
                    var ratio = render.RenderPictureBox.Size.Width / render.RenderPictureBox.Size.Height;
                    var size = new Size(image.Width * ratio, image.Height * ratio);
                    image = new Bitmap(image, size);
                }
            return image;
        }
    }


}
