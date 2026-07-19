using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Provider;
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
        }

        [Browsable(true)]
        public bool Horizontal { get; set; }

        #region Settings
        public override void LoadSettings(bool loadChildren = true)
        {
            base.LoadSettings(loadChildren);

            Horizontal = (bool)Core.Settings.GetValue("Horizontal", Name, Horizontal);
        }
        public override void SaveSettings(bool saveChildren = true)
        {
            Core.Settings.SetValue("Horizontal", Name, Horizontal);

            base.SaveSettings(saveChildren);
        }
        #endregion

        public List<IImageProvider> Providers = new();

        public override void Start()
        {
            Providers.Clear();
            base.Start();

            ProcessState = ThreadState.Running;
        }

        /**
         * Image
         * */
        public override void ImageChanged(IImageProvider sender, EventArgs e)
        {
            if (this.Disposing || this.IsDisposed)
                return;
            if (!Providers.Contains(sender))
                Providers.Add(sender);
            if (Providers.Count == 1 || Providers.Last() == sender)
            {
                Image = null;
                if (IsAsynchrone)
                {
                    var image = Image;//Generate in same thread
                }
                base.ImageChanged(sender, e);
            }
            else
                Performance.Debug($"Waiting for last provider {sender} => {Providers.Last()}");
        }

        private Bitmap _Image;
        public override Bitmap Image
        {
            get
            {
                if (_Image != null)
                    return _Image;
                if (Providers.Count == 0)
                    return _Image;
                if (Providers.Count == 1)
                    return _Image = Providers.First().Image;
                Performance.Resume($"Get Image from {Providers.Count}", true);
                Bitmap image;
                Size size = ImageSizeMax;
                if (size.IsEmpty)
                {
                    image = Providers.First().Image;
                    if (image == null)
                        return null;
                    size = image.Size;
                    if (size.IsEmpty)
                        return null;
                }
                Size itemSize;
                if (Horizontal)
                    itemSize = new Size(size.Width / Providers.Count, size.Height);
                else
                    itemSize = new Size(size.Width, size.Height / Providers.Count);
                _Image = image = new Bitmap(size.Width, size.Height);
                Point Position = new Point(0, 0);
                Graphics graphics = Graphics.FromImage(image);
                foreach (var provider in Providers)
                {
                    graphics.DrawImage(provider.Image, Position.X, Position.Y, itemSize.Width, itemSize.Height);
                    if (Horizontal)
                        Position.X += itemSize.Width;
                    else
                        Position.Y += itemSize.Height;
                }
                graphics.Dispose();
                Performance.Pause($"Get Image done => " + (_Image == null ? "<null>" : "Bitmap"));
                return _Image;
            }
            set
            {
                //Performance.Debug("Set Image : " + (_Image == null ? "<null>" : "Bitmap") + " => " + (value == null ? "<null>" : "Bitmap"));
                _Image = value;
            }
        }
    }
}
