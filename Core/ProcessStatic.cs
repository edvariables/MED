using MED.Core;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
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
        /**
         * GetHandlerDelegate
         * Initialise variables for handler
         * 
         * */
        private static Delegate? GetHandlerDelegate(IProvider handler_obj, string handler_field, Type consumer_type, object consumer_method, out FieldInfo? eventInfo, out MethodInfo? miHandler)
        {
            eventInfo = null;
            miHandler = null;
            var memberInfo = handler_obj.GetType().GetMember(handler_field);
            if (memberInfo == null || memberInfo.Length == 0)
                throw new Exception($"Le type {handler_obj.GetType().FullName} n'a pas de delegate {handler_field}");
            object? eventInfoO = memberInfo.GetValue(0);
            if (eventInfoO == null)
                return null;
            eventInfo = (System.Reflection.FieldInfo)eventInfoO;

            if (consumer_method is string consumerMethodName)
            {
                miHandler = consumer_type.GetMethod(consumerMethodName);
                if (miHandler == null)
                    throw new Exception($"Le type '{consumer_type.FullName}' n'a pas de méthode {consumerMethodName}");
            }
            else if (consumer_method is Delegate consumerDelegate)
                miHandler = consumerDelegate.Method;
            else
                throw new Exception($"Le paramètre 'consumer_method' n'est ni un nom de méthode ni un delegate");

            var currentEventValue = eventInfo.GetValue(handler_obj);
            if (currentEventValue != null && currentEventValue is Delegate handlerDelegates)
                return handlerDelegates;

            return null;
        }

        /**
         * AddHandler
         * Handles a property change event for a consumer
         * 
         * */
        public static Delegate? AddHandler(IProvider handler_obj, string handler_field, IConsumer consumer, Type consumer_type, object consumer_method)
        {
            var handlerDelegates = GetHandlerDelegate(handler_obj, handler_field, consumer_type, consumer_method, out FieldInfo? eventInfo, out MethodInfo? miHandler);
            if (eventInfo == null || miHandler == null)
                return null;
            string consumerMethodName;
            if (consumer_method is string)
                consumerMethodName = (string)consumer_method;
            else
                consumerMethodName = ((Delegate)consumer_method).Method.Name;
            //Check if exists
            if (handlerDelegates != null)
            {
                foreach (var targetHandler in handlerDelegates.GetInvocationList())
                    if (targetHandler.Target == consumer && targetHandler.Method.Name == consumerMethodName)
                        return handlerDelegates;//Exists
            }
            Delegate handler;
            try
            {
                handler = Delegate.CreateDelegate(eventInfo.FieldType,
                                             consumer,
                                             miHandler);
            }
            catch(Exception ex)
            {
                consumer.Performance?.Error($"{eventInfo.FieldType} {consumer} {miHandler}", ex);
                return null;
            }
            handler = Delegate.Combine(handlerDelegates, handler);
            eventInfo.SetValue(handler_obj, handler);

            if (handler_obj is Process process)
                PropertiesConsumersCacheReset(process, consumerMethodName);

            return handler;
        }

        /**
         * RemoveHandler
         * Remove handler of a property change event for a consumer
         * 
         * */
        public static Delegate? RemoveHandler(IProvider handler_obj, string handler_field, IConsumer consumer, Type consumer_type, object consumer_method)
        {
            var handlerDelegates = GetHandlerDelegate(handler_obj, handler_field, consumer_type, consumer_method, out FieldInfo? eventInfo, out MethodInfo? miHandler);
            if (eventInfo == null || miHandler == null)
                return null;
            string consumerMethodName;
            if (consumer_method is string)
                consumerMethodName = (string)consumer_method;
            else
                consumerMethodName = ((Delegate)consumer_method).Method.Name;
            Delegate? handler = null;
            if (handlerDelegates != null)
            {
                foreach (var targetHandler in handlerDelegates.GetInvocationList())
                    if (!(targetHandler.Target == consumer && targetHandler.Method.Name == consumerMethodName))
                        handler = Delegate.Combine(handler, targetHandler);

                eventInfo.SetValue(handler_obj, handler);
            }
            if (handler_obj is Process process)
                PropertiesConsumersCacheReset(process, consumerMethodName);
            return handler ?? handlerDelegates;
        }

        /**
         * 
         */
        public static string ParsePropertyAndConsumerMethod(string property, out string consumerMethodName, out string? providerSubProperty)
        {
            var propertyData = property.Split('.');
            var providerProperty = propertyData[0];
            if (propertyData.Length > 1)
            {
                providerSubProperty = property.Substring(providerProperty.Length + 1);
                consumerMethodName = providerProperty + providerSubProperty;
            }
            else
            {
                providerSubProperty = null;
                consumerMethodName = providerProperty;
            }
            return providerProperty;
        }

        /**
         * 
         */
        public static bool AddConsumer(IProvider provider, IConsumer consumer, string property = "ProcessState", MulticastDelegate? consumerDelegate = null)
        {
            RemoveConsumer(provider, consumer, property, consumerDelegate);

            property = ParsePropertyAndConsumerMethod(property, out string consumerMethodName, out string? providerSubProperty);
            var providerDelegate = AddHandler(provider, $"On{property}Changed", consumer, consumer.GetType(), consumerDelegate == null ? $"{consumerMethodName}Changed" : consumerDelegate);
            if (providerDelegate == null)
                return false;

            if (!String.IsNullOrEmpty(providerSubProperty)
                && provider is Process process
                && consumerDelegate != null)
            {
                process._PropertiesDelegatesConsumers ??= [];

                if (process._PropertiesDelegatesConsumers.TryGetValue(consumerMethodName, out KeyValuePair<MulticastDelegate, Dictionary<IProcess, Delegate>> pair))
                {
                    if (!pair.Value.ContainsKey(consumer))
                        pair.Value.Add(consumer, consumerDelegate);
                }
                else
                    pair = new((MulticastDelegate)providerDelegate, new() { { consumer, consumerDelegate } });

                process._PropertiesDelegatesConsumers[consumerMethodName] = pair;
            }
            return true;
        }
        /**
         * 
         */
        public static bool RemoveConsumer(IProvider provider, IConsumer consumer, string property = "ProcessState", MulticastDelegate? consumerDelegate = null)
        {
            property = ParsePropertyAndConsumerMethod(property, out string consumerMethodName, out string? providerSubProperty);

            var providerDelegate = RemoveHandler(provider, $"On{property}Changed", consumer, consumer.GetType(), consumerDelegate == null ? $"{consumerMethodName}Changed" : consumerDelegate);


            if (!String.IsNullOrEmpty(providerSubProperty)
                && provider is Process process
                && providerDelegate != null
                && process._PropertiesDelegatesConsumers != null)
            {
                if (process._PropertiesDelegatesConsumers.TryGetValue(consumerMethodName, out KeyValuePair<MulticastDelegate, Dictionary<IProcess, Delegate>> pair))
                {
                    if (pair.Value.ContainsKey(consumer))
                    {
                        pair.Value.Remove(consumer);
                        if (pair.Value.Count == 0)
                            process._PropertiesDelegatesConsumers.Remove(consumerMethodName);
                        else
                            process._PropertiesDelegatesConsumers[consumerMethodName] = pair;
                    }
                }

            }
            return true;
        }

        #region PropertiesDelegatesConsumers
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
            foreach (var (property, kvp) in GetPropertiesDelegatesConsumers(provider, propertyName).ToArray())
            {
                Dictionary<IProcess, Delegate> processes = kvp.Value;
                foreach (var (iProcess, consumerDelegate) in processes.ToArray())
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
        public static KeyValuePair<MulticastDelegate, Dictionary<IProcess, Delegate>> GetPropertyDelegateConsumers(Process provider, string propertyName = "", bool evenEmpty = true)
        {
            var dic = GetPropertiesDelegatesConsumers(provider, propertyName, evenEmpty);
            if (dic.Count == 0)
                return new();
            return dic.First().Value;
        }

        /**
         * 
         * */
        public static Dictionary<string, KeyValuePair<MulticastDelegate, Dictionary<IProcess, Delegate>>> GetPropertiesDelegatesConsumers(Process provider, string propertyName = "", bool evenEmpty = true)
        {
            if (provider._PropertiesDelegatesConsumers != null)
            {
                if (propertyName != "")
                {
                    if (provider._PropertiesDelegatesConsumers.TryGetValue(propertyName, out KeyValuePair<MulticastDelegate, Dictionary<IProcess, Delegate>> pair))
                    {
                        Dictionary<string, KeyValuePair<MulticastDelegate, Dictionary<IProcess, Delegate>>> dic = [];
                        dic.Add(propertyName, pair);
                        return dic;
                    }
                }
                else
                    return provider._PropertiesDelegatesConsumers;
            }
            Dictionary<string, KeyValuePair<MulticastDelegate, Dictionary<IProcess, Delegate>>> propertiesDelegatesConsumers = [];
            foreach (var onChangedDelegate in GetOnChangedDelegates(provider, propertyName))
            {
                string prop = onChangedDelegate.GetMethodInfo().Name;
                if (prop.StartsWith("On"))
                    prop = prop[2..];
                if (prop.EndsWith("Changed"))
                    prop = prop[..^"Changed".Length];

                Dictionary<IProcess, Delegate>? consumers;
                if ((consumers = GetOnChangedConsumers(onChangedDelegate)) != null || evenEmpty)
                {
                    consumers ??= [];
                    KeyValuePair<MulticastDelegate, Dictionary<IProcess, Delegate>> delegatesConsumers = new(onChangedDelegate, consumers);
                    propertiesDelegatesConsumers.Add(prop, delegatesConsumers);
                }
            }
            if (propertyName != "")
            {
                Dictionary<string, KeyValuePair<MulticastDelegate, Dictionary<IProcess, Delegate>>> dic = [];

                if (!propertiesDelegatesConsumers.TryGetValue(propertyName, out KeyValuePair<MulticastDelegate, Dictionary<IProcess, Delegate>> pair))
                    return dic;
                dic.Add(propertyName, pair);

                provider._PropertiesDelegatesConsumers ??= [];
                provider._PropertiesDelegatesConsumers[propertyName] = dic.First().Value;
                return dic;
            }

            return provider._PropertiesDelegatesConsumers = propertiesDelegatesConsumers;
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
        internal static Dictionary<IProcess, Delegate>? GetOnChangedConsumers(MulticastDelegate? onChangedDelegate)
        {
            if (onChangedDelegate == null)
                return null;
            Dictionary<IProcess, Delegate> consumers = new();
            foreach (var invocation in onChangedDelegate.GetInvocationList())
            {
                if (invocation.Target is IProcess)
                    consumers.Add((IProcess)invocation.Target, invocation);
            }
            return consumers;
        }
        #endregion

        #region Invoking
        private static Dictionary<IProcess, List<Delegate>> _IsInvokingPropertyChanged = [];
        public static bool IsInvokingPropertyChanged(IProcess process, Delegate delegateMethod)
        {
            lock (_IsInvokingPropertyChanged)
            {
                return _IsInvokingPropertyChanged.ContainsKey(process)
                && _IsInvokingPropertyChanged[process].Contains(delegateMethod);
            }
        }
        public static void InvokingPropertyChangedReset(IProcess? process = null)
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

        public static void InvokePropertyChanged(IProcess? process, IProvider? sender, Delegate? delegateMethod, EventArgs? eventArgs, string? propertyDomain = null)
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

                    Dictionary<IProcess, Delegate> targets = [];
                    //For specific (sub)property, consumer must be registred via AddConsumer
                    if (!String.IsNullOrEmpty(propertyDomain)
                        && eventArgs is PropertyChangedEventArgs propertyChangedEventArgs
                        && delegateMethod is MulticastDelegate multicastDelegate
                        && process is Process pProcess
                        && pProcess._PropertiesDelegatesConsumers != null
                    )
                    {
                        if (pProcess._PropertiesDelegatesConsumers.TryGetValue($"{propertyDomain}{propertyChangedEventArgs.Property}", out KeyValuePair<MulticastDelegate, Dictionary<IProcess, Delegate>> pair))
                            targets = pair.Value;
                    }
                    else
                    {   //Targets from InvocationList
                        foreach (var consumerDelegate in delegateMethod.GetInvocationList())
                        {
                            var consumer = consumerDelegate.Target as IConsumer;
                            if (consumer == null)
                                continue;
                            targets.Add(consumer, consumerDelegate);
                        }
                    }

                    var invokeHandler = ((IProvider)(process)).InvokeHandler;

                    foreach (var (target, consumerDelegate) in targets)
                    {
                        var consumer = target as IConsumer;
                        if (consumer == null)
                            continue;
                        //IsAsynchrone but if next Consumer is also asynchrone
                        bool invoke = ((IConsumer)process).IsAsynchrone && !consumer.IsAsynchrone;
                        string invoke_str = invoke ? "Invoke" : "Call";
                        if (invokeHandler == null || invokeHandler.Disposing || invokeHandler.IsDisposed
                            || (target is Control targetControl && targetControl.IsDisposed)
                            || (target is IProcess targetProcess && targetProcess.IsDisposed)
                            )
                        {
                            process.Performance?.Alert($"IsDisposed ({consumer.GetType().Name}.{consumerDelegate.Method.Name})"
                                + $"[InvokeHandler : {invokeHandler == null || invokeHandler.Disposing || invokeHandler.IsDisposed}"
                                + $", Target is Control : {(target is Control targetControl2 && targetControl2.IsDisposed)}"
                                + $", Target is IProcess : {(target is IProcess targetProcess2 && targetProcess2.IsDisposed)}]");
                            continue;
                        }

                        if (invoke)
                        {
                            process.Performance?.Debug($"-> PInvoke({consumer.GetType().Name}.{consumerDelegate.Method.Name}, {process})");

                            invokeHandler.Invoke(consumerDelegate, process /*sender*/, eventArgs);

                            process.Performance?.Debug($"{invoke_str} done");
                        }
                        else
                        {
                            //Performance.Step($"-> {invoke_str}({consumer.GetType().Name}.{consumerDelegate.Method.Name})");
                            consumerDelegate.DynamicInvoke(process /*sender*/, eventArgs);
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
        #endregion

        #region CreateProcess
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
        #endregion


        #region Tools
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
        #endregion

        /**
         * GetGameController
         * Search a GameController in processes tree
         * */
        public static IGameController? GetGameController(IProcess process) => GetGameController(process, new());
        /**
         * GetGameController
         * Search a GameController in processes tree
         * */
        private static IGameController? GetGameController(IProcess process, HashSet<IProcess> ignoreProcesses)
        {
            if (process is ProcessForm processForm)
            {
                ignoreProcesses.Add(process);
                return GetGameController(processForm.Project, ignoreProcesses);
            }
            if (process is IGameController gameController0)
                return gameController0;

            if (process is IProcesses processes)
                foreach (var item in processes.Items)
                    if (item.Enabled)
                        if (item is IGameController gameController)
                            return gameController;
                        else if (item is IProcesses subProcesses
                            && !ignoreProcesses.Contains(subProcesses))
                        {   //Deep search
                            ignoreProcesses.Add(process);
                            var found = GetGameController(subProcesses, ignoreProcesses);
                            if (found != null)
                                return found;
                        }
            if (process.Consumer != null
            && process != process.Consumer
            && !ignoreProcesses.Contains(process.Consumer))
                return GetGameController(process.Consumer, ignoreProcesses);

            return null;
        }
    }
}