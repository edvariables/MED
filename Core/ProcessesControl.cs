using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace MED
{
    public partial class ProcessesControl : TreeView
    {
        public ProcessesControl()
        {
            InitializeComponent();

            this.HideSelection = false;

            ImageList = Core.Settings.IconsImageList;
            StateImageList = Core.Settings.StatesImageList;
        }


        public object CurrentProperty
        {
            get => this.SelectedNode.Tag;
            set => ShowProperty(value);
        }
        public object[] CurrentProperties
        {
            get
            {
                List<object> objects = new();
                foreach (TreeNode node in this.Nodes)
                    objects.Add(node.Tag);
                return objects.ToArray();
            }
            set => ShowProperties(value);
        }

        public void ShowProperty(object o)
        {
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
            object currentObject = this.SelectedNode?.Tag;
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
                TreeNode node;
                foreach (var item in items)
                    if (item == null)
                        continue;
                    else if (ObjectsNodes.ContainsKey(item.GetHashCode()))
                    {
                        ObjectsNodes.Remove(item.GetHashCode(), out node);
                        if (node.Parent == rootNode)
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
        public void NodesClear(TreeNode rootNode = null)
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
                if (kvp.Value.Handle == 0
                    || kvp.Value.Tag is Process && ((kvp.Value.Tag as Process).IsDisposed || (kvp.Value.Tag as Process).Disposing)
                    || kvp.Value.Tag is Control && ((kvp.Value.Tag as Control).IsDisposed || (kvp.Value.Tag as Control).Disposing)
                    )
                {
                    TreeNode node;
                    ObjectsNodes.Remove(kvp.Key, out node);
                    node.Remove();
                }
            }
        }

        private Dictionary<int, TreeNode> ObjectsNodes = new Dictionary<int, TreeNode>();

        public void AddItems(object[] items, TreeNodeCollection nodes, bool addChildren = true)
        {
            foreach (var item in items)
                if (item != null)
                    AddItem(item, nodes, addChildren);
        }

        public TreeNode AddItem(object item, TreeNodeCollection nodes, bool addChildren = true)
        {
            try
            {
                var disposed = false;
                if (item is Control)
                {
                    if ((item as Control).IsDisposed || (item as Control).Disposing)
                        disposed = true;
                }
                else if (item is Process && ((item as Process).IsDisposed) || (item as Process).Disposing)
                    disposed = true;

                if (disposed)
                {
                    if (ObjectsNodes.ContainsKey(item.GetHashCode()))
                    {
                        TreeNode n;
                        ObjectsNodes.Remove(item.GetHashCode(), out n);
                        n.Remove();
                    }
                    return null;
                }
            }
            catch
            {
                if (ObjectsNodes.ContainsKey(item.GetHashCode()))
                {
                    TreeNode n;
                    ObjectsNodes.Remove(item.GetHashCode(), out n);
                    n.Remove();
                }
                return null;
            }
            bool isRootNodes = nodes == this.Nodes || nodes == this.Nodes[0].Nodes;
            bool replaceNodeCache = true;

            if (ObjectsNodes.ContainsKey(item.GetHashCode()))
            {
                TreeNode n = (TreeNode)ObjectsNodes[item.GetHashCode()];
                if (n.Handle == 0)
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
                    replaceNodeCache= !(n.Parent == null || n.Parent.Parent == null);

                //Priority to root
                if(replaceNodeCache)
                    ObjectsNodes.Remove(item.GetHashCode(), out n);
            }

            string name;
            string image = "";
            if (item is IProcess)
            {
                name = (item as IProcess).Name;
                image = (item as IProcess).ProcessIcon;
            }
            else if (item is Performance)
            {
                name = "Performance";
                image = (item as Performance).Icon;
            }
            else
                name = item.ToString();
            if (image == "")
                image = "Null";

            TreeNode node = nodes.Add(name);

            if(replaceNodeCache)
                ObjectsNodes.Add(item.GetHashCode(), node);

            node.Tag = item;
            node.ImageKey = image;
            node.SelectedImageKey = node.ImageKey;
            node.StateImageKey = "False";

            if (addChildren)
            {
                if (item is IProcesses)
                {
                    object[] items = (item as IProcesses).Items.ToArray();
                    //Reverse
                    if (node.Parent == null)
                        items = items.Reverse().ToArray<object>();
                    AddItems(items, node.Nodes);
                }
                if (item is IProcess)
                {
                    //AddItems((item as IProcess).ObjectsProperties.Values.ToArray(), node.Nodes);
                    foreach (var kvp in (item as IProcess).ObjectsProperties)
                    {
                        if (kvp.Value is List<IProcess> && (kvp.Value as List<IProcess>).Count > 0 && (kvp.Value as List<IProcess>).First() != item)
                        {
                            var subNode = node.Nodes.Add(kvp.Key);
                            subNode.SelectedImageKey = subNode.ImageKey = "next_blue";
                            AddItems((kvp.Value as List<IProcess>).ToArray(), subNode.Nodes, false);
                        }
                        //else if (kvp.Value is IConsumer)
                        //{
                        //    var subNode = node.Nodes.Add("Consumer");
                        //    subNode.SelectedImageKey = subNode.ImageKey = "next_blue";
                        //    AddItems([kvp.Value], subNode.Nodes, false);
                        //}
                    }
                }

                if (node.Parent == null || node.Parent.Parent == null)
                    node.Expand();
            }

            return node;
        }
    }
}
