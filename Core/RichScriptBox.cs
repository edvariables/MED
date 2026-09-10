using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using static MED.RichScriptBox;
using static System.Net.Mime.MediaTypeNames;

namespace MED
{
    public class RichScriptBox : RichTextBox
    {
        public RichScriptBox() : base()
        {
            //Default property value
            AcceptsTab = true;
            ScrollBars = RichTextBoxScrollBars.ForcedVertical | RichTextBoxScrollBars.Horizontal;
            ShowSelectionMargin = true;

            //Handle
            KeyDown += Control_KeyDown;
            TextChanged += RichScriptBox_TextChanged;
            SelectionChanged += RichScriptBox_SelectionChanged;
            MouseWheel += RichScriptBox_MouseWheel;
            DoubleClick += RichScriptBox_DoubleClick;

            InitCodeColors();
        }

        public RichScriptBox(EventScript? eventScript) : this()
        {
            EventScript = eventScript;
        }

        #region Colors
        private void InitCodeColors()
        {
            bool dark = true;
            if (dark)
            {
                CodeColors.Add("BackColor", Color.Black);
                CodeColors.Add("ForeColor", Color.LightGray);
                CodeColors.Add("keyword", Color.HotPink);
                CodeColors.Add("variable", Color.PaleGoldenrod);
                CodeColors.Add("type", Color.LightGreen);
                CodeColors.Add("comment", Color.Green);
                CodeColors.Add("string", Color.LightCoral);
                CodeColors.Add("stringz", Color.MediumPurple);
                CodeColors.Add("Highlight", Color.DarkBlue);
            }
            else
            {
                CodeColors.Add("BackColor", Color.White);
                CodeColors.Add("ForeColor", Color.Black);
                CodeColors.Add("variable", Color.BlueViolet);
                CodeColors.Add("keyword", Color.Blue);
                CodeColors.Add("type", Color.DarkCyan);
                CodeColors.Add("comment", Color.Green);
                CodeColors.Add("string", Color.Brown);
                CodeColors.Add("stringz", Color.Purple);
                CodeColors.Add("Highlight", Color.AliceBlue);
            }
            BackColor = CodeColors["BackColor"];
            ForeColor = CodeColors["ForeColor"];
        }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Dictionary<string, Color> CodeColors { get; set; } = [];


        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public override Color ForeColor
        {
            get => base.ForeColor;
            set => base.ForeColor = value;
        }
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public override Color BackColor
        {
            get => base.BackColor;
            set => base.BackColor = value;
        }
        #endregion

        #region Keys
        private void Control_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.ControlKey
                && ((ModifierKeys & Keys.Control) == Keys.Control))
                return;
            if (e.KeyCode == Keys.ShiftKey
                && ((ModifierKeys & Keys.Shift) == Keys.Shift))
                return;
            if (e.KeyCode == Keys.Menu
                && ((ModifierKeys & Keys.Alt) == Keys.Alt))
                return;

            //Copy as Text, not RTF
            if ((e.KeyCode == Keys.C | e.KeyCode == Keys.X)
            && e.Control)
            {
                Clipboard.SetData("Text", SelectedText);
                e.Handled = true;
                if (e.KeyCode == Keys.X)
                {
                    StackPushUndo();
                    base.SelectedText = "";
                }
                return;
            }

            if ((e.KeyCode == Keys.A | e.KeyCode == Keys.Space)
                && ((ModifierKeys & Keys.Control) == Keys.Control))
                return;

