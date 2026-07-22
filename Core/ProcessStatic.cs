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
            //eventInfo.RemoveEventHandler(this, handler);
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
        public static IProcess CreateProcess(JsonNode node, Performance performance, Control invokeHandler)
        {
            string processClass = node["ProcessClass"].GetValue<string>();
            string processLib = node["ProcessLib"].GetValue<string>();
            string name = node["Name"].GetValue<string>();
            bool isAsynchrone = (bool)Parser.ObjectFromJsonNode(node["IsAsynchrone"], false);

            if (processClass == "")
                processClass = MethodBase.GetCurrentMethod().DeclaringType.FullName;

            try
            {
                IProcess item = (IProcess)AssemblyLoader.CreateObjectInstance(processLib, processClass, [name, performance.Sub(name), invokeHandler, null, isAsynchrone]);
                //IProcess item = (IProcess)Activator.CreateInstance(processLib, processClass, [name, performance.Sub(name), invokeHandler, null, isAsynchrone]);
                //Process item = new Process(name, performance.Sub(name), invokeHandler, null, isAsynchrone);
                return item;
            }
            catch (Exception ex)
            {
                return null;
                //                throw ex;
            }
            return null;
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
        internal static List<IProcess> GetOnChangedConsumers(MulticastDelegate onChangedDelegate)
        {
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
            return _IsInvokingPropertyChanged.ContainsKey(process)
                && _IsInvokingPropertyChanged[process].Contains(delegateMethod);
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
                    if (!_IsInvokingPropertyChanged.ContainsKey(process))
                        _IsInvokingPropertyChanged[process] = new();
                    _IsInvokingPropertyChanged[process].Add(delegateMethod);

                    //if(!process.Equals(sender))
                    //    process.Performance.Debug($"InvokePropertyChanged TODO sender({sender}) != process({process}). process has priority over sender.");

                    foreach (var consumerDelegate in delegateMethod.GetInvocationList())
                    {
                        var consumer = consumerDelegate.Target as IConsumer;
                        //IsAsynchrone but if next Consumer is also asynchrone
                        bool invoke = (process as IConsumer).IsAsynchrone && !consumer.IsAsynchrone;
                        string invoke_str = invoke ? "Invoke" : "Call";

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
                catch (Exception ex)
                {
                    process.Performance?.Error("InvokePropertyChanged", ex);
                }
                finally
                {
                    _IsInvokingPropertyChanged[process].Remove(delegateMethod);
                    if (_IsInvokingPropertyChanged[process].Count == 0)
                        _IsInvokingPropertyChanged.Remove(process);
                }
            }
        }
    }

}
