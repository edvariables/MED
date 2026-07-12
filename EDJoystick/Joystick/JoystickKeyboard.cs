// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using DevDecoder.HIDDevices;
using DevDecoder.HIDDevices.Controllers;
using DynamicData;
using Microsoft.Extensions.Logging;

namespace MED.EDJoystick
{
    public class KBControl
    {
        public JoystickUsage Property { get; private set; }
        public JoystickUsage Usage { get; private set; }
        public Keys[] Keys { get; private set; }
        public string ControlKey { get; private set; }
        public Type ValueType { get; private set; }

        public KBControl(string controlKey, JoystickUsage usage, Keys[] keys)
        {
            ControlKey = controlKey;
            Usage = usage;
            Keys = keys;

            switch (usage)
            {
                case JoystickUsage.X:
                case JoystickUsage.Y:
                case JoystickUsage.Z:
                    ValueType = 0L.GetType();
                    break;
                case JoystickUsage.Hat:
                    ValueType = "".GetType();
                    break;
                default:
                    ValueType = true.GetType();
                    IsPushButton = IsBoolean = true;
                    break;
            }
        }

        public bool IsPushButton { get; internal set; }
        public bool IsBoolean { get; internal set; }
    }


    public class JoystickKeyboard : IJoystick
    {
        
        private Dictionary<JoystickUsage, Keys[]> _mapping = new();
        private Dictionary<Keys, KBControl> _keys_controls = new();

        private IKeyboardHook _KeyboardHook;

        public JoystickKeyboard(Form formHandler, ILogger<Devices> _logger): base(formHandler, _logger)
        {
            Init_Mapping();
        }

        private void Init_Mapping()
        {
            _mapping.Add(JoystickUsage.Y, [Keys.Z, Keys.S]);
            _mapping.Add(JoystickUsage.X, [Keys.D, Keys.Q]);
            _mapping.Add(JoystickUsage.Z, [Keys.Right,Keys.Left]);
            _mapping.Add(JoystickUsage.Start, [Keys.Enter]);
            _mapping.Add(JoystickUsage.LeftBumper, [Keys.Space]);

            //Start,
            //Select,
            //LeftTrigger,
            //RightTrigger,
            //AButton,
            //BButton,
            //XButton,
            //YButton,
            //LeftBumper,
            //RightBumper,
            //LeftStick,
            //RightStick,
            //Hat

        }

        public override void ClearControls()
        {
            base.ClearControls();
            _keys_controls.Clear();
        }

        /**
         * 
         * */
        public override bool Connect()
        {
            SetControls(_mapping );
            _KeyboardHook = new KeyboardFormEvents();
            _KeyboardHook.KeyChanged += KeyChange_Event;
            _KeyboardHook.StartHook(FormHandler);
            IsConnected = true;
            return true;
        }

        private void SetControls(Dictionary<JoystickUsage, Keys[]> mapping)
        {
            ClearControls();
            foreach (var (usage, keys) in mapping)
            {
                string ctrlKey = usage.ToString();
                KBControl control = new(ctrlKey, usage, keys);
                Controls.Add(ctrlKey, control);
                ControlsName.Add(ctrlKey, ctrlKey);
                ControlsValue.Add(ctrlKey, null);
                if (control.IsPushButton || control.IsBoolean)
                    ButtonControls.Add(ctrlKey, ctrlKey);
                foreach (var key in keys)
                    _keys_controls.Add(key, control);

                Logger.LogInformation(
                    // ReSharper disable once SuspiciousTypeConversion.Global
                    $"  {ctrlKey} => {string.Join(", ", control.Keys.Select(key => key.ToString()))}");
            }

        }

        public override string ControlKey(object control)
        {
            return ControlUsage(control);
        }

        public override string ControlUsage(object control)
        {
            return ((KBControl)control).ControlKey;
        }

        public override void Disconnect()
        {
            if (_KeyboardHook != null)
            {
                //FormHandler.PreviewKeyDown -= FormHandler_PreviewKeyDown;
                //FormHandler.KeyDown -= FormHandler_KeyDown;
                //FormHandler.KeyUp -= FormHandler_KeyUp;
                //FormHandler.KeyPreview = false;
                _KeyboardHook.StopHook();
                _KeyboardHook = null;
            }
            IsConnected = false;
        }

        //private void FormHandler_PreviewKeyDown(object? sender, PreviewKeyDownEventArgs e) => KeyChange_Event(e.KeyCode, true);
        //private void FormHandler_KeyDown(object? sender, System.Windows.Forms.KeyEventArgs e) => KeyChange_Event(e.KeyCode, true);
        //private void FormHandler_KeyUp(object? sender, System.Windows.Forms.KeyEventArgs e) => KeyChange_Event(e.KeyCode, false);

        /**
         * 
         * */
        private void KeyChange_Event(Keys key, bool pressed)
        {
            if (!_keys_controls.ContainsKey(key)) return;
            var control = _keys_controls[key];
            var nKey = control.Keys.IndexOf(key);
            if (((KBControl)control).IsBoolean)
                SetControlValue(control.ControlKey, pressed);
            else if (nKey == 0)
            {
                var value = pressed ? 1L : 0L;

                if (!pressed
                 && control.Keys.Length > 1
                 && _KeyboardHook.IsKeyDown(control.Keys[1]))
                {
                    value = -1L;
                }
                SetControlValue(control.ControlKey, value);
            }
            else if (nKey > 0)
            {
                var value = pressed ? -1L : 0L;

                if( ! pressed
                 && control.Keys.Length > 1
                 && _KeyboardHook.IsKeyDown(control.Keys[0]))
                {
                    value = 1L;
                }
                SetControlValue(control.ControlKey, value);
            }
        }
    }
}
