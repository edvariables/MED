using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MED
{
    /**
     * interface IProvider
     * <summary>A process that is able to provide a property</summary>
     * */
    public interface IProvider: IProcess
    {
        [Browsable(false)]
        Control? InvokeHandler { get; set; }

        bool AddConsumer(IConsumer consumer, string property, MulticastDelegate? consumerDelegate = null);
        bool RemoveConsumer(IConsumer consumer, string property, MulticastDelegate? consumerDelegate = null);
        void InvokePropertyChanged(IProvider sender, Delegate delegateMethod, EventArgs e, string? propertyDomain = null);
        bool IsInvokingPropertyChanged(Delegate delegateMethod);
    }

    public class PropertyChangedEventArgs(string property, object? value) : EventArgs
    {
        public string Property = property;
        public object? Value = value;
    }
}
