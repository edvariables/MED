using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace MED.Core
{
    class IniFile
    {
        private static string _Path;
        private string Path;
        private static string _DefaultSection;
        private string DefaultSection;

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        static extern long WritePrivateProfileString(string Section, string Key, string Value, string FilePath);

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        static extern int GetPrivateProfileString(string Section, string Key, string Default, StringBuilder RetVal, int Size, string FilePath);

        static IniFile()
        {
            _DefaultSection = Settings.Namespace;
            string directory = System.IO.Path.Combine( Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), Settings.Namespace);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            _Path = System.IO.Path.Combine(directory, _DefaultSection + ".ini");
            if (! File.Exists(_Path))
                File.WriteAllText(_Path, "#" + _DefaultSection);
        }

        public IniFile(string iniPath = "%Namespace%/%UserProfile", string defaultSection = "")
        {
            DefaultSection = String.IsNullOrEmpty(defaultSection) ? _DefaultSection : defaultSection;
            string directory;
            if (iniPath != null)
            {
                if (iniPath == "")
#pragma warning disable CS8602 // Déréférencement d'une éventuelle référence null.
                    directory = Directory.GetParent(Assembly.GetExecutingAssembly().Location).FullName;
#pragma warning restore CS8602 // Déréférencement d'une éventuelle référence null.
                else
                {
                    directory = iniPath.Replace("%Namespace%", Settings.Namespace);
                    directory = Environment.ExpandEnvironmentVariables(directory);
                }
            }
            else
                directory = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), Settings.Namespace);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            Path = System.IO.Path.Combine(directory, _DefaultSection + ".ini");
            if (!File.Exists(Path))
                File.WriteAllText(Path, "#" + _DefaultSection);
        }

        public static string ReadValue(string Key, string? Section = null, string? defaultValue = null)
        {
            var RetVal = new StringBuilder(1024);
            GetPrivateProfileString(Section ?? _DefaultSection, Key, "", RetVal, 1024, _Path);
            if (RetVal.Length > 0)
                return RetVal.ToString();
            return defaultValue;
        }

        public string Read(string Key, string? Section = null)
        {
            var RetVal = new StringBuilder(1024);
            GetPrivateProfileString(Section ?? DefaultSection, Key, "", RetVal, 1024, Path);
            return RetVal.ToString();
        }

        public static void WriteValue(string Key, string Value, string? Section = null)
        {
            WritePrivateProfileString(Section ?? _DefaultSection, Key, Value, _Path);
        }

        public void Write(string Key, string Value, string? Section = null)
        {
            WritePrivateProfileString(Section ?? DefaultSection, Key, Value, Path);
        }

        public void DeleteKey(string Key, string? Section = null)
        {
            Write(Key, null, Section ?? DefaultSection);
        }

        public void DeleteSection(string? Section = null)
        {
            Write(null, null, Section ?? DefaultSection);
        }

        public bool KeyExists(string Key, string? Section = null)
        {
            return Read(Key, Section ?? DefaultSection).Length > 0;
        }
    }
}