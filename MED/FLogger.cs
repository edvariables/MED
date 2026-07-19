using MED.EDWebCam;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;

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

            logFileName = (string)Core.Settings.GetValue("FLogger.FileName", this.Name, logFileName);

            Height = (int)Core.Settings.GetValue("FLogger.Height", this.Name, Height);
            if (Height < 20)
                Height = 20;
        }
        public void SaveSettings()
        {

            Core.Settings.SetValue("FLogger.Height", this.Name, Height);
            Core.Settings.SetValue("FLogger.FileName", this.Name, logFileName);
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


        private void RtbLog_KeyUp(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (e.Control)
            {
                if (e.KeyCode == Keys.C)
                    Clipboard.SetText(rtbLog.SelectedText);
                else if (e.KeyCode == Keys.X)
                {
                    Clipboard.SetText(rtbLog.SelectedText);
                    rtbLog.SelectedText = "";
                }
                else if (e.KeyCode == Keys.V)
                {
                    rtbLog.SelectedText = Clipboard.GetText();
                }
                else if (e.KeyCode == Keys.A)
                    rtbLog.Select(0, rtbLog.TextLength);

                else if (e.KeyCode == Keys.S)
                    cmdSave_Click(sender, e);
            }
        }

        private string logFileName = "";
        private void cmdSave_Click(object sender, EventArgs e)
        {
            if (logFileName == ""
            || e is System.Windows.Forms.KeyEventArgs && (e as System.Windows.Forms.KeyEventArgs).Control)
            {
                saveFileDialog1.DefaultExt = "log";
                saveFileDialog1.Filter = "Log files (*.log)|*.log|(*.txt)|*.txt|All files (*.*)|*.*";
                saveFileDialog1.FileName = logFileName;
                if (saveFileDialog1.ShowDialog(this) == DialogResult.Cancel) return;
                logFileName = saveFileDialog1.FileName;
            }
            File.WriteAllText(logFileName, rtbLog.Text);
            if (logFileName != ""
                && (Keyboard.GetKeyStates(Key.LeftShift) == KeyStates.Down
                || Keyboard.GetKeyStates(Key.RightShift) == KeyStates.Down))
                try
                {
                    var start = new ProcessStartInfo(logFileName);
                    start.UseShellExecute = true;
                    start.WorkingDirectory = Directory.GetParent(logFileName).FullName;
                    start.Verb = "OPEN";
                    System.Diagnostics.Process.Start(start);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.InnerException != null ? ex.InnerException.Message : ex.Message, "Ouverture du fichier de log", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
        }
    }
}
