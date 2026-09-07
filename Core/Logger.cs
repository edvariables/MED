using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MED
{
    /**
     * class Logger
     * <summary>Host of a StringBuilder.
     * Used by Performance class</summary>
     * */
    public class Logger : INullable
    {
        private StringBuilder Buffer = new();

        [Category("Logger")]
        public bool Enabled { get; set; } = true;

        public void Clear()
        {
            Buffer.Clear();
            LastErrorsClear();
        }

        public void AppendLine(string msg, params object[] args)
        {
            if (!Enabled) return;

            for (int i = 0; i < args.Length; i++)
            {
                msg = msg.Replace("{" + i.ToString() + "}", args[i].ToString());
            }
            lock (Buffer)
            {
                Buffer.AppendLine(msg);
            }
        }
        public void Append(string msg, params object[] args)
        {
            if (!Enabled) return;
            
            for (int i = 0; i < args.Length; i++)
            {
                msg = msg.Replace("{" + i.ToString() + "}", args[i].ToString());
            }

            lock (Buffer)
            {
                Buffer.Append(msg);
            }
        }

        public string BufferString(bool clear = true)
        {
            string s;
            lock (Buffer)
            {
                s = Buffer.ToString();
                if (clear) Buffer.Clear();
            }
            return s;
        }

        [Browsable(false)]
        public int BufferLength
        {
            get
            {
                lock (Buffer)
                {
                    return Buffer.Length;
                }
            }
        }

        [Browsable(false)]
        public bool IsNull => false;

        /**
         * 
         * */
        public delegate void BufferChangedDelegate(object sender, EventArgs e);

        /**
         * OnBufferChanged
         * 
         * This event is NOT raised for each text append but only when InvokeBufferChanged is called (by Performance.Stop()).
         * Needed after process stop and propagation of ImageChanged is freezed, so Performance.Report does not appear.
         * 
         * */
        public BufferChangedDelegate? OnBufferChanged;

        public void InvokeBufferChanged(object sender, EventArgs e)
        {
            if (BufferLength > 0 && OnBufferChanged != null)
                OnBufferChanged(this, e);
        }

        #region LastErrors
        private PerformanceException? _LastError;
        [Browsable(true)]
        [ReadOnly(false)]
        [Category("LastErrors")]
        [Description("Dernière erreur")]
        public PerformanceException? LastError
        {
            get => _LastError;
            internal set
            {
                _LastError = value;
                if (_LastError == null)
                    return;
                if (LastErrors.TryGetValue(_LastError.Performance, out Queue<PerformanceException>? lastExceptions))
                {
                    while (lastExceptions.Count >= LastExceptionsQueueLength)
                    {
                        lastExceptions.Dequeue();
                        LastErrorsCount--;
                    }
                    lastExceptions.Enqueue(_LastError);
                }
                else
                    LastErrors[_LastError.Performance] = new([_LastError]);
                LastErrorTime = DateTime.Now;
                LastErrorsCount++;
            }
        }

        [Browsable(true)]
        [ReadOnly(true)]
        [Category("LastErrors")]
        [Description("Historique des erreurs")]
        public Dictionary<Performance, Queue<PerformanceException>> LastErrors { get; internal set; } = new();

        public DateTime? LastErrorTime;
        public int LastErrorsCount = 0;

        [Browsable(true)]
        [ReadOnly(false)]
        [Category("LastErrors")]
        [Description("Nombre d'exceptions conservées en mémoire")]
        public int LastExceptionsQueueLength { get; set; } = 5;

        /**
         * LastErrorsClear
         * */
        public void LastErrorsClear()
        {
            LastError = null;
            LastErrors = new();
            LastErrorTime = DateTime.Now;
            LastErrorsCount = 0;
        }
        #endregion
    }
}
