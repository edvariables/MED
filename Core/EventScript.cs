using MED.Core;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MED
{
    [Editor(typeof(EventScriptEditor), typeof(UITypeEditor))]
    [TypeConverter(typeof(EventScriptConvertor))]
    public class EventScript(IProcess process, string eventName)
    {
        public IProcess Process { get; set; } = process;
        public string EventName { get; set; } = eventName;

        private string? _Script;
        public virtual string? Script
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
            var processType = process.GetType();
            string methodName = GetMethodName(process, eventName);
            var method = processType.GetMethod(methodName);
            if (method != null)
                return method;
            methodName += "Changed";
            return processType.GetMethod(methodName);
        }

        /**
         * 
         * 
         * */
        public bool CompileScript(params object[]? parameters)
        {
            var script = Script;

            if (string.IsNullOrEmpty(script)) return true;

            script = InjectParameters(script, Process, ParametersNames, out HashSet<Assembly> assemblies, parameters);

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
            return true;
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
                if (!CompileScript(parameters))
                    return false;
            if (CompiledScript == null)
                return true;

            return Eval(Process, CompiledScript, parameters);
        }

        private static bool Eval(IProcess process, Script script, params object[]? parameters)
        {
            try
            {
                var result = script.RunAsync(new ScriptGlobals(process, parameters)).Result;
            }
            catch (Exception ex)
            {
                process.Performance?.Sub($"EventScript").Error($"Error in evaluation of \n{script.Code}", ex);
                return false;
            }
            return true;
        }

        private static string InjectParameters(string script, IProcess process, Dictionary<string, Type>? parametersNames, out HashSet<Assembly> assemblies, params object[]? parameters)
        {
            assemblies = new();
            if (parametersNames == null || parameters == null)
                return script;
            int paramIndex = 0;
            StringBuilder scriptAdd = new();
            HashSet<string> namespaces = new();

            namespaces.Add(typeof(PointF).Namespace);

            foreach (var (name, paramType) in parametersNames)
            {
                if (paramType.Namespace != null && !namespaces.Contains(paramType.Namespace))
                    namespaces.Add(paramType.Namespace);
                if (!assemblies.Contains(paramType.Assembly))
                    assemblies.Add(paramType.Assembly);

                scriptAdd.AppendLine($"var {name} = ({paramType.FullName})Parameters[{paramIndex}];");

                if (paramType.Equals(typeof(PropertyChangedEventArgs)))
                    scriptAdd.AppendLine($"var property = {name}.Property;");

                paramIndex++;
            }

            //Process cast from Globals._Process
            var processType = process.GetType();
            if (processType.Namespace != null && !namespaces.Contains(processType.Namespace))
                namespaces.Add(processType.Namespace);
            if (!assemblies.Contains(processType.Assembly))
                assemblies.Add(processType.Assembly);
            scriptAdd.AppendLine($"var Process = ({processType.FullName})_Process;");

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

        public static string ClearComments(string script)
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

        public static EventScript GetNewEventScript(IProcess process, string eventName, ITypeDescriptorContext? context = null)
        {
            if (context == null || context.PropertyDescriptor == null)
                return new(process, eventName);
            var constructor = context.PropertyDescriptor.PropertyType.GetConstructors().First();
            object[] parameters = [process, eventName]; 
            return (EventScript)constructor.Invoke(parameters);
        }

        public static void LoadSetting(ProcessSettings settings, IProcess process, string propertyInfoName)
        {
            PropertyInfo? propertyInfo = process.GetType().GetProperty(propertyInfoName);
            if (propertyInfo == null)
                throw new Exception($"Property {propertyInfoName} does not exists in {process} object.");
            var currentValue = propertyInfo.GetValue(process);
            string? script = null;
            EventScript? eventScript = null;
            if (currentValue is EventScript)
            {
                eventScript = (EventScript)currentValue;
                script = eventScript == null ? "" : eventScript.Script;
            }
            if (script == null) script = "";
            script = (settings.GetValue(propertyInfo.Name, script) ?? script).ToString();
            if (!string.IsNullOrEmpty(script)
            && eventScript == null)
            {
                var propertyName = Regex.Replace(propertyInfo.Name, @"^On(.+)(Changed)?Script$", "$1");
                object[] parameters = [process, propertyName];
                eventScript = (EventScript)propertyInfo.PropertyType.GetConstructors().First().Invoke(parameters);
            }
            if (eventScript != null)
                if (!string.IsNullOrEmpty(script))
                {
                    eventScript.Script = script;
                    if (settings.SettingsRoot != null)
                    {
                        settings.SettingsRoot.OnLoadSettingsDone -= process.LoadSettingsDone;
                        settings.SettingsRoot.OnLoadSettingsDone += process.LoadSettingsDone;
                    }
                }
                else
                    eventScript = null;
            propertyInfo.SetValue(process, eventScript);
        }

        public class ScriptGlobals(IProcess process, object[]? parameters)
        {
            public object[]? Parameters = parameters;

            public IProcess _Process = process;

            public Performance? Performance = process.Performance;
        }
    }
}
