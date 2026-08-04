using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Provider;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace MED.Imaging
{
    public class ScreenSplitter : ImageProcess
    {
        public ScreenSplitter(string name = "ScreenSplitter", Performance performance = null, Control invokeHandler = null, IImageConsumer imageConsumer = null, bool isAsynchrone = false)
            : base(name, performance, invokeHandler, imageConsumer, isAsynchrone)
        {
            ImageIsProvided = false;
            ResetOnImageChanged = true;
        }


        #region Settings

        [Browsable(true)]
        public bool Horizontal { get; set; }
        [Browsable(true)]
        public Size Grid { get; set; }

        public override void LoadSettings(ProcessSettings settings = null, string fileName = "")
        {
            base.LoadSettings(settings, fileName);

            Horizontal = (bool)ProcessSettings.GetValue("Horizontal", Horizontal);
            Grid = (Size)ProcessSettings.GetValue("Grid", Grid);
        }
        public override JsonObject SaveProcess(JsonObject node = null)
        {
            node = base.SaveProcess(node);
            node.Add("Horizontal", Horizontal);
            node.Add("Grid", Core.Parser.ObjectToString(Grid));
            return node;
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

        /**
         * GetImage
         * 
         * */
        public override Bitmap? GetImage(IImageProvider provider = null)
        {
            Performance?.Resume($"Make Image from {ImageProviders.Count}", true);
            Bitmap image;
            Size size = ImageSizeMin;
            if (size.IsEmpty)
            {
                foreach (var prov in ImageProviders)
                {
                    image = prov.Image;
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
            if (!Grid.IsEmpty && Grid.Width > 0 && Grid.Height > 0)
            {
                itemSize = new Size(size.Width / Grid.Width, size.Height / Grid.Height);
            }
            else if (Horizontal)
                itemSize = new Size(size.Width / ImageProviders.Count, size.Height);
            else
                itemSize = new Size(size.Width, size.Height / ImageProviders.Count);
            image = new Bitmap(size.Width, size.Height);
            Point Position = new Point(0, 0);
            Graphics graphics = Graphics.FromImage(image);
            int nProvider = 0;
            int col = 0;
            int row = 0;
            foreach (var prov in ImageProviders)
            {
                if (prov.Image != null)
                {
                    if (prov.ClipRegion != null)
                    {
                        graphics.SetClip(prov.ClipRegion, CombineMode.Replace);
                        graphics.DrawImage(prov.Image, prov.Location.X, prov.Location.Y);
                        graphics.ResetClip();
                    }
                    else
                        graphics.DrawImage(prov.Image, Position.X + prov.Location.X, Position.Y + prov.Location.Y, itemSize.Width, itemSize.Height);
                }
                nProvider++;
                if (!Grid.IsEmpty && Grid.Width > 0 && Grid.Height > 0)
                {
                    col++;
                    if (col >= Grid.Width)
                    {
                        col = 0;
                        row++;
                    }
                    Position.X = col * itemSize.Width;
                    Position.Y = row * itemSize.Height;
                }
                else if (Horizontal)
                    Position.X += itemSize.Width;
                else
                    Position.Y += itemSize.Height;
            }
            graphics.Dispose();
            Performance?.Pause($"Get Image done => " + (image == null ? "<null>" : "Bitmap"));
            return image;
        }
    }
}
