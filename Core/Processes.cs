using MED.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MED
{
    public class Processes : Process, IProcesses
    {
        public Processes(string name = "MED.Project", Performance performance = null, Control invokeHandler = null, IConsumer consumer = null, bool isAsynchrone = false)
            : base(name == null || name == "" ? "MED.Project" : name, performance, invokeHandler, consumer, isAsynchrone)
        {
            Items = new();
        }
        public override void Dispose()
        {
            DisposeProcesses();
            base.Dispose();
        }

        private Logger _Logger { get; set; }

        [Browsable(false)]
        public Logger Logger
        {
            get => _Logger;
            set
            {
                _Logger = value;
                if (value != null && (Performance == null || Performance.IsEmpty))
                    Performance = new(Name, value/*, false TODO*/);
                else
                    Performance.Logger = value;
                //Propagate
                if (Items != null)
                    foreach (var proc in Items)
                        if (proc is Process)
                            (proc as Process).Performance.Logger = _Logger;
                        else if (proc is ProcessForm)
                            (proc as ProcessForm).Logger = _Logger;
            }
        }

        #region Settings

        public override void LoadSettings(ProcessSettings settings = null, string fileName = "")
        {
            base.LoadSettings(settings, fileName);

            InitializeProcesses(false);
        }
        public override void LoadProcess(JsonNode node)
        {
            base.LoadProcess(node);

            LoadProcesses(ProcessSettings);
        }

        public virtual void SaveSettings(ProcessSettings settings = null, string fileName = "")
        {
            if (settings == null)
                settings = ProcessSettings;

            if (settings == null)
                settings = ProcessSettings = new ProcessSettings(fileName);

            SaveProcesses(settings);

            base.SaveSettings(settings, fileName);
        }
        public virtual void SaveProcesses(ProcessSettings settings)
        {
            //Create children nodes in order
            var childSettings = settings.ChildSettings("Processes");
            if (childSettings.Root is JsonArray)
            {
                settings.Root["Processes"] = new JsonObject();
                childSettings = settings.ChildSettings("Processes");
            }
            else
                childSettings.Root.AsObject().Clear();

            foreach (var proc in Items)
                childSettings.ChildSettings(proc.Name);

            JsonObject nodes = childSettings.Root.AsObject();

            foreach (var proc in Items)
                if (proc is IProcesses)
                    proc.SaveSettings(childSettings.ChildSettings(proc.Name));
                else
                    nodes[proc.Name] = proc.SaveProcess();
        }
        #endregion

        #region Processes

        [Browsable(true)]
        public virtual List<IProcess> Items { get; protected set; }
        public virtual void DisposeProcesses()
        {
            if (Items != null)
            {
                foreach (var handler in Items)
                    if (handler is IDisposable)
                        (handler as IDisposable).Dispose();
                Items = null;
            }
        }

        public virtual void LoadProcesses(ProcessSettings settings)
        {
            ProcessSettings processesSettings = settings.ChildSettings("Processes", false);
            JsonObject nodes;
            if (processesSettings.Root is JsonArray)
            {//Compatibility
                nodes = new();
                foreach (var procNode in processesSettings.Root.AsArray())
                    nodes.Add(nodes.Count.ToString(), procNode.DeepClone());
            }
            else
                nodes = processesSettings.Root.AsObject();

            if (nodes == null)
                return;

            DisposeProcesses();
            Items = new();

            Items.Clear();
            var itemsNodes = new Dictionary<IProcess, JsonNode>();
            foreach (var (nodeName, procNode) in nodes)
            {
                try
                {
                    IProcess item = ProcessStatic.CreateProcess(procNode, Performance, InvokeHandler);
                    if (item is Process)
                        (item as Process).Consumer = this.Consumer ?? this;
                    item.LoadSettings(processesSettings.ChildSettings(item.Name));

                    Items.Add(item);
                    itemsNodes.Add(item, procNode);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString(), $"Création de {procNode.ToString()}", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            foreach (var kvp in itemsNodes)
            {
                LoadConsumers(kvp.Key, kvp.Value);
            }
        }
        public virtual void LoadConsumers(IProcess process, JsonNode node)
        {
            if (node["Consumers"] == null)
                return;
            var consumers = node["Consumers"].AsObject();
            if (consumers == null) return;
            foreach (var property in consumers)
            {
                var propertyName = property.Key;
                foreach (var consumerNode in property.Value.AsArray())
                {
                    LoadConsumer(process, consumerNode.AsObject(), propertyName);
                }
            }
        }
        public virtual void LoadConsumer(IProcess process, JsonObject consumerNode, string propertyName)
        {
            string consumerPath = consumerNode["Name"].ToString();
            IProcess consumerProcess = ProcessStatic.FindItem(this, consumerPath);
            if (consumerProcess == null)
                return;

            ProcessStatic.AddConsumer((IProvider)process, (IConsumer)consumerProcess, propertyName);
        }

        public virtual void InitializeProcesses(bool resetAll = false)
        {

            if (resetAll)
                DisposeProcesses();

            if (Items != null && !resetAll)
            {
                foreach (var handler in Items)
                    handler.Stop();

                return;
            }

            if (Logger == null)
            {
                Logger = new();
            }
            else
                Logger.Clear();

            if (Performance == null || resetAll)
            {
                Performance = new(this.Name, Logger);
            }

            if (Items == null)
                Items = new();
            else
                Items.Clear();
        }

        #endregion

        #region Process

        /**
         * 
         * 
         */
        public override void Start()
        {
            base.Start();

            InitializeProcesses();

            foreach (var item in Items)
            {
                item.Start();
            }

            ProcessState = ThreadState.Running;
        }

        /**
         * 
         * 
         */
        public override void Stop()
        {

            if (!IsRunning)
                return;

            base.Stop();

            if (Items == null)
                return;

            foreach (var item in Items.Reverse<IProcess>())
            {
                item.Stop();
            }
        }

        public override void Resume()
        {
            base.Resume();

            if (IsRunning)
                foreach (var item in Items)
                    item.Resume();

            ProcessState = ThreadState.Running;
        }

        public override void Pause()
        {
            ProcessState = ThreadState.Suspended;

            if (IsRunning)
                foreach (var item in Items)
                    item.Pause();
        }

        #endregion

        /**
         * ObjectsProperties
         */
        public override Dictionary<string, object> ObjectsProperties
        {
            get
            {
                var dict = new Dictionary<string, object>();
                if (this.IsDisposed)
                    return dict;
                if(Consumer != null)
                    dict.Add((Consumer as IProcess).Name, Consumer);
                if (ProcessSettings != null)
                    dict.Add("Settings", ProcessSettings);
                if (Performance != null && !Performance.IsEmpty)
                    dict.Add(this.Name + ".Performance", Performance);

                return dict;
            }
        }
    }
}