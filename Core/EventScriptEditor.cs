// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Design;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Forms.Design;

namespace MED
{
    /**
     * EventScriptConvertor
     * 
     * TypeConverter for PropertyGrid
     * 
        In Process class : 
        [Editor(typeof(MEDIconSelectorEditor), typeof(UITypeEditor))]
        [TypeConverter(typeof(EventScriptConvertor))]
        public virtual string ProcessIcon { get; set; }
    */
    public class EventScriptConvertor : TypeConverter
    {

        #region override TypeConverter
        //Thanks to György Kőszeg https://stackoverflow.com/questions/78590673/combobox-on-a-propertygrid

        // If your "ValueMember" is not a string, add its type as well
        // (eg. int, some custom enum, etc.)
        public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType) => destinationType == typeof(string);
        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) => sourceType == typeof(string);

        // Destination type is always string in a PropertyGrid but if your "ValueMember"
        // is some different type you might want to add it, too
        public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
        {

            if (destinationType != typeof(string))
                return base.ConvertTo(context, culture, value, destinationType);
            if (value is EventScript eventScript)
                return eventScript.Script;
            return value;
        }

        // You might want to parse from string and the type of your "ValueMember".
        // Both are strings in your example.
        public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
        {
            if (value is not string str)
                return base.ConvertFrom(context, culture, value);
            // 1. Parsing by text, case-insensitive
            string? script = str;

            if (context != null
            && context.PropertyDescriptor != null
            && context.Instance is IProcess process)
            {
                string eventName = context.PropertyDescriptor.Name;
                eventName = Regex.Replace(eventName, @"(^On)?(.*)((Changed)?Script)$", "$2");
                var eventScript = new EventScript(process, eventName);
                eventScript.Script = script;

                return eventScript;
            }

            return script ?? throw new ArgumentException($"Invalid value: {str}", nameof(value));
        }

        // This enables "ComboBox" for the property
        public override bool GetStandardValuesSupported(ITypeDescriptorContext? context) => true;

        // This tells that it's not a simple read-only drop down
        // but you can also type values to parse
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext? context) => false;

        #endregion
    }


    public class EventScriptEditor : UITypeEditor
    {
        private Panel? _editorUIWrapper;
        private RichTextBox? _editorUI;

        /// <inheritdoc />
        public override object? EditValue(ITypeDescriptorContext? context, IServiceProvider provider, object? value)
        {
            IWindowsFormsEditorService? editorService = (IWindowsFormsEditorService?)provider.GetService(typeof(IWindowsFormsEditorService));
            if (editorService == null)
            {
                return value;
            }
            EventScript? eventScript = null;
            if (value is EventScript)
                eventScript = (EventScript)value;

            if (_editorUI == null)
            {
                _editorUIWrapper = new();
                _editorUIWrapper.Height = 200;

                _editorUI = new RichTextBox();
                _editorUI.Dock = DockStyle.Fill;
                _editorUIWrapper.Controls.Add(_editorUI);

                if (eventScript == null
                && context != null
                && context.PropertyDescriptor != null
                && context.Instance is IProcess process)
                {
                    string eventName = context.PropertyDescriptor.Name;
                    eventName = Regex.Replace(eventName, @"(^On)?(.*)((Changed)?Script)$", "$2");
                    eventScript = new EventScript(process, eventName);
                }
                if (eventScript != null)
                {
                    var helper = new Label();
                    if (eventScript.ParametersNames != null)
                    {
                        helper.Font = new(_editorUI.Font.FontFamily, 7F);
                        helper.Text = /*eventScript.GetMethodName() + "(" +*/ String.Join(", ", eventScript.ParametersNames.Keys) /*+ ")"*/;
                    }
                    helper.Dock = DockStyle.Bottom;
                    _editorUIWrapper.Controls.Add(helper);
                }
            }
            if (eventScript != null)
                _editorUI.Text = eventScript.Script;
            else if (value is string script)
                _editorUI.Text = script;

            var oldValue = _editorUI.Text;

            editorService.DropDownControl(_editorUIWrapper);

            string newScript = _editorUI.Text;
            if (newScript == oldValue)
                return value;

            if (eventScript == null)
            {
                if (context != null
                && context.PropertyDescriptor != null
                && context.Instance is IProcess process)
                {
                    string eventName = context.PropertyDescriptor.Name;
                    eventName = Regex.Replace(eventName, @"(^On)?(.*)((Changed)?Script)$", "$2");
                    eventScript = new EventScript(process, eventName);
                }
            }
            if (eventScript != null)
            {
                eventScript.Script = newScript;
                return eventScript;
            }

            return value;
        }

        /// <summary>
        ///  The MultilineStringEditor is a drop down editor, so this returns UITypeEditorEditStyle.DropDown.
        /// </summary>
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext? context) => UITypeEditorEditStyle.DropDown;

        /// <summary>
        ///  Returns false; no extra painting is performed.
        /// </summary>
        public override bool GetPaintValueSupported(ITypeDescriptorContext? context) => false;
    }
}
