// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using DevDecoder.HIDDevices;
using DevDecoder.HIDDevices.Controllers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;

namespace MED.GameController
{
    public class JoystickHIDController : GameController
    {
        public JoystickHIDController(string name = "Joystick", Performance? performance = null, System.Windows.Forms.Control? invokeHandler = null, IConsumer? consumer = null, bool isAsynchrone = true)
            : base(name, performance, invokeHandler, consumer, isAsynchrone)
        {
            ProcessIconDefault = "joystick";
        }

        public override object? GetControllerPropertyValue(string controllerProperty)
        {
            if (UsagesState.TryGetValue(controllerProperty, out object? value))
                return value;
            return false;
        }

        public override void SetControllerPropertyValue(string controllerProperty, object? value)
        {
            UsagesState[controllerProperty] = value;
        }

        [Browsable(true)]
        [Category("Controller")]
        public Dictionary<string, object?> UsagesState { get; set; } = [];


        private Task? _taskRun;
        private CancellationTokenSource? _threadRunCancellation;
        private CancellationToken threadRunCancellationToken;

        /**
         * 
         * */
        private bool Connect()
        {
            if (!Enabled)
                return false;
            _threadRunCancellation = new CancellationTokenSource();
            threadRunCancellationToken = _threadRunCancellation.Token;

            TaskFactory factory = new TaskFactory(threadRunCancellationToken);
            _taskRun = factory.StartNew(StartConnect, threadRunCancellationToken);
            _taskRun.GetAwaiter().OnCompleted(Disconnect);

            return true;
        }

        private void StartConnect()
        {

            // Create a singleton instance of the controllers object, that we should dispose
            // on closing the game, here we use a using block, but can obviously call controllers.Dispose()
            using var devices = new Devices(null);


            // Holds a reference to the current gamepad, which is set asynchronously as they are detected.
            Gamepad? gamepad = null;

            long timestamp = 0L;

            // Controller to any gamepads as they are found
            using var subscription = devices.Controllers<Gamepad>().Subscribe(g =>
            {
                // If we already have a connected gamepad ignore any more.
                // ReSharper disable once AccessToDisposedClosure
                if (gamepad?.IsConnected == true)
                {
                    return;
                }

                if (g.Name.Contains("xbox ", StringComparison.InvariantCultureIgnoreCase))
                {
                    Performance?.Alert(
                        $"{g.Name} found!  Unfortunately, it appears XInput-compatible HID device driver only transmits events from the HID device whilst the current process has a focussed window, so console applications/background services cannot detect button presses. Please try a different controller.");
                    return;
                }

                // Assign this gamepad and connect to it.
                gamepad = g;
                g.Connect();
                Performance?.Step($"{gamepad.Name} found!  Following controls were mapped:");

                SetControls(g.Mapping);
                foreach (var (control, infos) in g.Mapping)
                {
                    Performance?.Step(
                        // ReSharper disable once SuspiciousTypeConversion.Global
                        $"  {Usage.GetName(control.Usages)} => {string.Join(", ", infos.Select(info => info.PropertyName))}");
                }

                timestamp = CheckChanges(gamepad);
            });

            try
            {
                // Our 'game loop'
                while (!threadRunCancellationToken.IsCancellationRequested)
                {
                    // Sleep to simulate a game loop.
                    Thread.Sleep(15);

                    timestamp = CheckChanges(gamepad, timestamp);

                    // Or directly access controls
                    //if (currentGamepad.AButton)
                    //{
                    //    Logger.LogInformation("A Button pressed, finishing.");
                    //    break;
                    //}
                }
            }
            catch (Exception ex)
            {
                Performance?.Error("Exception !", ex);
            }
            finally
            {
                // Ensure gamepad connection is disposed to stop listening to the gamepad
                if (gamepad != null)
                {
                    gamepad.Dispose();
                    Performance?.Step($"{gamepad.Device.Name} disconnected!");
                }
            }
        }

        /**
         * CheckChanges
         * 
         * Returns last change TimeStamp
         * */
        private long CheckChanges(Gamepad gamepad, long changeTimestamp = 0L)
        {
            // If we haven't got a gamepad, or the current one isn't connected, wait for a connected gamepad.
            var currentGamepad = gamepad;
            if (currentGamepad?.IsConnected != true) return changeTimestamp;

            // Look for any changes since the last detected change.
            var changes = currentGamepad.ChangesSince(changeTimestamp);
            if (changes.Count > 0)
            {

                DevDecoder.HIDDevices.Control prev_control = null;
                object? prev_Value = null;
                //logBuilder.Append("Batch ").Append(++batch).AppendLine();
                foreach (var change in changes)
                {
                    // We should update our timestamp to the last change we see.
                    if (changeTimestamp < change.Timestamp)
                        changeTimestamp = change.Timestamp;

                    var value = change.Value;
                    //Next Info
                    if (prev_control == change.Control)
                    {
                        //Z = RightTrigger - LeftTrigger
                        if (value is Double && prev_Value is Double)
                            value = (double)value - (double)prev_Value;
                    }
                    SetControlValue(change.PropertyName, value);

                    var valueStr = change.Value switch
                    {
                        bool b => b ? "Pressed" : "Not Pressed",
                        double d => d.ToString("F3"),
                        null => "<null>",
                        _ => change.Value.ToString()
                    };
                    Performance?.Step($"{change.PropertyName} : {valueStr} ({change.Elapsed.TotalMilliseconds} ms)");

                    prev_control = change.Control;
                    prev_Value = value;
                }
            }
            return changeTimestamp;
        }

        /**
         *  Set Control Value
         * */
        protected void SetControlValue(string controlKey, object? value)
        {
            if (!UsagesState.ContainsKey(controlKey))
                return;
            object? oldValue = UsagesState[controlKey];
            bool changedValue = oldValue == null ? value != null : !oldValue.Equals(value);

            if (changedValue)
            {
                UsagesState[controlKey] = value;

                InvokeControllerChanged(this, new(controlKey, value));
            }
        }
        private void SetControls(IReadOnlyDictionary<DevDecoder.HIDDevices.Control, IReadOnlyList<ControlInfo>> mapping)
        {
            UsagesState.Clear();
            foreach (var (control, infos) in mapping)
            {
                string ctrlKey = ControlKey(control);
                string usage = Usage.GetName(control.Usages);
                string name = infos.Count == 1 ? infos[0].PropertyName : usage;
                string propertiesName = string.Join(", ", infos.Select(info => info.PropertyName));
                UsagePropertiesMap.Add(propertiesName, ctrlKey, control.IsPushButton || control.IsBoolean ? typeof(bool) : typeof(object));
                UsagesState.Add(ctrlKey, control.IsPushButton || control.IsBoolean ? false : 0F);
                Performance?.Debug($"{usage} = {propertiesName} : {infos[0].Converter?.ToString()}");

                //Logger.LogInformation(
                //    // ReSharper disable once SuspiciousTypeConversion.Global
                //    $"  {Usage.GetName(control.Usages)} => {string.Join(", ", infos.Select(info => info.PropertyName))}");
            }

        }

        private string ControlKey(object control) => Usage.GetName(((DevDecoder.HIDDevices.Control)control).Usages);

        private void Disconnect()
        {
            if (_threadRunCancellation != null)
                _threadRunCancellation.Cancel();
        }


        #region Process
        public override void Start()
        {
            Connect();

            base.Start();

            ProcessState = ThreadState.Running;
        }
        public override void Stop()
        {
            Disconnect();

            base.Stop();
        }
        #endregion
    }
}