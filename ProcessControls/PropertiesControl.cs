using DynamicData;
using Emgu.CV.Aruco;
using MED.Core;
using MED.Imaging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using static System.ComponentModel.Design.ObjectSelectorEditor;

namespace MED
{
    public partial class PropertiesControl : UserControl
    {
        public PropertiesControl()
        {
            InitializeComponent();

            InitProcessClasses();

        }

        Dictionary<string, string> ProcessClasses = new();
        private void InitProcessClasses()
        {
            ProcessClasses.Add("Render", typeof(MED.Imaging.Render).FullName ?? "");
            ProcessClasses.Add("ScreenSplitter", typeof(MED.Imaging.ScreenSplitter).FullName ?? "");
            ProcessClasses.Add("Project", typeof(MED.Processes).FullName ?? "");
            ProcessClasses.Add("Images", typeof(MED.Imaging.Images).FullName ?? "");
            ProcessClasses.Add("Collider mover", typeof(MED.Imaging.ImageMover).FullName ?? "");
            ProcessClasses.Add("Attractor", typeof(MED.Imaging.Attractor).FullName ?? "");
            ProcessClasses.Add("Propulsor", typeof(MED.Imaging.Propulsor).FullName ?? "");
            ProcessClasses.Add("VideoMover", typeof(MED.Imaging.VideoMover).FullName ?? "");
            ProcessClasses.Add("VideoCapture", typeof(MED.Imaging.VideoCapture).FullName ?? "");
            ProcessClasses.Add("VideoFileReader", typeof(MED.Imaging.VideoFileReader).FullName ?? "");
            ProcessClasses.Add("Background", typeof(MED.Imaging.Background).FullName ?? "");
            ProcessClasses.Add("Gravity", typeof(MED.Imaging.Gravity).FullName ?? "");
            ProcessClasses.Add("ImageSourced", typeof(MED.Imaging.ImageSourced).FullName ?? "");
            ProcessClasses.Add("Keyboard", typeof(MED.GameController.KeyboardController).FullName ?? "");
            ProcessClasses.Add("Joystick", typeof(MED.GameController.JoystickHIDController).FullName ?? "");
            //ProcessClasses.Add("Ball", (typeof(MED.Imaging.ImageMover).FullName ?? "") + "(ImageFile=../Movers/Ball.*.png;)");

            contextMenuAddProcess.Items.Clear();
            foreach (var proc in ProcessClasses)
            {
                var item = contextMenuAddProcess.Items.Add(proc.Key);
                item.Click += contextMenuAddProcessItem_Click;
            }
        }

        [Setting]
        [SettingsDescription("Hauteur de l'arborescence")]
        public int SplitterDistance
        {
            get => splitContainer1.SplitterDistance;
            set => splitContainer1.SplitterDistance = value;
        }

        public ProcessesControl ProcessesControl { get => processesControl1; }

        public object CurrentProperty
        {
            get => propertyGrid.SelectedObject;
            set => ShowProperty(value);
        }
        public object[] CurrentProperties
        {
            get => cboObjectsList.Items.OfType<object>().ToArray();
            set => ShowProperties(value);
        }

        public void ShowProperty(object o)
        {
            propertyGrid.SelectedObject = o;
            processesControl1.ShowProperty(o);
        }

        public void ShowProperties(object[]? items, TreeNode? rootNode = null, bool clear = false)
        {
            if (items == null)
                return;

            processesControl1.SuspendLayout();

            processesControl1.ShowProperties(items, rootNode, clear);
            if (processesControl1.SelectedNode != null)
                ShowNodeProperties(processesControl1.SelectedNode);
            else if (items.Length == 0)
                ShowNodeProperties(null);
            else
                ShowNodeProperties(items[0]);

            processesControl1.ResumeLayout();
        }

        /**
         * 
         */
        public void ShowNodeProperties(object? nodeItem)
        {
            if (nodeItem == null)
                return;

            if (nodeItem is TreeNode node)
                nodeItem = node.Tag;

            object currentObject = propertyGrid.SelectedObject;
            cboObjectsList.Items.Clear();
            if (nodeItem == null)
                return;

            bool containsNode = false;
            if (nodeItem is IProcess iProcess)
                foreach (var props in iProcess.ObjectsProperties)
                {
                    cboObjectsList.Items.Add(new KeyValuePair<string, object>(props.Key, props.Value));
                    if (props.Value.Equals(nodeItem))
                        containsNode = true;
                }

            if (!containsNode)
                cboObjectsList.Items.Insert(0, nodeItem);

            if (cboObjectsList.Items.Count > 0)
            {
                if (currentObject != null)
                {
                    if (cboObjectsList.Items.Contains(currentObject))
                        cboObjectsList.SelectedIndex = cboObjectsList.Items.IndexOf(currentObject);
                    else
                    {
                        int index = 0;
                        foreach (var item in cboObjectsList.Items)
                            if (currentObject.GetType().Equals(item.GetType())
                                && currentObject.ToString() == item.ToString())
                            {
                                cboObjectsList.SelectedIndex = index;
                                break;
                            }
                            else
                                index++;
                    }
                }
            }
            if (cboObjectsList.Items.Count > 0 && cboObjectsList.SelectedIndex == -1)
                cboObjectsList.SelectedIndex = 0;
        }

