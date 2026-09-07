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
using System.Xml.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxTokenParser;

namespace MED
{
    [Editor(typeof(EventScriptEditor), typeof(UITypeEditor))]
    [TypeConverter(typeof(EventScriptConvertor))]
    public class EventScript(IProcess process, string eventName)
    {
        [Browsable(false)]
        public IProcess Process { get; set; } = process;

        [Browsable(true)]
        [ReadOnly(true)]
        [Category("Script")]
        public string EventName { get; set; } = eventName;

        [Browsable(false)]
        public virtual string Icon { get; set; } = "Script";

        private string? _Script;

        [Browsable(true)]
        [Category("Script")]
        [Editor(typeof(EventScriptEditor), typeof(UITypeEditor))]
        public virtual string? Script
        {
            get => _Script;
            set
            {
                _Script = value;
                CompiledScript = null;
                _ParametersNames = null;
                if (OnScriptChanged != null)
                    OnScriptChanged(this, EventArgs.Empty);
            }
        }

        public EventHandler? OnScriptChanged;

        public string GetMethodName() => GetMethodName(Process, EventName);

        private Microsoft.CodeAnalysis.Scripting.Script? CompiledScript { get; set; }
        public List<object>? CompiledScriptErrors { get; set; }

        private Dictionary<string, Type>? _ParametersNames;

        [Browsable(true)]
        [ReadOnly(true)]
        [Category("Script")]
        public Dictionary<string, Type>? ParametersNames
        {
            get
            {
                if (_ParametersNames != null)
                    return _ParametersNames;
                return _ParametersNames = GetParametersNames(Process, EventName);
            }
            protected set => _ParametersNames = value;
        }

        [Browsable(true)]
        [ReadOnly(true)]
        [Category("Script")]
        public Dictionary<string, Type>? VariablesNames { get; protected set; }

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

            Dictionary<string, Type>? variablesNames;

            script = InsertVariablesNames(script, Process, ParametersNames, out HashSet<Assembly> assemblies, out variablesNames, parameters);

            VariablesNames = variablesNames;

            try
            {
                Process.Performance?.Sub("EventScript").Debug($"Compiling {EventName}...");
                Process.Performance?.Logger?.InvokeBufferChanged(this, EventArgs.Empty);
                using (var loader = new InteractiveAssemblyLoader())
                {
                    CompiledScript = CSharpScript.Create<bool>(script, ScriptOptions.Default.WithReferences(assemblies), globalsType: typeof(ScriptGlobals), assemblyLoader: loader);
                }
                var results = CompiledScript.Compile();
                if (results.Length > 0)
                {
                    var message = $"Script {EventName}: Compilation error";
                    foreach (var result in results)
                        message += $"\n{result}";
                    message += $"\n* Script :\n{script}";
                    Process.Performance?.Error(message);
                    CompiledScriptErrors = results.ToList<object>();
                }
                else
                {
                    CompiledScriptErrors = null;
                }
            }
            catch (Exception ex)
            {
                Process.Performance?.Error($"Script {EventName}: Compilation error in\n {script}", ex);
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
            if (CompiledScriptErrors != null)
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
            catch (AggregateException ex)
            {
                process.Performance?.Error($"Script : Evaluation error in \n{script.Code}\n--- {process} ---\n", ex.InnerException);
                return false;
            }
            catch (Exception ex)
            {
                process.Performance?.Error($"Script : Evaluation error in \n{script.Code}\n--- {process} ---\n", ex);
                return false;
            }
            return true;
        }

        private static string InsertVariablesNames(string script, IProcess process
            , Dictionary<string, Type>? parametersNames, out HashSet<Assembly> assemblies
            , out Dictionary<string, Type>? variablesNames, params object[]? parameters)
        {
            assemblies = new();
            variablesNames = new();

            if (parametersNames == null || parameters == null)
                return script;
            int paramIndex = 0;
            StringBuilder scriptAdd = new();
            HashSet<string> namespaces = new();

            // namespaces
            namespaces.Add(typeof(Exception).Namespace ?? "");
            namespaces.Add(typeof(PointF).Namespace ?? "");

            // assemblies
            //assemblies.Add(typeof(Exception).Assembly);

            foreach (var (name, paramType) in parametersNames)
            {
                if (paramType.Namespace != null && !namespaces.Contains(paramType.Namespace))
                    namespaces.Add(paramType.Namespace);
                if (!assemblies.Contains(paramType.Assembly))
                    assemblies.Add(paramType.Assembly);

                scriptAdd.AppendLine($"var {name} = ({paramType.FullName})_p_[{paramIndex}];");
                variablesNames.Add(name, paramType);

                if (paramType.Equals(typeof(PropertyChangedEventArgs)))
                {
                    scriptAdd.AppendLine($"var property = {name}.Property;");
                    variablesNames.Add("property", typeof(string));
                }

                paramIndex++;
            }

            //Process cast from Globals._Process
            var processType = process.GetType();
            if (processType.Namespace != null
                && !namespaces.Contains(processType.Namespace))
                namespaces.Add(processType.Namespace);
            if (!assemblies.Contains(processType.Assembly))
                assemblies.Add(processType.Assembly);
            scriptAdd.AppendLine($"var process = ({processType.FullName})_process;");
            variablesNames.Add("process", processType);

            //ScriptGlobals properties in variablesNames
            foreach (var property in typeof(ScriptGlobals).GetFields(BindingFlags.Public | BindingFlags.Instance))
                if (!property.Name.StartsWith('_'))
                    variablesNames.Add(property.Name, property.FieldType);

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
            script = Regex.Replace(script, @"\/\*.+?\*\/", "", RegexOptions.Multiline);
            script = Regex.Replace(script, @"^//.*([\n\r]|$)", "");
            return script;
        }

        public static EventScript GetNew(IProcess process, string eventName, ITypeDescriptorContext? context = null)
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
            public object[]? _p_ = parameters;

            public IProcess _process = process;

            public Performance? perf = process.Performance;
        }
    }
}
