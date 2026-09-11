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
using MED.Imaging;

namespace MED
{
    public partial class FLogger : Form
    {
        public FLogger()
        {

            InitializeComponent();

            Current = this;

            LoadSettings();

            Logger.OnBufferChanged += Logger_OnBufferChanged;

            RefreshLastErrors();
        }

        private void FLogger_Activated(object sender, EventArgs e)
        {
            if (FProperties.CurrentProperty is ImageProcess imageProcess)
                FProperties.CurrentProperty = imageProcess.Performance;
            else
                FProperties.CurrentProperty = Performance;
        }
        public static FLogger? Current { get; private set; }

        public Logger Logger = new();

        public Color DefaultLoggerColor { get { return rtbLog.ForeColor; } }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string ProgressMessage
        {
            get => lblProgressMessage.Text;
            set => lblProgressMessage.Text = value;
        }

        public void Logger_OnBufferChanged(object sender, EventArgs e) => rtbLog.Invoke(RefreshProgress, sender is IProcess ? (IProcess)sender : null);

        #region Refresh
        public void RefreshProgress(IProcess? sender)
        {
            if (Logger == null)
                return;
            if (Logger.BufferLength > 0 && !rtbLog.IsDisposed)
            {
                rtbLog.SuspendLayout();

                if (rtbLog.TextLength > 1024 * 1024)
                {
                    rtbLog.Select(0, rtbLog.TextLength / 2);
                    rtbLog.SelectedText = "";
                }
                rtbLog.SelectionStart = int.MaxValue;
                if (Logger.BufferLength > 0)
                    RTGBAppend(rtbLog);

                rtbLog.SelectionStart = int.MaxValue;
                rtbLog.ScrollToCaret();

                rtbLog.ResumeLayout();

                RefreshLastErrors();
            }
        }

        private int _LastErrorsCount = -1;
        public void RefreshLastErrors()
        {
            if (_LastErrorsCount != Logger.LastErrorsCount)
            {
                if (Logger.LastErrorsCount == 0)
                    cmdLastErrors.Image = MEDIcons.ok;
                else if (_LastErrorsCount == 0)
                    cmdLastErrors.Image = MEDIcons.alert;
                cmdLastErrors.Text = (_LastErrorsCount = Logger.LastErrorsCount).ToString();
            }
        }

        Regex? RTGBAppendRegex = null;
        Performance? Performance;
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

                Performance?.Resume("RTGBAppendRegex.Matches", true);
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
                Performance?.Pause();
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

            chkClearLogOnRun.Checked = (bool)(Core.Settings.GetValue("ClearLogOnRun", this.Name, chkClearLogOnRun.Checked) ?? chkClearLogOnRun.Checked);

            logFileName = (string)(Core.Settings.GetValue("FLogger.FileName", this.Name, logFileName) ?? logFileName);

            Height = (int)(Core.Settings.GetValue("FLogger.Height", this.Name, Height) ?? Height);
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
        public System.Threading.ThreadState ProcessState;
        public void Start()
        {
            if (ProcessState != System.Threading.ThreadState.Running
            && chkClearLogOnRun.Checked)
            {
                rtbLog.Clear();
                Logger.Clear();
                Logger.AppendLine($"------ Log start at {DateTime.Now.ToString()}-------");
            }
            Performance?.Start();
            ProcessState = System.Threading.ThreadState.Running;
        }

        /**
         * 
         * 
         */
        public void Stop()
        {
            Performance?.Stop();

            RefreshProgress(null);

            ProcessState = System.Threading.ThreadState.Stopped;
        }
        #endregion


        private void RtbLog_KeyUp(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (e.Control)
            {
                var selectedText = rtbLog.SelectedText ?? "";
                if (e.KeyCode == Keys.C)
                {
                    if (selectedText != "")
                        Clipboard.SetText(selectedText);
                }
                else if (e.KeyCode == Keys.X)
                {
                    if (selectedText != "")
                        Clipboard.SetText(rtbLog.SelectedText ?? "");
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
            || e is System.Windows.Forms.KeyEventArgs && ((System.Windows.Forms.KeyEventArgs)e).Control)
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
                    var dirParent = Directory.GetParent(logFileName);
                    start.WorkingDirectory = dirParent == null ? "" : dirParent.FullName;
                    start.Verb = "OPEN";
                    System.Diagnostics.Process.Start(start);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.InnerException != null ? ex.InnerException.Message : ex.Message, "Ouverture du fichier de log", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
        }

        private void cmdLastErrors_Click(object sender, EventArgs e)
        {
            FProperties.Current?.ShowNodeProperties(Logger);
        }

        private void chkEnabled_CheckedChanged(object sender, EventArgs e)
        {
            Logger.Enabled = chkEnabled.Checked;
            chkEnabled.Image = Logger.Enabled ? MEDIcons.Pause_blue : MEDIcons.Start_blue;
        }
    }
}
