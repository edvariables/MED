// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using DevDecoder.HIDDevices;

namespace MED.EDJoystick
{
    public enum JoystickUsage
    {
        X,
        Y,
        Z,
        Start,
        Select,
        LeftTrigger,
        RightTrigger,
        AButton,
        BButton,
        XButton,
        YButton,
        LeftBumper,
        RightBumper,
        LeftStick,
        RightStick,
        Hat
    }

    public enum JoystickProperty
    {
        X,
        Y,
        Z,
        Button_0,
        Button_1,
        Button_2,
        Button_3,
        Button_4,
        Button_5,
        Button_6,
        Button_7,
        Button_8,
        Button_9,
        Hat_Switch
    }
    public static partial class Joystick
    {
    }
}
