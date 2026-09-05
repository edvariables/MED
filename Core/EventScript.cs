using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MED
{
    public partial class EventScript(IProcess process, string eventName)
    {
        public IProcess Process { get; set; } = process;
        public string EventName { get; set; } = eventName;

        private string? _Script;
        public string? Script
        {
            get => _Script;
            set
            {
                _Script = value;
                CompiledScript = null;
                _ParametersNames = null;
            }
        }
        public string GetMethodName() => GetMethodName(Process, EventName);

        private Microsoft.CodeAnalysis.Scripting.Script? CompiledScript { get; set; }

        private Dictionary<string, Type>? _ParametersNames;

        public Dictionary<string, Type>? ParametersNames
        {
            get
            {
                if (_ParametersNames != null)
                    return _ParametersNames;
                return _ParametersNames = GetParametersNames(process, eventName);
            }
            protected set => _ParametersNames = value;
        }
        private static Dictionary<string, Type>? GetParametersNames(IProcess process, string eventName)
        {
            var method = GetMethod(process, eventName);
            if (method == null)
            {
                process.Performance?.Sub($"EventScript").Alert($"Method {GetMethodName(process, eventName)} is missing.");
                return null;
            }
            Dictionary<string, Type>? parametersNames = new();
            foreach (var parameter in method.GetParameters())
                if (parameter.Name != null)
                    parametersNames.Add(parameter.Name, parameter.ParameterType);
            return parametersNames;
        }

        private static string GetMethodName(IProcess process, string eventName)
        {
            return $"{eventName}";
        }

        private static MethodInfo? GetMethod(IProcess process, string eventName)
        {
            string methodName = GetMethodName(process, eventName);
            return process.GetType().GetMethod(methodName);
        }

        /**
         * 
         * 
         * */
        public bool Eval(params object[]? parameters)
        {
            var script = Script;

            if (string.IsNullOrEmpty(script)) return true;

            if (CompiledScript == null)
            {
                script = InjectParameters(script, ParametersNames, out HashSet<Assembly> assemblies, parameters);

                try
                {
                    using (var loader = new InteractiveAssemblyLoader())
                    {
                        CompiledScript = CSharpScript.Create<bool>(script, ScriptOptions.Default.WithReferences(assemblies), globalsType: typeof(ScriptGlobals), assemblyLoader: loader);
                    }
                    CompiledScript.Compile();

                    Process.Performance?.Sub($"EventScript").Debug("\n" + CompiledScript.Code);
                }
                catch (Exception ex)
                {
                    Process.Performance?.Sub($"EventScript").Error($"Error in compilation of {script}", ex);
                    return false;
                }
            }

            return Eval(Process, CompiledScript, parameters);
        }

        private static bool Eval(IProcess process, Script script, params object[]? parameters)
        {
            try
            {
                if (process == parameters[0])
                    Console.Write("");
                var result = script.RunAsync(new ScriptGlobals(process, parameters)).Result;
            }
            catch (Exception ex)
            {
                process.Performance?.Sub($"EventScript").Error($"Error in evaluation of \n{script.Code}", ex);
                return false;
            }
            return true;
        }

        private static string InjectParameters(string script, Dictionary<string, Type>? parametersNames, out HashSet<Assembly> assemblies, params object[]? parameters)
        {
            assemblies = new();
            if (parametersNames == null || parameters == null)
                return script;
            int paramIndex = 0;
            StringBuilder scriptAdd = new();
            HashSet<string> namespaces = new();
            foreach (var (name, paramType) in parametersNames)
            {
                //string strValue = parameters[paramIndex] switch
                //{
                //    string str => "\"" + str.Replace("\"", "\\\"") + "\"",
                //    int i => i.ToString(),
                //    float i => i.ToString(),
                //    double i => i.ToString(),
                //    short i => i.ToString(),
                //    long i => i.ToString(),
                //    null => "<null>",
                //    _ => $"Parameters[{paramIndex}]"
                //};
                if (paramType.Namespace != null && !namespaces.Contains(paramType.Namespace))
                    namespaces.Add(paramType.Namespace);
                if (!assemblies.Contains(paramType.Assembly))
                    assemblies.Add(paramType.Assembly);
                scriptAdd.AppendLine($"var {name} = ({paramType.FullName})Parameters[{paramIndex}];");
                paramIndex++;
            }

            if (scriptAdd.Length > 0)
                script = $"{scriptAdd.ToString()}\n{script}";
            if (namespaces.Count > 0)
            {
                scriptAdd = new();
                foreach (var ns in namespaces)
                    scriptAdd.AppendLine($"using {ns};");
                script = $"{scriptAdd.ToString()}\n{script}";
            }
            return script;
        }

        private static string ClearComments(string script)
        {
            script = Regex.Replace(script, @"\/\*[\s\S]*\*\/", "");
            script = Regex.Replace(script, @"^//.*([\n\r]|$)", "");
            return script;
        }

        private static string IdentifyStrings(string script, out Dictionary<string, string>? stringsKeys)
        {
            stringsKeys = null;
            return script;
        }

        private static string RestoreStrings(string script, Dictionary<string, string>? stringsKeys)
        {
            if (stringsKeys != null)
                foreach (var (name, value) in stringsKeys)
                    script = script.Replace(name, value);
            return script;
        }

        private static string[] ParseScript(string script)
        {
            return script.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        public class ScriptGlobals(IProcess process, object[]? parameters)
        {
            public object[]? Parameters = parameters;

            public IProcess Process = process;
        }
    }
}
