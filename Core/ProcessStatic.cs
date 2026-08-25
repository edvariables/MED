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
    /**
     * static class ProcessStatic
     * <summary>Tools for process</summary>
     * */
    public static class ProcessStatic
    {
        public static Delegate? GetHandlerDelegate(IProvider handler_obj, string handler_field, Type consumer_type, string consumer_method, out FieldInfo? eventInfo, out MethodInfo? miHandler)
        {
            eventInfo = null;
            miHandler = null;
            var memberInfo = handler_obj.GetType().GetMember(handler_field);
            if (memberInfo == null)
                throw new Exception($"Le type {handler_obj.GetType().FullName} n'a pas de delegate {handler_field}");
            object? eventInfoO = memberInfo.GetValue(0);
            if (eventInfoO == null)
                return null;
            eventInfo = (System.Reflection.FieldInfo)eventInfoO;

            miHandler = consumer_type.GetMethod(consumer_method);
            if (miHandler == null)
                throw new Exception($"Le type '{consumer_type.FullName}' n'a pas de méthode {consumer_method}");

            var currentEventValue = eventInfo.GetValue(handler_obj);
            if (currentEventValue != null && currentEventValue is Delegate handlerDelegates)
                return handlerDelegates;

            return null;
        }

        public static void AddHandler(IProvider handler_obj, string handler_field, IConsumer consumer, Type consumer_type, string consumer_method)
        {
            var handlerDelegates = GetHandlerDelegate(handler_obj, handler_field, consumer_type, consumer_method, out FieldInfo? eventInfo, out MethodInfo? miHandler);
            if (eventInfo == null || miHandler == null)
                return;
            if (handlerDelegates != null)
            {
                foreach (var targetHandler in handlerDelegates.GetInvocationList())
                    if (targetHandler.Target == consumer && targetHandler.Method.Name == consumer_method)
                        return;//Exists
            }

            Delegate handler =
                 Delegate.CreateDelegate(eventInfo.FieldType,
                                         consumer,
                                         miHandler);
            handler = Delegate.Combine(handlerDelegates, handler);
            eventInfo.SetValue(handler_obj, handler);

            if (handler_obj is Process process)
                PropertiesConsumersCacheReset(process, consumer_method);
        }
        public static void RemoveHandler(IProvider handler_obj, string handler_field, IConsumer consumer, Type consumer_type, string consumer_method)
        {
            var handlerDelegates = GetHandlerDelegate(handler_obj, handler_field, consumer_type, consumer_method, out FieldInfo? eventInfo, out MethodInfo? miHandler);
            if (eventInfo == null || miHandler == null)
                return;
            if (handlerDelegates != null)
            {
                Delegate? handler = null;

                foreach (var targetHandler in handlerDelegates.GetInvocationList())
                    if (!(targetHandler.Target == consumer && targetHandler.Method.Name == consumer_method))
                        handler = Delegate.Combine(handler, targetHandler);

                eventInfo.SetValue(handler_obj, handler);
            }
            if (handler_obj is Process process)
                PropertiesConsumersCacheReset(process, consumer_method);

        }
        /**
         * 
         */
        public static bool AddConsumer(IProvider provider, IConsumer consumer, string property = "ProcessState")
        {
            RemoveConsumer(provider, consumer, property);
            AddHandler(provider, $"On{property}Changed", consumer, consumer.GetType(), $"{property}Changed");

            return true;
        }
        /**
         * 
         */
        public static bool RemoveConsumer(IProvider provider, IConsumer consumer, string property = "ProcessState")
        {
            RemoveHandler(provider, $"On{property}Changed", consumer, consumer.GetType(), $"{property}Changed");

            return true;
        }

        #region _PropertiesDelegatesConsumers
        public static void PropertiesConsumersCacheReset(Process provider, string propertyName = "")
        {
            if (provider._PropertiesDelegatesConsumers != null)
            {
                if (propertyName != "")
                {
                    if (propertyName.StartsWith("On"))
                        propertyName = propertyName[2..];
                    if (propertyName.EndsWith("Changed"))
                        propertyName = propertyName[..^"Changed".Length];

                    provider._PropertiesDelegatesConsumers.Remove(propertyName);
                }
                else
                    provider._PropertiesDelegatesConsumers = null;
            }
        }
        /**
         * 
         * */
        public static void RemovePropertyDelegateConsumers(Process provider, string propertyName = "")
        {
            if (provider._PropertiesDelegatesConsumers != null)
                if (propertyName != "")
                    provider._PropertiesDelegatesConsumers.Remove(propertyName);
                else
                    provider._PropertiesDelegatesConsumers = [];
        }
        /**
         * 
         * */
        public static void CleanPropertiesDelegatesConsumers(Process provider, string propertyName = "")
        {
            if (provider._PropertiesDelegatesConsumers == null)
                return;
            foreach (var kvp in GetPropertiesDelegatesConsumers(provider, propertyName).ToArray())
            {
                List<IProcess> processes = kvp.Value.Value;
                foreach (var iProcess in processes.ToArray())
                {
                    if (iProcess == null)
                        continue;
                    if ((iProcess is Process process) && process.IsDisposed
                        || (iProcess is Control control) && control.IsDisposed)
                    {
                        processes.Remove(iProcess);
                        if (processes.Count == 0)
                            provider._PropertiesDelegatesConsumers.Remove(propertyName);
                    }
                }
            }
        }

        /**
         * 
         * */
        public static KeyValuePair<MulticastDelegate, List<IProcess>> GetPropertyDelegateConsumers(Process provider, string propertyName = "", bool evenEmpty = true)
        {
            var dic = GetPropertiesDelegatesConsumers(provider, propertyName, evenEmpty);
            if (dic.Count == 0)
                return new();
            return dic.First().Value;
        }

        /**
         * 
         * */
        public static Dictionary<string, KeyValuePair<MulticastDelegate, List<IProcess>>> GetPropertiesDelegatesConsumers(Process provider, string propertyName = "", bool evenEmpty = true)
        {
            if (provider._PropertiesDelegatesConsumers != null)
            {
                if (propertyName != "")
                {
                    if (provider._PropertiesDelegatesConsumers.TryGetValue(propertyName, out KeyValuePair<MulticastDelegate, List<IProcess>> pair))
                    {
                        Dictionary<string, KeyValuePair<MulticastDelegate, List<IProcess>>> dic = [];
                        dic.Add(propertyName, pair);
                        return dic;
                    }
                }
                else
                    return provider._PropertiesDelegatesConsumers;
            }
            Dictionary<string, KeyValuePair<MulticastDelegate, List<IProcess>>> propertiesDelegatesConsumers = [];
            foreach (var onChangedDelegate in GetOnChangedDelegates(provider, propertyName))
            {
                string prop = onChangedDelegate.GetMethodInfo().Name;
                if (prop.StartsWith("On"))
                    prop = prop[2..];
                if (prop.EndsWith("Changed"))
                    prop = prop[..^"Changed".Length];

                List<IProcess>? consumers;
                if ((consumers = GetOnChangedConsumers(onChangedDelegate)) != null || evenEmpty)
                {
                    consumers ??= [];
                    KeyValuePair<MulticastDelegate, List<IProcess>> delegatesConsumers = new(onChangedDelegate, consumers);
                    propertiesDelegatesConsumers.Add(prop, delegatesConsumers);
                }
            }
            if (propertyName != "")
            {
                Dictionary<string, KeyValuePair<MulticastDelegate, List<IProcess>>> dic = [];

                if (!propertiesDelegatesConsumers.TryGetValue(propertyName, out KeyValuePair<MulticastDelegate, List<IProcess>> pair))
                    return dic;
                dic.Add(propertyName, pair);

                provider._PropertiesDelegatesConsumers ??= [];
                provider._PropertiesDelegatesConsumers[propertyName] = dic.First().Value;
                return dic;
            }

            return provider._PropertiesDelegatesConsumers = propertiesDelegatesConsumers;
        }
        #endregion
        /***
         * 
         * 
         * */
        public static IProcess? CreateProcess(JsonNode node, Performance? performance, Control? invokeHandler)
        {
            string? processClass = node["ProcessClass"]?.GetValue<string>();
            string? processLib = node["ProcessLib"]?.GetValue<string>();
            string? name = node["Name"]?.GetValue<string>();
            if (String.IsNullOrEmpty(name) && String.IsNullOrEmpty(processClass))
            {
                throw new Exception($"Erreur dans la source JSON pour créer un process. Name et ProcessClass manquants. Chemin : {node.GetPath()}");
            }
            bool isAsynchrone = (bool)(Parser.ObjectFromJsonNode(node["IsAsynchrone"] ?? false, false) ?? false);
            if (processClass == null || name == null)
                return null;
            return CreateProcess(processClass, processLib, name, isAsynchrone, performance, invokeHandler);
        }

        /***
         * 
         * 
         * */
        public static IProcess? CreateProcess(string processClass, string? processLib, string name, bool isAsynchrone, Performance? performance, Control? invokeHandler)
        {
            if (processClass == "")
                processClass = "MED.Process";
            if (processLib == null)
                processLib = "";
            object[] paramsObjects = [name, performance?.Sub(name), invokeHandler, null, isAsynchrone];
            return (IProcess?)AssemblyLoader.CreateObjectInstance(processLib, processClass, paramsObjects);
        }


        /**
         * 
         * */
        internal static List<MulticastDelegate> GetOnChangedDelegates(IProcess process, string propertyName = "")
        {
            List<MulticastDelegate> onChangedDelegates = new();
            foreach (var member in process.GetType().GetFields())
            {
                if (member.FieldType.BaseType == null
                    || !member.FieldType.BaseType.Equals(typeof(MulticastDelegate))) continue;
                if (propertyName == ""
                    || member.Name == $"On{propertyName}Changed"
                    || member.Name == $"{propertyName}Changed")
                {
                    MulticastDelegate? del = (MulticastDelegate?)(member.GetValue(process));
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
                else 
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

        public static void InvokePropertyChanged(IProcess? process, IProvider? sender, Delegate? delegateMethod, EventArgs? e)
        {
            if (process is not IProvider provider || provider.InvokeHandler == null || provider.InvokeHandler.Disposing || provider.InvokeHandler.IsDisposed)
                return;
            if (delegateMethod != null && process.IsRunning)
            {
                if (IsInvokingPropertyChanged(process, delegateMethod))
                {
                    process.Performance?.Alert($"(already)IsInvokingPropertyChanged {delegateMethod.Method.Name}");
                    process.Performance?.StackTrace("InvokePropertyChanged");
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
                        if (consumer == null)
                            continue;
                        //IsAsynchrone but if next Consumer is also asynchrone
                        bool invoke = ((IConsumer)process).IsAsynchrone && !consumer.IsAsynchrone;
                        string invoke_str = invoke ? "Invoke" : "Call";
                        var invokeHandler = ((IProvider)(process)).InvokeHandler;
                        if (invokeHandler == null || invokeHandler.Disposing || invokeHandler.IsDisposed
                            || (consumerDelegate.Target is Control && ((Control)consumerDelegate.Target).IsDisposed)
                            || (consumerDelegate.Target is IProcess && ((IProcess)consumerDelegate.Target).IsDisposed)
                            )
                        {
                            process.Performance?.Alert($"IsDisposed ({consumer.GetType().Name}.{consumerDelegate.Method.Name})"
                                + $"[InvokeHandler : {invokeHandler == null || invokeHandler.Disposing || invokeHandler.IsDisposed}"
                                + $", Target is Control : {(consumerDelegate.Target is Control targetControl && targetControl.IsDisposed)}"
                                + $", Target is IProcess : {(consumerDelegate.Target is IProcess targetProcess && targetProcess.IsDisposed)}]");
                            continue;
                        }
                        if (invoke)
                        {
                            process.Performance?.Debug($"-> PInvoke({consumer.GetType().Name}.{consumerDelegate.Method.Name}, {process})");

                            invokeHandler.Invoke(consumerDelegate, process /*sender*/, e);

                            process.Performance?.Debug($"{invoke_str} done");
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
                        if (_IsInvokingPropertyChanged.TryGetValue(process, out List<Delegate>? list))
                        {
                            list.Remove(delegateMethod);
                            if (list.Count == 0)
                                _IsInvokingPropertyChanged.Remove(process);
                        }
                    }
                }
            }
        }


        public static IProcess? FindItem(IProcess processRef, string relativePath)
        {
            IProcess? processItem = processRef;
            foreach (var itemName in relativePath.Split('/'))
            {
                if (itemName == "..")
                {
                    if (processItem is Process proc)
                        processItem = proc.Consumer;
                    else
                        throw new Exception("Impossible de trouver le process parent");
                    continue;
                }
                bool found = false;
                if (processItem is IProcesses processesItem)
                    foreach (var item in processesItem.Items)
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
                if (((Process)processRef).Consumer == processTo)
                    return "..";
                else if (processTo is Process)
                    if (((Process)processRef).Consumer == ((Process)processTo).Consumer)
                        return processTo.Name;
                    else if (((Process)processTo).Consumer is Process processToConsumer)
                        if (((Process)processRef).Consumer == processToConsumer.Consumer)
                            return processToConsumer.Name + "/" + processTo.Name;
            return processTo.Name;
        }
    }
}