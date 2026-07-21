//using MED.Core;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Text.Json;
//using System.Text.Json.Nodes;
//using System.Threading.Tasks;
//using System.Windows.Forms;
//using static System.Collections.Specialized.BitVector32;
//using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;

//namespace MED
//{
//    public class ProjectSettings
//    {
//        public ProjectSettings(string fileName = "")
//        {
//            FileName = fileName;

//            if (!String.IsNullOrEmpty(FileName) && File.Exists(FileName))
//            {
//                string s = File.ReadAllText(FileName);
//                JsonRoot = JsonObject.Parse(s);
//            }
//            if(JsonRoot==null)
//                JsonRoot = JsonObject.Parse("{\"name\" : \"MED.Project\", \"data\" : {}}");
//        }

//        public JsonNode JsonRoot { get; set; }
//        public string FileName { get; set; }

//        public bool Save(string fileName = "")
//        {
//            return false;

//        }

//        public static object GetValue(string setting, JsonNode jsonNode, object default_value = null)
//        {

//            if (jsonNode.GetValueKind() == JsonValueKind.Null 
//                || jsonNode.GetValueKind() == JsonValueKind.Undefined)//TODO
//                return default_value;

//            object value;
//            switch (jsonNode.GetValueKind())
//            {
//                case JsonValueKind.String:
//                    value = jsonNode.GetValue<String>();
//                    break;
//                case JsonValueKind.True:
//                    return true;

//                case JsonValueKind.False:
//                    return false;

//                case JsonValueKind.Object:
//                    value = jsonNode.GetString();

//                    break;
//                case JsonValueKind.Array:
//                    value = jsonNode.AsArray().ToArray();

//                    break;
//                case JsonValueKind.Number:
//                    return jsonNode.GetValue<Double>();

//                case JsonValueKind.Undefined:
//                case JsonValueKind.Null:
//                default:
//                    return default_value;
//            }
//            if( value is String)
//                value = Parser.ObjectFromString((string)value, default_value);
//            return value;
//        }

//        public object GetValue(string setting, string path = null, object default_value = null)
//        {
//            JsonNode jsonNode = GetNode(setting, path);
//            if(jsonNode == null)
//                return default_value;
            
//            return GetValue(setting, JsonRoot, default_value);
//        }

//        public void SetValue(string setting, string path = null, object set_value = null)
//        {
//            JsonNode jsonNode = GetNode(setting, path, JsonRoot, true);
//            jsonNode
//        }


//        public JsonNode GetNode(string setting, string path) => GetNode(setting, section, JsonRoot);

//        public static JsonNode GetNode(string setting, string path, JsonNode root, bool createMissings = false)
//        {
//            JsonNode parent = root;
//            JsonNode jsonElement;
//            foreach (string section in path.Split('/'))
//            {
//                if (section == "")
//                    continue;
//                try
//                {
//                    jsonElement = parent[section];
//                }
//                catch
//                {
//                    if (createMissings)
//                    {
//                        jsonElement = parent[section] = new JsonObject();
//                    }
//                    return null;
//                }
//                parent = jsonElement;
//            }
//            if (setting == "")
//                return parent;

//            try
//            {
//                jsonElement = parent[setting];
//            }
//            catch
//            {
//                if (createMissings)
//                {
//                    jsonElement = parent[setting] = new JsonObject();
//                    return jsonElement;
//                }
//            }
//            return null;
//        }
//    }
//}
