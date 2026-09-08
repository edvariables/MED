using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using static MED.RichTextBoxMED;

namespace MED
{
    public class RichTextBoxMED : RichTextBox
    {
        public RichTextBoxMED() : base()
        {
            ScrollBars = RichTextBoxScrollBars.ForcedVertical | RichTextBoxScrollBars.Horizontal;
            KeyDown += Control_KeyDown;
            TextChanged += RichTextBoxMED_TextChanged;
            SelectionChanged += RichTextBoxMED_SelectionChanged;

            InitCodeColors();
        }
        private void InitCodeColors()
        {
            bool dark = true;
            if (dark)
            {
                CodeColors.Add("BackColor", Color.Black);
                CodeColors.Add("ForeColor", Color.White);
                CodeColors.Add("keyword", Color.LightPink);
                CodeColors.Add("type", Color.LightGreen);
                CodeColors.Add("comment", Color.Green);
                CodeColors.Add("string", Color.LightCoral);
                CodeColors.Add("stringz", Color.MediumPurple);
            }
            else
            {
                CodeColors.Add("BackColor", Color.White);
                CodeColors.Add("ForeColor", Color.Black);
                CodeColors.Add("keyword", Color.Blue);
                CodeColors.Add("type", Color.DarkCyan);
                CodeColors.Add("comment", Color.Green);
                CodeColors.Add("string", Color.Brown);
                CodeColors.Add("stringz", Color.Purple);
            }
            BackColor = CodeColors["BackColor"];
            ForeColor = CodeColors["ForeColor"];
        }

        public RichTextBoxMED(EventScript? eventScript) : this()
        {
            EventScript = eventScript;
        }

        public Dictionary<string, Color> CodeColors { get; set; } = [];


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
                //Clipboard.SetData("Text", SelectedText);
                //e.Handled = true;
                //if (e.KeyCode == Keys.X)
                //    SelectedText = "";
                //else
                return;
            }

            if ((e.KeyCode == Keys.A | e.KeyCode == Keys.Space)
                && ((ModifierKeys & Keys.Control) == Keys.Control))
                return;

