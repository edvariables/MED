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
        IProcess Clone(Type? cloneType = null);

        string Name { get; set; }

        bool Enabled { get; set; }

        string ProcessIcon { get; set; }

        string ProcessIconDefault { get; }

        Performance? Performance { get; set; }

        #region Settings
        ProcessSettings? ProcessSettings { get; }
        void LoadSettings(ProcessSettings? settings = null, string fileName = "");
        void LoadProcess(JsonNode node);
        void LoadSettingsDone(object? sender, EventArgs e);

        void SaveSettings(ProcessSettings? settings = null, string fileName = "");
        JsonObject SaveProcess(JsonObject? node = null);


        IConsumer? Consumer { get; }

        void OnGameControllerChanged (IGameController gameController, PropertyChangedEventArgs eventArgs);

        GameControllerScript? OnGameControllerScript { get; }

        [Browsable(false)]
        Dictionary<string, object> ObjectsProperties { get; }

        Dictionary<string, object?>? Data { get; set; }

        object? Tag { get; set; }
        #endregion

        #region IDisposable
        bool Disposing { get; }
        bool IsDisposed { get; }
        #endregion

        #region Process
        bool IsRunning { get; }
        bool IsPaused { get; }

        delegate void ProcessStateChangedDelegate(IProcess sender, System.Threading.ThreadState state);

        void OnProcessStateChanged(IProcess sender, System.Threading.ThreadState state);
        EventScript? OnProcessStateScript { get; set; }

        ProcessStateChangedDelegate? ProcessStateChanged { get; set; }

        System.Threading.ThreadState ProcessState { get; set; } // Constructor must initiate :> ProcessState = ThreadState.Unstarted;

        void Start();
        void Pause();
        void Resume();
        void Stop();
        #endregion
    }
}
