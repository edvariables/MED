using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MED
{
    public interface IProvider
    {
        bool AddConsumer(IConsumer consumer, string property);
    }
}