            if (e.KeyCode == Keys.Apps || e.KeyCode == Keys.Home || e.KeyCode == Keys.End || e.KeyCode == Keys.NumLock || e.KeyCode == Keys.Insert
                || e.KeyCode == Keys.Left || e.KeyCode == Keys.Right || e.KeyCode == Keys.Up || e.KeyCode == Keys.Down
                || e.KeyCode == Keys.PageUp || e.KeyCode == Keys.PageDown
                 || e.KeyCode == Keys.F1 || e.KeyCode == Keys.F2 || e.KeyCode == Keys.F3 || e.KeyCode == Keys.F4 || e.KeyCode == Keys.F5 || e.KeyCode == Keys.F6 || e.KeyCode == Keys.F7 || e.KeyCode == Keys.F8 || e.KeyCode == Keys.F9 || e.KeyCode == Keys.F10 || e.KeyCode == Keys.F11 || e.KeyCode == Keys.F12
                )
                return;
            if (e.KeyCode == Keys.Z
            && e.Control)
            {
                if (e.Shift)
                    Redo();
                else
                    Undo();
                e.SuppressKeyPress = true;
            }
            else
            {
                StackPushUndo();

                if (e.KeyCode == Keys.V //Paste RTF
                && e.Control)
                {
                    CodeRenderClearRTF = true;
                    //SelectedText = (string)Clipboard.GetData("Text") ?? "";
                    //e.Handled = true;
                }
                else if (e.KeyCode == Keys.Tab)
                {
                    Control_TabKeyDown(e);
                }
                else if (e.KeyCode == Keys.Enter)
                {
                    Control_EnterKeyDown(e);
                }
                else if (e.KeyCode == Keys.Back)
                {
                    Control_BackKeyDown(e);
                }
            }

        }

        /**
         * Return
         * */
        private void Control_EnterKeyDown(KeyEventArgs e)
        {
            var lineIndex = GetLineFromCharIndex(SelectionStart);
            var lineCharIndex = GetFirstCharIndexFromLine(lineIndex);
            var lastCharIndex = GetFirstCharIndexFromLine(lineIndex + 1) - 1;
            if (lastCharIndex > SelectionStart)
                lastCharIndex = SelectionStart;
            else if (lastCharIndex < 0)
                lastCharIndex = TextLength;
            float nbTabs = 0F;
            if (lineCharIndex < 0 || lastCharIndex - lineCharIndex < 0)
                EventScript?.Process.Performance?.Debug("ICICIC");
            var lineText = Text.Substring(lineCharIndex, lastCharIndex - lineCharIndex);
            var lineTrimed = lineText.TrimEnd(' ', '\t');
            if (lineTrimed.Length > 0)
            {
                if (lineTrimed[lineTrimed.Length - 1] == '{')
                    nbTabs++;
                else
                {
                    lineTrimed = lineTrimed.TrimStart('\t').Replace(" ", "");
                    if (lineTrimed.StartsWith("if") || lineTrimed.StartsWith("do(") || lineTrimed.StartsWith("while("))
                        nbTabs++;
                }
            }
            for (var charIndex = 0; charIndex < lineText.Length; charIndex++)
            {
                if (lineText[charIndex] == ' ')
                    nbTabs += 0.25F;
                else if (lineText[charIndex] == '\t')
                    nbTabs += 1F;
                else
                    break;
            }
            base.SelectedText = "\n" + (new String(' ', 4 * (int)Math.Floor(nbTabs)));
            e.SuppressKeyPress = true;
        }

        /**
         * Tab
         * */
        private void Control_TabKeyDown(KeyEventArgs e)
        {
            var tabSpaces = "    ";
            if (SelectionLength > 0)
            {
                var originalSelectionStart = SelectionStart;
                var originalSelectionLength = SelectionLength;
                var selectionEnd = GetFirstCharIndexFromLine(GetLineFromCharIndex(SelectionStart + SelectionLength) + 1) - 1;
                var firstCharIndexFromLine = GetFirstCharIndexFromLine(GetLineFromCharIndex(SelectionStart));
                SelectionStart = firstCharIndexFromLine;
                SelectionLength = originalSelectionStart - firstCharIndexFromLine + originalSelectionLength;
                var lines = SelectedText.Split("\n");
                if (e.Shift)
                {
                    var deletedCharsFirstLine = 0;
                    for (var i = 0; i < lines.Length; i++)
                    {
                        if (lines[i].StartsWith(tabSpaces))
                        {
                            selectionEnd -= tabSpaces.Length;
                            if (i == 0)
                                deletedCharsFirstLine += tabSpaces.Length;
                            else
                                originalSelectionLength -= tabSpaces.Length;
                            lines[i] = lines[i].Substring(tabSpaces.Length);
                        }
                        else if (lines[i].StartsWith('\t'))
                        {
                            selectionEnd -= 1;
                            if (i == 0)
                                deletedCharsFirstLine += 1;
                            else
                                originalSelectionLength -= 1;
                            lines[i] = lines[i].Substring(1);
                        }
                    }
                    base.SelectedText = String.Join('\n', lines);
                    if (originalSelectionStart <= firstCharIndexFromLine + deletedCharsFirstLine)
                        SelectionStart = firstCharIndexFromLine;
                    else
                        SelectionStart = originalSelectionStart - deletedCharsFirstLine;
                    SelectionLength = originalSelectionLength;
                }
                else
                {
                    base.SelectedText = tabSpaces + String.Join("\n" + tabSpaces, lines);
                    SelectionStart = originalSelectionStart + tabSpaces.Length;
                    SelectionLength = originalSelectionLength + (lines.Length - 1) * tabSpaces.Length;
                }
            }
            else
            {
                if (e.Shift)
                {
                    var originalSelectionStart = SelectionStart;
                    var firstCharIndexFromLine = GetFirstCharIndexFromLine(GetLineFromCharIndex(SelectionStart));
                    SelectionStart = firstCharIndexFromLine;
                    if (Text.Substring(SelectionStart, tabSpaces.Length) == tabSpaces)
                    {
                        SelectionLength = tabSpaces.Length;
                        base.SelectedText = "";
                        if (originalSelectionStart <= firstCharIndexFromLine + tabSpaces.Length)
                            SelectionStart = firstCharIndexFromLine;
                        else
                            SelectionStart = originalSelectionStart - tabSpaces.Length;
                    }
                    else if (Text.Substring(SelectionStart, 1) == "\t")
                    {
                        SelectionLength = 1;
                        base.SelectedText = "";
                        if (originalSelectionStart <= firstCharIndexFromLine + 1)
                            SelectionStart = firstCharIndexFromLine;
                        else
                            SelectionStart = originalSelectionStart - 1;
                    }
                    else
                        SelectionStart = originalSelectionStart;
                }
                else
                {
                    base.SelectedText = tabSpaces;
                }
            }
            e.SuppressKeyPress = true;
            e.Handled = true;

        }

        /**
         * Back
         * */
        private void Control_BackKeyDown(KeyEventArgs e)
        {
            var lineIndex = GetLineFromCharIndex(SelectionStart);
            var lineCharIndex = GetFirstCharIndexFromLine(lineIndex);
            float nbSpaces = 0F;
            var lineText = Text.Substring(lineCharIndex, SelectionStart - lineCharIndex);
            for (var charIndex = lineText.Length - 1; charIndex >= 0; charIndex--)
                if (lineText[charIndex] == ' ')
                {
                    nbSpaces++;
                    if (nbSpaces == 4)
                    {
                        SelectionStart -= 4;
                        SelectionLength = 4;
                        base.SelectedText = "";
                        e.SuppressKeyPress = true;
                        return;
                    }
                }
                else
                    return;
        }

        #endregion

        #region Undo Redo
        public const int UndoStackMaxLength = 64;

        private Stack<Func<RichScriptBox>> undoStack = new Stack<Func<RichScriptBox>>();
        private Stack<Func<RichScriptBox>> redoStack = new Stack<Func<RichScriptBox>>();

        /**
         * Stack RichScriptBox state
         * */
        private void StackPushUndo(bool redoStackClear = true)
        {
            if (redoStackClear)
                redoStack.Clear();
            StackPush(this, undoStack);
        }
        private void StackPushRedo() => StackPush(this, redoStack);
        private void StackPush(RichScriptBox textBox, Stack<Func<RichScriptBox>> stack)
        {
            var tBT = textBox.Text(textBox.Text, textBox.SelectionStart);
            stack.Push(tBT);
            if (stack.Count > UndoStackMaxLength)
            {
                var newStack = new Stack<Func<RichScriptBox>>(stack.SkipLast(stack.Count - UndoStackMaxLength).Reverse());
                if (undoStack.Equals(stack))
                    undoStack = newStack;
                else if (redoStack.Equals(stack))
                    redoStack = newStack;
                else
                    throw new NotImplementedException();
            }
        }

        /**
         * Undo
         * */
        public new void Undo()
        {
            if (undoStack.Count > 0)
            {
                StackPushRedo();
                undoStack.Pop()();
            }
        }
        /**
         * Redo
         * */
        public new void Redo()
        {
            if (redoStack.Count > 0)
            {
                StackPushUndo(false);
                redoStack.Pop()();
            }
        }
        #endregion

        #region  LineNumbersTextBox

        private RichTextBox? _LineNumbersTextBox;

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public RichTextBox? LineNumbersTextBox
        {
            get => _LineNumbersTextBox;
            set
            {
                _LineNumbersTextBox = value;
                if (_LineNumbersTextBox != null)
                {
                    _LineNumbersTextBox.ReadOnly = true;
                    _LineNumbersTextBox.Font = Font;
                    _LineNumbersTextBox.BackColor = BackColor;
                    float gray = BackColor == Color.Black ? 0.7F : 1.3F;
                    var foreColor = Color.FromArgb((int)(ForeColor.R * gray), (int)(ForeColor.B * gray), (int)(ForeColor.B * gray));
                    _LineNumbersTextBox.ForeColor = foreColor;
                    _LineNumbersTextBox.ScrollBars = RichTextBoxScrollBars.None;
                    _LineNumbersTextBox.ZoomFactor = ZoomFactor;
                    SetSelectionLineSpacing(260);

                    SizeChanged += (object? sender, EventArgs e) => AddLineNumbers();
                    SelectionChanged += (sender, e) =>
                    {
                        if (!Enabled)
                            return;
                        Point pt = GetPositionFromCharIndex(SelectionStart);
                        if (pt.X == 1) AddLineNumbers();
                    };
                    VScroll += (sender, e) =>
                    {
                        AddLineNumbers();
                        _LineNumbersTextBox.Invalidate();
                    };
                    TextChanged += (sender, e) => AddLineNumbers();

                    FontChanged += (sender, e) =>
                    {
                        _LineNumbersTextBox.Font = Font;
                        Select();
                        AddLineNumbers();
                    };
                    _LineNumbersTextBox.MouseDown += (sender, e) =>
                    {
                        Select();
                        _LineNumbersTextBox.DeselectAll();
                    };
                }
            }
        }
        /**
         * AddLineNumbers()
         * https://www.c-sharpcorner.com/blogs/creating-line-numbers-for-richtextbox-in-c-sharp
         * */
        public void AddLineNumbers()
        {
            if (_LineNumbersTextBox == null || Disposing || IsDisposed || !Enabled)
                return;
            var enabled = _LineNumbersTextBox.Enabled;
            _LineNumbersTextBox.SuspendLayout();
            _LineNumbersTextBox.Enabled = false;
            // create & set Point pt to (0,0)
            Point pt = new Point(0, 0);
            // get First Index & First Line from richTextBox1
            int First_Index = GetCharIndexFromPosition(pt);
            int First_Line = GetLineFromCharIndex(First_Index);
            // set X & Y coordinates of Point pt to ClientRectangle Width & Height respectively
            pt.X = ClientRectangle.Width;
            pt.Y = ClientRectangle.Height;
            // get Last Index & Last Line from richTextBox1
            int Last_Index = GetCharIndexFromPosition(pt);
            int Last_Line = GetLineFromCharIndex(Last_Index);
            // set Center alignment to LineNumberTextBox
            //_LineNumbersTextBox.TextAlign = HorizontalAlignment.Center;
            // set LineNumberTextBox text to null & width to getWidth() function value
            //_LineNumbersTextBox.Text = "";
            //_LineNumbersTextBox.Width = getWidth();

            // now add each line number to LineNumberTextBox upto last line
            StringBuilder str = new();
            for (int i = First_Line; i <= Last_Line; i++)
                str.AppendLine((i + 1).ToString());

            _LineNumbersTextBox.Text = str.ToString();
            _LineNumbersTextBox.SelectAll();
            _LineNumbersTextBox.SelectionAlignment = HorizontalAlignment.Right;
            _LineNumbersTextBox.Select(0, 0);

            _LineNumbersTextBox.Enabled = enabled;
            _LineNumbersTextBox.SuspendLayout();
        }

        private void RichScriptBox_MouseWheel(object? sender, MouseEventArgs e)
        {
            if (ModifierKeys == Keys.Control
                && _LineNumbersTextBox != null)
                if (e.Delta < 0)
                    _LineNumbersTextBox.ZoomFactor = ZoomFactor / 1.1F;
                else
                    _LineNumbersTextBox.ZoomFactor = ZoomFactor * 1.1F;
        }
        #endregion

        #region Code editor

        private static void UpdateSatusLabel(Control control, string message, System.Drawing.Image? image = null)
        {
            if (control is StatusStrip statusStrip && statusStrip.Items.Count > 1)
            {
                statusStrip.Items[1].Text = message;
                statusStrip.Items[1].Image = image;
            }
            else if (control.Parent != null
            && control.Parent.Controls["statusStrip"] != null
            && control.Parent.Controls["statusStrip"] is StatusStrip statusStrip1)
                UpdateSatusLabel(statusStrip1, message, image);

        }
        private EventScript? _EventScript;

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public EventScript? EventScript
        {
            get => _EventScript;
            set
            {
                if (_EventScript == value)
                    return;
                _EventScript = value;
                if (_EventScript == null)
                {
                    var enabled = Enabled;
                    var focused = Focused;
                    Enabled = false;
                    ResetText();
                    Enabled = enabled;
                    if (focused) Focus();
                }
                CodeRenderPrepare();
                CodeRender();
            }
        }

        int PreviousSelectionStart;
        int LastSelectionStart;

        bool CodeRenderClearRTF = false;

        [Category("Script")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public new string Text
        {
            get => base.Text;
            set
            {
                PreviousSelectionStart = LastSelectionStart = 0;
                base.Text = value;
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public new string SelectedText
        {
            get => base.SelectedText;

            set
            {
                StackPushUndo();

                base.SelectedText = value;
            }
        }
        private void RichScriptBox_TextChanged(object? sender, EventArgs e)
        {
            if (!Enabled || Parent == null || (FindForm() is Form form && form.IsDisposed))
                return;
            LastSelectionStart = PreviousSelectionStart;
            CodeRender();
            LastSelectionStart = SelectionStart;
        }

        private void RichScriptBox_SelectionChanged(object? sender, EventArgs e)
        {
            if (!Enabled)
                return;
            PreviousSelectionStart = LastSelectionStart;
            LastSelectionStart = SelectionStart;
        }

        string? PreviousHighlightedWord;
        private void RichScriptBox_DoubleClick(object? sender, EventArgs e)
        {

            var selectedText = SelectedText;

            var enabled = Enabled;

            var selectionStart = SelectionStart;
            var selectionLength = SelectionLength;

            SuspendLayout();
            Enabled = false;

            if (!String.IsNullOrEmpty(PreviousHighlightedWord))
                HighlightWord(PreviousHighlightedWord);
            HighlightWord(selectedText, CodeColors["Highlight"]);

            SelectionStart = selectionStart;
            SelectionLength = selectionLength;
            Enabled = enabled;
            ResumeLayout();

            Focus();

            PreviousHighlightedWord = selectedText;
        }
        private void HighlightWord(string word, Color? highlightColor = null)
        {
            MatchCollection matches = new Regex(@"\b" + Regex.Escape(word)).Matches(Text);
            if (highlightColor == null)
                highlightColor = BackColor;

            foreach (Match m in matches)
            {
                SelectionStart = m.Index;
                SelectionLength = m.Length;
                SelectionBackColor = highlightColor ?? BackColor;
            }
        }
        #endregion

        #region CodeRender
        Dictionary<string, Regex> CodeRenderRegex = [];
        void CodeRenderPrepare()
        {
            CodeRenderRegex = new();

            EventScript? eventScript = EventScript;

            if (eventScript != null && eventScript.VariablesNames == null)
                eventScript.CompileScript();

            string variables = "";
            string varTypes = "";
            if (eventScript != null && eventScript.VariablesNames != null)
            {
                var vars = new Dictionary<string, Type>(eventScript.VariablesNames);
                variables = string.Join("|", vars.Keys);
                varTypes = String.Join('|', eventScript.VariablesNames.Select(kvp => kvp.Value.Name) ?? []);
            }

            // getting keywords
            string keywords = @"\b(abstract|as|base|break|case|catch|checked|continue|default|delegate|do|else|event|explicit|extern|false|finally|fixed|for|foreach|goto|if|implicit|in|interface|internal|is|lock|namespace|new|null|object|operator|out|override|params|private|protected|public|readonly|ref|return|sealed|sizeof|stackalloc|switch|this|throw|true|try|typeof|unchecked|unsafe|using|virtual|volatile|while|var)\b";
            CodeRenderRegex.Add("keywords", new(keywords));

            // getting variables & functions
            var functions = String.Join('|', eventScript?.ScriptGlobalsFunctions.Select(kvp => kvp.Value.Name) ?? []);
            //if (variables != "") variables += "|";
            if (functions != "") functions += "|";
            string varsfuncs = @"\b(" + functions + variables + @")\b";
            CodeRenderRegex.Add("variables", new(varsfuncs));

            // getting types/classes/keyobjects from the text 
            if (varTypes != "") varTypes = "|" + varTypes;
            string types = @"\b(Console" + varTypes + @")\b";
            CodeRenderRegex.Add("types", new(types));

            // getting comments (inline or multiline)
            string comments = @"(\/\/.+?$|\/\*[\s\S]*?\*\/)";
            CodeRenderRegex.Add("comments", new(comments, RegexOptions.Multiline));

            // getting strings
            string strings = "\".+?\"";
            CodeRenderRegex.Add("strings", new(strings));

            string stringz = @"\b(bool|byte|char|class|const|decimal|double|enum|float|int|long|sbyte|short|static|string|struct|uint|ulong|ushort|void)\b";
            CodeRenderRegex.Add("stringz", new(stringz));

        }

        /**
         * CodeRender()
         * 
         * */
        public void CodeRender()
        {
            RichScriptBox? richTextBox = this;

            if (richTextBox == null || richTextBox.IsDisposed || richTextBox.TextLength == 0)
                return;

            if (CodeRenderRegex.Count == 0)
                CodeRenderPrepare();

            var enabled = richTextBox.Enabled;

            string text;
            int selectionStart = GetFirstCharIndexFromLine(GetLineFromCharIndex(LastSelectionStart));
            int selectionEnd = GetFirstCharIndexFromLine(GetLineFromCharIndex(SelectionStart) + 1) - 1;
            if (selectionEnd < selectionStart)
                selectionEnd = TextLength;
            if (LastSelectionStart == 0/*richTextBox.Enabled && richTextBox.Focused*/)
            {
                text = Text;
                PreviousHighlightedWord = null;
            }
            else
                text = Text.Substring(selectionStart, selectionEnd - selectionStart);

            //UpdateSatusLabel(this, $"{selectionStart}->{selectionEnd}");

            // Source - https://stackoverflow.com/a/58481519
            // Posted by Momoro
            // Retrieved 2026-09-06, License - CC BY-SA 4.0

            // getting keywords
            MatchCollection keywordMatches = CodeRenderRegex["keywords"].Matches(text);

            // getting variables & functions
            MatchCollection variableMatches = CodeRenderRegex["variables"].Matches(text);

            // getting types/classes/keyobjects from the text 
            MatchCollection typeMatches = CodeRenderRegex["types"].Matches(text);

            // getting comments (inline or multiline)
            MatchCollection commentMatches = CodeRenderRegex["comments"].Matches(text);

            // getting strings
            MatchCollection stringMatches = CodeRenderRegex["strings"].Matches(text);

            // getting basic types
            MatchCollection stringzMatchez = CodeRenderRegex["stringz"].Matches(text);

            // saving the original caret position + forecolor
            int originalIndex = richTextBox.SelectionStart;
            int originalLength = richTextBox.SelectionLength;
            Color originalColor = ForeColor;

            bool isActiveControl = richTextBox.Focused;

            //UpdateSatusLabel(richTextBox, $"{richTextBox.UndoActionName}");

            Color color;

            richTextBox.SuspendLayout();
            richTextBox.Enabled = false;

            // removes any previous highlighting (so modified words won't remain highlighted)
            richTextBox.SelectionStart = selectionStart;
            richTextBox.SelectionLength = selectionEnd - selectionStart;
            richTextBox.SelectionColor = originalColor;
            richTextBox.SelectionBackColor = CodeColors["BackColor"];
            richTextBox.SelectionFont = Font;
            if (CodeRenderClearRTF)
            {
                //richTextBox.SelectedText = text;
                //richTextBox.SelectionStart = selectionStart;
                //richTextBox.SelectionLength = selectionEnd - selectionStart;
                SetSelectionLineSpacing(240);
                var ls = GetSelectionLineSpacing();
                CodeRenderClearRTF = false;
            }

            // scanning...
            color = CodeColors["keyword"];
            foreach (Match m in keywordMatches)
            {
                richTextBox.SelectionStart = m.Index + selectionStart;
                richTextBox.SelectionLength = m.Length;
                richTextBox.SelectionColor = color;//Blue
            }

            color = CodeColors["type"];
            foreach (Match m in typeMatches)
            {
                richTextBox.SelectionStart = m.Index + selectionStart;
                richTextBox.SelectionLength = m.Length;
                richTextBox.SelectionColor = color; //Color.DarkCyan;
            }

            color = CodeColors["variable"];
            foreach (Match m in variableMatches)
            {
                richTextBox.SelectionStart = m.Index + selectionStart;
                richTextBox.SelectionLength = m.Length;
                richTextBox.SelectionColor = color; //Color.Blue;
            }

            color = CodeColors["string"];
            foreach (Match m in stringMatches)
            {
                richTextBox.SelectionStart = m.Index + selectionStart;
                richTextBox.SelectionLength = m.Length;
                richTextBox.SelectionColor = color; //Color.Brown;
            }

            color = CodeColors["stringz"];
            foreach (Match m in stringzMatchez)
            {
                richTextBox.SelectionStart = m.Index + selectionStart;
                richTextBox.SelectionLength = m.Length;
                richTextBox.SelectionColor = color; //Color.Purple;
            }

            color = CodeColors["comment"];
            foreach (Match m in commentMatches)
            {
                richTextBox.SelectionStart = m.Index + selectionStart;
                richTextBox.SelectionLength = m.Length;
                richTextBox.SelectionColor = color; //Color.Green;
            }

            // restoring the original colors, for further writing
            richTextBox.SelectionStart = originalIndex;
            richTextBox.SelectionLength = originalLength;
            richTextBox.SelectionColor = originalColor;

            richTextBox.Enabled = enabled;

            if (isActiveControl)
                richTextBox.Focus();

            richTextBox.ResumeLayout();

        }

        // Source - https://stackoverflow.com/a/35540582
        // Posted by Reza Aghaei, modified by community. See post 'Timeline' for change history
        // Retrieved 2026-09-08, License - CC BY-SA 3.0

        [DllImport("user32.dll", EntryPoint = "SendMessage", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, Int32 msg,
                                                 Int32 wParam, ref PARAFORMAT2 lParam);

        private const int SCF_SELECTION = 1;
        public const int PFM_LINESPACING = 256;
        public const int EM_SETPARAFORMAT = 1095;
        public const int EM_GETPARAFORMAT = 1085;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct PARAFORMAT2
        {
            public int cbSize;
            public uint dwMask;
            public Int16 wNumbering;
            public Int16 wReserved;
            public int dxStartIndent;
            public int dxRightIndent;
            public int dxOffset;
            public Int16 wAlignment;
            public Int16 cTabCount;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public int[] rgxTabs;
            public int dySpaceBefore;
            public int dySpaceAfter;
            public int dyLineSpacing;
            public Int16 sStyle;
            public byte bLineSpacingRule;
            public byte bOutlineLevel;
            public Int16 wShadingWeight;
            public Int16 wShadingStyle;
            public Int16 wNumberingStart;
            public Int16 wNumberingStyle;
            public Int16 wNumberingTab;
            public Int16 wBorderSpace;
            public Int16 wBorderWidth;
            public Int16 wBorders;
        }

        public void SetSelectionLineSpacing(int dyLineSpacing)
        {
            byte bLineSpacingRule = 4;
            PARAFORMAT2 format = new PARAFORMAT2();
            format.cbSize = Marshal.SizeOf(format);
            format.dwMask = PFM_LINESPACING;
            format.dyLineSpacing = dyLineSpacing;
            format.bLineSpacingRule = bLineSpacingRule;
            SendMessage(this.Handle, EM_SETPARAFORMAT, SCF_SELECTION, ref format);
            if (LineNumbersTextBox != null)
                SendMessage(LineNumbersTextBox.Handle, EM_SETPARAFORMAT, 0, ref format);
        }
        public int GetSelectionLineSpacing()
        {
            byte bLineSpacingRule = 4;
            PARAFORMAT2 format = new PARAFORMAT2();
            format.cbSize = Marshal.SizeOf(format);
            format.dwMask = PFM_LINESPACING;
            format.bLineSpacingRule = bLineSpacingRule;
            var r = SendMessage(this.Handle, EM_GETPARAFORMAT, 0, ref format);
            //GetLastError();
            return format.dyLineSpacing;
        }

        #endregion
    }
    public static partial class Extensions
    {
        /**
         * Cache and restore Text and Location for Undo/Redo tasks
         * */
        public static Func<RichScriptBox> Text(this RichScriptBox textBox, string text, int sel)
        {
            return () =>
            {
                textBox.SuspendLayout();
                textBox.Text = text;
                textBox.SelectionStart = sel;
                textBox.ResumeLayout();
                return textBox;
            };
        }
    }
}
