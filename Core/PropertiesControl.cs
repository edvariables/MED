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
        }

        public void ShowProperties(object[] items)
        {
            processesControl1.ShowProperties(items);
            if (items.Length == 0)
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
            cboObjectsList.Items.Add(node);
            if (node is IProcess)
                cboObjectsList.Items.AddRange((node as IProcess).ObjectsProperties.Values.ToArray());

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
    }
}
