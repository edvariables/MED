using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace MED.GameController
{
    public class UsagePropertiesMap : Dictionary<string, UsagePropertiesMapItem>
    {
        public UsagePropertiesMapItem Add(string usage, string properties, Type? usageType = null, Type? propertyType = null)
        {
            UsagePropertiesMapItem item = new(usage, properties, usageType ?? typeof(string), propertyType ?? typeof(string));
            this.Add(usage, item);
            this.Add($"__p__{item.Properties}", item);
            return item;
        }

        public new bool Remove(string key)
        {
            return this.Remove(key, out _);
        }

        public new bool Remove(string key, out UsagePropertiesMapItem? item)
        {
            bool b = base.Remove(key, out item);
            if (b)
            {
                if (item == null)
                    base.Remove($"__p__{key}", out item);
                else
                    base.Remove($"__p__{item.Properties}");
            }
            else
            {
                b = base.Remove($"__p__{key}", out item);
                if (item != null)
                    base.Remove(item.Usage);
            }
            return b;
        }

        public string GetPropertyUsage(string properties)
        {
            if (TryGetValue($"__p__{properties}", out UsagePropertiesMapItem? item))
                return item.Usage;
            return properties;
        }
    }
    public class UsagePropertiesMapItem(string usage, string properties, Type usageType, Type propertyType)
    {
        public UsagePropertiesMapItem() : this("", "", typeof(string), typeof(string)) { }

        public string Properties { get; set; } = properties;
        public string Usage { get; set; } = usage;

        public Type PropertyType { get; set; } = propertyType;
        public Type UsageType { get; set; } = usageType;

        public override string ToString()
        {
            return $"{Usage} <= {Properties}";
        }
    }
}
