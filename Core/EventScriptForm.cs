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
            InitializeComponent();
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
                    dropDownVariables.DropDownItems.Add($"{varName} : {varType}");
            else{
                dropDownVariables.Text = $"{eventScript.VariablesNames.Count} var{(eventScript.VariablesNames.Count>1 ? "s" : "")}";
                foreach (var (varName, varType) in eventScript.VariablesNames)
                    dropDownVariables.DropDownItems.Add($"{varName} : {varType}");
            }

            RTBEditor.Text = eventScript.Script;

            cmdSave.IsLink = false;

            FormClosing += EventScriptForm_FormClosing;

            EventScript.OnScriptChanged += EventScript_OnScriptChanged;

            EventScriptEditor.CodeRender(RTBEditor, eventScript);
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
        EventScript? EventScript;


        private void Editor_TextChanged(object sender, EventArgs e)
        {
            cmdSave.IsLink = true;

            EventScriptEditor.CodeRender(RTBEditor, EventScript);
        }

        private void EventScript_OnScriptChanged(object? sender, EventArgs e)
        {
            if (EventScript == null)
                return;

            RTBEditor.Text = EventScript.Script;

            if (_editorUI != null && !_editorUI.IsDisposed)
                _editorUI.Text = RTBEditor.Text;

            cmdSave.IsLink = false;
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
                if (EventScript.Script != RTBEditor.Text)
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
                return true;
            }

            EventScript.Script = RTBEditor.Text;

            if (_editorUI != null && !_editorUI.IsDisposed)
                _editorUI.Text = RTBEditor.Text;

            cmdSave.IsLink = false;

            return true;
        }

        private void cmdSave_Click(object sender, EventArgs e)
        {
            Save();

            if (Control.ModifierKeys == Keys.Control)
                Close();
        }

        private void CmdClose_Click(object sender, EventArgs e) => Close();

    }
}
