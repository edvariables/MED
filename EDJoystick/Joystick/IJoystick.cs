// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using DevDecoder.HIDDevices;
using System.Collections;

namespace MED.EDJoystick
{
    public abstract class IJoystick
    {
        /**
         * Constructor
         * 
         */
        public IJoystick(Form formHandler, ILogger<Devices> _logger)
        {
            FormHandler = formHandler;
            Logger = _logger;

        }
        protected ILogger<Devices> Logger;
        public Form FormHandler {get; internal set;}
        public abstract bool Connect();
        public abstract void Disconnect();
        private bool _IsConnected = false;

        /**
         * IsConnected
         * 
         */
        public bool IsConnected { 
            get { 
                return _IsConnected;
            }
            protected set
            {
                if (_IsConnected == value) return;
                _IsConnected = value;
                if (IsConnectedChanged != null)
                    IsConnectedChanged(_IsConnected);
            } 
        }

        public readonly Dictionary<string, object> Controls = new();

        public readonly Dictionary<string, string> ControlsName = new();

        public readonly Dictionary<string, object> ControlsValue = new();

        public readonly Dictionary<string, string> ButtonControls = new();

        private readonly Dictionary<long, Dictionary<string, object>> HwndsChangedValues = new();

        /**
         * ClearControls
         * 
         */
        public virtual void ClearControls()
        {
            Controls.Clear();
            ControlsValue.Clear();
            ControlsName.Clear();
            ButtonControls.Clear();
        }

        public bool IsButton(string control)
        {
            return ButtonControls.ContainsKey(control);
        }

        public virtual string ControlUsage(string control_key)
        {
            return ControlUsage(Controls[control_key]);
        }
        public virtual string ControlUsage(object control)
        {
            if (control is string)
                return (string)control;
            throw new NotImplementedException();
        }

        public virtual string ControlKey(object control)
        {
            return control.GetHashCode().ToString();
        }

        public virtual string ControlName(string control_key)
        {
            return ControlName(Controls[control_key]);
        }
        public virtual string ControlName(object control)
        {
            return ControlsName[ControlKey(control)];
        }
        public abstract List<JoystickUsage> Usages { get; }

        /***
         * Delegates
         * 
         */

        public delegate void ValueChangedDelegate(string control, object new_value);
        private Dictionary<string, ValueChangedDelegate> ValueChangedUsagesDelegates = new();
        //public ValueChangedDelegate ValueChanged;

        public delegate void ButtonPressedDelegate(string control, bool pressed);
        public ButtonPressedDelegate ButtonPressed;

        public delegate void IsConnectedChangedDelegate(bool connected);
        public IsConnectedChangedDelegate IsConnectedChanged;

        /**
         * Get Usages[usage, ALL_USAGES] ValueChanged
         * 
         */
        private Dictionary<string, ValueChangedDelegate> GetValueChangedDelegates(string usage = Consts.ALL_USAGES){
            var dics = new Dictionary<string, ValueChangedDelegate>();
            if (ValueChangedUsagesDelegates.ContainsKey(usage))
                dics.Add(usage, ValueChangedUsagesDelegates[usage]);
            if (usage != Consts.ALL_USAGES
                && ValueChangedUsagesDelegates.ContainsKey(Consts.ALL_USAGES))
                dics.Add(Consts.ALL_USAGES, ValueChangedUsagesDelegates[Consts.ALL_USAGES]);
            return dics;
        }

        /**
         * AddValueChangedDelegate
         * */
        public ValueChangedDelegate AddValueChangedDelegate(long hwnd, ValueChangedDelegate _delegate, string usage = Consts.ALL_USAGES)
        {
            if (HwndsChangedValues.ContainsKey(hwnd))
            {
                Console.WriteLine($"PrivateValueChangedDelegate a déjà fourni un délégué/event pour {hwnd}. Il va être remplacé.");
                HwndsChangedValues.Remove(hwnd);
                //throw new Exception($"PrivateValueChangedDelegate a déjà fourni un délégué pour {hwnd}.");
            }
            HwndsChangedValues.Add(hwnd, new Dictionary<string, object>());

            if ( ValueChangedUsagesDelegates.ContainsKey(usage))
                ValueChangedUsagesDelegates[usage] += _delegate;
            else
                ValueChangedUsagesDelegates.Add(usage, new(_delegate));
            return ValueChangedUsagesDelegates[usage];
        }
        /**
         * AddValueChangedDelegate
         * */
        public ValueChangedDelegate AddValueChangedDelegate(long hwnd, ValueChangedDelegate _delegate, JoystickUsage usage) => AddValueChangedDelegate(hwnd, _delegate, usage.ToString());


        //////////////////////

        /**
         *  Set Control Value
         * */
        protected void SetControlValue(string controlKey, object value)
        {
            if (!ControlsValue.ContainsKey(controlKey))
                return;
            bool changedValue = ControlsValue[controlKey] == null ? value != null
                : ! ControlsValue[controlKey].Equals( value );

            if (changedValue)
            {
                ControlsValue[controlKey] = value;

                if (changedValue)
                {
                    //Cache for each Hwnd
                    foreach (KeyValuePair<long, Dictionary<string, object>> kvp in HwndsChangedValues)
                    {

                        if (!kvp.Value.ContainsKey(controlKey))
                            kvp.Value.Add(controlKey, value);
                        else
                            kvp.Value[controlKey] = value;
                    }
                    //Raise event
                    string eventUsage = ControlsName[controlKey];
                    foreach (var valueChanged in GetValueChangedDelegates(eventUsage) )
                    {
                        //if (valueChanged.Value != null)
                        //{
                            valueChanged.Value(controlKey, value);
                        //}
                    }
                    if (ButtonPressed != null && ButtonControls.ContainsKey(controlKey) && value != null && (value is bool))
                        ButtonPressed(controlKey, (bool)value);
                }
            }
        }
        /**
         * Get Control Value
         * 
         */
        protected object GetControlValue(string controlKey, object default_value = null)
        {
            if (!ControlsValue.ContainsKey(controlKey))
                return default_value;
            return ControlsValue[controlKey]?? default_value;
        }

        /***
         * Get Hwnd ValuesChanged
         * 
         */
        public Dictionary<string, object> GetValuesChanged(long hwnd)
        {
            if(HwndsChangedValues.ContainsKey(hwnd))
                return HwndsChangedValues[hwnd];
            return null;
        }
    }
}
