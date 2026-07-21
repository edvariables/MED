using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
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

            ImageList = Core.Settings.IconsImageList;
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

        public void ShowProperties(object[] items, TreeNode rootNode = null, bool clear = true)
        {
            object currentObject = this.SelectedNode?.Tag;
            TreeNodeCollection nodes;
            if (rootNode == null)
                nodes = this.Nodes;
            else
                nodes = rootNode.Nodes;
            if (clear)
                NodesClear(rootNode);

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
                    || kvp.Value.Tag is Process && (kvp.Value.Tag as Process).IsDisposed
                    || kvp.Value.Tag is Control && (kvp.Value.Tag as Control).IsDisposed
                    )
                    ObjectsNodes.Remove(kvp.Key);
            }
        }

        private Dictionary<int, TreeNode> ObjectsNodes = new Dictionary<int, TreeNode>();

        public void AddItems(object[] items, TreeNodeCollection nodes)
        {
            foreach (var item in items)
                AddItem(item, nodes);
        }

        public TreeNode AddItem(object item, TreeNodeCollection nodes)
        {
            if (ObjectsNodes.ContainsKey(item.GetHashCode()))
            {
                TreeNode n = (TreeNode)ObjectsNodes[item.GetHashCode()];
                if (n.Handle == 0)
                    NodesClean();
                else
                    return n;
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

            if (item is Processes)
            {
                AddItems((item as Processes).Items.ToArray(), node.Nodes);
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

            if (nodes == this.Nodes)
                node.Expand();

            return node;
        }
    }
}
