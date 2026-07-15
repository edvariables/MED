using MED.EDWebCam;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MED
{
    public partial class FLogger : Form
    {
        public FLogger()
        {

            InitializeComponent();

            Current = this;

            LoadSettings();


        }

        public static FLogger Current { get; private set; }

        public StringBuilder ProgressMessage = new();


        public void RefreshProgress(ImageProcess sender)
        {
            if (ProgressMessage == null)
                return;
            if (ProgressMessage.Length > 0)
            {
                rtbLog.SuspendLayout();

                if (rtbLog.TextLength > 1024 * 1024)
                {
                    rtbLog.Select(0, rtbLog.TextLength / 2);
                    rtbLog.SelectedText = "";
                }
                rtbLog.SelectionStart = int.MaxValue;
                if (ProgressMessage.Length > 0)
                    //if (ProgressMessage[0] == '\b')
                    //{
                    RTGBAppend(rtbLog, ProgressMessage);
                //}
                //else
                //{
                //    rtbLog.AppendText(ProgressMessage.ToString());
                //    ProgressMessage.Clear();
                //}
                rtbLog.SelectionStart = int.MaxValue;
                rtbLog.ScrollToCaret();

                rtbLog.ResumeLayout();
            }
        }

        Regex RTGBAppendRegex = null;
        Performance RTGBAppendPerf;
        private void RTGBAppend(RichTextBox rtb, StringBuilder strB)
        {
            var str = strB.ToString();
            strB.Clear();
            if (str.Contains('\b'))
            {
                if (RTGBAppendRegex == null)
                {
                    RTGBAppendPerf = new Performance("RTGBAppend", strB, chkLogColored.Checked, KnownColor.MediumPurple);
                    var delimter = Regex.Escape("\b");
                    var pattern = $"{delimter}{Regex.Escape("{")}(?<property>(\\w|\\d)+){Regex.Escape(":")}(?<value>[^{delimter}]*){Regex.Escape("}")}=(?<log>[^{delimter}]*){delimter}(?<crlf>[^{delimter}]*)";
                    RTGBAppendRegex = new(pattern);
                    RTGBAppendPerf.IsColored = true;
                    RTGBAppendPerf.Start();
                }

                RTGBAppendPerf.Resume("RTGBAppendRegex.Matches", true);
                var matches = RTGBAppendRegex.Matches(str);
                foreach (var match in matches)
                {
                    var startSel = rtb.Text.Length;
                    rtb.SelectionStart = startSel;
                    string value = ((Match)match).Groups["value"].Value;
                    var prop = ((Match)match).Groups["property"].Value.ToLower();
                    switch (prop)
                    {
                        case "color":
                            rtb.SelectionColor = Color.FromKnownColor((KnownColor)Enum.Parse(typeof(KnownColor), value));
                            break;
                        default:
                            Console.WriteLine($"RTGBAppend : unknown property {prop}");
                            break;
                    }
                    rtb.AppendText(((Match)match).Groups["log"].Value);
                    rtb.SelectionStart = int.MaxValue;
                    rtb.SelectionColor = rtb.ForeColor;
                    rtb.AppendText(((Match)match).Groups["crlf"].Value);
                    //rtb.Select(startSel, rtb.Text.Length);
                }
                RTGBAppendPerf.Pause();
                if (matches.Count > 0)
                    return;
            }

            rtbLog.AppendText(str);

        }

        private void chkLogColored_CheckedChanged(object sender, EventArgs e)
        {
            if (RTGBAppendPerf != null)
                RTGBAppendPerf.IsColored = chkLogColored.Checked;
        }

        private long _chkClearLogOnRun_CheckedChanged_ticks = 0L;
        private void chkClearLogOnRun_CheckedChanged(object sender, EventArgs e)
        {
            if (!((Control)sender).ContainsFocus)
                return;
            var now = DateTime.Now.Ticks;
            if ((now - _chkClearLogOnRun_CheckedChanged_ticks) / TimeSpan.TicksPerSecond < 1)
            {
                //Double-click < 1sec
                rtbLog.Clear();
            }
            _chkClearLogOnRun_CheckedChanged_ticks = now;
        }
        
        private void LoadSettings()
        {
            chkClearLogOnRun.Checked = (bool)Core.Settings.GetValue("ClearLogOnRun", this.Name, chkClearLogOnRun.Checked);
        }
        public void SaveSettings()
        {
            Core.Settings.SetValue("ClearLogOnRun", this.Name, chkClearLogOnRun.Checked);
            Core.Settings.Save();
        }


        /**
         * 
         * 
         */
        public void Run()
        {
            if (chkClearLogOnRun.Checked)
                rtbLog.Clear();
            RTGBAppendPerf?.Start();
        }

        /**
         * 
         * 
         */
        public void Stop()
        {
            RTGBAppendPerf?.Stop();
            RTGBAppendRegex = null;
            RTGBAppendPerf = null;

            RefreshProgress(null);
        }

        /**
         * 
         */
        public Color LoggerForeColor { get { return rtbLog.ForeColor; } }
    }
}
