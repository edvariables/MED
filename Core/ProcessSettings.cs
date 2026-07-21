using MED.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace MED
{
    public class ProcessSettings
    {
        public ProcessSettings(string fileName, JsonNode? root = null)
        {
            FileName = fileName;
            if (root == null)
                Root = JsonNode.Parse("{}");
            else
                Root = root;
        }

        public string FileName { get; set; }
        public JsonNode Root { get; set; }
        public JsonNode ChildNode(string childName, bool createIfNone = false)
        {
            if (Root[childName] == null)
                if (createIfNone)
                    Root[childName] = new JsonObject();
            return Root[childName];
        }
        public JsonArray ChildArray(string childName, bool createIfNone = false)
        {
            if (Root[childName] == null)
                if (createIfNone)
                    Root[childName] = new JsonArray();
                else
                    return null;
            return (JsonArray)Root[childName];
        }
        public ProcessSettings ChildSettings(string childName)
        {
            return new("", ChildNode(childName, true));
        }

        public static ProcessSettings FromFile(string fileName)
        {
            if (!File.Exists(fileName))
                if (!fileName.EndsWith(Settings.ProcessFileExtension))
                {
                    fileName = Path.Combine(Settings.MyProjectsDirectory, fileName) + Settings.ProcessFileExtension;
                }
            if (!File.Exists(fileName))
            {
                Console.WriteLine($"ProcessSettings.FromFile({fileName}) : Fichier introuvable");
                //throw new FileNotFoundException("Fichier introuvable", fileName);
                return new ProcessSettings("");
            }

            ProcessSettings settings = new(fileName);
            settings.Open();

            return settings;
        }
        public bool Open(string fileName = "")
        {
            if (fileName == "")
                fileName = FileName;

            Root = JsonNode.Parse(File.ReadAllText(fileName));

            return true;
        }

        public static bool Save(JsonNode root, string fileName)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };

            File.WriteAllText(fileName, root.ToJsonString(options));
            return true;
        }

        public bool Save(string fileName = "")
        {
            if (fileName == "")
                fileName = FileName;
            return Save(Root, fileName);
        }
        /**
         * Get value from INI, Settings or Cache
         * */
        public object GetValue(string setting, object default_value = null)
        {
            var childNode = ChildNode(setting, false);
            if (childNode == null)
                return default_value;
            return Parser.ObjectFromJsonNode(childNode, default_value);
        }
        /**
         * Set value
         * */
        public void SetValue(string setting, object set_value = null)
        {

            var o = Root.AsObject();
            if (set_value is int)
                o[setting] = (int)set_value;
            else if (set_value is long)
                o[setting] = (long)set_value;

            o[setting] = set_value switch
            {
                Point pt => $"{pt.X},{pt.Y}",
                PointF pt => $"{pt.X},{pt.Y}",
                Size sz => $"{sz.Width},{sz.Height}",
                SizeF sz => $"{sz.Width},{sz.Height}",
                bool b => b,
                int i => i,
                long l => l,
                double d => d,
                Single s => s,
                null => "<null>",
                _ => set_value.ToString()
            };
        }
    }
}