using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MED
{
    /**
     * interface IConsumer
     * <summary>A process that is able to consume a property from a provider</summary>
     * */
    public interface IConsumer : IProcess
    {
        bool IsAsynchrone { get; set; }
    }
}
