using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace MED
{
    public class Performance
    {
        public string Name;
        public KnownColor LoggerColor;
        private bool _IsColored;

        public long Ticks_Start;
        public long Ticks_Pause;
        public long Ticks_Stop;
        public long Ticks_Previous;
        public Logger Logger;
        public bool Enabled = true;
        public long IgnoreFirsts = 10L;
        public bool IgnoreFirsts_done;

        //Calculate average on last items
        public long AverageSample = 100L;
        public long AverageSample_Counter = 0L;
        public long AverageSample_Start = 0L;
        private const long AverageSample_Smooth = 10L;//Integrate previous average at 10%

        /***
         * 
         */
        public Performance(string name = "", Logger? logger = null, bool enabled = true, KnownColor color = KnownColor.Transparent)
        {
            LoggerColor = color;
            Logger = logger;
            Enabled = enabled && Logger != null;
            Name = name ?? "";
            Start();
        }

        /**
         * Empty
         * */
        #region Empty
        public bool IsEmpty { get; private set; }

        public static Performance Empty(string name = "", Logger? logger = null, bool enabled = true, KnownColor color = KnownColor.Transparent)
        {
            var p = new Performance(name, logger, enabled, color);
            p.IsEmpty = true;
            return p;
        }
        public Performance Empty(string name = "", bool enabled = true, KnownColor color = KnownColor.Transparent)
        {
            var p = Sub(
                name,
                enabled,
                color
            );
            p.IsEmpty = true;
            return p;
        }
        #endregion

        /**
         * Subs
         */
        #region Subs

        [Browsable(true)]
        public Dictionary<string, Performance> Subs { get; private set; }

        private Performance _Parent;

        //Sub performance, auto instanciated
        public Performance Sub(string name = "", bool enabled = true, KnownColor color = KnownColor.Transparent)
        {
            if (name != ""
            && name[0] == '.')
                name = Name + name;

            if (Subs == null)
                Subs = new();
            if (Subs.ContainsKey(name))
                return Subs[name];

            var p = new Performance(
                name,
                Logger,
                enabled,
                color == KnownColor.Transparent ? LoggerColor : color
            );
            Subs.Add(name, p);
            p._Parent = this;
            p.IsColored = IsColored;
            return p;
        }
        #endregion

        /**
         * Properties
         * */
        #region Properties
        /**
         * IsColored
         * Propagate to Subs
         */
        [Browsable(false)]
        public bool IsColored
        {
            get
            {
                return _IsColored;
            }
            set
            {
                _IsColored = value;
                if (Subs != null)
                    foreach (var sub in Subs.Values)
                        sub.IsColored = _IsColored;
            }
        }
        #endregion

        /**
         * Process
         * */
        #region Process
        /**
         * 
         */
        public bool IsRunning
        {
            get { return Ticks_Stop == 0L; }
        }
        public bool IsPaused
        {
            get { return Ticks_Pause != 0L; }
        }

        private long _Counter;

        [Browsable(false)]
        public long Counter
        {
            get { return _Counter; }
            set
            {
                if (IsEmpty)
                    return;
                if (!IgnoreFirsts_done && IgnoreFirsts <= value)
                {
                    IgnoreFirsts_done = true;
                    _Counter = value - IgnoreFirsts;
                    Ticks_Start = Now;
                }
                else if (AverageSample > 0 && AverageSample < value)
                {
                    if (AverageSample_Counter == 0)
                        AverageSample_Start = Ticks_Start;
                    AverageSample_Counter += Counter - AverageSample_Smooth;
                    Ticks_Start = Now - Average * AverageSample_Smooth;
                    _Counter = value - (AverageSample - AverageSample_Smooth);

                }
                else
                    _Counter = value;
            }
        }

        /***
         * Process
         * */
        #region Process
        public System.Threading.ThreadState ProcessState => throw new NotImplementedException();

        public string Start(string step = "Start", bool start_subs = true)
        {
            if (IsEmpty)
                return "";
            Counter = 0L;
            Ticks_Pause = 0L;
            Ticks_Stop = 0L;
            Steps.Clear();
            Ticks_Start = Now;
            AverageSample_Counter = 0L;
            IgnoreFirsts_done = IgnoreFirsts == 0;

            if (start_subs && Subs != null)
                foreach (var sub in Subs)
                    if (!sub.Value.IsRunning && !sub.Value.IsPaused)
                        sub.Value.Start(step, start_subs);

            if (step == "" || step == "Start")
                return $"{Name}.Start";

            return Step(step);
        }
        public string Stop(bool stop_subs = false)
        {
            if (IsEmpty)
                return "";
            if (IsPaused)
            {
                Ticks_Start += Now - Ticks_Pause;
                AverageSample_Start += Now - Ticks_Pause;
                Ticks_Pause = 0L;
            }
            Ticks_Stop = Now;

            if (stop_subs && Subs != null)
                foreach (var kvp in Subs)
                    kvp.Value.Stop(stop_subs);

            return Log(Report());
        }
        public string Suspend(string step = "Suspend", bool suspend_subs = false)
        {
            return Pause(step, suspend_subs);
        }
        public string Pause(string step = "Pause", bool pause_subs = false)
        {
            if (IsEmpty)
                return "";

            if (pause_subs && Subs != null)
                foreach (var kvp in Subs)
                    kvp.Value.Pause(step, pause_subs);

            if (step != "")
            {
                if (!step.Contains("Pause") && !step.Contains("Suspend"))
                    step += " (pause)";
                step = Step(step);
            }

            Ticks_Pause = Now;

            return step;
        }
        public string Resume(string step = "Resume", bool increment = false, bool resume_subs = false)
        {
            if (IsEmpty)
                return "";

            if (increment)
                Increment();

            if (Ticks_Pause > 0L)
            {
                Ticks_Start += Now - Ticks_Pause;
                AverageSample_Start += Now - Ticks_Pause;
                Ticks_Pause = 0L;

                if (resume_subs && Subs != null)
                    foreach (var kvp in Subs)
                        kvp.Value.Resume(step, false, resume_subs);
            }
            if (step == "")
                return "";
            if (!step.Contains("Resume"))
                step += " (Resume" + (increment ? "+1)" : ")");
            else if (increment)
                step += "+1";
            return Step(step);
        }
        #endregion

        public string Increment(string step = "", long add = 1)
        {
            if (IsEmpty)
                return "";

            Counter += add;
            if (step == "")
                return "";
            return Step(step); ;
        }
        public string Increment(long add)
        {
            return Increment("", add);
        }

        /**
         * 
         */
        public bool Steps_KeepAll = false;

        public List<KeyValuePair<long, string>> Steps = new();


        public string Step(string step, bool increment = false)
        {
            if (IsEmpty || !Enabled)
                return String.Empty;


            if (IsRunning)
            {
                if (increment)
                    Increment();
                var now = Now;
                if (!IsPaused)
                {
                    if (Steps.Count > short.MaxValue)
                        Steps.RemoveRange(0, short.MaxValue / 2);

                    Steps.Add(new KeyValuePair<long, string>(now, step ?? ""));
                    if (Steps.Count > 1)
                    {
                        var prev = Steps[Steps.Count - 2];
                        step = $"{To_msec(now - prev.Key).ToString("# ###").PadLeft(5, ' ')} msec [{Thread.CurrentThread.GetHashCode().ToString().PadLeft(2)}] {Name} .{step}";
                        if (!Steps_KeepAll)
                            Steps.Remove(prev);
                    }
                }
                else
                    step = $"{"".PadLeft(10, ' ')} [{Thread.CurrentThread.GetHashCode().ToString().PadLeft(2)}] {Name} .{step}";
                if (increment)
                    step += "+1";
            }
            return Log(step);
        }

        public string Alert(string step)
        {
            return Step("[ALERT] " + step);
        }

        public string Error(string step, Exception ex = null)
        {
            return Step("[ERROR] " + step
                + (ex == null ? "" : (" " + (ex.InnerException == null ? ex.Message : ex.InnerException.Message)
                    + "\r\t\t" + ex.StackTrace.ReplaceLineEndings("\n\t\t"))
                )
            );
        }

        public string Debug(string step)
        {
            return Sub(".[DEBUG]").Step(step);
        }

        public string StackTrace(string step)
        {
            step = $"[STACK]{step}\n\t\t" + GetStackTrace().ReplaceLineEndings("\n\t\t");
            return Debug(step);
        }
        string _PreviousStackTrace = "";

        public string GetStackTrace()
        {
            string current = Environment.StackTrace;
            for (var i = 3; i > 0; i--)
            {
                var pos = current.IndexOf('\n');
                if (pos < 0) break;
                current = current.Substring(pos + 1);
            }


            if (_PreviousStackTrace == current)
            {
                int pos = current.Length;
                for (var i = 2; i > 0; i--)
                {
                    pos = current.IndexOf('\n');
                    if (pos < 0) break;
                }
                return current.Substring(0, pos);
            }

            if (_PreviousStackTrace != "")
            {
                int posCur = current.Length - 1;
                for (int posPrev = _PreviousStackTrace.Length - 1; posPrev > 0; posPrev--)
                {
                    if (posCur < 0 || _PreviousStackTrace[posPrev] != current[posCur])
                        break;
                    posCur--;
                }
                if (posCur <= 0)
                    return "";
                _PreviousStackTrace = current;
                return current.Substring(0, posCur);
            }
            _PreviousStackTrace = current;
            return current;
        }

        public string Log(string s)
        {
            if (IsEmpty || !Enabled || s == "")
                return String.Empty;

            if (Logger != null)
                Logger.AppendLine(LogColored(s));
            return s;
        }

        public string LogColored(string s)
        {
            if (!IsColored
             || LoggerColor == KnownColor.Transparent)
                return s;
            return $"\b{{color:{LoggerColor.ToString()}}}={s}\b";
        }

        [Browsable(false)]
        public long Duration
        {
            get
            {
                if (Ticks_Stop == 0)
                    if (Ticks_Pause == 0)
                        return Now - Ticks_Start;
                    else
                        return Ticks_Pause - Ticks_Start;
                return Ticks_Stop - Ticks_Start;
            }
        }
        public long Duration_msec
        {
            get
            {
                return To_msec(Duration);
            }
        }
        [Browsable(false)]
        public long Average
        {
            get
            {
                return Duration / (Counter == 0L ? 1L : Counter);
            }
        }
        [Browsable(false)]
        public long Average_msec
        {
            get
            {
                return To_msec(Average);
            }
        }
        [Browsable(false)]
        public long Now
        {
            get
            {
                return DateTime.Now.Ticks;
            }
        }

        public long To_msec(long ticks)
        {
            return ticks / TimeSpan.TicksPerMillisecond;
        }

        /**
         * Report
         * 
         **/
        public string Report(bool get_subs = false)
        {
            if (IsEmpty)
                return "IsEmpty";

            var counter = AverageSample_Counter > 0 ? AverageSample_Counter + Counter : Counter;
            var start = AverageSample_Counter > 0 ? AverageSample_Start : Ticks_Start;
            var duration = To_msec(Now - start);
            var avg = duration / (counter == 0L ? 1L : counter);
            var s = $"{Name} : {duration.ToString("# ###")} msec / #{counter}";
            if (counter > 0)
                s += $" ={avg.ToString("# ###")} msec";
            else if (Name.Contains("[DEBUG]"))
                return "";

            if (get_subs && Subs != null)
                foreach (var kvp in Subs)
                    s += "\n\r" + kvp.Value.Report(get_subs);

            return s;
        }
        public override string ToString() => Report();

        #endregion

        /**
         * Settings
         * */
        #region Settings
        public virtual void LoadSettings(string paramSection = "Performance")
        {
            Core.Settings.ClearCache(true, true, paramSection);
            Enabled = bool.Parse(Core.Settings.GetValue("Perf.Enabled", paramSection, true).ToString());
            LoggerColor = (KnownColor)Enum.Parse(typeof(KnownColor), Core.Settings.GetValue("Perf.Color", paramSection, LoggerColor).ToString());
        }
        /**
         * 
         * */
        public virtual void SaveSettings(string ParamSection = "Performance", bool evenIsEmpty = false)
        {
            if (IsEmpty && !evenIsEmpty)
                return;
            Core.Settings.SetValue("Perf.Enabled", ParamSection, Enabled);
            Core.Settings.SetValue("Perf.Color", ParamSection, LoggerColor);
        }
        #endregion
    }
}
