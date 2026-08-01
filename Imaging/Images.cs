using Emgu.CV;
using MED.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        }

        public Processes ImageProcesses { get; set; }
        public Logger Logger { get => ImageProcesses.Logger; set => ImageProcesses.Logger = value; }


        #region Settings


        [ReadOnly(true)]
        public bool IsAsynchrone { get => ImageProcesses.IsAsynchrone; set => ImageProcesses.IsAsynchrone = value; }

        [Browsable(true)]
        public ProcessSettings ProcessSettings { get => ImageProcesses.ProcessSettings; set => ImageProcesses.ProcessSettings = value; }

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

        public override void Start() => ImageProcesses.Start();

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


        /**
         * GetImage
         * 
         * */
        public override Bitmap GetImage(IImageProvider provider = null)
        {
            Performance.Resume($"Make Image from {Items.Count}", true);
            Bitmap image;
            Size size = ImageSizeMax;
            if (size.IsEmpty)
            {
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

                            clipRegion.Translate(-imageSrc.Width / 2, -imageSrc.Height/ 2);
                            graphics.SetClip(clipRegion, CombineMode.Replace);

                            graphics.DrawImage(imageSrc, -imageSrc.Width / 2, -imageSrc.Height / 2/*, imageSrc.Width, imageSrc.Height*/);
                            //move image back
                            //graphics.TranslateTransform(-(float)imageSrc.Width / 2, -(float)imageSrc.Height / 2);
                            graphics.ResetTransform();
                        }
                        else
                        {
                            clipRegion.Translate(location.X, location.Y);
                            graphics.SetClip(clipRegion, CombineMode.Replace);
                            graphics.DrawImage(imageSrc, location.X, location.Y);
                        }
                        graphics.ResetClip();
                    }
                    else
                    {

                        if ((prov as IImageProvider).Rotation != 0F)
                        {
                            graphics.TranslateTransform(Position.X + location.X+ imageSrc.Width/ 2, Position.Y+ location.Y+ imageSrc.Height / 2);
                            //rotate
                            graphics.RotateTransform((prov as IImageProvider).Rotation,MatrixOrder.Prepend);
                            graphics.DrawImage(imageSrc, -imageSrc.Width/2, -imageSrc.Height/2/*, imageSrc.Width, imageSrc.Height*/);
                            //move image back
                            //graphics.TranslateTransform(-(float)imageSrc.Width / 2, -(float)imageSrc.Height / 2);
                            graphics.ResetTransform();
                        }
                        else

                            graphics.DrawImage(imageSrc, Position.X + location.X, Position.Y + location.Y, imageSrc.Width, imageSrc.Height);
                    }
                }
                nProvider++;
            }
            graphics.Dispose();
            Performance.Pause($"Get Image done => " + (image == null ? "<null>" : "Bitmap"));
            return image;
        }
    }
}
