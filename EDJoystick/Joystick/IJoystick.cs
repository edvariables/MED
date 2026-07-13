// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using DevDecoder.HIDDevices;
using DevDecoder.HIDDevices.Controllers;
using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public class ValueChangedData(string usage, string controlKey, ValueChangedDelegate valueChangedDelegate, long refresh_delay)
        {
            public string Usage = usage;
            public string ControlKey = controlKey;
            public ValueChangedDelegate ValueChangedDelegate = valueChangedDelegate;
            public long Refresh_delay = refresh_delay;

            public long Refresh_ticks;
            public Task Refresh_delayed_task;
        }

        public delegate void ValueChangedDelegate(string control, object new_value);
        private Dictionary<string, List<ValueChangedData>> ValueChangedUsagesDelegates = new();
        //public ValueChangedDelegate ValueChanged;

        public delegate void ButtonPressedDelegate(string control, bool pressed);
        public ButtonPressedDelegate ButtonPressed;

        public delegate void IsConnectedChangedDelegate(bool connected);
        public IsConnectedChangedDelegate IsConnectedChanged;

        /**
         * Get Usages[usage, ALL_USAGES] ValueChanged
         * 
         */
        private Dictionary<string, List<ValueChangedData>> GetValueChangedDelegates(string usage = Consts.ALL_USAGES){
            var dics = new Dictionary<string, List<ValueChangedData>>();
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
        public ValueChangedDelegate AddValueChangedDelegate(long hwnd, ValueChangedDelegate _delegate, string usage = Consts.ALL_USAGES, long delayed_ms = 0)
        {
            if (HwndsChangedValues.ContainsKey(hwnd))
            {
                Console.WriteLine($"PrivateValueChangedDelegate a déjà fourni un délégué/event pour {hwnd}. Il va être remplacé.");
                HwndsChangedValues.Remove(hwnd);
                //throw new Exception($"PrivateValueChangedDelegate a déjà fourni un délégué pour {hwnd}.");
            }
            HwndsChangedValues.Add(hwnd, new Dictionary<string, object>());

            if (! ValueChangedUsagesDelegates.ContainsKey(usage))
                ValueChangedUsagesDelegates.Add(usage, new List<ValueChangedData>());
            //ValueChangedUsagesDelegates[usage].ValueChangedDelegate += _delegate;
            //ValueChangedUsagesDelegates[usage].Refresh_delay = delayed_ms * TimeSpan.TicksPerMillisecond;
            var valueChangedData = new ValueChangedData(usage, usage, new(_delegate), delayed_ms * TimeSpan.TicksPerMillisecond);
            ValueChangedUsagesDelegates[usage].Add(valueChangedData);
            
            return valueChangedData.ValueChangedDelegate;
        }
        /**
         * AddValueChangedDelegate
         * */
        public ValueChangedDelegate AddValueChangedDelegate(long hwnd, ValueChangedDelegate _delegate, JoystickUsage usage) => AddValueChangedDelegate(hwnd, _delegate, usage.ToString());

        /**
         * DelayRefreshValuesChanged
         */
        private bool DelayRefreshValuesChanged(ValueChangedData data)
        {
            if (data.Refresh_delay > 0)
                if ((data.Refresh_ticks + data.Refresh_delay) > DateTime.Now.Ticks)
                {

                    if (data.Refresh_delayed_task == null
                        || data.Refresh_delayed_task.IsCompleted)
                    //|| !(_refresh_delayed_task.ThreadState == ThreadState.Running
                    //|| (_refresh_delayed_task != null && _refresh_delayed_task.ThreadState == ThreadState.Unstarted)))  //VS / .net bugg : _refresh_delayed_thread may be null between the two tests
                    {
                        data.Refresh_delayed_task = new(() =>
                        {
                            var delay = (data.Refresh_ticks + data.Refresh_delay) - DateTime.Now.Ticks;
                            if (delay < 0)
                                delay = 0L;
                            else if (delay > data.Refresh_delay)
                                delay = data.Refresh_delay;
                            delay /= TimeSpan.TicksPerMillisecond;
                            Logger.LogInformation("Delayed {0} ms", delay.ToString("# ##0"));
                            if (delay > 0L) Thread.Sleep((int)delay);
                            try
                            {
                                RaiseValuesChanged(data);
                            }
                            catch (Exception ex)
                            {
                                Logger.LogError("Slow Thread error : {0}", ex.ToString());
                            }
                            finally
                            {
                                data.Refresh_delayed_task = null;
                            }
                        });
                        try
                        {
                            data.Refresh_delayed_task.Start();
                        }
                        catch (Exception ex)
                        {
                            data.Refresh_delayed_task = null;
                            Logger.LogError("_refresh_delayed_thread.Start error : {0}", ex.ToString());
                        }
                    }
                    return true;
                }
            return false;
        }
        private void RaiseValuesChanged(List<ValueChangedData> listData, bool invokeMainThread = true, object value = null)
        {
            foreach (var data in listData)
                RaiseValuesChanged(data, invokeMainThread, value);
        }
        private void RaiseValuesChanged(ValueChangedData data, bool invokeMainThread = true, object value = null)
        {
            if (invokeMainThread
             || ! DelayRefreshValuesChanged(data))
            {
                if(value == null)
                {
                    //TODO controlKey(data.Usage)
                    if(ControlsValue.ContainsKey(data.Usage))
                        value = ControlsValue[data.Usage];
                }

                data.Refresh_ticks = DateTime.Now.Ticks;

                if(invokeMainThread)
                    FormHandler.Invoke( data.ValueChangedDelegate, data.Usage, value);
                else
                    data.ValueChangedDelegate(data.Usage, value);
            }
        }

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
                    foreach (var data in GetValueChangedDelegates(eventUsage))
                    {
                        RaiseValuesChanged(data.Value, false, value);
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
