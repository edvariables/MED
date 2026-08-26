using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MED.GameController
{
    /**
     * TODO Error : Raises an app crash
     * */
    public class KeyboardHookController : GameController
    {
        public KeyboardHookController(string name = "Keyboard", Performance? performance = null, Control? invokeHandler = null, IConsumer? consumer = null, bool isAsynchrone = true)
            : base(name, performance, invokeHandler, consumer, isAsynchrone)
        {
            ProcessIconDefault = "Button";
        }

        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll")]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;

        internal delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        protected IntPtr _hookId = IntPtr.Zero;

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
                    _keys_state[(Keys)key] = bValue;
        }

        protected Dictionary<Keys, bool> _keys_state = [];
        /**
         * 
         * 
         * */
        public virtual bool IsKeyDown(Keys key)
        {
            if (_keys_state.TryGetValue(key, out bool value))
                return value;
            return false;
        }

        /**
         * 
         * 
         * */
        private IntPtr SetHook(Control handler, LowLevelKeyboardProc proc)
        {
            nint handle;
            if (handler is Form formHandler)
            {
                using var curProcess = System.Diagnostics.Process.GetCurrentProcess();
                using var curModule = curProcess.MainModule;
                if (curModule == null)
                    return IntPtr.Zero;
                handle = GetModuleHandle(curModule.ModuleName);
            }
            else
                handle = handler.Handle;
            return SetWindowsHookEx(WH_KEYBOARD_LL, proc, handle, 0);
        }

        /**
         * 
         * 
         * */
        public void StopHook()
        {
            if (_hookId != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookId);
                _hookId = IntPtr.Zero;
            }
        }

        /**
         * 
         * 
         * */
        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            bool pressed = wParam == (IntPtr)WM_KEYDOWN;
            if (nCode >= 0 && (pressed || wParam == (IntPtr)WM_KEYUP))
            {
                int vkCode = Marshal.ReadInt32(lParam);
                Key iKey = KeyInterop.KeyFromVirtualKey(vkCode);
                object? oKey;
                if (Enum.TryParse(typeof(Keys), iKey.ToString(), out oKey) && oKey != null)
                {
                    Keys key = (Keys)oKey;
                    if (base.OnControllerChanged != null)
                    {
                        if (!_keys_state.TryAdd(key, pressed))
                            _keys_state[key] = pressed;

                        InvokeControllerChanged(this, new(key.ToString(), pressed));

                        //return (IntPtr)1; // Prevent further processing
                    }
                }
                else
                    Performance?.Debug($"Touche inconnue : {iKey}");
            }

            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        #region Process
        public override void Start()
        {
            if (InvokeHandler == null)
            {
                Performance?.Debug("InvokeHandler is null");
                return;
            }
            _keys_state = new();

            if (SetHook(InvokeHandler, HookCallback).Equals(IntPtr.Zero))
                return;

            base.Start();

            ProcessState = ThreadState.Running;
        }
        public override void Stop()
        {
            StopHook();

            base.Stop();
        }
        #endregion
    }
}
