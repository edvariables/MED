using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MED
{
    public interface IProvider
    {

        [Browsable(false)]
        Control InvokeHandler { get; set; }

        bool AddConsumer(IConsumer consumer, string property);
        void InvokePropertyChanged(IProvider sender, Delegate delegateMethod, EventArgs e);
        bool IsInvokingPropertyChanged(Delegate delegateMethod);
    }
}
