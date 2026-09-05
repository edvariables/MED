using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MED.GameController
{
    public class KeyboardController : GameController
    {
        public KeyboardController(string name = "Keyboard", Performance? performance = null, Control? invokeHandler = null, IConsumer? consumer = null, bool isAsynchrone = true)
            : base(name, performance, invokeHandler, consumer, isAsynchrone)
        {
            ProcessIconDefault = "Button";
        }

        public override object? GetControllerPropertyValue(string controllerProperty)
        {
            if (Enum.TryParse(typeof(Keys), controllerProperty, out object? key))
                return IsKeyDown((Keys)key);
            return false;
        }

        public override void SetControllerPropertyValue(string controllerProperty, object? value)
        {
            if (Enum.TryParse(typeof(Keys), controllerProperty, out object? key))
                if (bool.TryParse((string?)value, out bool bValue))
                    KeysState[(Keys)key] = bValue;
        }

        [Browsable(true)]
        [Category("Controller")]
        public Dictionary<Keys, bool> KeysState { get; set; } = [];

        [Browsable(true)]
        [Category("Controller")]
        public Dictionary<String, List<IProcess>> KeysConsumers
        {
            get
            {
                Dictionary<String, List<IProcess>> dic = [];

                foreach (var property in KeysConsumed)
                    dic.Add(property, GetConsumers(property).Keys.ToList());
                return dic;
            }
        }

        [Browsable(true)]
        [Category("Controller")]
        public List<String> KeysConsumed { get => GetProperties(""); }
        /**
         * 
         * 
         * */
        public virtual bool IsKeyDown(Keys key)
        {
            if (KeysState.TryGetValue(key, out bool value))
                return value;
            return false;
        }

        private void formHandler_KeyDown(object? sender, System.Windows.Forms.KeyEventArgs e) => HookCallback(e, true);
        private void formHandler_KeyUp(object? sender, System.Windows.Forms.KeyEventArgs e) => HookCallback(e, false);

        /**
         * 
         * 
         * */
        private void HookCallback(System.Windows.Forms.KeyEventArgs keyEventArgs, bool pressed)
        {
            if (OnGameControllerChanged != null)
            {
                Performance?.Debug($"HookCallback {keyEventArgs.KeyCode} + {keyEventArgs.Modifiers} : {pressed}");
                Performance?.Logger?.InvokeBufferChanged(this, keyEventArgs);
                Keys key = keyEventArgs.KeyCode;
                if (!KeysState.TryAdd(key, pressed))
                    KeysState[key] = pressed;
                string keys = key.ToString();
                if (key != Keys.ControlKey
                    && key != Keys.Menu
                    && key != Keys.ShiftKey)
                {
                    if (keyEventArgs.Alt)
                        keys = "Alt+" + key.ToString();
                    if (keyEventArgs.Shift)
                        keys = "Shift+" + key.ToString();
                    if (keyEventArgs.Control)
                        keys = "Control+" + key.ToString();
                }
                InvokeControllerChanged(this, new(keys, pressed));
            }
        }
        protected bool _saved_formHandler_KeyPreview;
        /**
         * 
         * 
         * */
        private void StartHook(Control handler)
        {
            if (handler is Form formHandler)
            {
                _saved_formHandler_KeyPreview = formHandler.KeyPreview;
                formHandler.KeyPreview = true;
            }
            KeysState = new();
            SetHook(handler);
        }

        /**
         * 
         * 
         * */
        private void SetHook(Control handler)
        {
            handler.KeyDown += formHandler_KeyDown;
            handler.KeyUp += formHandler_KeyUp;
        }

        /**
         * 
         * 
         * */
        private void StopHook(Control? handler)
        {
            if (handler != null)
            {
                if (handler is Form formHandler)
                    formHandler.KeyPreview = _saved_formHandler_KeyPreview;

                handler.KeyDown -= formHandler_KeyDown;
                handler.KeyUp -= formHandler_KeyUp;
            }
        }

        #region Process
        public override void Start()
        {
            if (!Enabled)
                return;

            if (InvokeHandler == null)
            {
                Performance?.Debug("InvokeHandler is null");
                return;
            }
            StartHook(InvokeHandler);

            base.Start();

            ProcessState = ThreadState.Running;
        }
        public override void Stop()
        {
            StopHook(InvokeHandler);

            base.Stop();
        }
        #endregion
    }
}
