// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Text;
using System.Threading;
using DevDecoder.HIDDevices;
using DevDecoder.HIDDevices.Controllers;
using HidSharp;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;
using MED.EDJoystick;

namespace MED
{
    public partial class FMain : Form
    {

        public FMain()
        {
            InitializeComponent();
        }

        public FMain(IJoystick joystick):this()
        {
            Joystick = joystick;
            _refresh_delay = 200 * TimeSpan.TicksPerMillisecond;
            if (joystick != null)
                chkRun.Checked = true;
        }

        private long _refresh_ticks = 0L;
        private readonly long _refresh_delay = 0L;
        //private Thread _refresh_delayed_thread;
        private Task _refresh_delayed_task;

        private void chkRun_CheckedChanged(object sender, EventArgs e)
        {
            if (Joystick == null && !chkRun.Checked)
                return;
            if (chkRun.Checked)
            {
                chkRun.Text = "En cours...";
                RunTask(Joystick);
            }
            else
            {
                Joystick?.Disconnect();
                chkRun.Text = "Activer";
            }

        }

        private ILogger<Devices> _RTBLogger;
        public ILogger<Devices> RTBLogger
        {
            get
            {
                if (_RTBLogger == null)
                    _RTBLogger = new RichTextBoxLogger<Devices>(LogLevel.Information, null, txtLog);
                return _RTBLogger;
            }
        }

        private IJoystick Joystick;
        private void RunTask(IJoystick joystick = null)
        {
            if(joystick == null)
                Joystick = new JoystickKeyboard(this, RTBLogger);
            else
                Joystick = joystick;
            Joystick.IsConnectedChanged += Joystick_IsConnectedChanged;
            Joystick.AddValueChangedDelegate(this.Handle, Joystick_ValueChanged);
            //Joystick.ButtonPressed += Joystick_ButtonPressed;
            if (joystick == null)
                Joystick.Connect();
        }

        private void Joystick_IsConnectedChanged(bool connected)
        {
            if (connected)
                Joystick_ValueChanged(null, null);
            else
                Joystick = null;
        }


        private void Joystick_ValueChanged(string control, object new_value)
        {
            RefreshValuesChanged();
        }

        private void RefreshValuesChanged( bool invokeMainThread=true)
        {
            if (this.IsDisposed || this.Disposing)
                return;
            if(invokeMainThread)
                lvwJoystickControls.Invoke(lvwJoystickControls.Invalidate);
            else if(!DelayRefreshValuesChanged())
                LvwJoystickControls_Refresh();
        }

        private void Joystick_ButtonPressed(string control, bool pressed)
        {
            Joystick_ValueChanged(control, pressed);
        }

        private void LvwJoystickControls_Invalidated(object sender, InvalidateEventArgs e)
        {
            RefreshValuesChanged(false);
        }

        private bool DelayRefreshValuesChanged()
        {
            if (_refresh_delay > 0)
                if ((_refresh_ticks + _refresh_delay) > DateTime.Now.Ticks)
                {

                    if (_refresh_delayed_task == null
                        || _refresh_delayed_task.IsCompleted) 
                        //|| !(_refresh_delayed_task.ThreadState == ThreadState.Running
                        //|| (_refresh_delayed_task != null && _refresh_delayed_task.ThreadState == ThreadState.Unstarted)))  //VS / .net bugg : _refresh_delayed_thread may be null between the two tests
                    {
                        _refresh_delayed_task = new(() =>
                        {
                            var delay = (_refresh_ticks + _refresh_delay) - DateTime.Now.Ticks;
                            if (delay < 0)
                                delay = 0L;
                            else if (delay > _refresh_delay)
                                delay = _refresh_delay;
                            delay /= TimeSpan.TicksPerMillisecond;
                            RTBLogger.LogInformation("Delayed {0} ms", delay.ToString("# ##0"));
                            if (delay > 0L) Thread.Sleep((int)delay);
                            try
                            {
                                RefreshValuesChanged();
                            }
                            catch (Exception ex)
                            {
                                RTBLogger.LogError("Slow Thread error : {0}", ex.ToString());
                            }
                            finally { 
                                _refresh_delayed_task = null;
                            }
                        });
                        try
                        {
                            _refresh_delayed_task.Start();
                        }
                        catch (Exception ex)
                        {
                            _refresh_delayed_task = null;
                            RTBLogger.LogError("_refresh_delayed_thread.Start error : {0}", ex.ToString());
                        }
                    }
                    return true;
                }
            return false;
        }

        private void LvwJoystickControls_Refresh()
        {
            _refresh_ticks = DateTime.Now.Ticks;

            if (Joystick == null)
                return;

            var controlsChangedValue = Joystick.GetValuesChanged(this.Handle);
            if (controlsChangedValue == null || controlsChangedValue.Count == 0)
                return;
            Dictionary<string, object> controlsValue = new(controlsChangedValue);
            controlsChangedValue.Clear();
            foreach (KeyValuePair<string, object> kvp in controlsValue)
            {
                LvwJoystickControls_RefreshItem(kvp.Key, kvp.Value);
            }
        }

        private void LvwJoystickControls_RefreshItem(string control, object value)
        {
            var valueStr = value switch
            {
                bool b => b ? "Pressed" : "Not Pressed",
                double d => d.ToString("F3"),
                null => "<null>",
                _ => value.ToString()
            };
            int image;
            if (value == null)
                image = EDVImageIndex("Null");
            else
            {
                var valueImage = value switch
                {
                    bool b => b ? "True" : "False",
                    double => "Num",
                    int => "Num",
                    string => "Selection",
                    DevDecoder.HIDDevices.Converters.Direction => "Selection",
                    null => "Null",
                    _ => value.GetType().Name
                };
                image = EDVImageIndex(valueImage);
            }
            ListViewItem item;
            if (!lvwJoystickControls.Items.ContainsKey(control))
            {
                string usage = Joystick.ControlUsage(control);
                string name = Joystick.ControlName(control);
                item = lvwJoystickControls.Items.Add(control, name, image);
                item.SubItems.Add(valueStr);
                item.ToolTipText = usage;
            }
            else
            {
                item = lvwJoystickControls.Items[control];
                if (item.SubItems.Count < 2)
                    item.SubItems.Add(valueStr);
                else
                    item.SubItems[1].Text = valueStr;

            }
            try
            {
                //item.StateImageIndex = image;
                item.ImageIndex = image;

            }
            catch (ArgumentOutOfRangeException ex)
            {
                RTBLogger.LogError(ex, ex.Message + "\nStateImageIndex > {0} => {1}", item.StateImageIndex, image);
            }
            //RTBLogger.LogInformation(">{0} = {1}", control, valueStr);
        }

        private int EDVImageIndex(string key)
        {
            return imageListEDV.Images.IndexOfKey(key);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            FMain f = new(Joystick);
            f.Show();
        }

    }
}
