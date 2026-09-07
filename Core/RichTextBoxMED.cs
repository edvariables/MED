using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static MED.RichTextBoxMED;

namespace MED
{
    public class RichTextBoxMED : RichTextBox
    {
        public RichTextBoxMED() : base()
        {
            KeyDown += Control_KeyDown;
        }

        private void Control_KeyDown(object? sender, KeyEventArgs e)
        {
            if ((e.KeyCode == Keys.ControlKey || e.KeyCode == Keys.ShiftKey)
                && ((ModifierKeys & Keys.Control) == Keys.Control))
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
            }

        }

        //public enum TextModes
        //{
        //    TM_PLAINTEXT = 1,
        //    TM_RICHTEXT = 2,
        //    TM_SINGLELEVELUNDO = 4,
        //    TM_MULTILEVELUNDO = 8,
        //    TM_SINGLECODEPAGE = 16,
        //    TM_MULTICODEPAGE = 32
        //}

        //[DllImport("user32.dll")]
        //private static extern int SendMessage(IntPtr hWnd, int wMsg, IntPtr wParam, IntPtr lParam);

        //private const int EM_GETTEXTMODE = 0x045a;
        //private const int EM_SETTEXTMODE = 0x0459;

        //private TextModes _TextMode;
        //public TextModes TextMode
        //{
        //    get
        //    {
        //        var value = SendMessage(Handle, EM_GETTEXTMODE, IntPtr.Zero, 0);

        //        return (TextModes)value;
        //    }
        //    set
        //    {
        //        _TextMode = value;
        //        var r = SendMessage(Handle, EM_SETTEXTMODE, (int)_TextMode, 0);
        //    }
        //}

        Stack<Func<object>> undoStack = new Stack<Func<object>>();
        Stack<Func<object>> redoStack = new Stack<Func<object>>();

        private void StackPush(object sender, Stack<Func<object>> stack)
        {
            RichTextBoxMED textBox = (RichTextBoxMED)sender;
            var tBT = textBox.Text(textBox.Text, textBox.SelectionStart);
            stack.Push(tBT);
        }

        public new void Undo()
        {
            if (undoStack.Count > 0)
            {
                StackPush(this, redoStack);
                undoStack.Pop()();
            }
        }
        public new void Redo()
        {
            if (redoStack.Count > 0)
            {
                StackPush(this, undoStack);
                redoStack.Pop()();
            }
        }
    }
    public static class Extensions
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
