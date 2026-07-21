using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MED.Core
{
    public static class Parser
    {

        public static string ObjectToString(object value)
        {
            var str_value = value switch
            {
                Point pt => $"{pt.X},{pt.Y}",
                Size sz => $"{sz.Width},{sz.Height}",
                null => "<null>",
                _ => value.ToString()
            };
            return str_value;
        }

        public static object ObjectFromString(string str_value, object type_as)
        {
            if (type_as == null)
                return str_value;
            if (str_value == "<null>")
                return null;

            Type out_type;
            if (type_as is Type)
                out_type = (Type)type_as;
            else
                out_type = type_as.GetType();

            if (out_type.Equals(typeof(Point)))
            {
                if (str_value[0] == '{')
                    str_value = Regex.Replace(str_value, @"[\{\}a-zA-Z=]", "");
                string[] coords = str_value.Split(',');
                return new Point(int.Parse(coords[0]), int.Parse(coords[1]));
            }
            if (out_type.Equals(typeof(Size)))
            {
                if (str_value[0] == '{')
                    str_value = Regex.Replace(str_value, @"[\{\}a-zA-Z=]", "");
                string[] coords = str_value.Split(',');
                return new Size(int.Parse(coords[0]), int.Parse(coords[1]));
            }
            object value = type_as switch
            {
                int => int.Parse(str_value),
                bool => str_value=="" ? false : bool.Parse(str_value),
                long => long.Parse(str_value),
                double => double.Parse(str_value),
                _ => str_value
            };
            return value;
        }

        public static object ObjectFromJsonNode(JsonNode node, object type_as)
        {
            if (type_as == null)
                return node.GetValue<object>();

            switch (node.GetValueKind())
            {
                case System.Text.Json.JsonValueKind.True:
                    return true;
                case System.Text.Json.JsonValueKind.False:
                    return false;
                case System.Text.Json.JsonValueKind.String:
                    return ObjectFromString(node.AsValue().ToString(), type_as);
                case System.Text.Json.JsonValueKind.Number:
                    return ObjectFromString(node.AsValue().ToString(), type_as);
                case System.Text.Json.JsonValueKind.Object:
                    return ObjectFromString(node.AsValue().ToString(), type_as);
                case System.Text.Json.JsonValueKind.Array:
                    return ObjectFromString(node.AsValue().ToString(), type_as);
                case System.Text.Json.JsonValueKind.Undefined:
                case System.Text.Json.JsonValueKind.Null:
                default:
                    return null;
            }
        }

        public static string SizeToPretty(Size size) => ObjectToString(size).Replace(",", " x ");
        public static Size SizeFromPretty(string size) => (Size)ObjectFromString(size.Replace("x", ","), typeof(Size));
    }
}
