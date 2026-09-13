using DevDecoder.HIDDevices.Controllers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace MED.GameController
{
    public abstract class GameController : Process, IGameController
    {
        public GameController(string name, Performance? performance = null, Control? invokeHandler = null, IConsumer? consumer = null, bool isAsynchrone = true)
            : base(name, performance, invokeHandler, consumer, isAsynchrone)
        {
            ProcessIconDefault = "Button";
        }

        public readonly string PropertyDomain = "GameController";

        public delegate void GameControllerChangedDelegate(IGameController sender, PropertyChangedEventArgs e);

        public GameControllerChangedDelegate? GameControllerChanged;

        [Browsable(false)]
        public override GameControllerScript? OnGameControllerScript { get => base.OnGameControllerScript; set => base.OnGameControllerScript = value; }

        [Browsable(true)]
        [Category("Controller")]
        public UsagePropertiesMap UsagePropertiesMap { get; } = [];

        public virtual void OnControllerChanged(string controlKey, object? value)
        {
            if (FPSMax > 0)
                OnControllerChanged(this, new(controlKey, value));
            else
                lock (ChangedQueue)
                    if (ChangedQueue.ContainsKey(controlKey))
                        ChangedQueue[controlKey] = value;
                    else
                        ChangedQueue.Add(controlKey, value);
        }
        public virtual void OnControllerChanged(IGameController sender, PropertyChangedEventArgs e)
        {
            e.Property = UsagePropertiesMap.GetPropertyUsage(e.Property);

            InvokePropertyChanged(sender, GameControllerChanged, e, PropertyDomain);
        }

        public override bool AddConsumer(IConsumer consumer, string controllerProperty, MulticastDelegate? consumerDelegate = null) => base.AddConsumer(consumer, $"{PropertyDomain}.{controllerProperty}", consumerDelegate);
        public override bool RemoveConsumer(IConsumer consumer, string controllerProperty, MulticastDelegate? consumerDelegate = null) => base.RemoveConsumer(consumer, $"{PropertyDomain}.{controllerProperty}", consumerDelegate);

        [Browsable(true)]
        [Category("Controller")]
        public Dictionary<string, object?> ControllerPropertiesValues { get; set; } = [];
        public virtual object? GetControllerPropertyValue(string usage)
        {
            double maxDouble = 0D;
            bool isDouble = false;
            //float maxFloat = 0F;
            //int maxInt = 0;
            bool maxBool = false;
            bool isBool = false;
            if (UsagePropertiesMap.TryGetValue(usage, out UsagePropertiesMapItem? item))
                if (item.IsNegative)
                {
                    if (ControllerPropertiesValues.TryGetValue(item.Properties[0].Substring(1), out object? value1))
                        if (value1 is double dbl && dbl > -DoubleLimitZero)
                            return ParseValue(0D, item.UsageType, item.KeepNegativeValue);
                        else
                            return ParseValue(value1, item.UsageType, item.KeepNegativeValue);
                    for (var i = 1; i < item.Properties.Length; i++)
                        if (ControllerPropertiesValues.TryGetValue(item.Properties[i], out object? value11))
                            if (value11 is double dbl && dbl > -DoubleLimitZero){
                                isDouble = true;
                                maxDouble = MaxDouble(maxDouble, (double)(ParseValue(0D, item.UsageType, item.KeepNegativeValue)??0D));
                            }
                            else if (value11 is double dbl2){
                                isDouble = true;
                                maxDouble = MaxDouble(maxDouble, (double)(ParseValue(value1, item.UsageType, item.KeepNegativeValue)??0D));
                            }
                            else
                                return ParseValue(value1, item.UsageType, item.KeepNegativeValue);
                }
                else
                    for (var i = 0; i < item.Properties.Length; i++)
                        if (ControllerPropertiesValues.TryGetValue(item.Properties[i], out object? value2))
                        {
                            if (value2 is double dbl && dbl < DoubleLimitZero){
                                isDouble = true; 
                                maxDouble = MaxDouble(maxDouble, (double)(ParseValue(0D, item.UsageType)??0D));
                            }
                            else if (value2 is double dbl3){
                                isDouble = true;
                                maxDouble = MaxDouble(maxDouble, (double)(ParseValue(value2, item.UsageType)??0D));
                            }
                            else if (value2 is bool bool1){
                                isBool = true;
                                maxBool= maxBool || (bool)(ParseValue(bool1, item.UsageType)??false);
                            }
                            else
                                return ParseValue(value2, item.UsageType);
                        }
            if (isDouble)
                return maxDouble;
            if (isBool)
                return maxBool;

            if (ControllerPropertiesValues.TryGetValue(usage, out object? value))
                if (UsagePropertiesMap.TryGetValue(usage, out UsagePropertiesMapItem? mapItem))
                    return ParseValue(value, mapItem.UsageType);
                else
                    return value;

            return false;
        }

        private const double DoubleLimitZero = 0.01D;

        /* Return the maximal value abstracting the sign (MaxDouble(-100D, -10D) == -100D */
        private double MaxDouble(double dbl1, double dbl2) => dbl1 < dbl2 ? (dbl1 >= 0 ? dbl2 : (dbl2 <= 0 ? dbl1 : dbl2)) : (dbl2 >= 0 ? dbl1 : (dbl1 <= 0 ? dbl2 : dbl1));

        protected object? ParseValue(object? value, Type usageType, bool keepNegativeValue = true)
        {
            if (usageType == typeof(bool))
                return value switch
                {
                    double dbl => dbl == 0D ? false : dbl > DoubleLimitZero || dbl < -DoubleLimitZero,
                    float f => f != 0F,
                    int i => i != 0,
                    null => false,
                    _ => value
                };

            if (usageType == typeof(int))
                return value switch
                {
                    double dbl => !keepNegativeValue && dbl < 0 ? -(int)dbl : (int)dbl,
                    float f => !keepNegativeValue && f < 0 ? -(int)f : (int)f,
                    null => 0,
                    _ => value
                };

            if (usageType == typeof(float))
                return value switch
                {
                    double dbl => dbl > DoubleLimitZero || dbl < -DoubleLimitZero
                                    ? (!keepNegativeValue && dbl < 0 ? -(float)dbl : (float)dbl)
                                    : 0F,
                    float f => f > DoubleLimitZero || f < -DoubleLimitZero
                                    ? (!keepNegativeValue && f < 0 ? -(float)f : (float)f)
                                    : 0F,
                    null => 0F,
                    _ => value
                };

            return value;
        }

        public virtual void SetControllerPropertyValue(string controllerProperty, object? value)
        {
            ControllerPropertiesValues[controllerProperty] = value;
        }

        #region Properties
        [Browsable(true)]
        [Category("Controller")]
        [DefaultValue(0)]
        public virtual int FPSMax { get; set; } = 0;
        protected int FPSMaxDuration
        {
            get
            {
                if (FPSMax <= 0) return 0;
                return 1000 / FPSMax;
            }
        }

        protected Dictionary<string, object?> ChangedQueue = [];

        public virtual void OnTickTime()
        {
            if (IsRunning && ChangedQueue.Count > 0)
                lock (ChangedQueue)
                {
                    foreach (var (property, value) in ChangedQueue)
                        OnControllerChanged(this, new(property, value));
                    ChangedQueue.Clear();
                }
        }

        public override void LoadSettings(ProcessSettings? settings = null, string fileName = "")
        {
            base.LoadSettings(settings, fileName);
            if (settings == null)
                if ((settings = ProcessSettings) == null)
                    return;
            FPSMax = (int)(settings.GetValue("FPSMax", FPSMax) ?? FPSMax);
        }
        public override JsonObject SaveProcess(JsonObject? node = null)
        {
            node = base.SaveProcess(node);
            node.Add("FPSMax", FPSMax);
            return node;
        }
        #endregion
    }
}
