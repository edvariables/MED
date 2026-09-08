using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MED
{
    public class PerformanceException(Performance performance, string? message, Exception? innerException) : Exception(message, innerException)
    {
        public Performance Performance { get; } = performance;

        public readonly DateTime DateTime = DateTime.Now;

        public TimeSpan Delay { get => DateTime.Now - this.DateTime; }
        public string DelayToString()
        {
            var str = Delay.ToString();
            while (str.StartsWith("00:"))
                str = str.Substring(3);
            var i = str.LastIndexOf('.');
            if (i < str.Length - 3)
                str = str.Substring(0, i + 4);
            return str;
        }
        public override string ToString()
        {
            var str = base.ToString();
            var className = GetType().ToString();
            if (str.StartsWith(className))
                str = str.Remove(0, className.Length + 2);
            if (str.StartsWith("[ERROR] "))
                str = str.Remove(0, "[ERROR] ".Length);
            return $"[{Performance.Name}]{str}\n{DelayToString()}";
        }
    }
}
