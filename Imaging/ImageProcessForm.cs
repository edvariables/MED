using MED.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace MED
{
    public class ImageProcessForm : ProcessForm, IImageConsumer
    {
        public ImageProcessForm() : base()
        {
            ProcessIcon = "VisualTrue";
        }
        #region Settings

        [Browsable(true)]
        public virtual Size ImageSizeMin { get; protected set; }

        public virtual void LoadSettings(ProcessSettings? processSettings = null, string fileName = "")
        {
            base.LoadSettings(processSettings, fileName);

            ImageSizeMin = (Size)ProcessSettings.GetValue("ImageSizeMin", ImageSizeMin);
        }

        public virtual JsonObject SaveProcess(JsonObject? node = null)
        {
            if (node == null)
                node = new JsonObject();
            node["ImageSizeMin"] = Parser.ObjectToString(ImageSizeMin);

            return base.SaveProcess(node);
        }
        #endregion

        #region Image

        public PictureBox? RenderPictureBox { get; set; }

        /**
         * Image
         * */
        public virtual void ImageChanged(IImageProvider sender, EventArgs e)
        {
            if (this.Disposing || this.IsDisposed || !IsRunning)
                return;

            Performance.Debug($"ImageChanged from {sender.ToString()}");

            if (RenderPictureBox != null)
                Imaging.Render.RefreshRender(sender, RenderPictureBox, Performance, e);
        }
        #endregion
    }
}
