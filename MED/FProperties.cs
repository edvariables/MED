using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MED
{
    public partial class FProperties : Form
    {
        public FProperties()
        {
            InitializeComponent();

            Current = this;
        }
        public static FProperties Current { get; private set; }

        private void FProperties_Load(object sender, EventArgs e)
        {

        }

        public static object CurrentProperty
        {
            get => Current?.propertiesControl1.CurrentProperty;
            set => Current?.ShowProperty(value);
        }
        public static object[] CurrentProperties
        {
            get => Current?.propertiesControl1.CurrentProperties;
            set => Current?.propertiesControl1.ShowProperties(value);
        }

        public void ShowProperty(object o)
        {
            propertiesControl1.ShowProperty(o);
        }

        public void ShowProperties(object[] items)
        {
            propertiesControl1.ShowProperties(items);
        }

    }
}
