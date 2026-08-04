using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Design;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.Design;

namespace MED
{
    /**
     * class MEDIcon
     * <summary>Image and images list provider</summary>
     * */
    public static class MEDIcon/*(string name, string label, Image image)*/
    {
        //public string Name { set; get; } = name;
        //public string Label { set; get; } = label;
        //public Image Image { set; get; } = image;

        //public override string ToString() => Label;

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
#pragma warning disable CS8602 // Déréférencement d'une éventuelle référence null.
                foreach (var kvp in MEDIcons.ResourceManager.GetResourceSet(System.Globalization.CultureInfo.InvariantCulture, false, false))
                    if (((DictionaryEntry)kvp).Value is Image)
                    {
                        string name = (string)((DictionaryEntry)kvp).Key;
                        Image? image = (Image?)((DictionaryEntry)kvp).Value;
                        if (image != null)
                            imageList.Add(name, image);
                    }
#pragma warning restore CS8602 // Déréférencement d'une éventuelle référence null.
                return imageList;
            }
        }

        private static ImageList? _StatesImageList = null;
        public static ImageList? StatesImageList
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
        public static Bitmap? GetImage(string? name)
        {
            if (String.IsNullOrEmpty(name))
                return null;

            name = name.Replace("-", "_");
            var prop = typeof(MEDIcons).GetProperty(name, System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            if (prop == null)
                return null;
            object? value = prop.GetValue(null, null);
            if (value is Bitmap)
                return (Bitmap?)prop.GetValue(null, null);
            if (name != "Null")
                return GetImage("Null");
            return null;
        }
        public static Icon? GetIcon(string? name)
        {
            Bitmap? image = GetImage(name);
            if (image == null)
                return null;
            return System.Drawing.Icon.FromHandle(image.GetHicon());
        }

        #endregion
    }


}
