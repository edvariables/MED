using MED.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace MED
{
    public static class ProcessStatic
    {
        public static string test = "ciic";

        public static void AddHandler(IProvider handler_obj, string handler_field, IConsumer consumer, Type consumer_type, string consumer_method)
        {
            var memberInfo = handler_obj.GetType().GetMember(handler_field);
            if (memberInfo == null)
                throw new Exception($"Le type {handler_obj.GetType().FullName} n'a pas de delegate {handler_field}");
            var eventInfo = (System.Reflection.FieldInfo)memberInfo.GetValue(0);

            var miHandler = consumer_type.GetMethod(consumer_method);
            if (miHandler == null)
                throw new Exception($"Le type '{consumer_type.FullName}' n'a pas de méthode {consumer_method}");
            Delegate handler =
                 Delegate.CreateDelegate(eventInfo.FieldType,
                                         consumer,
                                         miHandler);
            //TODO  
            //eventInfo.RemoveEventHandler(this, handler);
            eventInfo.SetValue(handler_obj, handler);
        }
        public static void RemoveHandler(IProvider handler_obj, string handler_field, IConsumer consumer, Type consumer_type, string consumer_method)
        {
            //TODO
            var memberInfo = handler_obj.GetType().GetMember(handler_field);
            if (memberInfo == null)
                throw new Exception($"Le type {handler_obj.GetType().FullName} n'a pas de delegate {handler_field}");
            var eventInfo = (System.Reflection.FieldInfo)memberInfo.GetValue(0);


            var miHandler = consumer_type.GetMethod(consumer_method);
            if (miHandler == null)
                throw new Exception($"Le type '{consumer_type.FullName}' n'a pas de méthode {consumer_method}");
            Delegate handler =
                 Delegate.CreateDelegate(eventInfo.FieldType,
                                         consumer,
                                         miHandler);
            //eventInfo.RemoveEventHandler(this, miHandler);
            //TODO eventInfo.SetValue(handler_obj, handler);

        }
        /**
         * 
         */
        public static bool AddConsumer(IProvider provider, IConsumer consumer, string property = "ProcessState")
        {
            RemoveHandler(provider, $"On{property}Changed", consumer, consumer.GetType(), $"{property}Changed");
            AddHandler(provider, $"On{property}Changed", consumer, consumer.GetType(), $"{property}Changed");

            return true;
        }

        /***
         * 
         * 
         * */
        public static IProcess CreateProcess(JsonNode node, Performance? performance, Control? invokeHandler)
        {
            string processClass = node["ProcessClass"]?.GetValue<string>();
            string processLib = node["ProcessLib"]?.GetValue<string>();
            string name = node["Name"]?.GetValue<string>();
            if(String.IsNullOrEmpty(name) && String.IsNullOrEmpty(processClass))
            {
                throw new Exception($"Erreur dans la source JSON pour créer un process. Name et ProcessClass manquants. Chemin : {node.GetPath()}");
            }
            bool isAsynchrone = (bool)Parser.ObjectFromJsonNode(node["IsAsynchrone"], false);

            return CreateProcess(processClass, processLib, name, isAsynchrone, performance, invokeHandler);
        }

        /***
         * 
         * 
         * */
        public static IProcess CreateProcess(string processClass, string? processLib, string name, bool isAsynchrone, Performance? performance, Control? invokeHandler)
        {

            if (processClass == "")
                processClass = "MED.Process";

            return (IProcess)AssemblyLoader.CreateObjectInstance(processLib, processClass, [name, performance.Sub(name), invokeHandler, null, isAsynchrone]);
        }


        /**
         * 
         * */
        internal static List<MulticastDelegate> GetOnChangedDelegates(IProcess process, string propertyName = "")
        {
            List<MulticastDelegate> onChangedDelegates = new();
            foreach (var member in process.GetType().GetFields())
            {
                if (!member.FieldType.BaseType.Equals(typeof(MulticastDelegate))) continue;
                if (propertyName == ""
                    || member.Name == $"On{propertyName}Changed"
                    || member.Name == $"{propertyName}Changed")
                {
                    MulticastDelegate del = (MulticastDelegate)(member.GetValue(process));
                    if (del == null)
                        if (propertyName != "")
                            return onChangedDelegates;
                        else
                            continue;

                    onChangedDelegates.Add(del);
                    if (propertyName != "")
                        break;
                }
            }
            return onChangedDelegates;
        }
        internal static List<IProcess>? GetOnChangedConsumers(MulticastDelegate? onChangedDelegate)
        {
            if (onChangedDelegate == null)
                return null;
            List<IProcess> consumers = new();
            foreach (var invocation in onChangedDelegate.GetInvocationList())
            {
                if (invocation.Target is IProcess)
                    consumers.Add((IProcess)invocation.Target);
            }
            return consumers;
        }

        private static Dictionary<IProcess, List<Delegate>> _IsInvokingPropertyChanged = new();
        public static bool IsInvokingPropertyChanged(IProcess process, Delegate delegateMethod)
        {
            lock (_IsInvokingPropertyChanged)
            {
                return _IsInvokingPropertyChanged.ContainsKey(process)
                && _IsInvokingPropertyChanged[process].Contains(delegateMethod);
            }
        }
        public static void InvokePropertyChangedReset(IProcess? process = null)
        {
            lock (_IsInvokingPropertyChanged)
            {

                if (process == null)
                    _IsInvokingPropertyChanged.Clear();
                else if (_IsInvokingPropertyChanged.ContainsKey(process))
                    _IsInvokingPropertyChanged.Remove(process);

                //Clean disposed
                foreach (KeyValuePair<IProcess, List<Delegate>> item in _IsInvokingPropertyChanged.ToArray())
                    if (!item.Key.IsRunning
                        || item.Key.IsDisposed
                        || item.Value == null
                        || item.Value.Count == 0)
                    {
                        _IsInvokingPropertyChanged.Remove(item.Key);
                    }
            }
        }

        public static void InvokePropertyChanged(IProcess process, IProvider sender, Delegate delegateMethod, EventArgs e)
        {
            if ((process as IProvider).InvokeHandler == null || (process as IProvider).InvokeHandler.Disposing || (process as IProvider).InvokeHandler.IsDisposed)
                return;
            if (delegateMethod != null && process.IsRunning)
            {
                if (IsInvokingPropertyChanged(process, delegateMethod))
                {
                    process.Performance.Alert($"(already)IsInvokingPropertyChanged {delegateMethod.Method.Name}");
                    return;
                }
                try
                {
                    lock (_IsInvokingPropertyChanged)
                    {
                        if (!_IsInvokingPropertyChanged.ContainsKey(process))
                            _IsInvokingPropertyChanged.Add(process, new());
                        _IsInvokingPropertyChanged[process].Add(delegateMethod);
                    }

                    //if(!process.Equals(sender))
                    //    process.Performance.Debug($"InvokePropertyChanged TODO sender({sender}) != process({process}). process has priority over sender.");

                    foreach (var consumerDelegate in delegateMethod.GetInvocationList())
                    {
                        var consumer = consumerDelegate.Target as IConsumer;
                        //IsAsynchrone but if next Consumer is also asynchrone
                        bool invoke = (process as IConsumer).IsAsynchrone && !consumer.IsAsynchrone;
                        string invoke_str = invoke ? "Invoke" : "Call";

                        if ((process as IProvider).InvokeHandler.Disposing || (process as IProvider).InvokeHandler.IsDisposed
                            || (consumerDelegate.Target is Control && (consumerDelegate.Target as Control).IsDisposed)
                            || (consumerDelegate.Target is IProcess && (consumerDelegate.Target as IProcess).IsDisposed)
                            )
                        {
                            process.Performance.Alert($"IsDisposed ({consumer.GetType().Name}.{consumerDelegate.Method.Name})"
                                + $"[InvokeHandler : {(process as IProvider).InvokeHandler.Disposing || (process as IProvider).InvokeHandler.IsDisposed}"
                                + $", Target is Control : {(consumerDelegate.Target is Control && (consumerDelegate.Target as Control).IsDisposed)}"
                                + $", Target is IProcess : {(consumerDelegate.Target is IProcess && (consumerDelegate.Target as IProcess).IsDisposed)}]");
                            continue;
                        }
                        if (invoke)
                        {
                            process.Performance.Debug($"-> PInvoke({consumer.GetType().Name}.{consumerDelegate.Method.Name}, {process})");

                            (process as IProvider).InvokeHandler.Invoke(consumerDelegate, process /*sender*/, e);

                            process.Performance.Debug($"{invoke_str} done");
                        }
                        else
                        {
                            //Performance.Step($"-> {invoke_str}({consumer.GetType().Name}.{consumerDelegate.Method.Name})");
                            consumerDelegate.DynamicInvoke(process /*sender*/, e);
                        }

                    }
                }
                catch (ObjectDisposedException ex)
                {
                    process.Performance?.Error("ObjectDisposedException", ex);
                }
                catch (Exception ex)
                {
                    process.Performance?.Error("InvokePropertyChanged", ex);
                    int index = 0;
                    lock (_IsInvokingPropertyChanged)
                    {
                        foreach (var kvp in _IsInvokingPropertyChanged.ToArray())
                        {
                            if (kvp.Key == null)
                            {
                                _IsInvokingPropertyChanged.Clear();
                                break;
                            }
                            else
                                index++;
                        }
                    }
                }
                finally
                {
                    lock (_IsInvokingPropertyChanged)
                    {
                        if (_IsInvokingPropertyChanged.ContainsKey(process))
                        {
                            if (_IsInvokingPropertyChanged[process].Contains(delegateMethod))
                                _IsInvokingPropertyChanged[process].Remove(delegateMethod);
                            if (_IsInvokingPropertyChanged[process].Count == 0)
                                _IsInvokingPropertyChanged.Remove(process);
                        }
                    }
                }
            }
        }


        public static IProcess FindItem(IProcess processRef, string relativePath)
        {
            IProcess processItem = processRef;
            foreach (var itemName in relativePath.Split('/'))
            {
                if (itemName == "..")
                {
                    if (processItem is Process)
                        processItem = (IProcess)(processItem as Process).Consumer;
                    else
                        throw new Exception("Impossible de trouver le process parent");
                    continue;
                }
                bool found = false;
                if (processItem is IProcesses)
                    foreach (var item in (processItem as IProcesses).Items)
                        if (item.Name == itemName)
                        {
                            found = true;
                            processItem = item;
                            break;
                        }
                if (!found)
                    return null;
            }
            return processItem;
        }
        public static string GetRelativePath(IProcess processRef, IProcess processTo)
        {
            if (processRef == processTo)
                return ".";

            if (processRef is Process)
                if ((processRef as Process).Consumer == processTo)
                    return "..";
                else if (processTo is Process)
                    if ((processRef as Process).Consumer == (processTo as Process).Consumer)
                        return processTo.Name;
                    else if ((processTo as Process).Consumer is Process)
                        if ((processRef as Process).Consumer == ((processTo as Process).Consumer as Process).Consumer)
                            return ((processTo as Process).Consumer as Process).Name + "/" + processTo.Name;
            return processTo.Name;
        }
    }
}