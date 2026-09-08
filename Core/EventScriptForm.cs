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

            RTBEditor.LineNumbersTextBox = LineNumberTextBox;

            this.Activated += EventScriptForm_Activated;
        }

        public EventScriptForm(EventScript eventScript, RichTextBox? editorUI = null) : this()
        {
            _editorUI = editorUI;

            string[] parametersNames = eventScript.ParametersNames == null ? [] : eventScript.ParametersNames.Keys.ToArray();

            EventScript = eventScript;

            Icon = MEDIcon.GetIcon(eventScript.Icon);

            Text = $"{eventScript.Process}.{eventScript.EventName} script";

            toolStripStatusLabel1.Text = $"{eventScript.EventName}( {String.Join(", ", eventScript.ParametersNames ?? [])} )";
            if (eventScript.VariablesNames == null)
                eventScript.CompileScript();
            if (eventScript.VariablesNames == null)
                foreach (var (varName, varType) in eventScript.ParametersNames ?? [])
                    dropDownVariables.DropDownItems.Add($"{varName} : {varType}", MEDIcons.Var);
            else
            {
                dropDownVariables.Text = $"{eventScript.VariablesNames.Count} var{(eventScript.VariablesNames.Count > 1 ? "s" : "")}";
                foreach (var (varName, varType) in eventScript.VariablesNames)
                    dropDownVariables.DropDownItems.Add($"{varName} : {varType}", MEDIcons.Var);
            }
            foreach (var (varCall, varMethode) in eventScript.ScriptGlobalsFunctions)
                dropDownVariables.DropDownItems.Add($"{varCall}", MEDIcons.Function);

            foreach (var (gameController, properties) in eventScript.ConsumerProperties)
                foreach (var property in properties)
                    dropDownVariables.DropDownItems.Add($"{gameController.Name}.{property}", MEDIcons.VarReadOnly);

            RTBEditor.Text = eventScript.Script ?? "";

            RTBEditor.ModifiedChanged += (sender, e) => cmdSave.IsLink = RTBEditor.Modified;
            RTBEditor.Modified = cmdSave.IsLink = false;

            FormClosing += EventScriptForm_FormClosing;

            EventScript.OnScriptChanged += EventScript_OnScriptChanged;
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
                if (_editorUI is RichTextBoxMED richTextBoxMED)
                    richTextBoxMED.EventScript = _EventScript;
                RTBEditor.EventScript = _EventScript;
            }
        }

        private void EventScript_OnScriptChanged(object? sender, EventArgs e)
        {
            if (EventScript == null)
                return;

            RTBEditor.Text = EventScript.Script ?? "";

            if (_editorUI != null && !_editorUI.IsDisposed)
            {
                _editorUI.Text = RTBEditor.Text;
                _editorUI.Modified = false;
            }
            RTBEditor.Modified = false;
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
                if (EventScript.Script != RTBEditor.Text || RTBEditor.Modified)
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

            EventScript.Script = RTBEditor.Text;

            if (!EventScript.CompileScript())
                UpdateSatusLabel("Compilation error", MEDIcons.alert);
            else
                UpdateSatusLabel("Saved", MEDIcons.ok);

            Cursor = Cursors.Default;

            if (_editorUI != null && !_editorUI.IsDisposed)
            {
                _editorUI.Text = RTBEditor.Text;
                _editorUI.Modified = false;
            }

            RTBEditor.Modified = false;

            return true;
        }

        private void UpdateSatusLabel(string message, Image? image = null)
        {
            statusStrip.Items[1].Text = message;
            statusStrip.Items[1].Image = image;
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
                RTBEditor.Modified = false;
            Close();
        }
    }
}
