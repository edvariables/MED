using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MED
{
    public interface IProcess
    {
        string Name { get; }

        bool IsRunning { get; }
        System.Threading.ThreadState ProcessState { get; } // Constructor must initiate :> ProcessState = ThreadState.Unstarted;

        void Start();
        void Pause();
        void Resume();
        void Stop();
    }
}
