// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using DevDecoder.HIDDevices;
using DevDecoder.HIDDevices.Controllers;
using DynamicData;
using HidSharp;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Xml.Linq;

namespace MED.EDJoystick
{
    public class JoystickHID(System.Windows.Forms.Form formHandler, ILogger<Devices> _logger) : IJoystick(formHandler, _logger)
    {
        private Task _taskRun;
        private CancellationTokenSource _threadRunCancellation;
        private CancellationToken threadRunCancellationToken;

        /**
         * 
         * */
        public override bool Connect()
        {
            //Thread th = new(StartConnect);
            //th.Start();

            _threadRunCancellation = new CancellationTokenSource();
            threadRunCancellationToken = _threadRunCancellation.Token;

            TaskFactory factory = new TaskFactory(threadRunCancellationToken);
            _taskRun = factory.StartNew(StartConnect, threadRunCancellationToken);
            _taskRun.GetAwaiter().OnCompleted(Disconnect);

            return true;
        }

        private void StartConnect() {

            // Create a singleton instance of the controllers object, that we should dispose
            // on closing the game, here we use a using block, but can obviously call controllers.Dispose()
            using var devices = new Devices(this.Logger);


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
                    Logger.LogWarning(
                        $"{g.Name} found!  Unfortunately, it appears XInput-compatible HID device driver only transmits events from the HID device whilst the current process has a focussed window, so console applications/background services cannot detect button presses. Please try a different controller.");
                    return;
                }

                // Assign this gamepad and connect to it.
                gamepad = g;
                g.Connect();
                Logger.LogInformation($"{gamepad.Name} found!  Following controls were mapped:");

                SetControls(g.Mapping);
                foreach (var (control, infos) in g.Mapping)
                {
                    Logger.LogInformation(
                        // ReSharper disable once SuspiciousTypeConversion.Global
                        $"  {Usage.GetName(control.Usages)} => {string.Join(", ", infos.Select(info => info.PropertyName))}");
                }

                timestamp = CheckChanges(gamepad);
            
                IsConnected = true;
            });            
            
            try
            {
                // Our 'game loop'
                while ( ! threadRunCancellationToken.IsCancellationRequested)
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
                Logger.LogError(ex.ToString());
            }
            finally
            {
                // Ensure gamepad connection is disposed to stop listening to the gamepad
                if (gamepad != null)
                {
                    gamepad.Dispose();
                    Logger.LogInformation($"{gamepad.Device.Name} disconnected!");
                }
                IsConnected = false;
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
                var logBuilder = new StringBuilder();

                DevDecoder.HIDDevices.Control prev_control = null;
                object prev_Value = null;
                //logBuilder.Append("Batch ").Append(++batch).AppendLine();
                foreach (var change in changes)
                {
                    // We should update our timestamp to the last change we see.
                    if (changeTimestamp < change.Timestamp)
                    {
                        changeTimestamp = change.Timestamp;
                    }
                    var value = change.Value;
                    //Next Info
                    if (prev_control == change.Control)
                    {
                        //Z = RightTrigger - LeftTrigger
                        if (value is Double && prev_Value is Double)
                            value = (double)value - (double)prev_Value;
                    }
                    SetControlValue(ControlKey(change.Control), value);

                    var valueStr = change.Value switch
                    {
                        bool b => b ? "Pressed" : "Not Pressed",
                        double d => d.ToString("F3"),
                        null => "<null>",
                        _ => change.Value.ToString()
                    };
                    logBuilder.Append("  ")
                        .Append(change.PropertyName)
                        .Append(": ")
                        .Append(valueStr)
                        .Append(" (")
                        .AppendFormat("{0:F3}", change.Elapsed.TotalMilliseconds).AppendLine("ms)");


                    prev_control = change.Control;
                    prev_Value = value;
                }
                Logger.LogInformation(logBuilder.ToString());
            }
            return changeTimestamp;
        }
        
        public override void Disconnect()
        {
            if(_threadRunCancellation != null)
                _threadRunCancellation.Cancel();

            IsConnected = false;
        }

        private void SetControls(IReadOnlyDictionary<DevDecoder.HIDDevices.Control, IReadOnlyList<ControlInfo>> mapping)
        {
            ClearControls();
            foreach (var (control, infos) in mapping)
            {
                string ctrlKey = ControlKey(control);
                string usage = Usage.GetName(control.Usages);
                string name = infos.Count == 1 ? infos[0].PropertyName : usage;
                Controls.Add(ctrlKey, new ControlData(control, name, usage, null, control.IsPushButton || control.IsBoolean));
                if( control.IsPushButton || control.IsBoolean)
                    ButtonControls.Add(ctrlKey, usage);
                string propertiesName = string.Join(", ", infos.Select(info => info.PropertyName));
                Logger.LogInformation($"{usage} = {propertiesName} : {infos[0].Converter?.ToString()}");

                //Logger.LogInformation(
                //    // ReSharper disable once SuspiciousTypeConversion.Global
                //    $"  {Usage.GetName(control.Usages)} => {string.Join(", ", infos.Select(info => info.PropertyName))}");
            }

        }

        public override string ControlKey(object control)
        {
            string usage = ControlUsage(control);
            return usage;
        }

        public override string ControlUsage(object control)
        {
            return Usage.GetName(((DevDecoder.HIDDevices.Control)control).Usages);
        }

        /**
         * Usages
         */
        public override List<JoystickUsage> Usages
        {
            get
            {
                List<JoystickUsage> usages = new();

                foreach (var (control_key, data) in Controls)
                {
                    var sUsage = data.Usage.Replace(' ', '_');
                    JoystickUsage jUsage;
                    if (Enum.TryParse<JoystickUsage>(sUsage, true, out jUsage))
                        usages.Add(jUsage);
                }
                return usages;
            }
        }
    }
}
