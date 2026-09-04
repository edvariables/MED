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
                ScriptRows = null;
                StringsKeys = null;
                _ParametersNames = null;
            }
        }
        public string GetMethodName()=> GetMethodName(Process, EventName);

        private string[]? ScriptRows { get; set; }

        private Dictionary<string, string>? StringsKeys;

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
                process.Performance?.Sub($"EventScript {eventName}").Alert($"Method {GetMethodName(process, eventName)} is missing.");
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
            return $"On{eventName}Changed";
        }

        private static MethodInfo? GetMethod(IProcess process, string eventName)
        {
            string methodName = GetMethodName(process,eventName);
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

            if (ScriptRows == null)
            {
                script = ClearComments(script);

                script = IdentifyStrings(script, out Dictionary<string, string>? StringsKeys);

                ScriptRows = ParseScript(script);

                script = RestoreStrings(script, StringsKeys);
            }

            script = InjectParameters(script, ParametersNames, parameters);

            if (ScriptRows.Length == 0)
                return true;
            return Eval(Process, ScriptRows, parameters);
        }

        private static bool Eval(IProcess process, string[] scripts, params object[]? parameters)
        {
            foreach (var script in scripts)
                if (!Eval(process, script, parameters))
                    return false;
            return true;
        }
        private static bool Eval(IProcess process, string script, params object[]? parameters)
        {
            if (string.IsNullOrEmpty(script)) return true;

            process.Performance?.Debug($"Eval Script : {script}");

            var matches = Regex.Matches(@"(?<variable>[^=]+)\s*(?<operator>[+\-*/=:]+)\s*(?<value>.+)\s*(;|$)", script);
            matches = Regex.Matches(@"^(?<variable>[^=]+)\s*(;|$)", script);

            foreach (var match in matches)
            {

            }

            return true;
        }

        //[GeneratedRegex(@"(?<left>[^=]+)\s*(?<operator>[+\-*/=:]+)\s*(?<operator>[+\-*/=:]+)\s*(;|$)")]
        //private static partial Regex MyRegex();


        private static string InjectParameters(string script, Dictionary<string, Type>? parametersNames, params object[]? parameters)
        {
            if (parametersNames == null || parameters == null)
                return script;
            int paramIndex = 0;
            foreach (var (name, paramType) in parametersNames)
            {
                string strValue = parameters[paramIndex] switch
                {
                    string str => "\"" + str.Replace("\"", "\\\"") + "\"",
                    int i => i.ToString(),
                    float i => i.ToString(),
                    double i => i.ToString(),
                    short i => i.ToString(),
                    long i => i.ToString(),
                    null => "<null>",
                    _ => $"_parameters[{paramIndex}]"
                };
                script = Regex.Replace(script, @"(^|\W)" + Regex.Escape(name) + @"(\W|$)", $"$1{strValue}$2");
                paramIndex++;
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
    }
}
