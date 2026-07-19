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
    public class Render : ImageProcess
    {
        public Render(string name = "Render", Performance performance = null, Control invokeHandler = null, IImageConsumer imageConsumer = null, bool isAsynchrone = false)
            : base(name, performance, invokeHandler, imageConsumer, isAsynchrone)
        {
            ImageIsProvided = true;
            ResetOnImageChanged = true;
            Centered = true;

            if (invokeHandler is PictureBox)
                RenderPictureBox = (PictureBox)invokeHandler;
        }

        #region Settings

        [Browsable(true)]
        public bool KeepRenderRatio { get; set; }
        [Browsable(true)]
        public bool ResizeToRenderSize { get; set; }
        [Browsable(true)]
        public bool Centered { get; set; }

        public override void LoadSettings(bool loadChildren = true)
        {
            base.LoadSettings(loadChildren);

            Centered = (bool)Core.Settings.GetValue("Centered", Name, Centered);
            KeepRenderRatio = (bool)Core.Settings.GetValue("KeepRenderRatio", Name, KeepRenderRatio);
            ResizeToRenderSize = (bool)Core.Settings.GetValue("ResizeToRenderSize", Name, ResizeToRenderSize);
        }
        public override void SaveSettings(bool saveChildren = true)
        {
            Core.Settings.SetValue("Centered", Name, Centered);
            Core.Settings.SetValue("KeepRenderRatio", Name, KeepRenderRatio);
            Core.Settings.SetValue("ResizeToRenderSize", Name, ResizeToRenderSize);

            base.SaveSettings(saveChildren);
        }
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
                Render.RefreshRender(this, RenderPictureBox, Performance, e);
        }

        public override Bitmap Image
        {
            get => base.Image == null ? WaitingImage : base.Image;
            set => base.Image = value;
        }

        /**
         * GetImage returns EmptyImage
         * 
         */
        //public override Bitmap GetImage(IImageProvider provider = null)
        //{
        //    Performance.Debug($"Render.GetImage ImageIsProvided={ImageIsProvided}, " + (provider == null ? "<null>" : "provider") + " / " + (ImageProvider == null ? "<null>" : $"ImageProvider {ImageProvider.Image}"));

        //    return base.GetImage(provider)/*WaitingImage*/;
        //}

        /**
         * RefreshRender
         * */
        public static void RefreshRender(IImageProvider sender, PictureBox renderPictureBox, Performance performance, EventArgs e)
        {
            if (sender is IProcess && !(sender as IProcess).IsRunning)
                return;
            //performance.Step("RefreshRender call stack :\n" + Environment.StackTrace);
            if (renderPictureBox != null && sender.Image != null)
            {
                performance.Resume("RefreshRender", true);
                try
                {
                    renderPictureBox.Image = ResizeImage(sender);
                }
                catch (Exception ex)
                {
                    performance.Error("RefreshRender", ex);
                }
                finally
                {
                    performance.Pause();
                }
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
            else if (render.Centered)
            {
                var source = image;
                image = new Bitmap(render.RenderPictureBox.Size.Width, render.RenderPictureBox.Size.Height, image.PixelFormat);
                Graphics graphics = Graphics.FromImage(image);

                graphics.DrawImageUnscaled(source, (image.Width - source.Width) / 2, (image.Height - source.Height) / 2);

                graphics.Dispose();
            }
            return image;
        }
    }


}
