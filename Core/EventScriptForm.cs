using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MED
{
    public partial class EventScriptForm : Form
    {
        public EventScriptForm()
        {
            Cursor.Current = Cursors.WaitCursor;

            InitializeComponent();

            RichEditor.ZoomFactor = (float)(Core.Settings.GetValue("ZoomFactor", typeof(RichScriptBox).Name, RichEditor.ZoomFactor) ?? RichEditor.ZoomFactor);

            RichEditor.LineNumbersTextBox = LineNumberTextBox;

            this.Activated += EventScriptForm_Activated;
        }

        public EventScriptForm(EventScript eventScript, RichTextBox? editorUI = null) : this()
        {
            _editorUI = editorUI;

            string[] parametersNames = eventScript.ParametersNames == null ? [] : eventScript.ParametersNames.Keys.ToArray();

            EventScript = eventScript;

            Icon = MEDIcon.GetIcon(eventScript.Icon);

            Text = $"{eventScript.Process}.{eventScript.EventName} script";

            InitVariables(eventScript, editorUI);

            RichEditor.Text = eventScript.Script ?? "";

            RichEditor.ModifiedChanged += (sender, e) => cmdSave.IsLink = RichEditor.Modified;
            RichEditor.Modified = cmdSave.IsLink = false;

            FormClosing += EventScriptForm_FormClosing;

            EventScript.OnScriptChanged += EventScript_OnScriptChanged;
        }

        private void InitVariables(EventScript eventScript, RichTextBox? editorUI = null)
        {
            dropDownVariables.DropDownItems.Clear();

            if (statusStripItem.Text == "")
                statusStripItem.Text = $"{eventScript.EventName}( {String.Join(", ", eventScript.ParametersNames ?? [])} )";
            if (eventScript.VariablesNames == null)
                eventScript.CompileScript();
            if (eventScript.VariablesNames == null)
            {
                if (eventScript.ParametersNames == null)
                    dropDownVariables.Text = "-";
                else
                {
                    dropDownVariables.Text = $"{eventScript.ParametersNames.Count} var{(eventScript.ParametersNames.Count > 1 ? "s" : "")}";
                    foreach (var (varName, varType) in eventScript.ParametersNames)
                        AddVariable($"{varName} : {varType}", "Parameter");
                }
            }
            else
            {
                dropDownVariables.Text = $"{eventScript.VariablesNames.Count} var{(eventScript.VariablesNames.Count > 1 ? "s" : "")}";
                foreach (var (varName, varType) in eventScript.VariablesNames)
                    AddVariable($"{varName} : {varType}", "Variable");
            }
            foreach (var (varCall, varMethode) in eventScript.ScriptGlobalsFunctions)
                AddVariable($"{varCall}", "Function");

            foreach (var (gameController, properties) in eventScript.ConsumerProperties)
                foreach (var property in properties)
                    AddVariable($"{gameController.Name}.{property}", "ConsumerProperty");

        }
        private void AddVariable(string text, string variableType)
        {
            Image? image;
            switch (variableType)
            {
                case "Function":
                    image = MEDIcons.Function;
                    break;
                case "ConsumerProperty":
                    image = MEDIcons.VarReadOnly;
                    break;
                case "Variable":
                case "Parameter":
                default:
                    image = MEDIcons.Var;
                    break;

            }

            var item = dropDownVariables.DropDownItems.Add(text, image);

            if (variableType != "ConsumerProperty")
                item.Click += Variable_Click;
            else
                item.Enabled = false;

            switch (variableType)
            {
                case "Function":
                    break;
                case "ConsumerProperty":
                    break;
                case "Variable":
                case "Parameter":
                default:
                    break;

            }
        }

        private void Variable_Click(object? sender, EventArgs e)
        {
            if (sender is ToolStripDropDownItem item)
                RichEditor.SelectedText = item.Text ?? "";
        }

        private void EventScriptForm_Activated(object? sender, EventArgs e)
        {
            Cursor.Current = Cursors.Default;
            this.Activated -= EventScriptForm_Activated;
        }

        private void EventScriptForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            e.Cancel = !Save(true);

            if (e.Cancel)
                return;

            Core.Settings.SetValue("ZoomFactor", typeof(RichScriptBox).Name, RichEditor.ZoomFactor);

            _editorUI = null;
            EventScript = null;
        }

        RichTextBox? _editorUI;
        EventScript? _EventScript;
        public EventScript? EventScript
        {
            get => _EventScript;
            set
            {
                _EventScript = value;
                if (_editorUI is RichScriptBox richTextBoxMED)
                    richTextBoxMED.EventScript = _EventScript;
                RichEditor.EventScript = _EventScript;
                if (_EventScript == null)
                    UpdateSatusLabel("No script linked", MEDIcons.False);
            }
        }

        private void EventScript_OnScriptChanged(object? sender, EventArgs e)
        {
            if (EventScript == null)
                return;

            RichEditor.Text = EventScript.Script ?? "";

            if (_editorUI != null && !_editorUI.IsDisposed)
            {
                _editorUI.Text = RichEditor.Text;
                _editorUI.Modified = false;
            }
            RichEditor.Modified = false;
        }

        private void Editor_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.S && e.Control)
                Save();
        }

        public bool Save(bool askIfNeed = false)
        {
            if (EventScript == null)
                return true;
            if (askIfNeed)
            {
                if (EventScript.Script != RichEditor.Text)
                    switch (MessageBox.Show("Voulez-vous enregistrer les modifications ?", Text, MessageBoxButtons.YesNoCancel))
                    {
                        case DialogResult.Yes:
                            Save(false);
                            return true;
                        case DialogResult.No:
                            return true;
                        default:
                            return false;
                    }
                UpdateSatusLabel("Unchanged", MEDIcons.ok);
                return true;
            }

            Cursor = Cursors.WaitCursor;

            UpdateSatusLabel("Compiling...", MEDIcons.info);

            EventScript.Script = RichEditor.Text;

            if (!EventScript.CompileScript())
                if (EventScript.Process.Performance != null && EventScript.Process.Performance.LastError != null)
                {
                    var lastError = EventScript.Process.Performance.LastError;
                    var message = $"{lastError.DelayToString()} sec {lastError.Message}";
                    UpdateSatusLabel(message, MEDIcons.alert);
                }
                else if (EventScript.CompiledScript != null)
                    UpdateSatusLabel($"Compilation error\n{EventScript.CompiledScript.Code}", MEDIcons.alert);
                else 
                    UpdateSatusLabel($"Compilation error\n(no script)", MEDIcons.alert);
                else if (EventScript.CompiledScript != null)
                UpdateSatusLabel($"Saved\n{EventScript.CompiledScript.Code}", MEDIcons.ok);

            InitVariables(EventScript, _editorUI);

            Cursor = Cursors.Default;

            if (_editorUI != null && !_editorUI.IsDisposed)
            {
                _editorUI.Text = RichEditor.Text;
                _editorUI.Modified = false;
            }

            RichEditor.Modified = false;

            return true;
        }

        private void UpdateSatusLabel(string message, Image? image = null)
        {
            var lines = message.Split('\n', 2);
            statusStripItem.Text = lines[0];
            statusStripItem.Image = image;
            statusStripItem.ToolTipText = message;
        }

        private void cmdSave_Click(object sender, EventArgs e)
        {
            Save();

            if (Control.ModifierKeys == Keys.Control)
                Close();
        }

        private void CmdClose_Click(object sender, EventArgs e)
        {
            if (Control.ModifierKeys == Keys.Control)
            {
                EventScript = null;
                RichEditor.Modified = false;
            }
            Close();
        }

        private void cmdPlayTest_Click(object sender, EventArgs e)
        {
            if (EventScript == null)
            {
                cmdPlayTest.Text = "No script object";
                cmdPlayTest.Image = MEDIcons.close;
                return;
            }

            cmdPlayTest.Text = "Compile";
            cmdPlayTest.Image = MEDIcons.Pause_blue;

            var selectionStart = RichEditor.SelectionStart;
            var selectionLength = RichEditor.SelectionLength;
            var previousScript = EventScript.Script;
            var previousError = EventScript.Process.Performance == null ? null : EventScript.Process.Performance.LastError;

            EventScript.Script = RichEditor.Text;

            object?[] parameters = [null, null];
            EventScript.CompileScript(parameters[0], parameters[1]);//TODO

            if (EventScript.Process.Performance != null
                && previousError != EventScript.Process.Performance.LastError
                && EventScript.Process.Performance.LastError != null)
            {
                UpdateSatusLabel(EventScript.Process.Performance.LastError.Message, MEDIcons.alert);
                cmdPlayTest.Text = "Compile error";
                cmdPlayTest.Image = MEDIcons.nextPage;
            }
            else
            {

                EventScript.Eval(parameters[0], parameters[1]);//TODO

                if (EventScript.Process.Performance != null
                    && previousError != EventScript.Process.Performance.LastError
                    && EventScript.Process.Performance.LastError != null)
                {
                    UpdateSatusLabel(EventScript.Process.Performance.LastError.Message, MEDIcons.alert);
                    cmdPlayTest.Text = "Eval error";
                    cmdPlayTest.Image = MEDIcons.nextPage;
                }
                else
                {
                    cmdPlayTest.Text = "Test";
                    cmdPlayTest.Image = MEDIcons.nextPage_blue;
                }
            }

            RichEditor.Text = previousScript ?? "";
            RichEditor.SelectionStart = selectionStart;
            RichEditor.SelectionLength = selectionLength;

        }
    }
}
