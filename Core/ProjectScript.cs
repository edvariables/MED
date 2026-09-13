using MED.GameController;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Design;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MED
{
    /**
     * Global script containing all items scripts.
     * Each IProcess may use a specific class instead of compiling his own script
     * */
    public class ProjectScript(IProcesses processes) : EventScript(processes, "Global")
    {
        public override IProcess Process
        {
            get => base.Process;
            set
            {
                if (value == null)
                    throw new ArgumentException($"Must not be null.");
                else if (value is not IProcesses processes1)
                    throw new ArgumentException($"Must be of IProcesses type. {value.GetType()} provided.");
                else
                    base.Process = processes1;
            }
        }

        [Browsable(true)]
        [ReadOnly(true)]
        [Category("Script")]
        [Editor(typeof(EventScriptEditor), typeof(UITypeEditor))]
        public override string? Script
        {
            get => base.Script??"";
            set => throw new NotImplementedException();
        }


        private Dictionary<string, EventScript> ProcessScriptIDs = [];

        /**
         * PrepareScript
         * 
         * */
        public override string PrepareScript(out HashSet<Assembly> assemblies, params object?[]? parameters)
        {
            assemblies = [];

            ProcessScriptIDs = [];

            StringBuilder script = new();

            List<string> namespaces = [];

            foreach (var eventScript in GetProcessScripts((Processes)Process))
            {
                if (eventScript == this)
                    continue;
                var scriptID = GetEventScriptID(eventScript);
                ProcessScriptIDs.Add(scriptID, eventScript);

                script.AppendLine($"/*****\n * {eventScript.Process.ToString()} {eventScript.EventName} */");

                var scriptGlobalsTypeName = (eventScript.ScriptGlobalsType.FullName ?? eventScript.ScriptGlobalsType.Name).Replace('+', '.');
                var scriptGlobalsEvalTypeName = (typeof(IScriptGlobalsEval).FullName ?? typeof(IScriptGlobalsEval).Name).Replace('+', '.');
                script.AppendLine($"public class {scriptID}(IProcess process, object?[] parameters) : {scriptGlobalsTypeName}(process, parameters), {scriptGlobalsEvalTypeName}{{");

                script.AppendLine($"public void Eval(IProcess _evalProcess, params object?[] _params_){{");
                script.AppendLine($"_set_parameters(this, _evalProcess, _params_);");

                var script1 = eventScript.PrepareScript(out HashSet<Assembly> eventAssemblies);
                foreach (var assembly in eventAssemblies)
                    if (!assemblies.Contains(assembly))
                        assemblies.Add(assembly);

                script.AppendLine(script1 ?? "");

                script.AppendLine("}}");

                script.AppendLine($"/* {eventScript.Process.ToString()} {eventScript.EventName}\n*****/\n\n");

            }
            var innerScript = ExtractNamespaces(script.ToString(), namespaces);

            script = new StringBuilder();
            foreach (var namesp in namespaces)
                script.AppendLine(namesp);

            script.AppendLine(innerScript);

            //Set eventScript.EvalScriptObject = new(classId);
            foreach (var classId in ProcessScriptIDs.Keys)
                script.AppendLine($"SetEvalScriptObject({nameof(ProcessScriptIDs)}[\"{classId}\"], typeof({classId}), []);");

            return base.Script = AddPreprocessorDirectives(script.ToString());
        }

        private string ExtractNamespaces(string script, List<string> namespaces)
        {
            Regex regex = new(@"\n\t*using\s.+;\r?");
            MatchEvaluator evaluator = new((match) =>
            {
                var v = match.Value.TrimStart('\n', '\t').TrimEnd('\r');
                if (!namespaces.Contains(v))
                    namespaces.Add(v);
                return "";
            });
            script = regex.Replace(script, evaluator);
            return script;
        }

        private string GetEventScriptID(EventScript eventScript)
        {
            return $"{eventScript.Process.Name}_{eventScript.EventName}_{eventScript.Process.GetHashCode()}";
        }

        private List<EventScript> GetProcessScripts(IProcess process)
        {
            List<EventScript> eventScripts = new();

            foreach (var property in process.GetType().GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.GetProperty))
                if (property.PropertyType.IsAssignableTo(typeof(EventScript)))
                {
                    var eventScript = (EventScript?)property.GetValue(process) ?? null;
                    if (eventScript == null || eventScript == this || String.IsNullOrEmpty(eventScript.Script))
                        continue;
                    eventScripts.Add(eventScript);
                }

            //Recursive on IProcesses.Items
            if (process is IProcesses processes1)
                foreach (var process1 in processes1.Items)
                    eventScripts.AddRange(GetProcessScripts(process1));

            return eventScripts;
        }

        private void ClearAllScriptEvalProcesses(IProcess process)
        {
            List<EventScript> eventScripts = new();

            foreach (var property in process.GetType().GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.GetProperty))
                if (property.PropertyType.IsAssignableTo(typeof(EventScript)))
                    if (property.GetValue(process) is EventScript eventScript)
                        eventScript.EvalScriptObject = null;

            //Recursive on IProcesses.Items
            if (process is IProcesses processes1)
                foreach (var process1 in processes1.Items)
                    ClearAllScriptEvalProcesses(process1);

        }

        [Category("Script")]
        [Description("Set to true to compile now. Always returns false value.")]
        public bool CompileScriptNow
        {
            get => false;
            set { if (value) CompileScript(); }
        }

        [Category("Script")]
        [Description("Compile script on project process load")]
        [DefaultValue(true)]
        public bool CompileScriptOnLoad { get; set; } = true;

        /**
         * CompileScript
         * 
         * */
        public override bool CompileScript(params object?[] parameters)
        {
            if (String.IsNullOrEmpty(Script))
                PrepareScript(out HashSet<Assembly> assemblies, parameters);//For Script not to be empty TODO better

            ClearAllScriptEvalProcesses(Process);

            if (base.CompileScript(parameters)
                && CompiledScript != null)
            {
                var scriptGlobals = new ScriptGlobalsProcesses(Process, ProcessScriptIDs);
                return Eval(Process, CompiledScript, scriptGlobals);
            }
            return true;
        }

        [Browsable(false)]
        public override Dictionary<string, Type>? ParametersNames => base.ParametersNames;

        [Browsable(false)]
        public override Dictionary<string, Type>? VariablesNames => base.VariablesNames;

        public override Type ScriptGlobalsType { get; } = typeof(ScriptGlobalsProcesses);

        public class ScriptGlobalsProcesses(IProcess process1, Dictionary<string, EventScript> processScriptIDs) : ScriptGlobals(process1, [])
        {
            public Dictionary<string, EventScript> ProcessScriptIDs = processScriptIDs;

            public static void SetEvalScriptObject(EventScript eventScript, Type scriptGlobalsEvalType, object?[] parameters)
            {
                List<object?> paramList = new([eventScript.Process]);
                paramList.Add(parameters);
                var constructor = scriptGlobalsEvalType.GetConstructors().First();
                eventScript.EvalScriptObject = (IScriptGlobalsEval)constructor.Invoke(paramList.ToArray());
            }
        }

        /**
         * Interface of internal classes
         * */
        public interface IScriptGlobalsEval
        {
            void Eval(IProcess _evalProcess, params object?[] parameters);
        }
    }
}
