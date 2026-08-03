using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

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
            ProcessClasses.Add("Render", "MED.Imaging.Render");
            ProcessClasses.Add("ScreenSplitter", "MED.Imaging.ScreenSplitter");
            ProcessClasses.Add("Project", "MED.Processes");
            ProcessClasses.Add("Images", "MED.Imaging.Images");
            ProcessClasses.Add("EmguMoving", "MED.Imaging.EmguMoving");
            ProcessClasses.Add("EDVideoCapture", "MED.Imaging.EDVideoCapture");
            ProcessClasses.Add("Background", "MED.Imaging.Background");
            ProcessClasses.Add("(Autre...)", "");

            toolStripCboProcAddClasses.Items.Clear();
            foreach (var proc in ProcessClasses)
            {
                toolStripCboProcAddClasses.Items.Add(proc.Key);
            }
        }

        [Setting]
        [SettingsDescription("Hauteur de l'arborescence")]
        public int SplitterDistance
        {
            get => splitContainer1.SplitterDistance;
            set => splitContainer1.SplitterDistance = value;
        }

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

        public void ShowProperties(object[] items, TreeNode rootNode = null, bool clear = false)
        {
            processesControl1.ShowProperties(items, rootNode, clear);
            if (processesControl1.SelectedNode != null)
                ShowNodeProperties(processesControl1.SelectedNode);
            else if (items.Length == 0)
                ShowNodeProperties(null);
            else
                ShowNodeProperties(items[0]);
        }

        /**
         * 
         */
        private void ShowNodeProperties(object node)
        {
            if (node is TreeNode)
                node = (node as TreeNode).Tag;

            object currentObject = propertyGrid.SelectedObject;
            cboObjectsList.Items.Clear();
            if (node == null)
                return;
            if (node is IProcess)
                cboObjectsList.Items.AddRange((node as IProcess).ObjectsProperties.Values.ToArray());

            if(!cboObjectsList.Items.Contains(node))
                cboObjectsList.Items.Insert(0, node);

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
                propertyGrid.SelectedObject = cboObjectsList.Items[cboObjectsList.SelectedIndex];
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

                toolStripMenuProcAdd.Visible = e.Node.Tag != null;
                toolStripCboProcAddClasses.Visible = e.Node.Tag != null;
                toolStripMenuProcAdd.Visible = e.Node.Tag != null;
                toolStripMenuProcRemove.Visible = e.Node.Tag != null;
                processesControl1.SelectedNode = e.Node;
                contextMenuProcesses.Show((Control)sender, e.Location);


            }
        }

        private void toolStripMenuProcAdd_Click(object sender, EventArgs e)
        {
            if (toolStripCboProcAddClasses.SelectedIndex == -1 || toolStripCboProcAddClasses.SelectedItem == null)
            {
                MessageBox.Show("Veuillez sélectionner un type de process.");
                contextMenuProcesses.Show(processesControl1, processesControl1.SelectedNode == null ? Point.Empty : processesControl1.SelectedNode.Bounds.Location);
                toolStripCboProcAddClasses.Visible = true;
                toolStripCboProcAddClasses.Focus();
                return;
            }

            try
            {
                var processName = toolStripCboProcAddClasses.SelectedItem.ToString();
                var processClass = ProcessClasses[processName];
                var process = ProcessStatic.CreateProcess(processClass, "", processName, true, Performance.Empty(), null);

                if (process is IProcesses)
                    if (processesControl1.SelectedNode == null || processesControl1.SelectedNode.Tag is not IProcess)
                    {
                        //TODO Add to Studio.Project.Processes
                        ShowProperties([process]);
                        return;
                    }
                TreeNode selectedNode = processesControl1.SelectedNode;
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
                    var items = (selectedProcess as IProcesses).Items;

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
                        if ((provider is IProvider) && (process is IConsumer))
                            (provider as IProvider).AddConsumer((IConsumer)process, "Image");//TODO default property
                    }
                    ShowProperties([selectedProcess], selectedNode.Parent);
                    return;
                }
                MessageBox.Show("Impossible de déterminer un jeu de process parent.", "Ajouter un process");
                process.Dispose();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Impossible de créer ce process {toolStripCboProcAddClasses.SelectedItem.ToString()} : \n{ex.ToString()}", "Ajout d'un process");
                return;
            }
        }

        private void toolStripCboProcAddClasses_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\n')
                toolStripMenuProcAdd_Click(sender, e);
        }

        private void toolStripCboProcAddClasses_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
                contextMenuProcesses.Visible = false;
        }

        private void toolStripMenuProcRemove_Click(object sender, EventArgs e)
        {

            if (processesControl1.SelectedNode == null || processesControl1.SelectedNode.Tag == null)
            {
                MessageBox.Show("Veuillez sélectionner un process.");
                return;
            }
            var process = (IProcess)processesControl1.SelectedNode.Tag;
            if (MessageBox.Show($"Êtes vous sûr de vouloir supprimer ce process {process.ToString()} ?", "Supprimer un process", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                return;

            TreeNode? selectedParentNode = processesControl1.SelectedNode.Parent == null || processesControl1.SelectedNode.Parent.Tag == null ? null
                                            : processesControl1.SelectedNode.Parent;
            IProcess? selectedParentProcess = processesControl1.SelectedNode.Parent == null || processesControl1.SelectedNode.Parent.Tag == null ? null
                                            : (IProcess)processesControl1.SelectedNode.Parent.Tag;
            if (selectedParentProcess != null)
                if (selectedParentProcess is IProcesses)
                    (selectedParentProcess as IProcesses).Items.Remove(process);
            process.Dispose();
            if (selectedParentProcess != null)
                ShowProperties([selectedParentProcess], selectedParentNode.Parent);
        }
    }
}
