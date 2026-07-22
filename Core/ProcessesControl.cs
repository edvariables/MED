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

        public void ShowProperties(object[] items, TreeNode rootNode = null, bool clear = false)
        {
            object currentObject = this.SelectedNode?.Tag;
            TreeNodeCollection nodes;
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
                    if (ObjectsNodes.ContainsKey(item.GetHashCode()))
                    {
                        ObjectsNodes.Remove(item.GetHashCode(), out node);
                        if (node.Parent == rootNode)
                            node.Remove();
                    }
                NodesClean();
            }

            AddItems(items, nodes);

            if (currentObject != null)
            {
                foreach (TreeNode node in nodes)
                    if (currentObject.Equals(node.Tag))
                    {
                        SelectedNode = node;
                        break;
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
                if (ObjectsNodes.ContainsKey(node.Tag.GetHashCode()))
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

        public void AddItems(object[] items, TreeNodeCollection nodes)
        {
            foreach (var item in items)
                AddItem(item, nodes);
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
                else
                    addChildren = false;

                //Priority to root
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

            ObjectsNodes.Add(item.GetHashCode(), node);

            node.Tag = item;
            node.ImageKey = image;
            node.SelectedImageKey = node.ImageKey;
            node.StateImageKey = "False";

            if (addChildren)
            {
                if (item is Processes)
                {
                    object[] items = (item as Processes).Items.ToArray();
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
                        if (kvp.Value is List<IProcess>)
                        {
                            var subNode = node.Nodes.Add(kvp.Key);
                            AddItems((kvp.Value as List<IProcess>).ToArray(), subNode.Nodes);
                        }
                    }
                }

                if (node.Parent == null || node.Parent.Parent == null)
                    node.Expand();
            }

            return node;
        }
    }
}
