using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MED.GameController
{
    public class UsagePropertiesMap : Dictionary<string, UsagePropertiesMapItem>
    {
        /***
         * Add usage for controller properties
         * 
         * <param properties>
         * e.g. : properties = "Z"
         * e.g. : properties = "Shift+F9"
         * e.g. : properties = "Control+F1"
         * e.g. : properties = "X|RightTrigger"
         * </param>
         * */
        public UsagePropertiesMapItem Add(string usage, string properties, Type? usageType = null, Type? propertyType = null)
        {
            UsagePropertiesMapItem item = new(usage, properties, usageType ?? typeof(string), propertyType ?? typeof(string));
            if (item.IsNegative)
            {
                if (this.TryGetValue(properties.Substring(1), out UsagePropertiesMapItem? positiveItem))
                {
                    item.NegativeUsage = positiveItem.Usage;
                    positiveItem.NegativeUsage = item.Usage;
                    item.KeepNegativeValue = false;
                }
                else if (this.TryGetValue("+" + properties.Substring(1), out UsagePropertiesMapItem? positiveItem2))
                {
                    item.NegativeUsage = positiveItem2.Usage;
                    positiveItem2.NegativeUsage = item.Usage;
                    item.KeepNegativeValue = true;
                }
            }
            else
            {
                string negProperty = "-" + (properties.StartsWith("+") ? properties.Substring(1) : properties);
                if (this.TryGetValue(negProperty, out UsagePropertiesMapItem? negativeItem))
                {
                    item.NegativeUsage = negativeItem.Usage;
                    item.KeepNegativeValue = properties.StartsWith("+");
                    negativeItem.NegativeUsage = item.Usage;
                }
            }
            this.Add(usage, item);
            //e.g. : properties = "X|RightTrigger"
            foreach (var property in item.Properties)
                this.Add($"__p__{property}", item);
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
                    base.Remove($"__p__{item.Property}");
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

        public string GetPropertyNegativeUsage(string properties)
        {
            if (properties.StartsWith('-'))
                properties = properties.Substring(1);
            else
                properties = "-" + properties;
            return GetPropertyUsage(properties);
        }
    }
    public class UsagePropertiesMapItem(string usage, string properties, Type usageType, Type propertyType, bool invokeIfActive = true)
    {
        public UsagePropertiesMapItem() : this("", "", typeof(string), typeof(string)) { }

        public string Property { get; set; } = properties;
        public string[] Properties { get; } = properties.Split('|');
        public string Usage { get; set; } = usage;
        public bool IsNegative { get; set; } = properties.StartsWith('-');
        public bool KeepNegativeValue { get; set; }
        public string NegativeUsage { get; set; } = usage;

        public Type PropertyType { get; set; } = propertyType;
        public Type UsageType { get; set; } = usageType;

        [Description("Invoke property changed if the control is active (key down)")]
        public bool InvokeIfActive { get; set; } = invokeIfActive;

        public override string ToString()
        {
            return $"{Usage} <= {Property}";
        }
    }
}
