using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MED
{
    public class Performance
    {
        public string Name;
        public KnownColor LoggerColor;
        public bool IsColored;

        public long Ticks_Start;
        public long Ticks_Pause;
        public long Ticks_Stop;
        public long Ticks_Previous;
        public StringBuilder Logger;
        public bool Enabled = true;
        public long IgnoreFirsts = 10L;
        public bool IgnoreFirsts_done;

        //Calculate average on last items
        public long AverageSample = 100L;
        public long AverageSample_Counter = 0L;
        public long AverageSample_Start = 0L;
        private const long AverageSample_Smooth = 10L;//Integrate previous average
        /***
         * 
         */
        public Performance(string name = "", StringBuilder? logger = null, bool enabled = true, KnownColor color = KnownColor.Transparent)
        {
            LoggerColor = color;
            Logger = logger;
            Enabled = enabled && Logger != null;
            Name = name ?? "";
            Start();
        }

        /**
         * IsEmpty
         * 
         * */
        public bool IsEmpty { get; private set; }

        public static Performance Empty(string name = "", StringBuilder? logger = null, bool enabled = true, KnownColor color = KnownColor.Transparent)
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

        /**
         * Subs
         */
        private Dictionary<string, Performance> Subs;
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
            return p;
        }

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
                    _Counter = value - AverageSample + AverageSample_Smooth;

                }
                else
                    _Counter = value;
            }
        }
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
            if (step == "" || step == "Start")
                return $"{Name}.Start";
            IgnoreFirsts_done = IgnoreFirsts == 0;

            if (start_subs && Subs != null)
                foreach (var sub in Subs)
                    if (!sub.Value.IsRunning && !sub.Value.IsPaused)
                        sub.Value.Start(step, start_subs);

            return Step(step);
        }
        public string Stop(bool stop_subs = true)
        {
            if (IsEmpty)
                return "";
            Ticks_Stop = Now;

            if (stop_subs && Subs != null)
                foreach (var kvp in Subs)
                    kvp.Value.Stop(stop_subs);

            return Log(Report());
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
                if (!step.Contains("Pause"))
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

            if (Ticks_Pause == 0L)
                return "";
            if (increment)
                Increment();

            Ticks_Start += Now - Ticks_Pause;
            Ticks_Pause = 0L;

            if (resume_subs && Subs != null)
                foreach (var kvp in Subs)
                    kvp.Value.Resume(step, false, resume_subs);

            if (step == "")
                return "";
            if (!step.Contains("Resume"))
                step += " (Resume)";
            return Step(step);
        }
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

            if (increment)
                Increment();
            var now = Now;

            if (!IsPaused && IsRunning)
            {
                if (Steps.Count > short.MaxValue)
                    Steps.RemoveRange(0, short.MaxValue / 2);

                Steps.Add(new KeyValuePair<long, string>(now, step ?? ""));
                if (Steps.Count > 1)
                {
                    var prev = Steps[Steps.Count - 2];
                    step = $"{To_msec(now - prev.Key).ToString("# ###").PadLeft(5, ' ')} msec {Name} .{step}";
                    if (!Steps_KeepAll)
                        Steps.Remove(prev);
                }
            }
            return Log(step);
        }

        public string Log(string s)
        {
            if (IsEmpty || !Enabled)
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
        public long Average
        {
            get
            {
                return Duration / (Counter == 0L ? 1L : Counter);
            }
        }
        public long Average_msec
        {
            get
            {
                return To_msec(Average);
            }
        }
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
            var s = $"{Name} : {duration.ToString("# ###")} msec / #{counter} = {avg.ToString("# ###")} msec";

            if (get_subs && Subs != null)
                foreach (var kvp in Subs)
                    s += "\n\r" + kvp.Value.Report(get_subs);

            return s;
        }
        public override string ToString() => Report();


        /**
         * 
         * */
        public virtual void LoadSettings(string ParamSection = "Performance")
        {
            Enabled = bool.Parse(Core.Settings.GetValue("Perf.Enabled", ParamSection, true).ToString());
            LoggerColor = (KnownColor)Enum.Parse(typeof(KnownColor), Core.Settings.GetValue("Perf.Color", ParamSection, KnownColor.Green).ToString());
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
    }
}
