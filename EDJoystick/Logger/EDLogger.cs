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
