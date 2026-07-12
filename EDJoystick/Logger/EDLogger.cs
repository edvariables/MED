// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MED.EDJoystick
{
    internal static class EDLogger
    {
        public static ILogger<T> Create<T>(LogLevel logLevel = LogLevel.Information, RichTextBox? richTextBox = null)
        {
            if(richTextBox ==  null)
                return new SimpleConsoleLogger<T>(logLevel);
            else
                return new RichTextBoxLogger<T>(logLevel, null, richTextBox);
            //return (ILogger<T>)_logger; // new RichTextBoxLogger<T>(logLevel, null, richTextBox);
        }
    }
}
