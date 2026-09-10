using Microsoft.CodeAnalysis.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MED
{
    public class PaintScript(IImageProvider process, string eventName) : EventScript(process, eventName)
    {

        public override Type ScriptGlobalsType { get; } = typeof(ScriptGlobalsPaint);

        public class ScriptGlobalsPaint(IImageProvider process, object[]? parameters) : ScriptGlobals(process, parameters)
        {
        }
    }
}
