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

namespace MED.EDJoystick;

public partial class FDemo : Form
{

    public FDemo()
    {
        InitializeComponent();
    }

    public FDemo(IJoystick joystick):this()
    {
        Joystick = joystick;
        _slow_form_delay = 2000 * TimeSpan.TicksPerMillisecond;
        if (joystick != null)
            chkRun.Checked = true;
    }

    private long _slow_form = 0L;
    private readonly long _slow_form_delay = 0L;

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
        if (joystick == null)
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
        lvwJoystickControls.Invoke(lvwJoystickControls.Invalidate);
    }

    private void Joystick_ButtonPressed(string control, bool pressed)
    {
        Joystick_ValueChanged(control, pressed);
    }

    private void LvwJoystickControls_Invalidated(object sender, InvalidateEventArgs e)
    {
        LvwJoystickControls_Refresh();
    }

    private void LvwJoystickControls_Refresh()
    {
        if (Joystick == null)
            return;

        if (_slow_form_delay > 0)
            if (_slow_form + _slow_form_delay > DateTime.Now.Ticks)
                return;
            else
                _slow_form = DateTime.Now.Ticks;

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
        FDemo f = new(Joystick);
        f.Show();
    }
}
