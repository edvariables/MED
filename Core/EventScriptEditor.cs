// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Design;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

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
            if (value == null && context != null && context.PropertyDescriptor != null && context.Instance != null)
                if( (value = context.PropertyDescriptor.GetValue(context.Instance)) is EventScript eventScript1)
                    value = eventScript1.Script;
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
                var eventScript = EventScript.GetNew(process, eventName, context);
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


    /**
     * Show script editor.
     * Context.Instance may be IProcess or EventScript
     * 
     * */
    public class EventScriptEditor : UITypeEditor
    {
        private Panel? _editorUIWrapper;
        private RichScriptBox? _editorUI;
        private ITypeDescriptorContext? Context;
        private IProcess? ProcessComponent;

        /// <inheritdoc />
        public override object? EditValue(ITypeDescriptorContext? context, IServiceProvider provider, object? value)
        {
            IWindowsFormsEditorService? editorService = (IWindowsFormsEditorService?)provider.GetService(typeof(IWindowsFormsEditorService));
            if (editorService == null)
            {
                return value;
            }
            EventScript? eventScript = null;
            if (context != null && context.Instance is EventScript eventScript0)
                eventScript = eventScript0;
            else if (value is EventScript)
                eventScript = (EventScript)value;
            else if (context != null && context.Instance is IProcess process1
                && context.PropertyDescriptor != null)
            {
                ProcessComponent = process1;
                eventScript = (EventScript?)context.PropertyDescriptor.GetValue(process1);
            }
            if (eventScript != null)
                ProcessComponent = eventScript.Process;

            Context = context;

            if (_editorUI == null)
            {
                CreateEditorUI(context, eventScript);
                if (_editorUI == null)
                    return value;
            }
            if (eventScript != null)
                _editorUI.Text = eventScript.Script ?? "";
            else if (value is string script)
                _editorUI.Text = script;

            //if (editorNewlyCreated)
            //    _editorUI.CodeRender();
            _editorUI.EventScript = eventScript;

            var oldValue = _editorUI.Text;

            editorService.DropDownControl(_editorUIWrapper ?? (Control)_editorUI);

            string newScript = _editorUI.Text;
            if (newScript == oldValue)
                return value;

            if (eventScript == null)
            {
                if (_editorUI.EventScript != null)
                    if (!_editorUI.Modified)
                        return _editorUI.EventScript;
                    else
                        eventScript = _editorUI.EventScript;
                else if (context != null
                && context.PropertyDescriptor != null)
                {
                    IProcess? process = ProcessComponent;
                    if (context.Instance is IProcess process2)
                        process = process2;
                    if (process != null)
                    {
                        string eventName = context.PropertyDescriptor.Name;
                        eventName = Regex.Replace(eventName, @"(^On)?(.*)((Changed)?Script)$", "$2");
                        eventScript = EventScript.GetNew(process, eventName, context);
                    }
                }
            }
            if (eventScript != null)
            {
                eventScript.Script = newScript;
                if (context != null && context.PropertyDescriptor != null
                    && context.PropertyDescriptor.PropertyType.Equals(typeof(string)))
                    return newScript;
                return eventScript;
            }

            return value;
        }

        private void CreateEditorUI(ITypeDescriptorContext? context, EventScript? eventScript)
        {
            _editorUIWrapper = new();
            _editorUIWrapper.Size = new(400, 300);

            _editorUI = new RichScriptBox(eventScript);
            _editorUI.Font = new Font("Cascadia Code", 9F);
            _editorUI.Dock = DockStyle.Fill;
            _editorUI.AcceptsTab = true;
            _editorUI.ShowSelectionMargin = true;
            _editorUI.WordWrap = false;

            _editorUIWrapper.Controls.Add(_editorUI);

            if (eventScript == null
            && context != null
            && context.PropertyDescriptor != null
            && context.Instance is IProcess process)
            {
                string eventName = context.PropertyDescriptor.Name;
                eventName = Regex.Replace(eventName, @"(^On)?(.*)((Changed)?Script)$", "$2");
                eventScript = EventScript.GetNew(process, eventName, context);
            }
            if (eventScript != null)
            {
                var helper = new StatusStrip();
                helper.Name = "statusStrip";
                if (eventScript.ParametersNames != null)
                {
                    helper.Font = new(_editorUI.Font.FontFamily, 7F);
                    var _ = helper.Items.Add(String.Join(", ", eventScript.ParametersNames.Keys));
                    //helper.Text = /*eventScript.GetMethodName() + "(" +*/ String.Join(", ", eventScript.ParametersNames.Keys) /*+ ")"*/;
                }
                helper.GripStyle = ToolStripGripStyle.Hidden;
                helper.SizingGrip = false;

                helper.Items.Add("");

                ToolStripStatusLabel itemL = new();
                itemL.Text = "";
                itemL.Spring = true;
                helper.Items.Add(itemL);

                var item = helper.Items.Add(MEDIcons.above);
                item.Tag = new SizeF(0.5F, 0.5F);
                item.Click += EnlargePanel;

                item = helper.Items.Add(MEDIcons.below);
                item.Tag = new SizeF(2F, 2F);
                item.Click += EnlargePanel;

                item = helper.Items.Add(MEDIcons.VisualTrue);
                item.Dock = DockStyle.Right;
                item.Click += ShowInForm;

                _editorUIWrapper.Controls.Add(helper);
            }
        }

        private void ShowInForm(object? sender, EventArgs e)
        {
            if (sender is ToolStripItem toolStripItem
                && Context != null
                && Context.PropertyDescriptor != null
                && _editorUI != null)
            {
                EventScript? eventScript = null;

                IProcess process;
                if (Context.Instance is IProcess process0)
                {
                    process = process0;
                    if (Context.PropertyDescriptor.GetValue(process) is EventScript eventScript0)
                        eventScript = eventScript0;
                    else
                    {
                        string eventName = Context.PropertyDescriptor.Name;
                        eventName = Regex.Replace(eventName, @"(^On)?(.*)((Changed)?Script)$", "$2");
                        eventScript = EventScript.GetNew(process, eventName, Context);
                    }
                }
                else if (Context.Instance is EventScript eventScripti)
                {
                    eventScript = eventScripti;
                    process = eventScript.Process;
                }
                else
                    return;

                Cursor.Current = Cursors.WaitCursor;
                var f = new EventScriptForm(eventScript, _editorUI);
                f.Show(_editorUI.FindForm());
                Cursor.Current = Cursors.Default;

            }
        }

        private void EnlargePanel(object? sender, EventArgs e)
        {
            if (sender is ToolStripItem toolStripItem
                && toolStripItem.Tag is SizeF resize
                && _editorUIWrapper != null)
            {
                _editorUIWrapper.Size = new((int)(_editorUIWrapper.Height * resize.Height), (int)(_editorUIWrapper.Width * resize.Width));
            }
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
