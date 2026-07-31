using Emgu.CV;
using MED.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

            ImageProviders = new();

            ImageProcesses = new(name, performance, invokeHandler, imageConsumer, isAsynchrone);

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

        public override JsonObject SaveProcess(JsonObject node = null)
        {
            node = base.SaveProcess(node);
            return node;
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
            get
            {
                var dict = ImageProcesses.ObjectsProperties;
                dict.Add(this.Name + "[Images]", this);
                return dict;
            }
        }

        public virtual List<IProcess> Items => ImageProcesses.Items;
    }
}
