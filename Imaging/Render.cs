using DirectShowLib;
using Emgu.CV;
using MED.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Nodes;
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
                RenderImageControl = (PictureBox)invokeHandler;

            if(imageConsumer == null)
                RenderImageControl = (Control)invokeHandler;
        }

        #region Settings

        [Browsable(true)]
        public bool KeepRenderRatio { get; set; }
        [Browsable(true)]
        public bool ResizeToRenderSize { get; set; }
        [Browsable(true)]
        public bool Centered { get; set; }

        public override void LoadSettings(string fileName)
        {
            base.LoadSettings(fileName);

            Centered = (bool)ProcessSettings.GetValue("Centered", Centered);
            KeepRenderRatio = (bool)ProcessSettings.GetValue("KeepRenderRatio", KeepRenderRatio);
            ResizeToRenderSize = (bool)ProcessSettings.GetValue("ResizeToRenderSize", ResizeToRenderSize);
        }
        public override JsonObject SaveProcess(JsonObject node = null)
        {
            node = base.SaveProcess(node);

            node.Add("Centered", Centered);
            node.Add("KeepRenderRatio", KeepRenderRatio);
            node.Add("ResizeToRenderSize", ResizeToRenderSize);

            return node;
        }
        #endregion

        public override void Start()
        {
            base.Start();

            ProcessState = System.Threading.ThreadState.Running;
        }

        public Control RenderImageControl { get; set; }

        /**
         * Image
         * */
        public override void ImageChanged(IImageProvider sender, EventArgs e)
        {
            if (this.Disposing || this.IsDisposed)
                return;
            base.ImageChanged(sender, e);

            if (RenderImageControl != null)
                Render.RefreshRender(this, RenderImageControl, Performance, e);
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
        public static void RefreshRender(IImageProvider sender, Control renderImageControl, Performance performance, EventArgs e)
        {
            if (sender is IProcess && !(sender as IProcess).IsRunning)
                return;
            //performance.Step("RefreshRender call stack :\n" + Environment.StackTrace);
            if (renderImageControl != null && sender.Image != null)
            {
                performance.Resume("RefreshRender", true);
                try
                {
                    if(renderImageControl is PictureBox)
                        (renderImageControl as PictureBox).Image = ResizeImage(sender);
                    else
                        renderImageControl.BackgroundImage = ResizeImage(sender);
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
            if (render.RenderImageControl == null)
                return image;
            if (render.ResizeToRenderSize)
                if (render.KeepRenderRatio)
                    image = new Bitmap(image, render.RenderImageControl.Size);
                else
                {
                    var ratio = render.RenderImageControl.Size.Width / render.RenderImageControl.Size.Height;
                    var size = new Size(image.Width * ratio, image.Height * ratio);
                    image = new Bitmap(image, size);
                }
            else if (render.Centered)
            {
                var source = image;
                image = new Bitmap(render.RenderImageControl.Size.Width, render.RenderImageControl.Size.Height, image.PixelFormat);
                Graphics graphics = Graphics.FromImage(image);

                graphics.DrawImageUnscaled(source, (image.Width - source.Width) / 2, (image.Height - source.Height) / 2);

                graphics.Dispose();
            }
            return image;
        }
    }


}
