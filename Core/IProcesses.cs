using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace MED
{
    public interface IProcesses
    {
        Performance? Performance { get; }

        Logger? Logger { get; set; }

        List<IProcess>? Items { get; }
    }
}
