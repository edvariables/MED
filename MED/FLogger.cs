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

        private void FLogger_Activated(object sender, EventArgs e)
        {
            if (FProperties.CurrentProperty is ImageProcess)
                FProperties.CurrentProperty = (FProperties.CurrentProperty as ImageProcess).Performance;
            else
                FProperties.CurrentProperty = Performance;
        }
        public static FLogger Current { get; private set; }

        public Logger Logger = new();

        public Color DefaultLoggerColor { get { return rtbLog.ForeColor; } }

        public string ProgressMessage
        {
            get => lblProgressMessage.Text;
            set => lblProgressMessage.Text = value;
        }

        #region Refresh
        public void RefreshProgress(ImageProcess sender)
        {
            if (Logger == null)
                return;
            if (Logger.BufferLength > 0)
            {
                rtbLog.SuspendLayout();

                if (rtbLog.TextLength > 1024 * 1024)
                {
                    rtbLog.Select(0, rtbLog.TextLength / 2);
                    rtbLog.SelectedText = "";
                }
                rtbLog.SelectionStart = int.MaxValue;
                if (Logger.BufferLength > 0)
                    //if (Logger.ProgressMessage[0] == '\b')
                    //{
                    RTGBAppend(rtbLog);
                //}
                //else
                //{
                //    rtbLog.AppendText(Logger.ProgressMessage.ToString());
                //    Logger.ProgressMessage.Clear();
                //}
                rtbLog.SelectionStart = int.MaxValue;
                rtbLog.ScrollToCaret();

                rtbLog.ResumeLayout();
            }
        }

        Regex RTGBAppendRegex = null;
        Performance Performance;
        private void RTGBAppend(RichTextBox rtb)
        {
            var str = Logger.BufferString(true);
            if (str.Contains('\b'))
            {
                if (RTGBAppendRegex == null)
                {
                    Performance = new Performance("RTGBAppend", Logger, chkLogColored.Checked, KnownColor.MediumPurple);
                    var delimter = Regex.Escape("\b");
                    var pattern = $"{delimter}{Regex.Escape("{")}(?<property>(\\w|\\d)+){Regex.Escape(":")}(?<value>[^{delimter}]*){Regex.Escape("}")}=(?<log>[^{delimter}]*){delimter}(?<crlf>[^{delimter}]*)";
                    RTGBAppendRegex = new(pattern);
                    Performance.IsColored = true;
                    Performance.Start();
                }

                Performance.Resume("RTGBAppendRegex.Matches", true);
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
                Performance.Pause();
                if (matches.Count > 0)
                    return;
            }

            rtbLog.AppendText(str);

        }
        #endregion

        #region Form controls
        private void chkLogColored_CheckedChanged(object sender, EventArgs e)
        {
            if (Performance != null)
                Performance.IsColored = chkLogColored.Checked;
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
        #endregion


        /**
         * Settings
         * */

        #region Settings
        private void LoadSettings()
        {
            Core.Settings.ClearCache(true, true, this.Name);

            chkClearLogOnRun.Checked = (bool)Core.Settings.GetValue("ClearLogOnRun", this.Name, chkClearLogOnRun.Checked);
        }
        public void SaveSettings()
        {
            Core.Settings.SetValue("ClearLogOnRun", this.Name, chkClearLogOnRun.Checked);
            Core.Settings.Save();
        }
        #endregion

        /**
         * 
         * 
         */
        #region Run and Stop
        public void Start()
        {
            if (chkClearLogOnRun.Checked)
                rtbLog.Clear();
            Performance?.Start();
        }

        /**
         * 
         * 
         */
        public void Stop()
        {
            Performance?.Stop();
            RTGBAppendRegex = null;
            Performance = null;

            RefreshProgress(null);
        }
        #endregion


        private void RtbLog_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.C && e.Control)
                Clipboard.SetText(rtbLog.SelectedText);
            else if (e.KeyCode == Keys.X && e.Control)
            {
                Clipboard.SetText(rtbLog.SelectedText);
                rtbLog.SelectedText = "";
            }
            else if (e.KeyCode == Keys.V && e.Control)
            {
                rtbLog.SelectedText = Clipboard.GetText();
            }
            else if (e.KeyCode == Keys.A && e.Control)
                rtbLog.Select(0, rtbLog.TextLength);
        }
    }
}