        private void ProcessesControl1_BeforeSelect(object sender, TreeViewCancelEventArgs e)
        {
            ShowNodeProperties(e.Node);
        }

        private void cboObjectsList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboObjectsList.SelectedIndex == -1)
                propertyGrid.SelectedObject = null;
            else
            {
                var obj = cboObjectsList.Items[cboObjectsList.SelectedIndex];
                if (obj is KeyValuePair<string, object> pair)
                    obj = pair.Value;
                //if (obj is IEnumerable collection)
                //    obj = Parser.ConvertToExpando(collection);

                propertyGrid.SelectedObject = obj;
            }
        }

        private void cmdRefresh_Click(object sender, EventArgs e)
        {
            //SIC Does not work : Les objets semblent être une copie
            propertyGrid.SelectedObject = null;
            cboObjectsList_SelectedIndexChanged(sender, e);
        }

        /***
         * Menus
         * 
         * TODO
         * 
         * */
        private void processesControl1_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                contextMenuProcesses.Show((Control)sender, e.Location);
            }
        }

        private void processesControl1_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                IProcess? process = e.Node.Tag != null && e.Node.Tag is IProcess ? (IProcess)e.Node.Tag : null;
                IProcesses? processes = e.Node.Parent != null && process != null && e.Node.Parent.Tag is IProcesses ? (IProcesses)e.Node.Parent.Tag : null;
                toolStripMenuProcAdd.Visible = process != null;
                toolStripMenuProcRemove.Visible = process != null;
                toolStripMenuItemProcessEnabled.Visible = process != null;
                if (process != null)
                {
                    toolStripMenuItemProcessEnabled.Image = MEDIcon.GetImage(process.Enabled ? "ok" : "close");
                    //toolStripMenuItemProcessEnabled.Text= process.Enabled ? "Process actif" : "Process désactivé";
                    toolStripMenuItemProcessEnabled.Font = process.Enabled ? this.Font : new(this.Font, FontStyle.Strikeout);
                }
                toolStripMenuItemMoveBefore.Visible = processes != null && e.Node.Index > 0;
                toolStripMenuItemMoveAfter.Visible = processes != null && e.Node.Parent != null && e.Node.Index < e.Node.Parent.Nodes.Count - 1;

                processesControl1.SelectedNode = e.Node;
                contextMenuProcesses.Show((Control)sender, e.Location);
            }
        }

        private void ProcessesControl1_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            EventScript? eventScript = e.Node.Tag != null && e.Node.Tag is EventScript ? (EventScript)e.Node.Tag : null;
            if (eventScript != null)
            {
                var f = new EventScriptForm(eventScript);
                f.Show(FindForm());
            }
        }

        private void toolStripMenuProcAdd_Click(object sender, EventArgs e)
        {
            contextMenuAddProcess.Show(contextMenuProcesses.Left, contextMenuProcesses.Top);
        }

        private void contextMenuAddProcessItem_Click(object? sender, EventArgs e)
        {
            if (sender == null)
                return;
            var processName = ((ToolStripMenuItem)sender).ToString();
            try
            {
                var processClass = ProcessClasses[processName];
                var process = ProcessStatic.CreateProcess(processClass, "", processName, true, Performance.Empty(), null);
                if (process == null)
                    return;
                if (process is IProcesses)
                    if (processesControl1.SelectedNode == null || processesControl1.SelectedNode.Tag is not IProcess)
                    {
                        //TODO Add to Studio.Project.Processes
                        ShowProperties([process]);
                        return;
                    }
                TreeNode? selectedNode = processesControl1.SelectedNode;
                IProcess selectedProcess = (IProcess)selectedNode.Tag;
                TreeNode? selectedParentNode = selectedNode.Parent == null ? null : selectedNode.Parent;
                IProcess? selectedParentProcess = selectedParentNode == null || selectedParentNode.Tag == null ? null
                                            : (IProcess)selectedParentNode.Tag;
                if (selectedProcess is not IProcesses
                    && selectedParentProcess is IProcesses)
                {
                    selectedProcess = selectedParentProcess;
                    selectedNode = selectedParentNode;
                }

                if (selectedProcess is IProcesses)
                {
                    var items = ((IProcesses)selectedProcess).Items;

                    //Name
                    int processNameIndex = 0;
                    foreach (var item in items)
                        if (item.Name == processName || item.Name == $"{processName}{processNameIndex}")
                            processNameIndex++;
                    if (processNameIndex > 0)
                        process.Name = processName = $"{processName}{processNameIndex}";

                    //Add or Insert
                    if (items.Count == 0)
                        items.Add(process);
                    else
                    {
                        items.Insert(items.Count - 1, process);
                        var render = items.First();
                        var provider = items.Last();
                        if ((provider is IProvider) && (process is IConsumer consumer) && provider.Enabled)
                            ((IProvider)provider).AddConsumer(consumer, "Image");//TODO default property
                    }
                    ShowProperties([selectedProcess], selectedNode?.Parent);
                    return;
                }
                MessageBox.Show("Impossible de déterminer un jeu de process parent.", "Ajouter un process");
                process.Dispose();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Impossible de créer ce process {processName} : \n{ex.ToString()}", "Ajout d'un process");
                return;
            }
        }

        private void toolStripMenuProcRemove_Click(object sender, EventArgs e)
        {

            if (processesControl1.SelectedNode == null || processesControl1.SelectedNode.Tag == null)
            {
                MessageBox.Show("Veuillez sélectionner un process.");
                return;
            }
            var process = (IProcess)processesControl1.SelectedNode.Tag;

            TreeNode? selectedParentNode = processesControl1.SelectedNode.Parent == null || processesControl1.SelectedNode.Parent.Tag == null ? null
                                            : processesControl1.SelectedNode.Parent;
            IProcess? selectedParentProcess = processesControl1.SelectedNode.Parent == null || processesControl1.SelectedNode.Parent.Tag == null ? null
                                            : (IProcess)processesControl1.SelectedNode.Parent.Tag;
            if (selectedParentProcess != null)
            {
                if (MessageBox.Show($"Êtes vous sûr de vouloir supprimer ce process {process.ToString()} ?", "Supprimer un process", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                    return;
                if (selectedParentProcess is IProcesses)
                    ((IProcesses)selectedParentProcess).Items.Remove(process);
                process.Dispose();
            }
            else
            {
                selectedParentNode = processesControl1.SelectedNode.Parent == null || processesControl1.SelectedNode.Parent.Parent == null || processesControl1.SelectedNode.Parent.Parent.Tag == null ? null
                                            : processesControl1.SelectedNode.Parent.Parent;
                selectedParentProcess = selectedParentNode == null ? null
                                            : (IProcess)selectedParentNode.Tag;
                if (selectedParentProcess != null && processesControl1.SelectedNode.Parent != null)
                {
                    switch (processesControl1.SelectedNode.Parent.Text)
                    {
                        case "Images vers":
                            if (MessageBox.Show($"Êtes vous sûr de vouloir retirer ce consommateur d'images {process.ToString()} ?", "Supprimer un consommateur d'images", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                                return;
                            if (selectedParentProcess is ImageProcess imageProcess && process is IImageConsumer consumer)
                                imageProcess.RemoveConsumer(consumer, "Image");
                            break;
                        case "Frames vers":
                            if (MessageBox.Show($"Êtes vous sûr de vouloir retirer ce consommateur de frames {process.ToString()} ?", "Supprimer un consommateur de frames", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                                return;
                            if (selectedParentProcess is ImageProcess imageProcess2 && process is IMatFrameConsumer consumer2)
                                imageProcess2.RemoveConsumer(consumer2, "Frame");
                            break;
                        default:
                            MessageBox.Show($"Non implémenté : {processesControl1.SelectedNode.Parent.Text}");
                            break;
                    }
                }
            }
            if (selectedParentProcess != null)
                ShowProperties([selectedParentProcess], selectedParentNode?.Parent);
        }

        private void MoveItemInProcessesItems(TreeNode node, int offset)
        {
            if (node == null)
                return;
            if (node.Parent == null)
                return;
            if (node.Tag is IProcess process
                && node.Parent.Tag is IProcesses processes)
            {
                var currentIndex = node.Index;
                processes.Items.Remove(process);
                processes.Items.Insert(currentIndex + offset, process);
                processesControl1.ShowProperties(processes.Items.ToArray(), node.Parent, true);
            }
        }
        private void toolStripMenuItemMoveBefore_Click(object sender, EventArgs e) => MoveItemInProcessesItems(processesControl1.SelectedNode, -1);

        private void toolStripMenuItemMoveAfter_Click(object sender, EventArgs e) => MoveItemInProcessesItems(processesControl1.SelectedNode, +1);

        private void toolStripMenuItemProcessEnabled_Click(object sender, EventArgs e)
        {
            var node = processesControl1.SelectedNode;
            if (node == null)
                return;
            if (node.Parent == null)
                return;
            if (node.Tag is IProcess process)
            {
                process.Enabled = !process.Enabled;

                ShowProperties([process], node.Parent);
            }
        }
    }
}
