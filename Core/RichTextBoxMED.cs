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

        public const int UndoStackMaxLength = 64;

        Stack<Func<object>> undoStack = new Stack<Func<object>>();
        Stack<Func<object>> redoStack = new Stack<Func<object>>();

        /**
         * Stack RichTextBoxMED
         * */
        private void StackPush(RichTextBoxMED textBox, Stack<Func<object>> stack)
        {
            var tBT = textBox.Text(textBox.Text, textBox.SelectionStart);
            stack.Push(tBT);
            if (stack.Count > UndoStackMaxLength)
            {
                var newStack = new Stack<Func<object>>(stack.SkipLast(stack.Count - UndoStackMaxLength).Reverse());
                if (undoStack.Equals(stack))
                    undoStack = newStack;
                else if (redoStack.Equals(stack))
                    redoStack = newStack;
            }
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
