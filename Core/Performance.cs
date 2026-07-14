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
        public bool LoggerColored;

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
        public string Start(string step = "Start")
        {
            Counter = 0L;
            Ticks_Pause = 0L;
            Ticks_Stop = 0L;
            Steps.Clear();
            Ticks_Start = Now;
            AverageSample_Counter = 0L;
            if (step == "")
                return $"{Name}.Start";
            IgnoreFirsts_done = IgnoreFirsts == 0;
            return Step(step);
        }
        public string Stop()
        {
            Ticks_Stop = Now;
            return Log(ToString());
        }
        public string Pause(string step = "Pause")
        {
            if (step != "")
            {
                if (!step.Contains("Pause"))
                    step += " (pause)";
                step = Step(step);
            }
            Ticks_Pause = Now;
            return step;
        }
        public string Resume(string step = "Resume", bool increment = false)
        {
            if (Ticks_Pause == 0L)
                return "";
            if (increment)
                Increment();

            Ticks_Start += Now - Ticks_Pause;
            Ticks_Pause = 0L;

            if (step == "")
                return "";
            if (!step.Contains("Resume"))
                step += " (Resume)";
            return Step(step);
        }
        public string Increment(string step = "", long add = 1)
        {
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
            if (!Enabled)
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
            if (Enabled && Logger != null)
                Logger.AppendLine(LogColored(s));
            return s;
        }

        public string LogColored(string s)
        {
            if (!LoggerColored
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
        public override string ToString()
        {
            var counter = AverageSample_Counter > 0 ? AverageSample_Counter + Counter : Counter;
            var start = AverageSample_Counter > 0 ? AverageSample_Start : Ticks_Start;
            var duration = To_msec(Now - start);
            var avg = duration / (counter == 0L ? 1L : counter);
            var s = $"{Name} : {duration.ToString("# ###")} msec / #{counter} = {avg.ToString("# ###")} msec";
            return s;
        }
    }
}
