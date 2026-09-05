using MED.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Design;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace MED
{
    /**
     * interface IProcess
     * <summary>A process with a process state</summary>
     * */
    public interface IProcess : IDisposable, IUndo
    {
        string Name { get; set; }

        bool Enabled { get; set; }

        string ProcessIcon { get; }

        string ProcessIconDefault { get; }

        Performance? Performance { get; }

        #region Settings
        ProcessSettings? ProcessSettings { get; }
        void LoadSettings(ProcessSettings? settings = null, string fileName = "");
        void LoadProcess(JsonNode node);
        void LoadSettingsDone(object? sender, EventArgs e);

        void SaveSettings(ProcessSettings? settings = null, string fileName = "");
        JsonObject SaveProcess(JsonObject? node = null);


        IConsumer? Consumer { get; }

        void GameControllerChanged (IGameController gameController, PropertyChangedEventArgs eventArgs);

        GameControllerScript? OnGameControllerScript { get; }

        [Browsable(false)]
        Dictionary<string, object> ObjectsProperties { get; }
        #endregion

        #region IDisposable
        bool Disposing { get; }
        bool IsDisposed { get; }
        #endregion

        #region Process
        bool IsRunning { get; }
        bool IsPaused { get; }

        delegate void ProcessStateChangedDelegate(IProcess sender, System.Threading.ThreadState state);
        
        IProcess.ProcessStateChangedDelegate? OnProcessStateChanged { get; set; }

        System.Threading.ThreadState ProcessState { get; } // Constructor must initiate :> ProcessState = ThreadState.Unstarted;

        void Start();
        void Pause();
        void Resume();
        void Stop();
        #endregion
    }
}
