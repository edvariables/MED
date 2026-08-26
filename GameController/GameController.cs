using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MED.GameController
{
    public abstract class GameController : Process
    {
        public GameController(string name, Performance? performance = null, Control? invokeHandler = null, IConsumer? consumer = null, bool isAsynchrone = true)
            : base(name, performance, invokeHandler, consumer, isAsynchrone)
        {
            ProcessIconDefault = "Button";
        }

        const string PropertyDomain = "Controller";

        public delegate void ControllerChangedDelegate(GameController sender, PropertyChangedEventArgs e);

        public ControllerChangedDelegate? OnControllerChanged;


        [Browsable(true)]
        [Category("Controller")]
        public UsagePropertiesMap UsagePropertiesMap { get; } = [];

        public virtual void InvokeControllerChanged(GameController sender, PropertyChangedEventArgs e)
        {
            e.Property = UsagePropertiesMap.GetPropertyUsage(e.Property);

            InvokePropertyChanged(sender, OnControllerChanged, e, PropertyDomain);
        }

        public override bool AddConsumer(IConsumer consumer, string controllerProperty, MulticastDelegate? consumerDelegate=null) => base.AddConsumer(consumer, $"{PropertyDomain}.{controllerProperty}", consumerDelegate );
        public override bool RemoveConsumer(IConsumer consumer, string controllerProperty, MulticastDelegate? consumerDelegate = null) => base.RemoveConsumer(consumer, $"{PropertyDomain}.{controllerProperty}", consumerDelegate );

        public abstract object? GetControllerPropertyValue(string controllerProperty);
        public abstract void SetControllerPropertyValue(string controllerProperty, object? value);
    }
}