            if (e.KeyCode == Keys.Apps || e.KeyCode == Keys.NumLock || e.KeyCode == Keys.Insert
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
                redoStack.Clear();
                StackPush(this, undoStack);

                if (e.KeyCode == Keys.V //Paste RTF
                && e.Control)
                {
                    CodeRenderClearRTF = true;
                    //SelectedText = (string)Clipboard.GetData("Text") ?? "";
                    //e.Handled = true;
                }
            }

        }

        public new string SelectedText
        {
            get => base.SelectedText;

            set
            {
                redoStack.Clear();
                StackPush(this, undoStack);

                base.SelectedText = value;
            }
        }

        public const int UndoStackMaxLength = 64;

        private Stack<Func<RichTextBoxMED>> undoStack = new Stack<Func<RichTextBoxMED>>();
        private Stack<Func<RichTextBoxMED>> redoStack = new Stack<Func<RichTextBoxMED>>();

        /**
         * Stack RichTextBoxMED state
         * */
        private void StackPush(RichTextBoxMED textBox, Stack<Func<RichTextBoxMED>> stack)
        {
            var tBT = textBox.Text(textBox.Text, textBox.SelectionStart);
            stack.Push(tBT);
            if (stack.Count > UndoStackMaxLength)
            {
                var newStack = new Stack<Func<RichTextBoxMED>>(stack.SkipLast(stack.Count - UndoStackMaxLength).Reverse());
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
                StackPush(this, redoStack);
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
                StackPush(this, undoStack);
                redoStack.Pop()();
            }
        }
        #region  LineNumbersTextBox

        private RichTextBox? _LineNumbersTextBox;
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

        #endregion

        #region Code editor

        private static void UpdateSatusLabel(Control control, string message, Image? image = null)
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
        public EventScript? EventScript
        {
            get => _EventScript;
            set
            {
                if (_EventScript == value)
                    return;
                _EventScript = value;
                if (_EventScript == null)
                    ResetText();
                CodeRenderPrepare();
                CodeRender();
            }
        }

        int PreviousSelectionStart;
        int LastSelectionStart;

        bool CodeRenderClearRTF = false;

        public new void Paste(System.Windows.Forms.DataFormats.Format clipFormat)
        {
            base.Paste(DataFormats.GetFormat(DataFormats.Text));
        }

        public new string Text
        {
            get => base.Text;
            set
            {
                PreviousSelectionStart = LastSelectionStart = 0;
                base.Text = value;
                redoStack.Clear();
                StackPush(this, undoStack);
            }
        }
        private void RichTextBoxMED_TextChanged(object? sender, EventArgs e)
        {
            LastSelectionStart = PreviousSelectionStart;
            CodeRender();
            LastSelectionStart = SelectionStart;
        }

        private void RichTextBoxMED_SelectionChanged(object? sender, EventArgs e)
        {
            if (!Enabled)
                return;
            PreviousSelectionStart = LastSelectionStart;
            LastSelectionStart = SelectionStart;
        }

        Dictionary<string, Regex> CodeRenderRegex = [];
        void CodeRenderPrepare()
        {
            CodeRenderRegex = new();

            EventScript? eventScript = EventScript;

            // getting keywords/functions
            var functions = String.Join('|', eventScript?.ScriptGlobalsFunctions.Select(kvp => kvp.Value.Name) ?? []);
            if (functions != "") functions += "|";
            string keywords = @"\b(" + functions + @"abstract |as|base|break|case|catch|checked|continue|default|delegate|do|else|event|explicit|extern|false|finally|fixed|for|foreach|goto|if|implicit|in|interface|internal|is|lock|namespace|new|null|object|operator|out|override|params|private|protected|public|readonly|ref|return|sealed|sizeof|stackalloc|switch|this|throw|true|try|typeof|unchecked|unsafe|using|virtual|volatile|while|var)\b";
            CodeRenderRegex.Add("keywords", new(keywords));

            // getting types/classes/keyobjects from the text 
            if (eventScript != null && eventScript.VariablesNames == null)
                eventScript.CompileScript();
            var variables = new Dictionary<string, Type>(eventScript?.VariablesNames ?? []);
            string types = @"\b(Console|" + string.Join("|", variables.Keys) + @")\b";
            CodeRenderRegex.Add("types", new(types));

            // getting comments (inline or multiline)
            string comments = @"(\/\/.+?$|\/\*.+?\*\/)";
            CodeRenderRegex.Add("comments", new(comments, RegexOptions.Multiline));

            // getting strings
            string strings = "\".+?\"";
            CodeRenderRegex.Add("strings", new(strings));

            string stringz = "bool|byte|char|class|const|decimal|double|enum|float|int|long|sbyte|short|static|string|struct|uint|ulong|ushort|void";
            CodeRenderRegex.Add("stringz", new(stringz));

        }

        /**
         * CodeRender()
         * 
         * */
        public void CodeRender()
        {
            RichTextBoxMED? richTextBox = this;

            if (richTextBox == null || richTextBox.IsDisposed)
                return;

            if (CodeRenderRegex.Count == 0)
                CodeRenderPrepare();

            string text;
            int selectionStart = GetFirstCharIndexFromLine(GetLineFromCharIndex(LastSelectionStart));
            int selectionEnd = GetFirstCharIndexFromLine(GetLineFromCharIndex(SelectionStart) + 1) - 1;
            if (selectionEnd < selectionStart)
                selectionEnd = TextLength;
            if (LastSelectionStart == 0/*richTextBox.Enabled && richTextBox.Focused*/)
                text = Text;
            else
                text = Text.Substring(selectionStart, selectionEnd - selectionStart);

            //UpdateSatusLabel(this, $"{selectionStart}->{selectionEnd}");

            // Source - https://stackoverflow.com/a/58481519
            // Posted by Momoro
            // Retrieved 2026-09-06, License - CC BY-SA 4.0

            // getting keywords/functions
            MatchCollection keywordMatches = CodeRenderRegex["keywords"].Matches(text);

            // getting types/classes/keyobjects from the text 
            MatchCollection typeMatches = CodeRenderRegex["types"].Matches(text);

            // getting comments (inline or multiline)
            MatchCollection commentMatches = CodeRenderRegex["comments"].Matches(text);

            // getting strings
            MatchCollection stringMatches = CodeRenderRegex["strings"].Matches(text);
            MatchCollection stringzMatchez = CodeRenderRegex["stringz"].Matches(text);

            // saving the original caret position + forecolor
            int originalIndex = richTextBox.SelectionStart;
            int originalLength = richTextBox.SelectionLength;
            Color originalColor = ForeColor;

            bool isActiveControl = richTextBox.Focused;

            //UpdateSatusLabel(richTextBox, $"{richTextBox.UndoActionName}");

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
            foreach (Match m in keywordMatches)
            {
                richTextBox.SelectionStart = m.Index + selectionStart;
                richTextBox.SelectionLength = m.Length;
                richTextBox.SelectionColor = CodeColors["keyword"];//Blue
            }

            foreach (Match m in typeMatches)
            {
                richTextBox.SelectionStart = m.Index + selectionStart;
                richTextBox.SelectionLength = m.Length;
                richTextBox.SelectionColor = CodeColors["type"]; //Color.DarkCyan;
            }

            foreach (Match m in commentMatches)
            {
                richTextBox.SelectionStart = m.Index + selectionStart;
                richTextBox.SelectionLength = m.Length;
                richTextBox.SelectionColor = CodeColors["comment"]; //Color.Green;
            }

            foreach (Match m in stringMatches)
            {
                richTextBox.SelectionStart = m.Index + selectionStart;
                richTextBox.SelectionLength = m.Length;
                richTextBox.SelectionColor = CodeColors["string"]; //Color.Brown;
            }

            foreach (Match m in stringzMatchez)
            {
                richTextBox.SelectionStart = m.Index + selectionStart;
                richTextBox.SelectionLength = m.Length;
                richTextBox.SelectionColor = CodeColors["stringz"]; //Color.Purple;
            }

            // restoring the original colors, for further writing
            richTextBox.SelectionStart = originalIndex;
            richTextBox.SelectionLength = originalLength;
            richTextBox.SelectionColor = originalColor;

            richTextBox.Enabled = true;

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
        public static Func<RichTextBoxMED> Text(this RichTextBoxMED textBox, string text, int sel)
        {
            return () =>
            {
                textBox.Text = text;
                textBox.SelectionStart = sel;
                return textBox;
            };
        }
    }
}
