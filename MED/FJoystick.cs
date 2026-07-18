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
    public partial class FJoystick : ProcessForm
    {
        int _refresh_delay;

        public FJoystick()
        {
            InitializeComponent();
            Init_Joystick_Config();

        }

        private void FJoystick_Activated(object sender, EventArgs e)
        {
            FProperties.CurrentProperty = Joystick;
        }

        public FJoystick(IJoystick joystick) : this()
        {
            Joystick = joystick;
            _refresh_delay = 500;
            if (joystick != null)
                chkRun.Checked = true;
        }

        public IProcess.ProcessStateChangedDelegate ProcessStateChanged;

        /**
         * RTBLogger
         */
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

        public bool IsRunning => throw new NotImplementedException();

        public ThreadState ProcessState => throw new NotImplementedException();

        /***
         * Init_Joystick_Config
         */
        public void Init_Joystick_Config()
        {
            cboJoystickConfig.SelectedIndex = 0;
        }

        /***
         * Init_Joystick_Usages
         */
        public void Init_Joystick_Usages()
        {
            var value = cboUsages.Text;
            cboUsages.Items.Clear();
            cboUsages.Items.Add($"(tous)");
            foreach (var usage in Joystick.Usages)
                cboUsages.Items.Add(usage);
            cboUsages.Text = value;
        }

        /**
         * chkRun CheckedChanged
         * 
         * */
        private void chkRun_CheckedChanged(object sender, EventArgs e)
        {
            if (Joystick == null && !chkRun.Checked)
                return;
            if (chkRun.Checked)
            {
                chkRun.Text = "Connexion...";
                if (!RunTask(Joystick))
                    chkRun.Checked = false;
            }
            else
            {
                Joystick?.Disconnect();
                chkRun.Text = "Activer";
            }

        }

        private IJoystick Joystick;

        /**
         * Create Joystick
         * 
         */
        private IJoystick CreateJoystick()
        {
            if (cboJoystickConfig.SelectedIndex < 0)
            {
                MessageBox.Show("Veuillez sélectionner une config.");
                return null;
            }

            var sConfig = cboJoystickConfig.Items[cboJoystickConfig.SelectedIndex].ToString();
            int nConfig = int.Parse(sConfig.Split(':')[0].Trim());
            switch (nConfig)
            {
                //1 : Clavier
                //2 : Clavier / Hook
                //3 : Joystick
                //4 : Joystick #2
                //5 : Joystick #3
                case 1:
                    return new JoystickKeyboard(this, RTBLogger, typeof(KeyboardFormEvents));
                case 2:
                    return new JoystickKeyboard(this, RTBLogger, typeof(KeyboardHook));
                case 3:
                case 4:
                case 5:
                    return new JoystickHID(this, RTBLogger);
                default:
                    break;
            }
            return null;
        }

        /***
         * 
         * 
         */
        private bool RunTask(IJoystick joystick = null)
        {
            if (joystick == null)
            {
                Joystick = CreateJoystick();
                if (Joystick == null)
                    return false;
            }
            else
                Joystick = joystick;
            Joystick.IsConnectedChanged += Joystick_IsConnectedChanged;

            string usage;
            if (cboUsages.SelectedIndex <= 0)
                usage = Consts.ALL_USAGES;
            else
                usage = cboUsages.Text;
            Joystick.AddValueChangedDelegate(this.Handle, Joystick_ValueChanged, usage, _refresh_delay);
            //Joystick.ButtonPressed += Joystick_ButtonPressed;
            if (Joystick.IsConnected)
            {
                Joystick_IsConnectedChanged(true);
                return true;
            }
            return Joystick.Connect();
        }

        private void Joystick_IsConnectedChanged(bool connected)
        {
            if (connected)
            {
                chkRun.Invalidated += OnConnexion;
                chkRun.Invoke(chkRun.Invalidate);
            }
            else
            {
                chkRun.Checked = false;
                Joystick = null;
            }
        }

        private void OnConnexion(object sender, InvalidateEventArgs e)
        {
            chkRun.Invalidated -= OnConnexion;

            chkRun.Text = "En cours...";

            Init_Joystick_Usages();

            Joystick_ValueChanged(null);
        }


        private void Joystick_ValueChanged(ValueChangedData valueChangedData)
        {
            RefreshValuesChanged();
        }

        private void RefreshValuesChanged(bool invokeMainThread = true)
        {
            //if (this.IsDisposed || this.Disposing)
            //    return;
            //if (invokeMainThread)
            //    lvwJoystickControls.Invoke(lvwJoystickControls.Invalidate);
            //else if (!DelayRefreshValuesChanged())
            LvwJoystickControls_Refresh();
        }

        private void Joystick_ButtonPressed(ValueChangedData valueChangedData)
        {
            Joystick_ValueChanged(valueChangedData);
        }

        private void LvwJoystickControls_Invalidated(object sender, InvalidateEventArgs e)
        {
            RefreshValuesChanged(false);
        }


        private void LvwJoystickControls_Refresh()
        {
            if (Joystick == null || this.IsDisposed || this.Disposing)
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
                    long => "Num",
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
            FJoystick f = new(Joystick);
            f.Show();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            FJoystick f = new();
            f.Show();
        }

        private void cboUsages_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
        private void cboJoystickConfig_SelectedIndexChanged(object sender, EventArgs e)
        {
            lvwJoystickControls.Items.Clear();
        }

        private void button3_Click(object sender, EventArgs e)
        {

            //var view = new EDWebCam.WebcamCaptureForm();
            //var presenter = new WebcamCapturer.Core.WebcamCapturePresenter(view);
            //view.Show();
        }

        public void Start()
        {
            throw new NotImplementedException();
        }

        public void Pause()
        {
            throw new NotImplementedException();
        }

        public void Resume()
        {
            throw new NotImplementedException();
        }

        public void Stop()
        {
            throw new NotImplementedException();
        }
    }
}
