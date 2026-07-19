using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Provider;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MED.Imaging
{
    public class ScreenSplitter : ImageProcess
    {
        public ScreenSplitter(string name = "ScreenSplitter", Performance performance = null, Form formHandler = null, IImageConsumer imageConsumer = null, bool isAynchrone = false)
            : base(name, performance, formHandler, imageConsumer, isAynchrone)
        {
            ImageIsProvided = false;
            ResetOnImageChanged = true;
        }


        #region Settings

        [Browsable(true)]
        public bool Horizontal { get; set; }
        [Browsable(true)]
        public Size Grid { get; set; }

        public override void LoadSettings(bool loadChildren = true)
        {
            base.LoadSettings(loadChildren);

            Horizontal = (bool)Core.Settings.GetValue("Horizontal", Name, Horizontal);
            Grid = (Size)Core.Settings.GetValue("Grid", Name, Grid);
        }
        public override void SaveSettings(bool saveChildren = true)
        {
            Core.Settings.SetValue("Horizontal", Name, Horizontal);
            Core.Settings.SetValue("Grid", Name, Grid);

            base.SaveSettings(saveChildren);
        }
        #endregion

        public override void Start()
        {
            base.Start();

            Image = null;

            ProcessState = ThreadState.Running;
        }

        /**
         * Image
         * */
        public override void ImageChanged(IImageProvider sender, EventArgs e)
        {
            base.ImageChanged(sender, e);
        }

        //private Bitmap _Image;
        public override Bitmap Image
        {
            get
            {
                if (base.Image != null)
                    return base.Image;
                var firstProvider = ImageProvider;
                if (firstProvider == null)
                    return null;
                Performance.Resume($"Make Image from {ImageProviders.Count}", true);
                Bitmap image;
                Size size = ImageSizeMax;
                if (size.IsEmpty)
                {
                    foreach (var provider in ImageProviders)
                    {
                        image = provider.Image;
                        if (image == null)
                            continue;
                        size = image.Size;
                        if (size.IsEmpty)
                            continue;
                        break;
                    }
                    if (size.IsEmpty)
                        return null;
                }
                Size itemSize;
                if (Horizontal)
                    itemSize = new Size(size.Width / ImageProviders.Count, size.Height);
                else
                    itemSize = new Size(size.Width, size.Height / ImageProviders.Count);
                image = new Bitmap(size.Width, size.Height);
                Point Position = new Point(0, 0);
                Graphics graphics = Graphics.FromImage(image);
                foreach (var provider in ImageProviders)
                {
                    if (provider.Image != null)
                        graphics.DrawImage(provider.Image, Position.X, Position.Y, itemSize.Width, itemSize.Height);
                    if (Horizontal)
                        Position.X += itemSize.Width;
                    else
                        Position.Y += itemSize.Height;
                }
                graphics.Dispose();
                Performance.Pause($"Get Image done => " + (base.Image == null ? "<null>" : "Bitmap"));
                return base.Image = image;
            }
            set
            {
                //Performance.Debug($"Set_Image {Name} : " + (base.Image == null ? "<null>" : "Bitmap") + " => " + (value == null ? "<null>" : "Bitmap"));
                base.Image = value;
            }
        }
    }
}
