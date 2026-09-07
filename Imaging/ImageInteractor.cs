using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using MED.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MED.Imaging
{
    /**
     * class Mover : ImageSourced, IImageProvider, IImageCollidable
     * <summary>Image as a physical object that can collide</summary>
     * */
    public class ImageInteractor : ImageSourced, IImageProvider, IImageInteractor
    {
        public ImageInteractor(string name = "ImageInteractor", Performance? performance = null, Control? invokeHandler = null, IConsumer? consumer = null, bool isAsynchrone = true)
        : base(name, performance, invokeHandler, consumer, isAsynchrone)
        {
        }

        #region Properties

        [Category("Interactor")]
        public virtual float Mass { get; set; } = 1F;

        Vector2 _LocationVector;
        [Browsable(false)]
        public virtual Vector2 LocationVector
        {
            get
            {
                if (_LocationVector.Equals(Vector2.Zero))
                    return _LocationVector = base.Location.ToVector2();
                return _LocationVector;
            }
            private set { _LocationVector = value; }
        }

        #endregion

        #region Interactor
        public virtual void InteractWithItems(List<IProcess> items)
        {
            foreach (var item2 in items)
            {
                if (item2 == this)
                    continue;
                if (!item2.Enabled)
                    continue;
                if (item2 is not IImageInteractor mover2)
                    continue;
                if (!mover2.Visible)
                    continue;
                InteractWithItem(mover2);
            }
        }

        public virtual bool InteractWithItem(IImageInteractor item2) => false;

        #endregion

        #region Settings
        public override void LoadSettings(ProcessSettings? settings = null, string fileName = "")
        {
            base.LoadSettings(settings, fileName);
            if (settings == null && (settings = ProcessSettings) == null)
                return;
            Mass = (float)(settings.GetValue("Mass", Mass) ?? Mass);
        }
        public override JsonObject SaveProcess(JsonObject? node = null)
        {
            node = base.SaveProcess(node);
            node.Add("Mass", Mass);
            return node;
        }
        #endregion
    }
}