using MED.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using static MED.Imaging.Images;
using static MED.Imaging.VideoMover;

namespace MED.Imaging
{
    public class Images : ImageCollider, IProcesses
    {
        public Images(string name = "Images", Performance? performance = null, Control? invokeHandler = null, IConsumer? consumer = null, bool isAsynchrone = true)
            : base(name, performance, invokeHandler, consumer, isAsynchrone)
        {
            ProcessIcon = ProcessIconDefault = "icon-folder-open";

            ResetOnImageChanged = true;

            ImageProviders = new();

            ImageProcesses = new(name, performance, invokeHandler, this, isAsynchrone);

            ImageProcesses.OnProcessStateChanged += Invoke_ProcessStateChanged;

            //ImageConsumer = imageConsumer;

            ImagesCollider = new(this);
        }

        [Category("Processes")]
        public Processes ImageProcesses { get; set; }
        [Browsable(false)]
        public Logger? Logger { get => ImageProcesses.Logger; set => ImageProcesses.Logger = value; }


        #region Settings

        public override bool IsAsynchrone
        {
            get { return ImageProcesses == null ? base.IsAsynchrone : ImageProcesses.IsAsynchrone; }
            set { if (ImageProcesses == null) base.IsAsynchrone = value; else ImageProcesses.IsAsynchrone = value; }
        }

        [Browsable(true)]
        public override ProcessSettings? ProcessSettings
        {
            get { return ImageProcesses == null ? base.ProcessSettings : ImageProcesses.ProcessSettings; }
            set { if (ImageProcesses == null) base.ProcessSettings = value; else ImageProcesses.ProcessSettings = value; }
        }

        public override void LoadSettings(ProcessSettings? settings = null, string fileName = "")
        {
            base.LoadSettings(settings, fileName);
            if (settings == null && (settings = ProcessSettings) == null)
                return;
            if (Enum.TryParse((settings.GetValue(nameof(BorderBehavior), BorderBehavior) ?? BorderBehavior).ToString(), out BorderBehaviors a))
                BorderBehavior = a;

        }
        public override void LoadProcess(JsonNode node)
        {
            base.LoadProcess(node);
            ImageProcesses.LoadProcess(node);

        }

        public override void SaveSettings(ProcessSettings? settings = null, string fileName = "")
        {
            if (settings == null
            && (settings = ProcessSettings) == null)
                settings = ProcessSettings = new ProcessSettings(fileName);

            ImageProcesses.SaveSettings(settings, fileName);

            base.SaveSettings(settings, fileName);
        }
        public override JsonObject SaveProcess(JsonObject? node = null)
        {
            node = base.SaveProcess(node);
            node.Add(nameof(BorderBehavior), BorderBehavior.ToString());
            return node;
        }

        #endregion

        public override Performance? Performance => ImageProcesses.Performance;

        public override bool IsRunning => ImageProcesses.IsRunning;

        public override System.Threading.ThreadState ProcessState => ImageProcesses.ProcessState;

        public void Invoke_ProcessStateChanged(IProcess sender, System.Threading.ThreadState state)
        {
            OnProcessStateChanged?.Invoke(this, state);
            MoveItemsTimeOnProcessStateChanged(state);
        }

        /**
         * Process
         * 
         */
        #region Process

