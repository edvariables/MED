using MED.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MED
{
    public class ImageProcessForm : ProcessForm, IImageConsumer
    {
        public ImageProcessForm() : this("ImageProcessForm") { }
        public ImageProcessForm(string name) : base(name)
        {
            ProcessIcon = "VisualTrue";

            RenderPictureBox = new();
            RenderPictureBox.BackColor = System.Drawing.Color.LightSteelBlue;
            RenderPictureBox.Size = ClientSize;
            RenderPictureBox.Dock = DockStyle.Fill;
            Controls.Add(RenderPictureBox);
        }
        #region Settings

        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual Size ImageSizeMin { get; protected set; }

        public override void LoadSettings(ProcessSettings? settings = null, string fileName = "")
        {
            base.LoadSettings(settings, fileName);
            if (settings == null && (settings = ProcessSettings) == null)
                return;
            ImageSizeMin = (Size)(settings.GetValue("ImageSizeMin", ImageSizeMin) ?? ImageSizeMin);
        }

        public override JsonObject SaveProcess(JsonObject? node = null)
        {
            if (node == null)
                node = new JsonObject();
            node["ImageSizeMin"] = Parser.ObjectToString(ImageSizeMin);

            return base.SaveProcess(node);
        }
        #endregion

        #region Image

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public PictureBox? RenderPictureBox { get; set; }

        /**
         * Image
         * */
        public virtual void OnImageChanged(IImageProvider sender, EventArgs e)
        {
            if (this.Disposing || this.IsDisposed || !IsRunning)
                return;

            Performance?.Debug($"ImageChanged from {sender.ToString()}");

            if (RenderPictureBox != null)
                Imaging.Render.RefreshRender(sender, RenderPictureBox, Performance, e);
        }

        public override void Start()
        {
            base.Start();

            if (RenderPictureBox != null)
            {
                //Activate();
                RenderPictureBox.Focus();
            }
        }
        #endregion
    }
}
