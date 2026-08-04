using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace MED
{
    /**
     * interface IProcesses
     * <summary>A process that hosts a collection of processes</summary>
     * */
    public interface IProcesses:IProcess
    {
        Logger? Logger { get; set; }

        List<IProcess> Items { get; }
    }
}