        public override void Start()
        {
            ImageProcesses.Start();

            ImagesCollider.Colliders = null;
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

        [Category("Processes")]
        public virtual List<IProcess> Items => ImageProcesses.Items;

        protected ImagesCollider ImagesCollider { get; set; }

        public override Bitmap? Image
        {
            get => base.Image;
            set
            {
                if (_Image != null)
                    PreviousImage = _Image;
                _Image = value;
            }
        }

        [Browsable(false)]
        public virtual Bitmap? PreviousImage { get; set; }

        [Browsable(false)]
        public virtual Bitmap? DebugImage { get; set; }

        [Category("Debug")]
        public virtual bool DebugImageEnabled { get; set; }

        [Category("Debug")]
        [DefaultValue(false)]
        public virtual bool DrawEdges { get; set; } = false;

        /**
         * GetImage
         * 
         * */
        public override Bitmap? GetImage(IImageProvider? provider = null)
        {
            Performance?.Resume($"Generate Image from {Items.Count}", true);

            Bitmap? image;
            Size size = ImageSizeMin;
            if (size.IsEmpty)
            {
                if (Consumer is IImageConsumer)
                    size = ((IImageConsumer)Consumer).ImageSizeMin;

                if (size.IsEmpty)
                    foreach (var prov in Items)
                    {
                        if (prov is not IImageProvider)
                            continue;
                        image = ((IImageProvider)prov).Image;
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

            image = new System.Drawing.Bitmap(size.Width, size.Height);

            if (DebugImageEnabled)
                DebugImage = (Bitmap)image.Clone();
            else
                DebugImage = null;

            MoveItems();

            Graphics graphics = Graphics.FromImage(image);

            //Collider.Collide(image, graphics);

            int nProvider = 0;
            foreach (var item in Items)
            {
                if (!item.Enabled)
                    continue;

                if (item is not IImageProvider itemImage
                    || !itemImage.Visible)
                    continue;

                try
                {
                    AppendImage(graphics, size, itemImage);
                }
                catch (Exception ex)
                {
                    Performance?.Error($"AppendImage Image {item}", ex);
                }

                nProvider++;
            }
            if (DebugImageEnabled && DebugImage != null)
            {
                graphics.DrawImage(DebugImage, 0, 0);
            }
            graphics.Dispose();
            Performance?.Pause($"Get Image done => " + (image == null ? "<null>" : "Bitmap"));
            return image;
        }

        /**
         * Append item image to the global one
         * 
         * */
        private void AppendImage(Graphics graphics, Size size, IImageProvider item)
        {
            Bitmap? imageSrc = item.Image;
            if (imageSrc != null)
            {
                Region? clipRegion;
                if (DrawEdges && item is IImageCollider)
                    clipRegion = ((IImageCollider)item).ClipEdgesRegion;
                else
                    clipRegion = item.ClipRegion;

                var location = item.Location;

                var rotation = item.RotationAngle;

                if (clipRegion != null)
                {
                    if (rotation != 0F)
                    {
                        graphics.TranslateTransform(location.X + imageSrc.Width / 2, location.Y + imageSrc.Height / 2);
                        //rotate
                        graphics.RotateTransform(rotation, MatrixOrder.Prepend);

                        clipRegion = clipRegion.Clone();

                        clipRegion.Translate(-imageSrc.Width / 2, -imageSrc.Height / 2);

                        if (DrawEdges)
                            graphics.FillRegion(Brushes.Black, clipRegion);
                        else
                        {
                            graphics.SetClip(clipRegion, CombineMode.Replace);

                            graphics.DrawImageUnscaled(imageSrc, -imageSrc.Width / 2, -imageSrc.Height / 2/*, imageSrc.Width, imageSrc.Height*/);
                        }
                    }
                    else
                    {
                        if (!location.IsEmpty)
                            graphics.TranslateTransform(location.X, location.Y); //clipRegion.Translate(location.X, location.Y);

                        if (DrawEdges)
                            graphics.FillRegion(Brushes.Black, clipRegion);
                        else
                        {
                            graphics.SetClip(clipRegion, CombineMode.Replace);

                            if (location.IsEmpty && imageSrc.Size != size && item.ImageSizeMin.IsEmpty)
                                graphics.DrawImage(imageSrc, 0, 0, size.Width, size.Height);
                            else
                                graphics.DrawImageUnscaled(imageSrc, 0, 0);
                        }
                    }
                    graphics.ResetTransform();
                    graphics.ResetClip();
                }
                else
                {

                    if (rotation != 0F)
                    {
                        graphics.TranslateTransform(location.X + imageSrc.Width / 2, location.Y + imageSrc.Height / 2);
                        //rotate
                        graphics.RotateTransform(rotation, MatrixOrder.Prepend);
                        //draw
                        graphics.DrawImage(imageSrc, -imageSrc.Width / 2, -imageSrc.Height / 2/*, imageSrc.Width, imageSrc.Height*/);

                        graphics.ResetTransform();
                    }
                    else
                    {
                        graphics.DrawImage(imageSrc, location.X, location.Y, imageSrc.Width, imageSrc.Height);
                    }
                }
            }
        }

        //Move items
        long _MoveItemsTime = 0;
        void MoveItems()
        {
            if (_MoveItemsTimePaused != 0L)
            {
                Performance?.Debug($"_MoveItemsTimePaused = {_MoveItemsTimePaused}. Skip MoveItems");
                return;
            }

            long now = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            long elapsedTime = _MoveItemsTime == 0L ? long.MaxValue : now - _MoveItemsTime;
            _MoveItemsTime = now;

            if (elapsedTime > 100000)
            {
                if (_MoveItemsElapsedTime != 0)
                    elapsedTime = _MoveItemsElapsedTime;//After Pause
                else
                    elapsedTime = _MoveItemsTimePauseDuration;
            }
            else if (elapsedTime > 800 || elapsedTime == 0)
            {
                Performance?.Debug($"(elapsedTime > 1000 || elapsedTime == 0) <= {elapsedTime}");
                return;
            }

            Performance?.Step($"MoveItems {elapsedTime} msec");

            _MoveItemsElapsedTime = elapsedTime;

            foreach (var item in Items)
            {
                if (!item.Enabled)
                    continue;

                if (item is IImageInteractor interactor)
                    interactor.InteractWithItems(Items);

                if (item is not IImageMover mover)
                    continue;

                mover.Move(elapsedTime);

                //DEBUG
                if (DebugImage != null && item is IImageMover && !mover.Location.IsEmpty)
                {
                    PointF location = mover.Location;
                    Bitmap? imageSrc = mover.Image;
                    if (imageSrc != null)
                    {
                        var graphics = Graphics.FromImage(DebugImage);
                        var font = new Font(FontFamily.GenericMonospace, 8F);
                        var brush = new SolidBrush(SystemColors.WindowText);
                        graphics.DrawString(mover.Speed.ToString("#0"), font, brush, location.X + 5, location.Y + imageSrc.Height);

                        var pen = new Pen(brush);
                        var center = new PointF(location.X + imageSrc.Width / 2, location.Y + imageSrc.Height / 2);
                        var direction = new PointF(center.X + mover.Velocity.X * imageSrc.Width * 2, center.Y + mover.Velocity.Y * imageSrc.Height * 2);
                        graphics.DrawLine(pen, center, direction);
                    }
                }
            }

        }
        long _MoveItemsElapsedTime { get; set; }

        long _MoveItemsTimePauseDuration = 40;//msec

        long _MoveItemsTimePaused = 0;
        void MoveItemsTimeOnProcessStateChanged(System.Threading.ThreadState state)
        {
            long now = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            if (state == ThreadState.Suspended)
            {
                _MoveItemsTimePaused = now;
            }
            else if (state == ThreadState.Running)
            {
                if (_MoveItemsTimePaused != 0L)
                {
                    Performance?.Step($"_MoveItemsTime Resume => MoveItems({_MoveItemsElapsedTime})");

                    _MoveItemsTime = 0L;
                }
                _MoveItemsTimePaused = 0L;
            }
            else if (state == ThreadState.Stopped)
            {
                _MoveItemsTime = _MoveItemsTimePaused = 0L;
            }
        }


        #region Collider

        public enum BorderBehaviors
        {
            None,
            Bump,
            Circular
        }

        [Category("Collider")]
        [DefaultValue(BorderBehaviors.Bump)]
        [Description("Comportement en bordure d'image")]
        public virtual BorderBehaviors BorderBehavior { get; set; } = BorderBehaviors.Bump;

        [Browsable(false)]
        public virtual Bitmap? ModelImage
        {
            get
            {
                Bitmap? modelImage = DebugImage;
                if (modelImage == null)
                    modelImage = PreviousImage;
                return modelImage;
            }
        }
        public virtual PointF CollideItemWithOthers(IImageCollider item, PointF offset)
        {
            Bitmap? modelImage = ModelImage;
            if (modelImage != null)
            {
                Graphics gr = Graphics.FromImage(modelImage);

                if (item is IImageMover mover
                    && ImagesCollider.CollideItemWithImageBorders(BorderBehavior, modelImage, gr, mover, offset))
                {
                    OnCollideItemScript?.Eval(item, offset);
                    item.OnCollideItemScript?.Eval(this, offset);

                    return item.Location;
                }

                var itemsCollided = ImagesCollider.Collide(modelImage, gr, item, offset);
                if (itemsCollided.Count > 0)
                {
                    foreach (var collidedItem in itemsCollided.Keys)
                    {
                        //item.OnCollideItemScript?.Eval(collidedItem, offset);
                        //collidedItem.OnCollideItemScript?.Eval(item, offset);
                    }
                    return item.Location;
                }
            }

            if (offset.IsEmpty)
                return item.Location;

            var location = item.Location;

            location.X += offset.X;
            location.Y += offset.Y;

            return location;
        }

        public override bool CollideItem(IImageCollider item2, PointF offset2)
        {
            if (item2 is IImageMover mover
                && mover.Speed != 0F)
            {
                Bitmap? modelImage = ModelImage;
                if (modelImage != null)
                {
                    Graphics gr = Graphics.FromImage(modelImage);
                    if (ImagesCollider.CollideItemWithImageBorders(BorderBehavior, modelImage, gr, mover, offset2))
                    {
                        OnCollideItemScript?.Eval(this, item2, offset2);
                        item2.OnCollideItemScript?.Eval(item2, this, offset2);
                        return true;
                    }
                }
            }
            return false;
        }

        #endregion

        #region IUndo
        public override void UndoClear()
        {
            foreach (var item in Items)
                item.UndoClear();
            base.UndoClear();
        }
        public override Dictionary<string, object> UndoModeSaveProperties()
        {
            foreach (var item in Items)
                item.UndoModeSaveProperties();
            var dic = base.UndoModeSaveProperties();
            dic.Add("_MoveItemsElapsedTime", _MoveItemsElapsedTime);//TODO is previous
            return dic;
        }
        public override Dictionary<string, object>? Undo(int length = 1)
        {
            foreach (var item in Items)
                item.Undo(length);
            return base.Undo(length);
        }
        #endregion
    }
}

