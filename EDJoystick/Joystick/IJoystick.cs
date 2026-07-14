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
    using static Consts;
    using static MED.EDJoystick.IJoystick;

    public class ControlData(object control, string name, string usage, object value, bool isButton)
    {
        public readonly object Control = control;
        public readonly string Name = name;
        public readonly string Usage = usage;
        public object Value = value;
        public readonly bool IsButton = isButton;
    }
    public class ValueChangedData(string usage, string controlKey, ValueChangedDelegate valueChangedDelegate, long refresh_delay)
    {
        public string Usage = usage;
        public string ControlKey = controlKey;
        public ValueChangedDelegate ValueChangedDelegate = valueChangedDelegate;
        public long Refresh_delay = refresh_delay;

        public long Refresh_ticks;
        public Task? Refresh_delayed_task;
    }

    public abstract class IJoystick(Form formHandler, ILogger<Devices> _logger)
    {
        protected ILogger<Devices> Logger = _logger;
        public Form FormHandler { get; internal set; } = formHandler;
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

        public readonly Dictionary<string, ControlData> Controls = [];

        //public readonly Dictionary<string, string> ControlsName = [];

        //public readonly Dictionary<string, object> ControlsValue = [];

        public readonly Dictionary<string, string> ButtonControls = [];

        private readonly Dictionary<long, Dictionary<string, object>> HwndsChangedValues = [];

        /**
         * ClearControls
         * 
         */
        public virtual void ClearControls()
        {
            Controls.Clear();
            ButtonControls.Clear();
        }

        public bool IsButton(string control)
        {
            return Controls.TryGetValue(control, out ControlData? value) ? value.IsButton : false;
        }

        public virtual string ControlUsage(string control)
        {
            return Controls.TryGetValue(control, out ControlData? value) ? value.Usage : "";
        }

        public virtual object ControlValue(string control, object default_value = null)
        {
            return Controls.TryGetValue(control, out ControlData? value) ? value.Value : default_value;
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

        public virtual string ControlName(string control)
        {
            return Controls.TryGetValue(control, out ControlData? value) ? value.Name : "";
        }
        public virtual string ControlName(object control)
        {
            return Controls.TryGetValue(ControlKey(control), out ControlData? value) ? value.Name : "";
        }
        public abstract List<JoystickUsage> Usages { get; }

        /***
         * Delegates
         * 
         */

        public delegate void ValueChangedDelegate(ValueChangedData valueChangedData);
        private Dictionary<string, List<ValueChangedData>> ValueChangedUsagesDelegates = [];
        //public ValueChangedDelegate ValueChanged;

        public delegate void ButtonPressedDelegate(string control, bool pressed);
        public ButtonPressedDelegate? ButtonPressed;

        public delegate void IsConnectedChangedDelegate(bool connected);
        public IsConnectedChangedDelegate? IsConnectedChanged;

        /**
         * Get Usages[usage, ALL_USAGES] ValueChanged
         * 
         */
        private Dictionary<string, List<ValueChangedData>> GetValueChangedDelegates(string usage = ALL_USAGES){
            var dics = new Dictionary<string, List<ValueChangedData>>();
            if (ValueChangedUsagesDelegates.ContainsKey(usage))
                dics.Add(usage, ValueChangedUsagesDelegates[usage]);
            if (usage != ALL_USAGES
                && ValueChangedUsagesDelegates.ContainsKey(ALL_USAGES))
                dics.Add(ALL_USAGES, ValueChangedUsagesDelegates[ALL_USAGES]);
            return dics;
        }

        /**
         * AddValueChangedDelegate
         * */
        public ValueChangedDelegate AddValueChangedDelegate(long hwnd, ValueChangedDelegate _delegate, string usage = ALL_USAGES, long delayed_ms = 0)
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
        private bool DelayRaiseValuesChanged(ValueChangedData data)
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
        private void RaiseValuesChanged(ValueChangedData data, bool noDelay = true, object value = null)
        {
            if (noDelay
             || ! DelayRaiseValuesChanged(data))
            {
                data.Refresh_ticks = DateTime.Now.Ticks;

                FormHandler.Invoke( data.ValueChangedDelegate, data);
            }
        }

        //////////////////////

        /**
         *  Set Control Value
         * */
        protected void SetControlValue(string controlKey, object value)
        {
            if (!Controls.ContainsKey(controlKey))
                return;
            ControlData controlData = Controls[controlKey];
            object oldValue = controlData.Value;
            bool changedValue = oldValue == null ? value != null : ! oldValue.Equals( value );

            if (changedValue)
            {
                controlData.Value = value;

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
                    string eventUsage = controlData.Usage;
                    foreach (var data in GetValueChangedDelegates(eventUsage))
                    {
                        RaiseValuesChanged(data.Value, false, value);
                    }
                    if (ButtonPressed != null && controlData.IsButton && value != null && (value is bool))
                        ButtonPressed(controlKey, (bool)value);
                }
            }
        }

        /***
         * Get Hwnd ValuesChanged cached since last Clear
         * 
         */
        public Dictionary<string, object> GetValuesChanged(long hwnd)
        {
            if(HwndsChangedValues.TryGetValue(hwnd, out Dictionary<string, object>? value))
                return value;
            return null;
        }
    }
}
