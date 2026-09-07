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
            if (e.KeyCode == Keys.ControlKey
                && ((ModifierKeys & Keys.Control) == Keys.Control))
                return;
            if (e.KeyCode == Keys.ShiftKey
                && ((ModifierKeys & Keys.Shift) == Keys.Shift))
                return;
            if (e.KeyCode == Keys.Menu
                && ((ModifierKeys & Keys.Alt) == Keys.Alt))
                return;
            if ((e.KeyCode == Keys.C)
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
