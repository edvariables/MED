using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using static System.Collections.Specialized.BitVector32;
using static System.Runtime.InteropServices.JavaScript.JSType;


/**
 * Les paramètres par défaut sont dans Settings.Settings.
 * Toutefois, les valeurs modifiées le sont pour tous les utilisateurs du PC. 
 * Les données sont donc enregistrées dans un fichier INI.
 * Sinon; on avait une confusion entre l'utilisateur Système pour le service.
 * 
 * */

namespace MED.Core
{
    public static class Settings
    {
        static Settings()
        {
            MyProjectsDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), Namespace) + "\\Projects";
        }

        private static ConcurrentDictionary<string, object> _values = new();
        private static ConcurrentDictionary<string, object> _saveValues = new();
        private static readonly bool _useCache = true;

        public static string Namespace
        {
            get
            {
                return Assembly.GetExecutingAssembly().GetTypes().First().Namespace;
            }
        }
        public const string ProcessFileExtension = ".med.json";
        public static readonly string MyProjectsDirectory;

        private static ImageList _IconsImageList = null;
        public static ImageList IconsImageList
        {
            get
            {
                if (_IconsImageList != null)
                    return _IconsImageList;
                //First call initialize ResourceSet
                if (EDIcons.ResourceManager.GetObject("EDV", System.Globalization.CultureInfo.InvariantCulture) == null)
                    return null;
                ImageList imageList = new();
                foreach (var kvp in EDIcons.ResourceManager.GetResourceSet(System.Globalization.CultureInfo.InvariantCulture, false, false))
                    if (((DictionaryEntry)kvp).Value is Image)
                    {
                        string name = (string)((DictionaryEntry)kvp).Key;
                        Image image = (Image)((DictionaryEntry)kvp).Value;
                        imageList.Images.Add(name, image);
                    }
                return _IconsImageList = imageList;
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
                if (EDIcons.ResourceManager.GetObject("EDV", System.Globalization.CultureInfo.InvariantCulture) == null)
                    return null;
                ImageList imageList = new();

                string[] images = ["False", "True", "AutoReset", "Alert"];
                foreach (string name in images) {
                    var image = EDIcons.ResourceManager.GetObject(name);
                    if (image is Image)
                        imageList.Images.Add(name, (Image)image);
                }
                return _StatesImageList = imageList;
            }
        }
        public static Bitmap GetImage(string name)
        {
            //System.Reflection.PropertyInfo prop = (System.Reflection.PropertyInfo)typeof(EDIcons).GetProperty(name);
            var prop = typeof(EDIcons).GetProperty(name, System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
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
            return System.Drawing.Icon.FromHandle(image.GetHicon());
        }
        /**
         * Get section|setting
         * */
        private static string GetCacheKey(string section, string setting)
        {
            return (section == null ? "" : section) + "|" + setting;
        }

        /**
         * Get section|setting
         * */
        private static string[] ParseCacheKey(string cacheKey)
        {

            string[] path = cacheKey.Split('|');
            if (path.Length == 1)
            {
                path.Append(cacheKey);
                path[0] = null;
            }
            else if (path[0] == "")
                path[0] = null;
            return path;
        }

        /**
         * Save
         * */
        public static void Save()
        {

            //MED.Core.Properties.Settings.Default.Save();

            foreach (string cacheKey in _saveValues.Keys)
            {
                object value = _saveValues[cacheKey];
                string[] path = ParseCacheKey(cacheKey);

                string settingsPath, settingsFile;
                string sectionPath = ParseSettingsPathSection(path[0], out settingsPath, out settingsFile);

                SaveValue(value == null ? "" : Parser.ObjectToString(value), path[1], sectionPath, settingsPath, settingsFile);
            }
            ClearCache();
        }

        /**
         * TODO
         */
        public static void SaveValue(object value, string item, string section, string settingsPath = "", string settingsFile = "")
        {
            switch (settingsFile)
            {
                case "file":
                default:

                    IniFile.WriteValue(item, value == null ? "" : Parser.ObjectToString(value), section);

                    break;

            }
        }

        /**
         * ParseSettingsPathSectionItem
         * Parse [file:]path:section|item
         * */
        public static string ParseSettingsPathSectionItem(in string item, out string section, out string settingsPath, out string settingsFile)
        {
            settingsPath = "";
            settingsFile = "";
            section = "";
            string sectionItem = ParseSettingsPathSection(settingsPath, out settingsPath, out settingsFile);

            int sep;
            if ((sep = sectionItem.IndexOf('|')) == -1)
                return sectionItem;

            section = sectionItem.Substring(0, sep).Trim();

            return sectionItem.Substring(sep + 1).Trim();
        }

        /**
         * ParseSettingsPathSection
         * Parse [file:]path:section
         * */
        public static string ParseSettingsPathSection(in string section, out string settingsPath, out string settingsFile)
        {

            settingsPath = "";
            settingsFile = "";

            if (section == null)
                return section;

            int sep;
            if ((sep = section.IndexOf(':')) == -1)
                return section;

            settingsFile = section.Substring(0, sep).Trim();
            if ((sep = settingsFile.IndexOf(':')) == -1)
            {
                settingsPath = settingsFile;
                settingsFile = "";
                return section;
            }

            settingsPath = section.Substring(0, sep).Trim();

            return section.Substring(sep + 1).Trim();
        }

        public static void ClearCache(bool saveValues = true, bool values = true, string section = null)
        {
            string settingsPath, settingsFile;
            string sectionPath = ParseSettingsPathSection(section, out settingsPath, out settingsFile);


            if (saveValues)
            {
                if (section != null && section != "")
                {
                    string pattern = GetCacheKey(section, "");
                    foreach (var kvp in _saveValues)
                        if (kvp.Key.StartsWith(pattern))
                        {
                            object value = kvp.Value;
                            _saveValues.Remove(kvp.Key, out value);
                        }
                }
                else
                    _saveValues.Clear();
            }
            if (values)
            {
                if (section != null && section != "")
                {
                    string pattern = GetCacheKey(section, "");

                    foreach (var kvp in _values)
                        if (kvp.Key.StartsWith(pattern))
                        {
                            object value = kvp.Value;
                            _values.Remove(kvp.Key, out value);
                        }
                }
                else
                    _values.Clear(); //safely
            }
        }

        /**
         * Get value from INI, Settings or Cache
         * */
        public static object GetValue(string setting, string section = null, object default_value = null)
        {
            string key = GetCacheKey(section, setting);
            if (_useCache
                && _values.ContainsKey(key))
                return _values[key];
            object value = IniFile.ReadValue(setting, section, default_value == null ? "" : default_value?.ToString());
            value = Parser.ObjectFromString((string)value, default_value);
            if (_useCache
                && !_values.ContainsKey(key))
                _values.TryAdd(key, value);
            return value;
        }
        /**
         * Set value
         * */
        public static void SetValue(string setting, string section = null, object set_value = null)
        {
            if (_useCache)
            {
                string key = GetCacheKey(section, setting);

                if (_values.ContainsKey(key))
                    _values[key] = set_value;
                else if (set_value != null)
                    _values.TryAdd(key, set_value);
                if (_saveValues.ContainsKey(key))
                    _saveValues[key] = set_value;
                else if (set_value != null)
                    _saveValues.TryAdd(key, set_value);
            }
            else
            {
                IniFile.WriteValue(setting, Parser.ObjectToString(set_value), section);
            }
        }

        /*********
         * 
         * Properties
         * 
         * *******/


        //public static string IP_Address
        //{
        //    get
        //    {
        //        return GetValue("IP_Address", null, MED.Core.Properties.Settings.Default.IP_Address).ToString();
        //    }
        //    set
        //    {
        //        SetValue("IP_Address", null, value);
        //    }
        //}

        //public static int IP_Port
        //{
        //    get
        //    {
        //        return int.Parse(GetValue("IP_Port", null, MED.Core.Properties.Settings.Default.IP_Port).ToString());
        //    }
        //    set
        //    {
        //        SetValue("IP_Port", null, value.ToString());
        //    }
        //}

        //public static int Service_Delay
        //{
        //    get
        //    {
        //        return int.Parse(GetValue("Service_Delay", null, MED.Core.Properties.Settings.Default.service_delay).ToString());
        //    }
        //    set
        //    {
        //        SetValue("Service_Delay", null, value.ToString());
        //    }
        //}

        //public static int COM_num
        //{
        //    get
        //    {
        //        return int.Parse(GetValue("COM_num", null, MED.Core.Properties.Settings.Default.COM_num).ToString());
        //    }
        //    set
        //    {
        //        SetValue("COM_num", null, value.ToString());
        //    }
        //}

        //public static string Bridge_Mode
        //{
        //    get
        //    {
        //        return GetValue("Bridge_Mode", null, MED.Core.Properties.Settings.Default.bridge_mode).ToString();
        //    }
        //    set
        //    {
        //        SetValue("Bridge_Mode", null, value);
        //    }
        //}
        //public static bool Bridge_Hub4Com
        //{
        //    get
        //    {
        //        return Bridge_Mode == "Hub4Com";
        //    }
        //}
        //public static bool Bridge_Internal
        //{
        //    get
        //    {
        //        return Bridge_Mode == "Internal";
        //    }
        //}

        //public static bool Com0Com_CreateCOM
        //{
        //    get
        //    {
        //        return bool.Parse(GetValue("Com0Com_CreateCOM", null, true).ToString());
        //    }
        //    set
        //    {
        //        SetValue("Com0Com_CreateCOM", null, value.ToString());
        //    }
        //}

        //public static string Com0Com_path
        //{
        //    get
        //    {
        //        return GetValue("Com0Com_path", null, MED.Core.Properties.Settings.Default.com0com_path).ToString();
        //    }
        //    set
        //    {
        //        SetValue("Com0Com_path", null, value);
        //    }
        //}

        //public static string Hub4Com_path
        //{
        //    get
        //    {
        //        return GetValue("Hub4Com_path", null, MED.Core.Properties.Settings.Default.hub4com_path).ToString();
        //    }
        //    set
        //    {
        //        SetValue("Hub4Com_path", null, value);
        //    }
        //}

        //public static string Hub4Com_options
        //{
        //    get
        //    {
        //        return GetValue("Hub4Com_options", null, MED.Core.Properties.Settings.Default.hub4com_options).ToString();
        //    }
        //    set
        //    {
        //        SetValue("Hub4Com_options", null, value);
        //    }
        //}

        //public static string Hub4Com_Download
        //{
        //    get
        //    {
        //        return GetValue("Hub4Com_Download", null, MED.Core.Properties.Settings.Default.hub4com_download).ToString();
        //    }
        //    set
        //    {
        //        SetValue("Hub4Com_Download", null, value);
        //    }
        //}
        //public static string Com0Com_Download
        //{
        //    get
        //    {
        //        return GetValue("Com0Com_Download", null, MED.Core.Properties.Settings.Default.com0com_download).ToString();
        //    }
        //    set
        //    {
        //        SetValue("Com0Com_Download", null, value);
        //    }
        //}
        //public static bool LogEnabled
        //{
        //    get
        //    {
        //        return bool.Parse(GetValue("LogEnabled", null, true).ToString());
        //    }
        //    set
        //    {
        //        SetValue("LogEnabled", null, value.ToString());
        //    }
        //}
    }
}
