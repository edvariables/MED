using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace MED
{
    public interface IProcess : IDisposable
    {
        string Name { get; }
        string ProcessIcon { get; }

        #region Settings
        ProcessSettings ProcessSettings { get; }
        void LoadSettings(ProcessSettings settings = null, string fileName = "");
        void LoadProcess(JsonNode node);

        //void LoadSettings(string fileName);
        void SaveSettings(ProcessSettings settings = null, string fileName = "");
        JsonObject SaveProcess(JsonObject node = null);

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
