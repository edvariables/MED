using MED.Core;
using MED.GameController;
using MED.Imaging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Xml.Linq;

namespace MED
{
    /**
     * class ProcessesControl : TreeView
     * <summary>A treeview representation of processes</summary>
     * */
    public partial class ProcessesControl : TreeView
    {
        public ProcessesControl()
        {
            InitializeComponent();

            this.HideSelection = false;

            ImageList = MEDIcon.IconsImageList;
            StateImageList = MEDIcon.StatesImageList;
        }


        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public object? CurrentProperty
        {
            get => this.SelectedNode?.Tag;
            set => ShowProperty(value);
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public object[] CurrentProperties
        {
            get
            {
                List<object> objects = new();
                foreach (TreeNode node in this.Nodes)
                    if (node.Tag != null)
                        objects.Add(node.Tag);
                return objects.ToArray();
            }
            set => ShowProperties(value);
        }

        public void ShowProperty(object? o)
        {
            if (o == null)
                return;
            foreach (TreeNode node in this.Nodes)
                if (node.Tag == o)
                {
                    this.SelectedNode = node;
                    return;
                }
            this.SelectedNode = null;
        }

        public void ShowProperties(object[] items, TreeNode? rootNode = null, bool clear = false)
        {

            object? currentObject = this.SelectedNode?.Tag;
            TreeNodeCollection nodes;
            int insertNodeIndex = int.MaxValue;
            if (rootNode == null)
                nodes = this.Nodes;
            else
                nodes = rootNode.Nodes;
            if (clear)
                NodesClear(rootNode);
            else
            {
                foreach (var item in items)
                    if (item == null)
                        continue;
                    else if (ObjectsNodes.ContainsKey(item.GetHashCode()))
                    {
                        ObjectsNodes.Remove(item.GetHashCode(), out TreeNode? node);
                        if (node != null && node.Parent == rootNode)
                        {
                            insertNodeIndex = node.Index;
                            node.Remove();
                        }
                    }
                NodesClean();
            }

            AddItems(items, nodes);

            if (currentObject != null)
            {
                if (ObjectsNodes.ContainsKey(currentObject.GetHashCode()))
                {
                    SelectedNode = ObjectsNodes[currentObject.GetHashCode()];
                }
            }
            if (nodes.Count > 0 && SelectedNode == null)
                SelectedNode = nodes[0];
        }
        public void NodesClear(TreeNode? rootNode = null)
        {
            if (rootNode == null)
            {
                this.Nodes.Clear();
                return;
            }

            var nodes = rootNode.Nodes;

            foreach (TreeNode node in nodes)
            {
                if (node == null)
                    continue;

                if (node.Tag != null && ObjectsNodes.ContainsKey(node.Tag.GetHashCode()))
                    ObjectsNodes.Remove(node.Tag.GetHashCode());
                if (node.Nodes.Count > 0)
                    NodesClear(node);
                nodes.Clear();
            }
            NodesClean();
        }
        public void NodesClean()
        {
            foreach (KeyValuePair<int, TreeNode> kvp in ObjectsNodes.ToArray())
            {
                if (kvp.Value == null
                    || kvp.Value.Handle == 0
                    || kvp.Value.Tag == null
                    || (kvp.Value.Tag is Process && (((Process)kvp.Value.Tag).IsDisposed || ((Process)kvp.Value.Tag).Disposing))
                    || (kvp.Value.Tag is Control && (((Control)kvp.Value.Tag).IsDisposed || ((Control)kvp.Value.Tag).Disposing))
                )
                {
                    TreeNode? node;
                    ObjectsNodes.Remove(kvp.Key, out node);
                    node?.Remove();
                }
            }
        }

        private Dictionary<int, TreeNode> ObjectsNodes = new Dictionary<int, TreeNode>();

        /**
         * AddItems
         * 
         * */
        public void AddItems(object[] items, TreeNodeCollection nodes, bool addChildren = true)
        {
            foreach (var item in items)
                if (item != null)
                    AddItem(item, nodes, addChildren);
        }

        /**
         * AddItem
         * 
         * */
        public TreeNode? AddItem(object? item, TreeNodeCollection nodes, bool addChildren = true)
        {
            if (item == null)
                return null;
            try
            {
                var disposed = false;
                if (item is Control ctrl)
                {
                    if (ctrl.IsDisposed || ctrl.Disposing)
                        disposed = true;
                }
                else if (item is Process process1 && (process1.IsDisposed || process1.Disposing))
                    disposed = true;

                if (disposed)
                {
                    if (ObjectsNodes.ContainsKey(item.GetHashCode()))
                    {
                        ObjectsNodes.Remove(item.GetHashCode(), out TreeNode? n);
                        n?.Remove();
                    }
                    return null;
                }
            }
            catch
            {
                if (item != null && ObjectsNodes.ContainsKey(item.GetHashCode()))
                {
                    TreeNode? n;
                    ObjectsNodes.Remove(item.GetHashCode(), out n);
                    n?.Remove();
                }
                return null;
            }
            bool isRootNodes = nodes == this.Nodes || nodes == this.Nodes[0].Nodes;
            bool replaceNodeCache = true;

            if (ObjectsNodes.ContainsKey(item.GetHashCode()))
            {
                TreeNode n = (TreeNode)ObjectsNodes[item.GetHashCode()];
                if (n == null || n.Handle == 0)
                    NodesClean();
                else if (isRootNodes)
                {
                    if (n.Parent == null || n.Parent.Parent == null)
                        return n;
                }
                else if (n.Parent != null && n.Parent.Nodes == nodes)
                    return n;
                else if (addChildren)
                    addChildren = !(n.Parent == null || n.Parent.Parent == null);
                else
                    replaceNodeCache = !(n.Parent == null || n.Parent.Parent == null);

                //Priority to root
                if (replaceNodeCache)
                    ObjectsNodes.Remove(item.GetHashCode());
            }

            string? name;
            string image = "";
            if (item is IProcess iprocess)
            {
                name = iprocess.Name;
                image = iprocess.ProcessIcon;

                iprocess.ProcessStateChanged -= ItemProcess_StateChanged;
                iprocess.ProcessStateChanged += ItemProcess_StateChanged;
            }
            else if (item is Performance performance)
            {
                name = "Performance";
                image = performance.Icon;
            }
            else if (item is Logger logger1)
            {
                name = "Logger";
                image = "help";
            }
            else
                name = item.ToString();
            if (image == "")
                image = "Null";

            TreeNode node = nodes.Add(name);

            if (replaceNodeCache)
                ObjectsNodes.Add(item.GetHashCode(), node);

            bool nodeExpand = node.Parent == null || node.Parent.Parent == null;

            node.Tag = item;
            node.ImageKey = image;
            node.SelectedImageKey = node.ImageKey;
            if (item is IProcess process)
            {
                if (!process.Enabled)
                    nodeExpand = false;

                ItemProcess_StateChanged(process, process.ProcessState);
                if (!process.Enabled)
                {
                    Font font = new(this.Font, FontStyle.Strikeout);
                    node.NodeFont = font;
                }
                else if (process is ImageProcess provider
                    && provider.FPSMax > 0)
                {
                    Font font = new(this.Font, FontStyle.Bold);
                    node.NodeFont = font;
                }
            }
            //else
            //    node.SelectedImageKey = "False";

            if (addChildren)
            {
                if (item is IProcesses processes)
                {
                    object[] items = processes.Items.ToArray();
                    //Reverse
                    if (node.Parent == null)
                        items = items.Reverse().ToArray<object>();
                    AddItems(items, node.Nodes);

                    if (isRootNodes
                        && processes.Performance != null
                        && processes.Performance.Logger != null
                        && processes.Performance.Logger.LastError != null
                        )
                    {
                        AddItem(processes.Performance.Logger, node.Nodes);
                    }
                }
                if (item is IProcess iProcess)
                {
                    //AddItems((item as IProcess).ObjectsProperties.Values.ToArray(), node.Nodes);
                    foreach (var kvp in iProcess.ObjectsProperties)
                    {
                        if (kvp.Value != null
                            && kvp.Value is List<IProcess> list
                            && list.Count > 0
                            && list.First() != item)
                        {
                            var subNode = node.Nodes.Add(kvp.Key);
                            subNode.SelectedImageKey = subNode.ImageKey = "next_blue";
                            AddItems(list.ToArray(), subNode.Nodes, false);
                        }
                    }

                    var eventScripts = ProcessStatic.GetEventScripts(iProcess, false);
                    foreach (var (prop, eventScript) in eventScripts)
                    {
                        var subNode = node.Nodes.Add(prop);
                        subNode.ImageKey = eventScript == null ? "Script" : eventScript.Icon;
                        subNode.SelectedImageKey = subNode.ImageKey;
                        subNode.Tag = eventScript;
                        subNode.ToolTipText = "Double-click to edit";
                        if (eventScript != null && !String.IsNullOrEmpty(eventScript.Script))
                            nodeExpand = true;
                        else
                            subNode.ForeColor = Color.Gray;
                    }

                    if (iProcess.Performance != null
                        && iProcess.Performance.LastError != null)
                    {
                        var error1 = iProcess.Performance.LastError;
                        var errNode = node.Nodes.Add($"{error1}");
                        errNode.ImageKey = "alert";
                        errNode.SelectedImageKey = errNode.ImageKey;
                        errNode.Tag = error1;
                        nodeExpand = true;
                    }
                }
            }

            if (item is GameController.GameController controller)
            {
                nodeExpand = false;
                var properties = controller.GetPropertiesDelegatesConsumers();
                var usagePropertiesMap = controller.UsagePropertiesMap;
                if (properties.Count > 0)
                {
                    foreach (var (property, consumers) in properties)
                    {
                        string propertyLabel;
                        if (property.StartsWith(controller.PropertyDomain))
                            propertyLabel = property.Substring(controller.PropertyDomain.Length);
                        else if (property.StartsWith("Controller"))
                            propertyLabel = property.Substring("Controller".Length);
                        else
                            propertyLabel = property;
                        if (usagePropertiesMap.TryGetValue(propertyLabel, out UsagePropertiesMapItem? usagePropertiesMapItem))
                            propertyLabel += " = " + usagePropertiesMapItem.Properties;
                        var subNode = node.Nodes.Add(propertyLabel);
                        subNode.SelectedImageKey = subNode.ImageKey = "next_blue";
                        AddItems(consumers.Value.Keys.ToArray(), subNode.Nodes, false);
                    }
                }

                //if (controller.UsagePropertiesMap.Count > 0)
                //{
                //    var mapNode = node.Nodes.Add("Map");
                //    mapNode.SelectedImageKey = mapNode.ImageKey = "array";
                //    foreach (var (usage, usagePropertiesMapItem) in controller.UsagePropertiesMap)
                //    {
                //        if (usage.StartsWith("__p__"))
                //            continue;
                //        var subNode = mapNode.Nodes.Add($"{usage} = {usagePropertiesMapItem.Properties}");
                //        subNode.SelectedImageKey = subNode.ImageKey = "next_blue";

                //    }
                //}
            }
            else if (item is Logger logger
                && logger.LastErrorsCount > 0)
            {
                foreach (var (perf, errors) in logger.LastErrors)
                    if (errors.Count > 0)
                    {
                        var error0 = errors.Last();
                        var perfNode = node.Nodes.Add($"{perf.Name} ({errors.Count}) {error0.DelayToString()} : {error0.Message}");
                        perfNode.ImageKey = "alert";
                        perfNode.SelectedImageKey = perfNode.ImageKey;
                        perfNode.Tag = perf;
                        int index = 0;
                        foreach (var error in errors.Reverse())
                            if (index++ > 0)
                            {
                                var subNode = perfNode.Nodes.Add($"{error.DelayToString()} : {error.Message}");
                                subNode.ImageKey = "alert";
                                subNode.SelectedImageKey = subNode.ImageKey;
                                subNode.Tag = error;
                            }
                    }
                nodeExpand = true;
            }

            if (nodeExpand)
                node.Expand();

            return node;
        }

        void ItemProcess_StateChanged(IProcess sender, System.Threading.ThreadState state)
        {
            Invoke(UpdateNodeState, sender, state);
        }

        void UpdateNodeState(IProcess sender, System.Threading.ThreadState state)
        {
            if (ObjectsNodes.ContainsKey(sender.GetHashCode()))
            {
                TreeNode? node = ObjectsNodes[sender.GetHashCode()];
                if (node == null)
                    return;
                node.StateImageKey = state == System.Threading.ThreadState.Suspended ? "AutoReset" : (state == System.Threading.ThreadState.Running ? "True" : "False");
            }
        }
    }
}
