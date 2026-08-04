using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MED.Core
{
    /**
     * static class Parser
     * <summary>Parse data to and from json or .ini file</summary>
     * */
    public static class Parser
    {

        public static string? ObjectToString(object value)
        {
            var str_value = value switch
            {
                Point pt => $"{pt.X},{pt.Y}",
                PointF pt => $"{DecimalDotSeparator(pt.X.ToString())},{DecimalDotSeparator(pt.Y.ToString())}",
                Size sz => $"{sz.Width},{sz.Height}",
                SizeF sz => $"{DecimalDotSeparator(sz.Width.ToString())},{DecimalDotSeparator(sz.Height.ToString())}",
                KnownColor color => $"{color.ToString()}",
                Color color => $"{color.ToString()}",
                null => "<null>",
                _ => value.ToString()
            };
            return str_value;
        }

        public static object? ObjectFromString(string str_value, object type_as)
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

            if (out_type.Equals(typeof(Point))
             || out_type.Equals(typeof(PointF)))
            {
                if (str_value[0] == '{')
                    str_value = Regex.Replace(str_value, @"[\{\}a-zA-Z=]", "");
                string[] coords = str_value.Split(',');
                if(out_type.Equals(typeof(PointF)))
                    return new PointF(float.Parse(DecimalSeparator(coords[0])), float.Parse(DecimalSeparator(coords[1])));
                return new Point(int.Parse(coords[0]), int.Parse(coords[1]));
            }
            if (out_type.Equals(typeof(Size))
             || out_type.Equals(typeof(SizeF)))
            {
                if (str_value[0] == '{')
                    str_value = Regex.Replace(str_value, @"[\{\}a-zA-Z=]", "");
                string[] coords = str_value.Split(',');
                if (out_type.Equals(typeof(SizeF)))
                    return new SizeF(float.Parse(DecimalSeparator(coords[0])), float.Parse(DecimalSeparator(coords[1])));
                return new Size(int.Parse(coords[0]), int.Parse(coords[1]));
            }
            if (out_type.Equals(typeof(KnownColor)))
            {
                return (KnownColor)Enum.Parse(typeof(KnownColor), str_value);
            }
            if (out_type.Equals(typeof(Color)))
            {
                return Color.FromName(str_value.Replace("Color [", "").Replace("]", ""));
            }
            object value = type_as switch
            {
                int => int.Parse(str_value),
                bool => str_value == "" ? false : bool.Parse(str_value),
                long => long.Parse(str_value),
                float => float.Parse(DecimalSeparator(str_value)),
                double => double.Parse(DecimalSeparator(str_value)),
                decimal=> decimal.Parse(DecimalSeparator(str_value)),
                _ => str_value
            };
            return value;
        }

        static string _DecimalSeparator = (5.5F).ToString().Replace("5", "");
        static string _DecimalNonSeparator = _DecimalSeparator=="." ? "," : ".";
        public static string DecimalSeparator(string number)
        {
            return number.Replace(_DecimalNonSeparator, _DecimalSeparator);
        }
        public static string DecimalDotSeparator(string number)
        {
            return number.Replace(_DecimalSeparator, ".");
        }

        public static object? ObjectFromJsonNode(JsonNode node, object? type_as)
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

        public static string? SizeToPretty(Size size) => ObjectToString(size)?.Replace(",", " x ");
        public static Size SizeFromPretty(string size)
        {
#pragma warning disable CS8605 // Conversion unboxing d'une valeur peut-être null.
            return (Size)ObjectFromString(size.Replace("x", ","), typeof(Size));
#pragma warning restore CS8605 // Conversion unboxing d'une valeur peut-être null.
        }
    }
}
