using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MED
{
    internal abstract class IKeyboardHook : IDisposable
    {

        protected Form FormHandler;
            
        public delegate void KeyChangedDelegate(Keys key, bool pressed);
        public KeyChangedDelegate? KeyChanged;

        protected Dictionary<Keys, bool> _keys_state;

        /**
         * 
         * 
         * */
        ~IKeyboardHook()
        {
            Dispose();
        }

        /**
         * 
         * 
         * */
        public void Dispose()
        {
            StopHook();
        }

        /**
         * 
         * 
         * */
        public virtual void StartHook(Form formHandler)
        {
            FormHandler = formHandler;

            _keys_state = new();
        }

        /**
         * 
         * 
         * */
        public virtual void StopHook()
        {
            if (FormHandler != null)
            {
                FormHandler = null;
            }
        }

        /**
         * 
         * 
         * */
        public bool IsKeyDown(Keys key)
        {
            if (_keys_state.ContainsKey(key))
                return _keys_state[key];
            return false;
        }
    }
}
