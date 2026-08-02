using Emgu.CV;
using MED.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace MED.Imaging
{
    public class Images : ImageProcess, IProcesses
    {
        public Images(string name = "Images", Performance performance = null, Control invokeHandler = null, IImageConsumer imageConsumer = null, bool isAsynchrone = true)
            : base(name, performance, invokeHandler, imageConsumer, isAsynchrone)
        {
            ProcessIcon = "Image";

            ResetOnImageChanged = true;

            ImageProviders = new();

            ImageProcesses = new(name, performance, invokeHandler, this, isAsynchrone);

            ImageProcesses.OnProcessStateChanged += Invoke_ProcessStateChanged;

            //ImageConsumer = imageConsumer;

            Collider = new(this);
        }

        public Processes ImageProcesses { get; set; }
        public Logger Logger { get => ImageProcesses.Logger; set => ImageProcesses.Logger = value; }


        #region Settings


        [ReadOnly(true)]
        public override bool IsAsynchrone
        {
            get { return ImageProcesses == null ? base.IsAsynchrone : ImageProcesses.IsAsynchrone; }
            set { if (ImageProcesses == null) base.IsAsynchrone = value; else ImageProcesses.IsAsynchrone = value; }
        }

        [Browsable(true)]
        public override ProcessSettings ProcessSettings
        {
            get { return ImageProcesses == null ? base.ProcessSettings : ImageProcesses.ProcessSettings; }
            set { if (ImageProcesses == null) base.ProcessSettings = value; else ImageProcesses.ProcessSettings = value; }
        }

        public override void LoadSettings(ProcessSettings processSettings = null, string fileName = "")
        {
            if (processSettings != null)
                ProcessSettings = processSettings;
            base.LoadSettings(processSettings, fileName);
            ImageProcesses.LoadSettings(processSettings, fileName);

        }
        public override void LoadProcess(JsonNode node)
        {
            base.LoadProcess(node);
            ImageProcesses.LoadProcess(node);

        }

        public override void SaveSettings(ProcessSettings settings = null, string fileName = "")
        {
            if (settings == null)
                settings = ProcessSettings;

            if (settings == null)
                settings = ProcessSettings = new ProcessSettings(fileName);

            ImageProcesses.SaveSettings(settings, fileName);

            base.SaveSettings(settings, fileName);
        }

        #endregion

        public override Performance Performance { get => ImageProcesses.Performance; }

        public override bool IsRunning { get => ImageProcesses.ProcessState == ThreadState.Running || ImageProcesses.ProcessState == ThreadState.Suspended; }

        public override System.Threading.ThreadState ProcessState { get => ImageProcesses.ProcessState; set => ImageProcesses.ProcessState = value; }

        public void Invoke_ProcessStateChanged(IProcess sender, System.Threading.ThreadState state) => OnProcessStateChanged?.Invoke(sender, state);

        /**
         * Process
         * 
         */
        #region Process

        public override void Start()
        {
            ImageProcesses.Start();

            Collider.CollidersRegions = null;
        }

        public override void Stop() => ImageProcesses.Stop();

        public override void Resume() => ImageProcesses.Resume();

        public override void Pause() => ImageProcesses.Pause();

        #endregion

        /**
         * ObjectsProperties
         * */
        public override Dictionary<string, object> ObjectsProperties
        {
            get => ImageProcesses.ObjectsProperties;
        }

        public virtual List<IProcess> Items => ImageProcesses.Items;

        protected ImagesCollider Collider { get; set; }

        /**
         * GetImage
         * 
         * */
        public override Bitmap GetImage(IImageProvider provider = null)
        {
            Performance.Resume($"Make Image from {Items.Count}", true);

            Bitmap image;
            Size size = ImageSizeMin;
            if (size.IsEmpty)
            {
                if (Consumer is IImageConsumer)
                    size = (Consumer as IImageConsumer).ImageSizeMin;

                if (size.IsEmpty)
                    foreach (var prov in Items)
                    {
                        if (prov is not IImageProvider)
                            continue;
                        image = (prov as IImageProvider).Image;
                        if (image == null)
                            continue;
                        size = image.Size;
                        if (size.IsEmpty)
                            continue;
                        //TODO Chercher le + grand
                        break;
                    }
                if (size.IsEmpty)
                    return null;
            }

            image = new Bitmap(size.Width, size.Height);

            Point Position = new Point(0, 0);
            Graphics graphics = Graphics.FromImage(image);

            Collider.Collide(image, graphics);

            int nProvider = 0;
            foreach (var prov in Items)
            {
                if (prov is not IImageProvider)
                    continue;

                Bitmap imageSrc = (prov as IImageProvider).Image;
                if (imageSrc != null)
                {
                    var clipRegion = (prov as IImageProvider).ClipRegion;

                    var location = (prov as IImageProvider).Location;

                    if (clipRegion != null)
                    {
                        if (!location.IsEmpty)
                        {
                            clipRegion = clipRegion.Clone();
                        }

                        if ((prov as IImageProvider).Rotation != 0F)
                        {

                            graphics.TranslateTransform(Position.X + location.X + imageSrc.Width / 2, Position.Y + location.Y + imageSrc.Height / 2);
                            //rotate
                            graphics.RotateTransform((prov as IImageProvider).Rotation, MatrixOrder.Prepend);

                            clipRegion.Translate(-imageSrc.Width / 2, -imageSrc.Height / 2);
                            graphics.SetClip(clipRegion, CombineMode.Replace);

                            graphics.DrawImageUnscaled(imageSrc, -imageSrc.Width / 2, -imageSrc.Height / 2/*, imageSrc.Width, imageSrc.Height*/);
                            
                            graphics.ResetTransform();
                        }
                        else
                        {
                            if (!location.IsEmpty)
                                clipRegion.Translate(location.X, location.Y);
                            graphics.SetClip(clipRegion, CombineMode.Replace);
                            if (location.IsEmpty && imageSrc.Size != size && (prov as IImageProvider).ImageSizeMin.IsEmpty)
                                graphics.DrawImage(imageSrc, 0, 0, size.Width, size.Height);
                            else
                                graphics.DrawImageUnscaled(imageSrc, 0, 0);
                            //var bounds = clipRegion.GetBounds(graphics);
                        }
                        graphics.ResetClip();
                        
                        if (!location.IsEmpty && prov is IImageCollidable)
                            Collider.UpdateColliderRegion(graphics, (IImageCollidable)prov);

                        if (true && prov is IImageMove)
                        {
                            var font = new Font(FontFamily.GenericMonospace, 8F);
                            var brush = new SolidBrush(SystemColors.WindowText);
                            graphics.DrawString((prov as IImageMove).Speed.ToString("#.##"), font, brush, location.X, location.Y + imageSrc.Height);
                        }
                    }
                    else
                    {

                        if ((prov as IImageProvider).Rotation != 0F)
                        {
                            graphics.TranslateTransform(Position.X + location.X + imageSrc.Width / 2, Position.Y + location.Y + imageSrc.Height / 2);
                            //rotate
                            graphics.RotateTransform((prov as IImageProvider).Rotation, MatrixOrder.Prepend);
                            graphics.DrawImage(imageSrc, -imageSrc.Width / 2, -imageSrc.Height / 2/*, imageSrc.Width, imageSrc.Height*/);
                            //move image back
                            //graphics.TranslateTransform(-(float)imageSrc.Width / 2, -(float)imageSrc.Height / 2);
                            graphics.ResetTransform();
                        }
                        else
                        {
                            graphics.DrawImage(imageSrc, Position.X + location.X, Position.Y + location.Y, imageSrc.Width, imageSrc.Height);
                        }
                    }
                }
                nProvider++;
            }
            graphics.Dispose();
            Performance.Pause($"Get Image done => " + (image == null ? "<null>" : "Bitmap"));
            return image;
        }
        /**
         * GetImage
         * 
         * */
        public void Collide(Bitmap image)
        {
            if (Items.Count < 2) return;

            Performance.Sub(".Collider").Resume($"{Items.Count} Items", true);

            Dictionary<IImageCollidable, Region> itemsRegions = new();

            Graphics gr = Graphics.FromImage(image);
            foreach (var prov in Items)
            {
                if (prov is not IImageCollidable)
                    continue;

                var clipRegion = (prov as IImageCollidable).ClipRegion;

                var location = (prov as IImageCollidable).Location;

                var velocity = (prov as IImageCollidable).Direction;

                if (clipRegion != null)
                {
                    if (!location.IsEmpty)
                    {
                        clipRegion = clipRegion.Clone();
                        clipRegion.Translate(location.X, location.Y);

                        var bounds = clipRegion.GetBounds(gr);
                        bool changed = false;
                        if (bounds.Top < 0)
                        {
                            location.Y = 0;
                            if (velocity.X < 0)
                                velocity.Y *= -1;
                            changed = true;
                        }
                        if (bounds.Left < 0)
                        {
                            location.X = 0;
                            if (velocity.X < 0)
                                velocity.X *= -1;
                            changed = true;
                        }
                        if (bounds.Bottom > image.Height)
                        {
                            location.Y = image.Height - bounds.Height;
                            if (velocity.Y > 0)
                                velocity.Y *= -1;
                            changed = true;
                        }
                        if (bounds.Right > image.Width)
                        {
                            location.X = image.Width - bounds.Width;
                            if (velocity.X > 0)
                                velocity.X *= -1;
                            changed = true;
                        }
                        if (changed)
                        {
                            (prov as IImageCollidable).Location = location;

                            (prov as IImageCollidable).Direction = velocity;

                            clipRegion = (prov as IImageCollidable).ClipRegion.Clone();
                            clipRegion.Translate(location.X, location.Y);
                        }
                    }

                    if ((prov as IImageCollidable).Mass == 0F)
                        continue;
                    itemsRegions.Add((IImageCollidable)prov, clipRegion);
                }
            }
            if (itemsRegions.Count > 1)
            {
                var items = itemsRegions.Keys.ToArray();
                for (var i1 = 0; i1 < items.Length - 1; i1++)
                {
                    var item1 = items[i1];
                    var region1 = itemsRegions[item1];
                    for (var i2 = i1 + 1; i2 < items.Length; i2++)
                    {
                        var item2 = items[i2];
                        var region2 = itemsRegions[item2];

                        var intersect = region1.Clone();
                        intersect.Intersect(region2);
                        if (!intersect.IsEmpty(gr))
                        {
                            var intersectBounds = intersect.GetBounds(gr);
                            var intersectBoundsCenter = new PointF((intersectBounds.Right - intersectBounds.Left) / 2, (intersectBounds.Bottom - intersectBounds.Top) / 2);

                            var itemBounds = region1.GetBounds(gr);
                            var itemBoundsCenter = new PointF((itemBounds.Right - itemBounds.Left) / 2, (itemBounds.Bottom - itemBounds.Top) / 2);
                            var speed = item1.Direction;
                            var changed = false;
                            if (!item1.Location.IsEmpty)
                            {
                                if (Math.Abs(itemBoundsCenter.X - intersectBounds.X) > 1)
                                {
                                    speed.X *= -1 * item1.Mass;
                                    changed = true;
                                }
                                if (Math.Abs(itemBoundsCenter.Y - intersectBounds.Y) > 1)
                                {
                                    speed.Y *= -1 * item1.Mass;
                                    changed = true;
                                }
                                if (changed)
                                    item1.Direction = speed;
                            }

                            if (item2.Location.IsEmpty == false)
                            {
                                itemBounds = region2.GetBounds(gr);
                                itemBoundsCenter = new PointF((itemBounds.Right - itemBounds.Left) / 2, (itemBounds.Bottom - itemBounds.Top) / 2);
                                speed = item2.Direction;
                                changed = false;
                                if (Math.Abs(itemBoundsCenter.X - intersectBounds.X) > 1)
                                {
                                    speed.X *= -1 * item2.Mass;
                                    changed = true;
                                }
                                if (Math.Abs(itemBoundsCenter.Y - intersectBounds.Y) > 1)
                                {
                                    speed.Y *= -1 * item2.Mass;
                                    changed = true;
                                }
                                if (changed)
                                    item2.Direction = speed;
                            }

                        }
                    }
                }

            }

            gr.Dispose();
            Performance.Sub(".Collider").Pause($"Collider done {itemsRegions.Count}");
        }
    }
}

