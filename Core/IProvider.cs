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
        public Control InvokeHandler { get; set; }

        bool AddConsumer(IConsumer consumer, string property);
    }
}
