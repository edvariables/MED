using Microsoft.CodeAnalysis.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MED.GameController;
using System.ComponentModel;

namespace MED
{
    public class ProcessStateScript(IProcess process, string eventName) : EventScript(process, eventName)
    {

        protected override void AddConsumers(){}
        protected override void RemoveConsumers(){}

        public override Type ScriptGlobalsType { get; } = typeof(ScriptGlobalsProcessState);

        public class ScriptGlobalsProcessState(IProcess process, object?[] parameters) : ScriptGlobals(process, parameters)
        {
            public IGameController? gameController
            {
                get => ProcessStatic.GetGameController(_process);
            }
        }
    }
}
