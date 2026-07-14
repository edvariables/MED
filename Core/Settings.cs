using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using static System.Collections.Specialized.BitVector32;


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
        private static Hashtable _values = new Hashtable();
        private static Hashtable _saveValues = new Hashtable();
        private static readonly bool _useCache = true;

        public static string Namespace
        {
            get
            {
                return Assembly.GetExecutingAssembly().GetTypes().First().Namespace;
            }
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
                IniFile.WriteValue(path[1], value == null ? "" : value.ToString(), path[0]);
            }
            ClearCache();
        }

        public static void ClearCache(bool saveValues = true, bool values = true)
        {
            if (saveValues)
                _saveValues.Clear();
            if (values)
                _values.Clear(); //safely
        }

        /**
         * Get value from INI, Settings or Cache
         * */
        public static object GetValue(string setting, string section = null, object default_value = null)
        {
            string key = GetCacheKey(section, setting);
            if (_useCache
                && _values.ContainsKey(key) )
                return _values[key].ToString();
            object value = IniFile.ReadValue(setting, section, default_value == null ? "" : default_value.ToString());
            if (_useCache
                && ! _values.ContainsKey(key))
                _values.Add(key, value);
            return value;
        }
        /**
         * Set value
         * */
        public static void SetValue( string setting, string section = null, object set_value = null)
        {
            if (_useCache)
            {
                string key = GetCacheKey(section, setting);
                if (_values.ContainsKey(key))
                    _values[key] = set_value;
                else if (set_value != null)
                    _values.Add(key, set_value);
                if (_saveValues.ContainsKey(key))
                    _saveValues[key] = set_value;
                else if (set_value != null)
                    _saveValues.Add(key, set_value);
            }
            else
                IniFile.WriteValue(setting, set_value == null ? "" : set_value.ToString(), section);
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
