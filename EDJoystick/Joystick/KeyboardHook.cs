using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using static MED.EDJoystick.KeyboardHook;

namespace MED.EDJoystick
{
    public class KeyboardHook : IKeyboardHook
    {
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

        protected Form formHandler;

        /**
         * 
         * 
         * */
        public override void StartHook(Form formHandler)
        {
            base.StartHook(formHandler);
            _hookId = SetHook(formHandler, HookCallback);
        }

        /**
         * 
         * 
         * */
        private IntPtr SetHook(Form formHandler, LowLevelKeyboardProc proc)
        {
            using var curProcess = System.Diagnostics.Process.GetCurrentProcess();
            using var curModule = curProcess.MainModule;
            var handle = GetModuleHandle(curModule.ModuleName);
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

                base.StopHook();
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
                object oKey;
                if (Enum.TryParse(typeof(Keys), iKey.ToString(), out oKey))
                {
                    Keys key = (Keys)oKey;
                    if (KeyChanged != null)
                    {
                        if (!_keys_state.ContainsKey(key))
                            _keys_state.Add(key, pressed);
                        else
                            _keys_state[key] = pressed;
                        KeyChanged(key, pressed);
                        //return (IntPtr)1; // Prevent further processing
                    }
                }
                else
                    Console.WriteLine("Touche inconnue : {0}", iKey);
            }

            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

    }
}
