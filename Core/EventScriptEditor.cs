// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Design;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Forms;
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
        private RichTextBox? _editorUI;
        private ITypeDescriptorContext? Context;

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
            Context = context;

            bool editorNewlyCreated = false;

            if (_editorUI == null)
            {
                editorNewlyCreated = true;

                _editorUIWrapper = new();
                _editorUIWrapper.Size = new(400, 300);

                _editorUI = new RichTextBox();
                _editorUI.Font = new Font("Cascadia Code", 8F);
                _editorUI.Dock = DockStyle.Fill;
                _editorUI.TextChanged += (object? sender, EventArgs e) => CodeRender((RichTextBox?)sender, eventScript);
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
            if (eventScript != null)
                _editorUI.Text = eventScript.Script;
            else if (value is string script)
                _editorUI.Text = script;
            if (editorNewlyCreated)
                CodeRender(_editorUI, eventScript);

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
                    eventScript = EventScript.GetNew(process, eventName, context);
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

                var f = new EventScriptForm(eventScript, _editorUI);
                f.Show(_editorUI.FindForm());

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


        #region Code editor
        public static void CodeRender(RichTextBox? richTextBox, EventScript? eventScript)
        {
            if (richTextBox == null || richTextBox.IsDisposed)
                return;

            // Source - https://stackoverflow.com/a/58481519
            // Posted by Momoro
            // Retrieved 2026-09-06, License - CC BY-SA 4.0

            // getting keywords/functions
            string keywords = @"\b(abstract|as|base|break|case|catch|checked|continue|default|delegate|do|else|event|explicit|extern|false|finally|fixed|for|foreach|goto|if|implicit|in|interface|internal|is|lock|namespace|new|null|object|operator|out|override|params|private|protected|public|readonly|ref|return|sealed|sizeof|stackalloc|switch|this|throw|true|try|typeof|unchecked|unsafe|using|virtual|volatile|while|var)\b";
            MatchCollection keywordMatches = Regex.Matches(richTextBox.Text, keywords);

            // getting types/classes/keyobjects from the text 
            if (eventScript != null && eventScript.VariablesNames == null)
                eventScript.CompileScript();
            var variables = new Dictionary<string, Type>(eventScript?.VariablesNames ?? []);
            string types = @"\b(Console|" + string.Join("|", variables.Keys) + @")\b";
            MatchCollection typeMatches = Regex.Matches(richTextBox.Text, types);

            // getting comments (inline or multiline)
            string comments = @"(\/\/.+?$|\/\*.+?\*\/)";
            MatchCollection commentMatches = Regex.Matches(richTextBox.Text, comments, RegexOptions.Multiline);

            // getting strings
            string strings = "\".+?\"";
            MatchCollection stringMatches = Regex.Matches(richTextBox.Text, strings);

            string stringz = "bool|byte|char|class|const|decimal|double|enum|float|int|long|sbyte|short|static|string|struct|uint|ulong|ushort|void";
            MatchCollection stringzMatchez = Regex.Matches(richTextBox.Text, stringz);

            // saving the original caret position + forecolor
            int originalIndex = richTextBox.SelectionStart;
            int originalLength = richTextBox.SelectionLength;
            Color originalColor = Color.Black;

            richTextBox.SuspendLayout();

            // removes any previous highlighting (so modified words won't remain highlighted)
            richTextBox.SelectionStart = 0;
            richTextBox.SelectionLength = richTextBox.Text.Length;
            richTextBox.SelectionColor = originalColor;

            // scanning...
            foreach (Match m in keywordMatches)
            {
                richTextBox.SelectionStart = m.Index;
                richTextBox.SelectionLength = m.Length;
                richTextBox.SelectionColor = Color.Blue;
            }

            foreach (Match m in typeMatches)
            {
                richTextBox.SelectionStart = m.Index;
                richTextBox.SelectionLength = m.Length;
                richTextBox.SelectionColor = Color.DarkCyan;
            }

            foreach (Match m in commentMatches)
            {
                richTextBox.SelectionStart = m.Index;
                richTextBox.SelectionLength = m.Length;
                richTextBox.SelectionColor = Color.Green;
            }

            foreach (Match m in stringMatches)
            {
                richTextBox.SelectionStart = m.Index;
                richTextBox.SelectionLength = m.Length;
                richTextBox.SelectionColor = Color.Brown;
            }

            foreach (Match m in stringzMatchez)
            {
                richTextBox.SelectionStart = m.Index;
                richTextBox.SelectionLength = m.Length;
                richTextBox.SelectionColor = Color.Purple;
            }

            // restoring the original colors, for further writing
            richTextBox.SelectionStart = originalIndex;
            richTextBox.SelectionLength = originalLength;
            richTextBox.SelectionColor = originalColor;

            richTextBox.ResumeLayout();

        }
        #endregion

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
