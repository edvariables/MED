using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using System.Globalization;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using System.Xml;
using System.Xml.Linq;
using static System.ComponentModel.TypeConverter;

namespace MED
{
    public class MEDIcon(string name, string label, Image image)
    {
        public string Name { set; get; } = name;
        public string Label { set; get; } = label;
        public Image Image { set; get; } = image;

        public override string ToString() => Label;

        #region static

        private static ImageList? _IconsImageList = null;
        public static ImageList IconsImageList
        {
            get
            {
                if (_IconsImageList != null)
                    return _IconsImageList;

                ImageList imageList = new();
                foreach (var (name, image) in IconsImageDictionary)
                    imageList.Images.Add(name, image);
                return _IconsImageList = imageList;
            }
        }
        public static Dictionary<string, Image> IconsImageDictionary
        {
            get
            {
                //First call initialize ResourceSet
                if (MEDIcons.ResourceManager.GetObject("MED", System.Globalization.CultureInfo.InvariantCulture) == null)
                    return new();
                Dictionary<string, Image> imageList = new();
                foreach (var kvp in MEDIcons.ResourceManager.GetResourceSet(System.Globalization.CultureInfo.InvariantCulture, false, false))
                    if (((DictionaryEntry)kvp).Value is Image)
                    {
                        string name = (string)((DictionaryEntry)kvp).Key;
                        Image image = (Image)((DictionaryEntry)kvp).Value;
                        imageList.Add(name, image);
                    }
                return imageList;
            }
        }

        private static ImageList _StatesImageList = null;
        public static ImageList StatesImageList
        {
            get
            {
                if (_StatesImageList != null)
                    return _StatesImageList;
                //First call initialize ResourceSet
                if (MEDIcons.ResourceManager.GetObject("MED", System.Globalization.CultureInfo.InvariantCulture) == null)
                    return null;
                ImageList imageList = new();

                string[] images = ["False", "True", "AutoReset", "Alert"];
                foreach (string name in images)
                {
                    var image = MEDIcons.ResourceManager.GetObject(name);
                    if (image is Image)
                        imageList.Images.Add(name, (Image)image);
                }
                return _StatesImageList = imageList;
            }
        }
        public static Bitmap GetImage(string name)
        {
            name = name.Replace("-", "_");
            var prop = typeof(MEDIcons).GetProperty(name, System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            if (prop == null)
                return null;
            object value = prop.GetValue(null, null);
            if (value is Bitmap)
                return (Bitmap)prop.GetValue(null, null);
            if (name != "Null")
                return GetImage("Null");
            return null;
        }
        public static Icon GetIcon(string name)
        {
            Bitmap image = GetImage(name);
            if (image == null)
                return null;
            return System.Drawing.Icon.FromHandle(image.GetHicon());
        }

        #endregion
    }

    /**
     * MEDIconNameConverter
     * 
     * TypeConverter for PropertyGrid
     * 
        In Process class : 
        [Editor(typeof(MEDIconSelectorEditor), typeof(UITypeEditor))]
        [TypeConverter(typeof(MEDIconNameConverter))]
        public virtual string ProcessIcon { get; set; }
    */
    public class MEDIconNameConverter : TypeConverter
    {
        static MEDIconNameConverter()
        {
            _items = ItemsToArray(MEDIcon.IconsImageDictionary);
        }

        #region Items
        // The selectable items
        private static readonly string[] _items;
        private static string[] ItemsToArray(Dictionary<string, Image> imageDictionary)
        {
            List<string> items = new();
            foreach (var (name, image) in imageDictionary)
                items.Add(name);

            return items.ToArray();
        }
        #endregion

        #region override TypeConverter
        //Thanks to György Kőszeg https://stackoverflow.com/questions/78590673/combobox-on-a-propertygrid

        // If your "ValueMember" is not a string, add its type as well
        // (eg. int, some custom enum, etc.)
        public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType) => destinationType == typeof(string);
        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) => sourceType == typeof(string);

        // Destination type is always string in a PropertyGrid but if your "ValueMember"
        // is some different type you might want to add it, too
        public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
        {

            if (destinationType != typeof(string))
                return base.ConvertTo(context, culture, value, destinationType);
            return value;
        }

        // You might want to parse from string and the type of your "ValueMember".
        // Both are strings in your example.
        public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
        {
            if (value is not string str)
                return base.ConvertFrom(context, culture, value);
            // 1. Parsing by text, case-insensitive
            string? result = _items.FirstOrDefault(i => String.Equals(i, str, StringComparison.OrdinalIgnoreCase));

            // 2. Parsing by value, case-sensitive
            result ??= _items.FirstOrDefault(i => i == str);

            return result ?? throw new ArgumentException($"Invalid value: {str}", nameof(value));
        }

        // This enables "ComboBox" for the property
        public override bool GetStandardValuesSupported(ITypeDescriptorContext? context) => true;

