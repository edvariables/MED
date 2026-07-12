using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MED.EDJoystick
{
    public class KeyboardFormEvents : IKeyboardHook
    {
        protected bool _saved_formHandler_KeyPreview;
        /**
         * 
         * 
         * */
        public override void StartHook(Form formHandler)
        {
            FormHandler = formHandler;
            _saved_formHandler_KeyPreview = formHandler.KeyPreview;
            formHandler.KeyPreview = true;
            _keys_state = new();
            SetHook();
        }

        /**
         * 
         * 
         * */
        private void SetHook()
        {
            FormHandler.KeyDown += FormHandler_KeyDown;
            FormHandler.KeyUp += FormHandler_KeyUp;
        }

        /**
         * 
         * 
         * */
        public override void StopHook()
        {
            if (FormHandler != null)
            {
                FormHandler.KeyPreview = _saved_formHandler_KeyPreview;

                FormHandler.KeyDown -= FormHandler_KeyDown;
                FormHandler.KeyUp -= FormHandler_KeyUp;

                FormHandler = null;
            }
        }

        private void FormHandler_KeyDown(object? sender, System.Windows.Forms.KeyEventArgs e) => HookCallback(e.KeyCode, true);
        private void FormHandler_KeyUp(object? sender, System.Windows.Forms.KeyEventArgs e) => HookCallback(e.KeyCode, false);

        /**
         * 
         * 
         * */
        private void HookCallback(Keys key, bool pressed)
        {
            if (KeyChanged != null)
            {
                if (!_keys_state.ContainsKey(key))
                    _keys_state.Add(key, pressed);
                else
                    _keys_state[key] = pressed;
                KeyChanged(key, pressed);
            }
        }
    }
}
