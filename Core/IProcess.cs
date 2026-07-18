using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MED
{
    public interface IProcess : IDisposable
    {
        string Name { get; }

        #region Settings
        void LoadSettings(bool loadChildren = false);
        void SaveSettings(bool saveChildren = true);

        [Browsable(false)]
        Dictionary<string, object> ObjectsProperties { get; }
        #endregion

        #region Process
        bool IsRunning { get; }

        delegate void ProcessStateChangedDelegate(IProcess sender, System.Threading.ThreadState state);

        System.Threading.ThreadState ProcessState { get; } // Constructor must initiate :> ProcessState = ThreadState.Unstarted;

        void Start();
        void Pause();
        void Resume();
        void Stop();
        #endregion
    }
}