        // This tells that it's not a simple read-only drop down
        // but you can also type values to parse
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext? context) => false;

        // This returns the items to display in the drop-down area
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext? context) => new(_items);

        #endregion
    }


    /**
     * MEDIconSelectorEditor
     * UITypeEditor for PropertyGrid
     * 
        In Process class : 

        [Editor(typeof(MEDIconSelectorEditor), typeof(UITypeEditor))]
        [TypeConverter(typeof(MEDIconNameConverter))]
        public virtual string ProcessIcon { get; set; }
    */
    public class MEDIconSelectorEditor : UITypeEditor
    {
        public MEDIconSelectorEditor()
        {
        }

        // Indicates whether the UITypeEditor provides a form-based (modal) dialog,
        // drop down dialog, or no UI outside of the properties window.
        public override System.Drawing.Design.UITypeEditorEditStyle GetEditStyle(System.ComponentModel.ITypeDescriptorContext context)
        {
            return UITypeEditorEditStyle.DropDown;
        }

        // Displays the UI for value selection.
        public override object EditValue(System.ComponentModel.ITypeDescriptorContext context, System.IServiceProvider provider, object value)
        {
            //return base.EditValue(context, provider, value);

            // Return the value if the value is not of type String.
            if (value.GetType() != typeof(string))
                return value;

            // Uses the IWindowsFormsEditorService to display a
            // drop-down UI in the Properties window.
            edSvc = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));
            if (edSvc != null)
            {
                if (_IconListBox == null)
                {
                    _IconListBox = new IconListBox(MEDIcon.IconsImageDictionary, value);
                    _IconListBox.SelectedIndexChanged += IconListBox_SelectedIndexChanged;
                }
                // Display a selection control and retrieve the value.
                edSvc.DropDownControl(_IconListBox);

                if (_IconListBox.SelectedItem != null)
                    return _IconListBox.SelectedItem;
            }
            return value;
        }

        private IWindowsFormsEditorService edSvc;

        private void IconListBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            var t = (sender as Control).Parent.GetType().Namespace;
            edSvc?.CloseDropDown();
        }
        IconListBox _IconListBox;

        // Draws a representation of the property's value.
        public override void PaintValue(System.Drawing.Design.PaintValueEventArgs e)
        {
            // remove the lines (you cannot draw on these lines anymore)
            e.Graphics.ExcludeClip(
                new Rectangle(e.Bounds.X, e.Bounds.Y, e.Bounds.Width, 1));
            e.Graphics.ExcludeClip(
                new Rectangle(e.Bounds.X, e.Bounds.Y, 1, e.Bounds.Height));
            e.Graphics.ExcludeClip(
                new Rectangle(e.Bounds.Width, e.Bounds.Y, 1, e.Bounds.Height));
            e.Graphics.ExcludeClip(
                new Rectangle(e.Bounds.X, e.Bounds.Height, e.Bounds.Width, 1));

            string name = (String)e.Value;
            Image image = MEDIcon.GetImage(name);
            if (image != null)
            {
                e.Graphics.DrawImage(image, e.Bounds.X + 1, e.Bounds.Y);
            }


            base.PaintValue(e);
        }

        // Indicates whether the UITypeEditor supports painting a
        // representation of a property's value.
        public override bool GetPaintValueSupported(System.ComponentModel.ITypeDescriptorContext context)
        {
            return true;
        }
    }

    public class IconListBox : ListBox
    {
        public IconListBox(Dictionary<string, Image> imageDictionary, object selectValue) : base()
        {
            DrawMode = DrawMode.OwnerDrawFixed;

            _ImageDictionary = imageDictionary;

            Height = 16 * ListBox.DefaultItemHeight;
            Items.AddRange(imageDictionary.Keys.ToArray());
            SelectedItem = selectValue;
            DrawItem += IconListBox_DrawItem;
        }

        private void IconListBox_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if ((int)e.State == (int)DrawItemState.Selected + (int)DrawItemState.Focus)
                return;
            //base.OnDrawItem(e);
            e.DrawBackground();
            // Define the default color of the brush as black.
            Brush myBrush = Brushes.Black;


            if (_ImageDictionary.Count <= e.Index)
                return;
            var item = _ImageDictionary.ElementAt(e.Index);

            var bounds = e.Bounds;
            if (item.Value != null)
            {
                e.Graphics.DrawImage(item.Value, bounds.X, bounds.Y);
            }

            bounds.Offset(18, 0);

            // Draw the current item text based on the current Font 
            // and the custom brush settings.
            e.Graphics.DrawString(item.Key,
                e.Font, myBrush, bounds, StringFormat.GenericDefault);

            // If the ListBox has focus, draw a focus rectangle around the selected item.
            e.DrawFocusRectangle();
        }

        private Dictionary<string, Image> _ImageDictionary;



    }
}
