// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using System;
using System.Reactive.Disposables;
using System.Text;
using Microsoft.Extensions.Logging;

namespace MED.EDJoystick
{
    public class RichTextBoxLogger<T> : ILogger<T>
    {
        public readonly string Name;

        public LogLevel LogLevel { get; set; }

        public RichTextBox RTBox { get; private set; }

        public RichTextBoxLogger(LogLevel logLevel, string name, RichTextBox richTextBox){
            LogLevel = logLevel;
            Name = name ?? typeof(T).Name;

            RTBox = richTextBox;
            RTBox.Invalidated += RTBox_Invalidated;
        }

        /// <inheritdoc />
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            var message = formatter(state, exception);

            if (!string.IsNullOrEmpty(message))
            {
                if (RTBox == null || RTBox.Disposing || RTBox.IsDisposed)
                    Console.WriteLine(message);
                else
                {
                    _logBuffer.AppendLine(message);
                    try
                    {
                        RTBox.Invoke(RTBox.Invalidate);//Thread cross
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine(message);
                        Console.WriteLine(ex.ToString());
                        RTBox = null;
                    }
                }
            }
        }

        private StringBuilder _logBuffer = new();
        private void RTBox_Invalidated(object? sender, InvalidateEventArgs e)
        {
            if (_logBuffer.Length > 0)
            {
                if (RTBox.TextLength + _logBuffer.Length > 1024 * 1024)
                {
                    RTBox.Select(0, RTBox.TextLength / 2);
                    RTBox.SelectedText = "";
                }
                RTBox.AppendText(_logBuffer.ToString());
                _logBuffer.Clear();
                RTBox.Select(RTBox.TextLength, 0);
                RTBox.ScrollToCaret();
            }
        }

        /// <inheritdoc />
        public bool IsEnabled(LogLevel logLevel) => LogLevel <= logLevel;

        /// <inheritdoc />
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => Disposable.Empty;
    }
}
